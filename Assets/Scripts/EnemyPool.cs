using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    public List<EnemyTier> enemyTiers;
    public int poolSizePerTier = 10;

    private readonly Dictionary<GameObject, Queue<Enemy>> pools = new();

    public Vector3 poolGraveyardPosition = new(0, -1000, 0);

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (EnemyTier tier in enemyTiers)
        {
            Queue<Enemy> pool = new();
            for (int i = 0; i < poolSizePerTier; i++)
            {
                GameObject enemyPrefab = Instantiate(tier.prefab);
                Enemy enemy = enemyPrefab.GetComponent<Enemy>();
                enemyPrefab.SetActive(false);
                pool.Enqueue(enemy);
            }
            pools[tier.prefab] = pool;
        }
    }

    public Enemy GetEnemy(GameObject prefab, Vector3 spawnPosition)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning("No pool found for prefab: " + prefab.name);
            return null;
        }

        Queue<Enemy> currentEnemyPool = pools[prefab];
        Enemy enemy = currentEnemyPool.Dequeue();

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
        {
            ragdollController.DisableRagdoll();
        }

        enemy.transform.position = poolGraveyardPosition;
        enemy.gameObject.SetActive(false);

        PrefabID id = enemy.GetComponent<PrefabID>();
        GameObject enemyPrefab = PrefabRegistry.GetPrefabByKey(id.prefabKey);

        Queue<Enemy> pool = pools[enemyPrefab];
        pool.Enqueue(enemy);
    }

    public List<EnemyTier> GetAvailableTiers(int currentDay)
    {
        return enemyTiers.FindAll(tier => currentDay >= tier.unlockDay);
    }
}

[System.Serializable]
public class EnemyTier
{
    public GameObject prefab;
    public int unlockDay;
}
