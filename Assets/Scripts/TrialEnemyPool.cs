using System.Collections.Generic;
using UnityEngine;

public class TrialEnemyPool : MonoBehaviour
{
    public static TrialEnemyPool Instance;

    public GameObject[] trialEnemyPrefabs;
    public int poolSizePerType = 10;

    private readonly Dictionary<GameObject, List<GameObject>> pools = new();

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (GameObject prefab in trialEnemyPrefabs)
        {
            List<GameObject> pool = new();
            for (int i = 0; i < poolSizePerType; i++)
            {
                GameObject enemy = Instantiate(prefab);
                enemy.SetActive(false);
                pool.Add(enemy);
            }
            pools[prefab] = pool;
        }
    }

    public GameObject GetEnemy(GameObject prefab, Vector3 position)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"No trial pool found for: {prefab.name}");
            return null;
        }

        foreach (GameObject enemy in pools[prefab])
        {
            if (enemy.GetComponent<TrialEnemyMarker>() == null)
                enemy.AddComponent<TrialEnemyMarker>();

            if (!enemy.activeInHierarchy)
            {
                enemy.transform.position = position;
                enemy.SetActive(true);
                if (enemy.TryGetComponent(out TrialEnemyMarker marker))
                {
                    marker.OnTrialSpawn(); // Optional hook
                }
                return enemy;
            }
        }

        // Expand pool if needed
        GameObject newEnemy = Instantiate(prefab, position, Quaternion.identity);
        newEnemy.SetActive(false);
        pools[prefab].Add(newEnemy);

        if (newEnemy.GetComponent<TrialEnemyMarker>() == null)
            newEnemy.AddComponent<TrialEnemyMarker>();

        newEnemy.SetActive(true);

        if (newEnemy.TryGetComponent(out TrialEnemyMarker newMarker))
        {
            newMarker.OnTrialSpawn();
        }

        return newEnemy;
    }
}
