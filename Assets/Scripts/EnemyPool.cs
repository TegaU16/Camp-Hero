using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    public DayNightCycle dayNightCycle;
    public List<EnemyTier> enemyTiers;

    private readonly Dictionary<GameObject, Queue<Enemy>> pools = new();
    private readonly Dictionary<GameObject, int> currentPoolSizes = new();

    public Vector3 poolGraveyardPosition = new(0, -1000, 0);

    [Tooltip("How much the pool increases per day (as a multiplier). Example: 0.1 = 10% more per day.")]
    public float dailyGrowthRate = 0.1f;
    [Tooltip("Maximum allowed pool size multiplier (to prevent infinite growth).")]
    public float maxGrowthMultiplier = 3f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializePools(GameManager.Instance != null ? dayNightCycle.GetCurrentDay() : 1);
    }

    public void InitializePools(int currentDay)
    {
        foreach (EnemyTier tier in enemyTiers)
        {
            int adjustedPoolSize = CalculateAdjustedPoolSize(tier.poolSize, currentDay);

            Queue<Enemy> pool = new();
            for (int i = 0; i < adjustedPoolSize; i++)
            {
                Enemy enemy = CreatePooledEnemy(tier.prefab);
                pool.Enqueue(enemy);
            }

            pools[tier.prefab] = pool;
            currentPoolSizes[tier.prefab] = adjustedPoolSize;
        }
    }

    private Enemy CreatePooledEnemy(GameObject prefab)
    {
        GameObject enemyPrefab = Instantiate(prefab, poolGraveyardPosition, Quaternion.identity);
        enemyPrefab.SetActive(false);
        return enemyPrefab.GetComponent<Enemy>();
    }

    private int CalculateAdjustedPoolSize(int baseSize, int currentDay)
    {
        float multiplier = Mathf.Min(1f + dailyGrowthRate * (currentDay - 1), maxGrowthMultiplier);
        return Mathf.RoundToInt(baseSize * multiplier);
    }

    public Enemy GetEnemy(GameObject prefab, Vector3 spawnPosition)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning("No pool found for prefab: " + prefab.name);
            return null;
        }

        Queue<Enemy> pool = pools[prefab];

        // 🔁 Expand pool if empty (lazy growth)
        if (pool.Count == 0)
        {
            Enemy newEnemy = CreatePooledEnemy(prefab);
            currentPoolSizes[prefab]++;
            Debug.Log($"Pool for {prefab.name} expanded dynamically (size: {currentPoolSizes[prefab]})");
            return PrepareEnemy(newEnemy, spawnPosition);
        }

        Enemy enemy = pool.Dequeue();
        return PrepareEnemy(enemy, spawnPosition);
    }

    private Enemy PrepareEnemy(Enemy enemy, Vector3 spawnPosition)
    {
        enemy.gameObject.SetActive(true);
        enemy.animator.enabled = false;
        enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        enemy.Init(spawnPosition);
        enemy.animator.enabled = true;
        return enemy;
    }

    public void ReturnEnemy(Enemy enemy)
    {
        enemy.CancelInvoke();
        enemy.StopAllCoroutines();

        if (enemy.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.SetFloat("Speed", 0f);
        }

        if (enemy.TryGetComponent(out SimpleRagdollController ragdollController))
            ragdollController.DisableRagdoll();

        enemy.transform.position = poolGraveyardPosition;
        enemy.gameObject.SetActive(false);

        PrefabID id = enemy.GetComponent<PrefabID>();
        GameObject prefab = PrefabRegistry.GetPrefabByKey(id.prefabKey);

        if (pools.TryGetValue(prefab, out Queue<Enemy> pool))
            pool.Enqueue(enemy);
    }

    // Called when a new day starts
    public void AdjustPoolsForNewDay(int currentDay)
    {
        foreach (EnemyTier tier in enemyTiers)
        {
            int desiredSize = CalculateAdjustedPoolSize(tier.poolSize, currentDay);
            int currentSize = currentPoolSizes.ContainsKey(tier.prefab) ? currentPoolSizes[tier.prefab] : 0;

            if (desiredSize > currentSize)
            {
                int toAdd = desiredSize - currentSize;
                if (!pools.TryGetValue(tier.prefab, out var pool))
                {
                    pool = new Queue<Enemy>();
                    pools[tier.prefab] = pool;
                }

                for (int i = 0; i < toAdd; i++)
                    pool.Enqueue(CreatePooledEnemy(tier.prefab));

                currentPoolSizes[tier.prefab] = desiredSize;
                Debug.Log($"Expanded pool for {tier.prefab.name} to {desiredSize}");
            }
        }
    }

    // Return all tiers unlocked for the current day
    public List<EnemyTier> GetAvailableTiers(int currentDay)
    {
        return enemyTiers.FindAll(tier => currentDay >= tier.unlockDay);
    }

    // Return all tiers of a specific type unlocked for the current day
    public List<EnemyTier> GetTiersByType(Enemy.EnemyType type, int currentDay)
    {
        return enemyTiers.FindAll(tier =>
        {
            if (!tier.prefab.TryGetComponent(out Enemy enemy)) return false;
            return enemy.enemyType == type && currentDay >= tier.unlockDay;
        });
    }

    // Return a random prefab for a type, considering unlock day
    public GameObject GetTierPrefab(Enemy.EnemyType type, int currentDay)
    {
        List<EnemyTier> tiers = GetTiersByType(type, currentDay);
        if (tiers.Count == 0) return null;
        return tiers[Random.Range(0, tiers.Count)].prefab;
    }

    public EnemyTier GetTierByPrefab(GameObject prefab)
    {
        foreach (EnemyTier enemyTier in enemyTiers)
        {
            if (enemyTier.prefab == prefab) return enemyTier;
        }

        return null;
    }
}

[System.Serializable]
public class EnemyTier
{
    public GameObject prefab;
    public int unlockDay;
    public int poolSize = 10;

    [Header("Optional Grouping for Elite")]
    public List<GameObject> regularsToSpawnWith;
}
