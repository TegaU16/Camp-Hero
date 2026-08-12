using System.Collections.Generic;
using UnityEngine;

namespace Game.AI.Enemies
{
    public class EnemyPool : MultiObjectPool<Enemy>
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        [SerializeField] private List<EnemyTier> enemyTiers;
        private readonly Dictionary<GameObject, int> currentPoolSizes = new();

        [Tooltip("How much the pool increases per day")]
        [SerializeField] private float dailyGrowthRate = 0.1f;

        [Tooltip("Maximum allowed pool size multiplier")]
        [SerializeField] private float maxGrowthMultiplier = 3f;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        private void Start()
        {
            DayNightCycle.Instance.OnDayAdvanced += AdjustPoolsForNewDay;
            InitializePools(DayNightCycle.Instance.GetCurrentDay());
        }

        public void InitializePools(int currentDay)
        {
            foreach (EnemyTier tier in enemyTiers)
            {
                int adjustedPoolSize = CalculateAdjustedPoolSize(tier.poolSize, currentDay);

                Queue<Enemy> pool = new();

                for (int i = 0; i < adjustedPoolSize; i++)
                    pool.Enqueue(CreatePooledObject(tier.prefab));

                pools[tier.prefab] = pool;
                currentPoolSizes[tier.prefab] = adjustedPoolSize;
            }
        }

        protected override Enemy CreatePooledObject(GameObject prefab)
        {
            GameObject enemyObj = Instantiate(prefab, poolGraveyardPosition, Quaternion.identity);
            enemyObj.SetActive(false);
            enemyObj.transform.SetParent(transform);
            return enemyObj.GetComponent<Enemy>();
        }

        protected override void OnGetObject(Enemy enemy) => enemy.gameObject.SetActive(true);

        protected override void OnReturnObject(Enemy enemy)
        {
            enemy.CancelInvoke();
            enemy.StopAllCoroutines();

            if (enemy.TryGetComponent(out Animator animator))
            {
                animator.enabled = true;
                animator.SetFloat(SpeedHash, 0f);
            }

            if (enemy.TryGetComponent(out SimpleRagdollController ragdoll))
                ragdoll.DisableRagdoll();

            base.OnReturnObject(enemy);
        }

        public Enemy GetEnemy(GameObject prefab, Vector3 spawnPosition)
        {
            if (!pools.ContainsKey(prefab))
            {
                Debug.LogWarning($"No pool found for prefab: {prefab.name}");
                return null;
            }

            Enemy enemy = Get(prefab);
            if (enemy == null) return null;

            enemy.animator.enabled = false;
            enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            enemy.Init(spawnPosition);
            enemy.animator.enabled = true;

            return enemy;
        }

        private int CalculateAdjustedPoolSize(int baseSize, int currentDay)
        {
            float multiplier = Mathf.Min(1f + dailyGrowthRate * (currentDay - 1), maxGrowthMultiplier);
            return Mathf.RoundToInt(baseSize * multiplier);
        }

        public void AdjustPoolsForNewDay(int currentDay)
        {
            foreach (EnemyTier tier in enemyTiers)
            {
                int desiredSize = CalculateAdjustedPoolSize(tier.poolSize, currentDay);
                int currentSize = currentPoolSizes.ContainsKey(tier.prefab) ? currentPoolSizes[tier.prefab] : 0;

                if (desiredSize <= currentSize) continue;

                int toAdd = desiredSize - currentSize;

                if (!pools.TryGetValue(tier.prefab, out Queue<Enemy> pool))
                {
                    pool = new Queue<Enemy>();
                    pools[tier.prefab] = pool;
                }

                for (int i = 0; i < toAdd; i++)
                    pool.Enqueue(CreatePooledObject(tier.prefab));

                currentPoolSizes[tier.prefab] = desiredSize;
            }
        }

        public List<EnemyTier> GetAvailableTiers(int currentDay) => enemyTiers.FindAll(tier => currentDay >= tier.unlockDay);

        private List<EnemyTier> GetTiersByType(Enemy.EnemyType type, int currentDay)
        {
            return enemyTiers.FindAll(tier =>
            {
                if (!tier.prefab.TryGetComponent(out Enemy enemy)) return false;
                return enemy.enemyType == type && currentDay >= tier.unlockDay;
            });
        }

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
        public List<GameObject> regularsToSpawnWith;
    }
}