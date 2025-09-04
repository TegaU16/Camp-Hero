using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public List<EnemyTier> enemyTiers;
    public int poolSizePerTier = 10;

    private readonly Dictionary<GameObject, List<GameObject>> pools = new();

    void Start()
    {
        foreach (EnemyTier tier in enemyTiers)
        {
            List<GameObject> pool = new();
            for (int i = 0; i < poolSizePerTier; i++)
            {
                GameObject enemy = Instantiate(tier.prefab);
                enemy.SetActive(false);
                pool.Add(enemy);
            }
            pools[tier.prefab] = pool;
        }
    }

    public GameObject GetEnemy(GameObject prefab, Vector3 spawnPosition)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning("No pool found for prefab: " + prefab.name);
            return null;
        }

        foreach (GameObject enemy in pools[prefab])
        {
            if (!enemy.activeInHierarchy)
            {
                enemy.SetActive(true);
                enemy.transform.position = spawnPosition;
                if (enemy.TryGetComponent(out Enemy script))
                {
                    script.ResetEnemy(spawnPosition);
                }
                return enemy;
            }
        }

        // Optional: Expand pool
        GameObject newEnemy = Instantiate(prefab);
        newEnemy.SetActive(false);
        pools[prefab].Add(newEnemy);
        newEnemy.SetActive(true);

        if (newEnemy.TryGetComponent(out Enemy newScript))
        {
            newScript.ResetEnemy(spawnPosition);
        }

        return newEnemy;
    }

    public List<EnemyTier> GetAvailableTiers(int currentDay)
    {
        return enemyTiers.FindAll(tier => currentDay >= tier.unlockDay);
    }

    public GameObject GetPrefabByName(string prefabName)
    {
        foreach (EnemyTier tier in enemyTiers)
        {
            if (tier.prefab.name == prefabName)
                return tier.prefab;
        }

        return null;
    }
}

[System.Serializable]
public class EnemyTier
{
    public GameObject prefab;
    public int unlockDay;
}
