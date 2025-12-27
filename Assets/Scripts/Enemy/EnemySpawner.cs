using System.Collections.Generic;
using Game.Registries;
using Game.Saving;
using Game.Terrain;
using UnityEngine;
using Worlds;

namespace Game.AI.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance;

        private GameObject player;
        [SerializeField] private float spawnRadius = 20f;
        [SerializeField] private float[] spawnInterval = new float[2];
        private int maxEnemies;

        private float timer;
        private int currentEnemyCount = 0;

        [SerializeField] private LayerMask groundLayer;

        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private EnemyPool enemyPool;

        [Header("Rare Enemy Chances")]
        public float eliteSpawnChance = 0.007f; // 0.7% chance per spawn attempt
        public float blightSpawnChance = 0.003f; // 0.3% chance per spawn attempt

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            if (dayNightCycle != null)
                maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());
        }

        void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (dayNightCycle != null && !dayNightCycle.IsNight()) return;

            maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());

            timer += Time.deltaTime;
            float randomSpawnInterval = Random.Range(spawnInterval[0], spawnInterval[1]);

            if (timer >= randomSpawnInterval && currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
                timer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (player == null || enemyPool == null) return;

            Vector3 candidatePos = player.transform.position + Random.onUnitSphere * spawnRadius;

            int spawnPosX = Mathf.RoundToInt(candidatePos.x);
            int spawnPosZ = Mathf.RoundToInt(candidatePos.z);

            float height = Utility.GetHeightAt(spawnPosX, spawnPosZ);

            Vector3 spawnPos = new(spawnPosX, height, spawnPosZ);

            if (!VoxelGrid.Instance.IsWithinBorders(spawnPos)) return;

            Vector3Int spawnPosInt = Utility.WorldToVoxelCoord(spawnPos);
            if (!VoxelGrid.Instance.IsWalkable(spawnPosInt)) return;

            int day = dayNightCycle.GetCurrentDay();
            List<EnemyTier> availableTiers = enemyPool.GetAvailableTiers(day);

            if (availableTiers.Count == 0) return;

            GameObject selectedPrefab;

            // Decide if a rare enemy should spawn
            float roll = Random.value;

            if (roll < blightSpawnChance)
                selectedPrefab = enemyPool.GetTierPrefab(Enemy.EnemyType.Blight, day);
            else if (roll < eliteSpawnChance + blightSpawnChance)
                selectedPrefab = enemyPool.GetTierPrefab(Enemy.EnemyType.Elite, day);
            else
                selectedPrefab = availableTiers[Random.Range(0, availableTiers.Count)].prefab; // Regular enemy

            if (selectedPrefab == null) return;

            // Spawn from pool (or instantiate if boss)
            Enemy enemy;
            if (selectedPrefab.TryGetComponent(out Enemy e) && e.enemyType == Enemy.EnemyType.Boss)
            {
                GameObject bossGO = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
                enemy = bossGO.GetComponent<Enemy>();
            }
            else
            {
                enemy = enemyPool.GetEnemy(selectedPrefab, spawnPos);
            }

            if (enemy == null) return;

            enemy.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            currentEnemyCount++;

            // If elite, spawn a few regular enemies nearby
            if (enemy.enemyType == Enemy.EnemyType.Elite)
            {
                EnemyTier enemyTier = enemyPool.GetTierByPrefab(selectedPrefab);

                if (enemyTier == null)
                {
                    Debug.LogError($"Enemy tier for the prefab: {selectedPrefab.name} hasn't been assigned!");
                    return;
                }

                SpawnEliteGroup(enemy.transform.position, enemyTier.regularsToSpawnWith);
            }
        }

        private void SpawnEliteGroup(Vector3 elitePos, List<GameObject> regularGroup)
        {
            if (regularGroup == null || regularGroup.Count == 0) return;

            foreach (GameObject regularPrefab in regularGroup)
            {
                Vector3 offset = Random.insideUnitSphere * 3f; // Small random spread
                Vector3 spawnPos = elitePos + offset;

                Vector3Int spawnPosInt = Utility.WorldToVoxelCoord(spawnPos);
                if (!VoxelGrid.Instance.IsWalkable(spawnPosInt)) continue;

                Enemy enemy = enemyPool.GetEnemy(regularPrefab, spawnPos);
                if (enemy != null)
                    currentEnemyCount++;
            }
        }

        public void DecreaseEnemyCount() => currentEnemyCount--;

        public void SetPlayer(GameObject player)
        {
            if (player != null)
                this.player = player;
            else
                Debug.LogWarning("Player is null!");
        }

        private int CalculateMaxEnemies(int currentDay)
        {
            return Mathf.Min(currentDay * 5, 50); // Cap at 50 enemies max
        }

        private List<EnemySaveData> GetAllEnemySaveData()
        {
            List<EnemySaveData> dataList = new();
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (enemy == null) continue;
                if (!enemy.TryGetComponent(out BreakableObject breakable)) continue;
                if (!enemy.TryGetComponent(out PrefabID id)) continue;

                dataList.Add(new EnemySaveData
                {
                    prefabName = id.prefabKey,
                    position = enemy.transform.position,
                    currentHealth = breakable.GetHealth()
                });
            }
            return dataList;
        }

        public void SaveAllEnemies()
        {
            List<EnemySaveData> data = GetAllEnemySaveData();
            SaveSystem.SaveEnemies(WorldSession.CurrentWorldName, data);
        }

        public void LoadAllEnemies()
        {
            BossHealthBarManager.Instance.ClearHealthBars();

            List<EnemySaveData> savedEnemies = SaveSystem.LoadEnemies(WorldSession.CurrentWorldName);
            if (savedEnemies == null) return;

            foreach (EnemySaveData data in savedEnemies)
            {
                GameObject prefab = PrefabRegistry.GetPrefabByKey(data.prefabName);
                if (prefab == null) continue;

                Enemy enemy;
                GameObject boss = null;

                if (prefab.TryGetComponent(out Enemy e) && e.enemyType == Enemy.EnemyType.Boss)
                {
                    // Instantiate normally instead of using pool
                    boss = Instantiate(prefab, data.position, Quaternion.identity);
                    enemy = boss.GetComponent<Enemy>();
                }
                else
                {
                    // Use pool for regular enemies
                    enemy = enemyPool.GetEnemy(prefab, data.position);
                }

                // Restore health
                if (enemy.TryGetComponent(out BreakableObject breakable))
                {
                    breakable.SetHealth(data.currentHealth);

                    if (boss != null)
                        BossHealthBarManager.Instance.SpawnBossHealthBar(boss);
                }

                // Register with manager
                EnemyManager.Instance.RegisterEnemy(enemy);
            }
        }
    }
}
