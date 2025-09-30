using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private GameObject player;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float spawnInterval = 5f;
    private int maxEnemies;

    private float timer;
    private int currentEnemyCount = 0;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private EnemyPool enemyPool;

    void Start()
    {
        if (dayNightCycle != null)
            maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (dayNightCycle != null && !dayNightCycle.IsNight())
            return;

        if (dayNightCycle != null)
            maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());

        timer += Time.deltaTime;

        if (timer >= spawnInterval && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        if (player == null || enemyPool == null) return;

        Vector3 candidatePos = player.transform.position + Random.onUnitSphere * spawnRadius;

        if (Physics.Raycast(candidatePos + Vector3.up * 100f, Vector3.down, out RaycastHit enemyHit, 200f, groundLayer))
        {
            Vector3 spawnPos = enemyHit.point;

            Vector3Int spawnPosInt = Vector3Int.RoundToInt(spawnPos);
            if (VoxelGrid.Instance.IsOccupied(spawnPosInt)) return;

            int day = dayNightCycle.GetCurrentDay();
            List<EnemyTier> availableTiers = enemyPool.GetAvailableTiers(day);

            if (availableTiers.Count == 0) return;

            GameObject selectedPrefab = availableTiers[Random.Range(0, availableTiers.Count)].prefab;

            Enemy enemy = enemyPool.GetEnemy(selectedPrefab, spawnPos);

            if (enemy != null)
            {
                enemy.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                currentEnemyCount++;
            }
        }
    }

    public void DecreaseEnemyCount()
    {
        currentEnemyCount--;
    }

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

            if (enemy.TryGetComponent(out BreakableObject breakable) && enemy.TryGetComponent(out PrefabID id))
            {
                dataList.Add(new EnemySaveData
                {
                    prefabName = id.prefabKey,
                    position = enemy.transform.position,
                    currentHealth = breakable.GetHealth()
                });
            }
        }
        return dataList;
    }

    public void SaveAllEnemies()
    {
        List<EnemySaveData> data = GetAllEnemySaveData();
        SaveSystem.SaveEnemies(GameManager.Instance.currentWorldName, data);
    }

    public void LoadAllEnemies()
    {
        List<EnemySaveData> savedEnemies = SaveSystem.LoadEnemies(GameManager.Instance.currentWorldName);

        if (savedEnemies != null)
        {
            foreach (EnemySaveData data in savedEnemies)
            {
                GameObject prefab = PrefabRegistry.GetPrefabByKey(data.prefabName);
                if (prefab == null) continue;

                Enemy enemy = enemyPool.GetEnemy(prefab, data.position).GetComponent<Enemy>();
                if (enemy.TryGetComponent(out BreakableObject breakable))
                {
                    breakable.SetHealth(data.currentHealth);
                }

                // Register with manager
                EnemyManager.Instance.RegisterEnemy(enemy);
            }
        }
    }
}
