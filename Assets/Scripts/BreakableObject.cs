using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Inventory;
using Game.Level;
using Game.Players;
using Game.Registries;
using Game.Saving;
using Game.Smelting;
using Game.Storage;
using Game.Terrain;
using UnityEngine;

public enum ObjectType
{
    Wood,
    Stone,
    Flesh,
    None
}

public class BreakableObject : MonoBehaviour, ISaveableObject
{
    [System.Serializable]
    public struct Drop
    {
        public Item drop;
        public int minValue;
        public int maxValue;
        [Range(0f, 1f)] public float dropChance;
    }

    public readonly struct DamageInfo
    {
        public readonly int Damage;
        public readonly int PoiseDamage;
        public readonly bool Crit;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly bool FromEnemy;

        public DamageInfo(
            int damage,
            int poiseDamage = 0,
            bool crit = false,
            Vector3 hitPoint = default,
            Vector3 hitNormal = default,
            bool fromEnemy = false)
        {
            Damage = damage;
            PoiseDamage = poiseDamage;
            Crit = crit;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            FromEnemy = fromEnemy;
        }
    }

    private Player player;
    public GameObject damagePopupPrefab;
    private int maxHealth;
    public int baseHealth;
    private int health;
    public int expDropped;
    public Drop[] drops;
    public ObjectType objectType;
    public EntityType entityType;
    public int objectLevel;

    private bool isDestroyed = false;

    public bool PlacedByPlayer { get; set; }

    public bool DestroyedByEnemy { get; set; }

    [HideInInspector] public VoxelChunk owningChunk;

    public Transform torsoBone;

    private bool canBounce = true;
    [SerializeField] private float bounceCooldown = 0.2f;
    private Coroutine activeBounce;

    public delegate void EnemyKilledDelegate(Enemy enemy, int damageDealt, int damageRequired);
    public event EnemyKilledDelegate OnEnemyKilled;

    public delegate void BossHealthChangeDelegate(int newCurrentHealth);
    public event BossHealthChangeDelegate OnBossHealthChange;

    private void OnEnable()
    {
        DifficultyManager.Instance.OnDifficultyChanged += ApplyDifficultyScaling;
        ResetObject();
    }

    private void OnDisable() => DifficultyManager.Instance.OnDifficultyChanged -= ApplyDifficultyScaling;

    private void ApplyDifficultyScaling(bool reset = false)
    {
        float healthMultiplier = DifficultyManager.Instance.GetHealthMultiplier(entityType);
        float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 1f;

        maxHealth = (int)(baseHealth * healthMultiplier);
        health = reset ? maxHealth : (int)(maxHealth * healthPercent);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDestroyed || health <= 0) return;

        int damage = damageInfo.Damage;
        bool crit = damageInfo.Crit;
        Vector3 hitPoint = damageInfo.HitPoint;
        Vector3 hitNormal = damageInfo.HitNormal;

        ShowDamagePopup(damage, crit, hitPoint);

        int damageRequired = health;
        health -= damage;
        health = Mathf.Max(health, 0);

        if (TryGetComponent(out Enemy enemy) && enemy.enemyType == Enemy.EnemyType.Boss)
            OnBossHealthChange?.Invoke(health);

        if (health > 0)
        {
            HitEffectManager hitEffectManager = FindFirstObjectByType<HitEffectManager>();
            if (hitEffectManager != null && hitNormal != default)
                hitEffectManager.SpawnSparks(hitPoint, hitNormal);

            if (entityType == EntityType.Static)
                TryBounce(transform);

            if (enemy != null)
            {
                int poiseDamage = (int)(damageInfo.PoiseDamage * player.TotalPoiseDamageMultiplier);
                enemy.ApplyStagger(poiseDamage);
            }

            return;
        }

        if (enemy != null)
        {
            enemy.Die();
            OnEnemyKilled?.Invoke(enemy, damage, damageRequired);
            return;
        }

        if (TryGetComponent(out Animal animal))
        {
            animal.Die();
            return;
        }

