using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private GameObject player;
    public float spawnRadius = 20f;
    public float spawnInterval = 5f;
    private int maxEnemies;

    private float timer;
    private int currentEnemyCount = 0;

    public LayerMask groundLayer;

    public DayNightCycle dayNightCycle;

    public EnemyPool enemyPool;

    void Start()
    {
        if (dayNightCycle != null)
        {
            maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());
        }
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (dayNightCycle != null && !dayNightCycle.IsNight())
            return;

        if (dayNightCycle != null)
        {
            maxEnemies = CalculateMaxEnemies(dayNightCycle.GetCurrentDay());
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (player == null || enemyPool == null) return;

        Vector3 spawnPos = player.transform.position + Random.onUnitSphere * spawnRadius;
        spawnPos.y = player.transform.position.y;

        int day = dayNightCycle.GetCurrentDay();
        var availableTiers = enemyPool.GetAvailableTiers(day);

        if (availableTiers.Count == 0) return;

        GameObject selectedPrefab = availableTiers[Random.Range(0, availableTiers.Count)].prefab;

        GameObject enemy = enemyPool.GetEnemy(selectedPrefab, spawnPos);

        if (enemy != null)
        {
            enemy.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            currentEnemyCount++;
        }
    }

    public void DecreaseEnemyCount()
    {
        currentEnemyCount--;
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            this.player = player;
        }
        else
        {
            Debug.LogWarning("Player is null!");
        }
    }

    int CalculateMaxEnemies(int currentDay)
    {
        // For example, the maximum enemies increase by 5 for each day
        return Mathf.Min(currentDay * 5, 50); // Cap at 50 enemies max
    }
}
