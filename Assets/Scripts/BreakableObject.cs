using System.Collections;
using UnityEngine;

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

    public enum ObjectType
    {
        Wood,
        Stone,
        Flesh,
        None
    }

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
    [HideInInspector] public int savedObjectIndex = -1;

    [SerializeField] private Transform torsoBone;

    private bool canBounce = true;
    [SerializeField] private float bounceCooldown = 0.2f;
    private Coroutine activeBounce;

    public delegate void EnemyKilledDelegate(Enemy enemy, int damageDealt, int damageRequired, BreakableObject breakableObject);
    public event EnemyKilledDelegate OnEnemyKilled;

    private void OnEnable()
    {
        DifficultyManager.Instance.OnDifficultyChanged += HandleDifficultyChange;
        ResetObject();
    }

    private void OnDisable()
    {
        DifficultyManager.Instance.OnDifficultyChanged -= HandleDifficultyChange;
    }

    private void HandleDifficultyChange(Difficulty newDifficulty)
    {
        ApplyDifficultyScaling();
    }

    private void ApplyDifficultyScaling(bool reset = false)
    {
        float healthMultiplier = DifficultyManager.Instance.GetHealthMultiplier(entityType);

        float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 1f;

        maxHealth = (int)(baseHealth * healthMultiplier);
        health = reset ? maxHealth : (int)(maxHealth * healthPercent);
    }

    public void TakeDamage(int damage, bool crit, Vector3 hitPoint = default, Vector3 hitNormal = default, bool fromEnemy = false)
    {
        if (isDestroyed) return;

        ShowDamagePopup(damage, crit, hitPoint);

        int damageRequired = health;
        health -= damage;
        health = Mathf.Max(health, 0);

        if (health == 0)
        {
            if (TryGetComponent(out Enemy enemy))
            {
                enemy.Die();
                OnEnemyKilled?.Invoke(enemy, damage, damageRequired, this);
                return;
            }

            if (TryGetComponent(out Animal animal))
            {
                animal.Die();
                return;
            }

            DestroyedByEnemy = fromEnemy;
            DestroyObject();
        }
        else
        {
            HitEffectManager hitEffectManager = FindFirstObjectByType<HitEffectManager>();
            if (hitEffectManager != null && hitNormal != Vector3.zero)
                hitEffectManager.SpawnSparks(hitPoint, hitNormal);

            if (entityType == EntityType.Static)
                TryBounce(transform);
        }
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
            popup.Setup(damage, crit);
    }

    public void DestroyObject()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (!DestroyedByEnemy || GetComponent<Enemy>() != null)
        {
            foreach (Drop itemDrop in drops)
            {
                if (Random.value <= itemDrop.dropChance)
                {
                    int dropAmount = Random.Range(itemDrop.minValue, itemDrop.maxValue + 1);
                    dropAmount = Mathf.Clamp(dropAmount, itemDrop.minValue, itemDrop.maxValue);

                    SpawnDrop(itemDrop.drop, dropAmount, transform.position, torsoBone, scatter: false);
                }
            }

            LevelManager.Instance.AddExp(expDropped);
        }

        // Drop items in furnace slots
        if (TryGetComponent(out FurnaceUnit furnaceUnit))
        {
            foreach (FurnaceSlot slot in furnaceUnit.furnaceSlots)
            {
                slot.SyncNameFromItem();
                if (!string.IsNullOrEmpty(slot.itemName))
                {
                    SpawnDrop(
                        ItemRegistry.GetItemByName(slot.itemName),
                        slot.count,
                        transform.position
                    );
                }
            }
        }

        // Drop items in chest slots
        if (TryGetComponent(out StorageUnit storageUnit))
        {
            foreach (StoredItem storedItem in storageUnit.items)
            {
                storedItem.SyncNameFromItem();
                if (!string.IsNullOrEmpty(storedItem.itemName))
                {
                    SpawnDrop(
                        ItemRegistry.GetItemByName(storedItem.itemName),
                        storedItem.count,
                        transform.position
                    );
                }
            }
        }

        if (TryGetComponent(out WallSegment wall))
        {
            BuildingManager buildingManager = FindAnyObjectByType<BuildingManager>();
            if (buildingManager != null)
            {
                Vector3 wallPos = wall.transform.position;
                Vector3Int wallPosInt = buildingManager.WorldToGrid(wallPos);
                buildingManager.DestroyWall(wallPosInt);
            }
        }

        if (entityType == EntityType.Static)
        {
            if (owningChunk != null && savedObjectIndex >= 0 && savedObjectIndex < owningChunk.savedObjects.Count)
            {
                VoxelGrid.Instance.RemoveObjectFromChunk(owningChunk, savedObjectIndex);
                owningChunk.isDirty = true;
            }

            VoxelGrid.Instance.MarkAreaOccupied(gameObject, false);
            Destroy(gameObject);
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
        isDestroyed = false;
        ApplyDifficultyScaling(true);
    }

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