        DestroyedByEnemy = damageInfo.FromEnemy;
        DestroyObject();
    }

    private void ShowDamagePopup(int damage, bool crit, Vector3 hitPoint)
    {
        GameObject popupInstance = DamagePopupPool.Instance.GetPopup();
        Camera cam = Camera.main;
        if (!cam) return;

        // Spawn a bit above the hit point
        Vector3 spawnOffset = Vector3.up * Random.Range(0.2f, 0.5f);

        // Offset slightly toward the camera so it's not occluded by the object
        Vector3 directionToCamera = (cam.transform.position - hitPoint).normalized;
        float cameraOffset = 0.3f; // adjust as needed
        Vector3 spawnPosition = hitPoint + spawnOffset + directionToCamera * cameraOffset;

        popupInstance.transform.position = spawnPosition;

        // Make it always face the camera
        popupInstance.transform.LookAt(cam.transform);
        popupInstance.transform.Rotate(0, 180f, 0); // because LookAt faces the back of the object

        if (popupInstance.TryGetComponent(out DamagePopup popup))
            popup.Setup(damage, crit, transform);
    }

    public void DestroyObject()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (!DestroyedByEnemy || GetComponent<Enemy>() != null)
        {
            foreach (Drop itemDrop in drops)
            {
                if (Random.value > itemDrop.dropChance) continue;

                int dropAmount = Random.Range(itemDrop.minValue, itemDrop.maxValue + 1);
                dropAmount = Mathf.Clamp(dropAmount, itemDrop.minValue, itemDrop.maxValue);

                if (TryGetComponent(out ObjectCategory category) && category != null)
                    dropAmount = (int)(dropAmount * player.TotalResourceDropMultiplier);

                SpawnDrop(itemDrop.drop, dropAmount, transform.position, torsoBone, scatter: false);
            }

            float expGain = expDropped * DifficultyManager.Instance.GetExpMultiplier();
            LevelManager.Instance.AddExp((int)expGain);
        }

        if (entityType != EntityType.Static) return;

        if (TryGetComponent(out StorageContainer storageContainer))
            DropStoredItems(storageContainer.items);

        if (TryGetComponent(out WallSegment wall))
        {
            Vector3 wallPos = wall.transform.position;
            Vector3Int wallPosInt = BuildingManager.Instance.WorldToGrid(wallPos);
            BuildingManager.Instance.DestroyWall(wallPosInt);
        }

        if (TryGetComponent(out OreRockGroup oreRockGroup))
            oreRockGroup.ReleaseRocks();

        VoxelGrid.Instance.RemoveObjectFromChunk(owningChunk, gameObject);
        VoxelGrid.Instance.MarkVoxelArea(gameObject, occupy: false, walkable: true);
        Destroy(gameObject);
    }

    private void DropStoredItems(ItemData[] items)
    {
        foreach (ItemData data in items)
        {
            if (data == null || string.IsNullOrEmpty(data.itemName)) continue;

            SpawnDrop(
                ItemRegistry.GetItemByName(data.itemName),
                data.count,
                transform.position
            );
        }
    }

    private void SpawnDrop(Item drop, int amount, Vector3 origin, Transform bone = null, bool scatter = true)
    {
        if (drop == null || amount <= 0) return;

        Vector3 pos = origin + Vector3.up * 1.5f;
        float scatterDistance = 0.5f;
        float scatterForce = 1.5f;
      
        ItemSpawner.Spawn(drop, pos, amount, bone, scatter, scatterDistance, scatterForce);
    }

    private void TryBounce(Transform objTransform)
    {
        if (!canBounce) return;

        if (activeBounce != null)
            StopCoroutine(activeBounce);

        activeBounce = StartCoroutine(BounceObject(objTransform));
        StartCoroutine(BounceCooldown());
    }

    IEnumerator BounceObject(Transform objTransform)
    {
        Vector3 originalScale = objTransform.localScale;
        Vector3 shrunkenScale = originalScale * 0.95f; // 95% size

        float duration = 0.1f; // shrink duration
        float elapsed = 0f;

        // Shrink
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(originalScale, shrunkenScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = shrunkenScale;

        // Expand back
        elapsed = 0f;
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(shrunkenScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = originalScale;
    }

    private IEnumerator BounceCooldown()
    {
        canBounce = false;
        yield return new WaitForSeconds(bounceCooldown);
        canBounce = true;
    }

    public void ResetObject()
    {
        StartCoroutine(SetPlayer());
        isDestroyed = false;
        ApplyDifficultyScaling(reset: true);
    }

    private IEnumerator SetPlayer()
    {
        while (GameManager.Instance.playerInstance == null)
            yield return null;

        player = GameManager.Instance.playerInstance.GetComponent<Player>();
    }

    public int GetMaxHealth() => maxHealth;

    public int GetHealth() => health;

    public void SetHealth(int currentHealth) => health = currentHealth;

    public string SaveState()
    {
        BreakableObjectData data = new()
        {
            currentHealth = health
        };

        return JsonUtility.ToJson(data);
    }

    public void LoadState(string json)
    {
        BreakableObjectData data = JsonUtility.FromJson<BreakableObjectData>(json);
        if (data != null)
            health = data.currentHealth;
    }
}
