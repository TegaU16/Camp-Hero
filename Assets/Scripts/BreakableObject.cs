using System.Collections;
using Game;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Inventory;
using Game.Level;
using Game.Players;
using Game.Registries;
using Game.Saving;
using Game.Storage;
using Game.Terrain;
using UnityEngine;
using Worlds;

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
        public readonly Vector3 KnockbackDirection;
        public readonly float KnockbackForce;
        public readonly bool FromEnemy;

        public DamageInfo(
            int damage,
            int poiseDamage = 0,
            bool crit = false,
            Vector3 hitPoint = default,
            Vector3 hitNormal = default,
            Vector3 knockbackDirection = default,
            float knockbackForce = 0f,
            bool fromEnemy = false)
        {
            Damage = damage;
            PoiseDamage = poiseDamage;
            Crit = crit;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            KnockbackDirection = knockbackDirection;
            KnockbackForce = knockbackForce;
            FromEnemy = fromEnemy;
        }
    }

    private Player player;
    private int maxHealth;
    private int health;
    [SerializeField] private int baseHealth;

    [SerializeField] private int expDropped;
    [SerializeField] private Drop[] drops;

    [SerializeField] private EntityType entityType;
    public ObjectType objectType;
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

        HealthUI activeHealthUI = GetComponentInChildren<HealthUI>();

        if (health > 0)
        {
            HitEffectManager hitEffectManager = FindFirstObjectByType<HitEffectManager>();

            if (hitEffectManager != null && hitNormal != default)
                hitEffectManager.SpawnSparks(hitPoint, hitNormal);

            if (entityType == EntityType.Static)
                TryBounce(transform);

            if (enemy != null)
            {
                int poiseDamage = Mathf.RoundToInt(
                    damageInfo.PoiseDamage * player.TotalPoiseDamageMultiplier
                );

                enemy.ApplyStagger(poiseDamage, damageInfo.KnockbackDirection, damageInfo.KnockbackForce);
            }

            if (activeHealthUI == null)
            {
                activeHealthUI = HealthUIPool.Instance.Get();
                activeHealthUI.Setup(this);
            }

            activeHealthUI.SetHealth(health);
            return;
        }

        if (activeHealthUI != null)
            activeHealthUI.SetHealth(0);

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
        using (ScriptPerformanceTracker.Measure("DestroyObject.Total"))
        {
            if (isDestroyed) return;

            isDestroyed = true;

            using (ScriptPerformanceTracker.Measure("DestroyObject.Drops"))
            {
                DropItems();
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.AnimalStats"))
            {
                if (TryGetComponent(out Animal _))
                    WorldSession.CurrentRunStats.animalsKilled++;
            }

            if (entityType != EntityType.Static) return;

            using (ScriptPerformanceTracker.Measure("DestroyObject.Storage"))
            {
                if (TryGetComponent(out StorageContainer storageContainer))
                    DropStoredItems(storageContainer.items);
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.ReleaseRocks"))
            {
                if (TryGetComponent(out OreRockGroup oreRockGroup))
                    oreRockGroup.ReleaseRocks();
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.RemoveChunkObject"))
            {
                VoxelGrid.Instance.RemoveObjectFromChunk(owningChunk, gameObject);
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.MarkVoxelArea"))
            {
                TerrainGenerator.Instance.MarkVoxelArea(
                    gameObject,
                    occupy: false,
                    walkable: true
                );
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.UnityDestroy"))
            {
                Destroy(gameObject);
            }
        }
    }

    private void DropItems()
    {
        if (DestroyedByEnemy && !TryGetComponent(out Enemy _)) return;

        bool hasCategory = TryGetComponent(out ObjectCategory category);
        Vector3 dropPosition = transform.position;

        float resourceMultiplier = hasCategory ? player.TotalResourceDropMultiplier : 1f;

        foreach (Drop itemDrop in drops)
        {
            if (itemDrop.drop == null) continue;
            if (Random.value > itemDrop.dropChance) continue;

            int dropAmount = Random.Range(
                itemDrop.minValue,
                itemDrop.maxValue + 1
            );

            if (hasCategory)
            {
                dropAmount = Mathf.FloorToInt(
                    dropAmount * resourceMultiplier
                );

                if (category.objectType != Game.Terrain.ObjectType.None)
                    WorldSession.CurrentRunStats.resourcesCollected++;
            }

            using (ScriptPerformanceTracker.Measure("DestroyObject.SpawnDrop"))
            {
                SpawnDrop(
                    itemDrop.drop,
                    dropAmount,
                    dropPosition,
                    torsoBone,
                    scatter: false
                );
            }
        }

        float expGain = expDropped * DifficultyManager.Instance.GetExpMultiplier();
        LevelManager.Instance.AddExp(Mathf.FloorToInt(expGain));
    }

    private void DropStoredItems(ItemData[] items)
    {
        foreach (ItemData data in items)
        {
            if (data == null || string.IsNullOrEmpty(data.itemName)) continue;

            SpawnDrop(
                ItemRegistry.Instance.GetByKey(data.itemName),
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
        float scatterForce = 0.5f;
      
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
        Vector3 shrinkScale = originalScale * 0.9f;
        Vector3 overshootScale = originalScale * 1.08f;

        float shrinkTime = 0.08f;
        float expandTime = 0.12f;
        float settleTime = 0.08f;

        float elapsed = 0f;

        // Shrink
        while (elapsed < shrinkTime)
        {
            objTransform.localScale = Vector3.Lerp(
                originalScale,
                shrinkScale,
                elapsed / shrinkTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        objTransform.localScale = shrinkScale;

        // Overshoot expand
        elapsed = 0f;
        while (elapsed < expandTime)
        {
            objTransform.localScale = Vector3.Lerp(
                shrinkScale,
                overshootScale,
                elapsed / expandTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        objTransform.localScale = overshootScale;

        // Settle back
        elapsed = 0f;
        while (elapsed < settleTime)
        {
            objTransform.localScale = Vector3.Lerp(
                overshootScale,
                originalScale,
                elapsed / settleTime
            );

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
