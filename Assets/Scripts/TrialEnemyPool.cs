using System.Collections.Generic;
using UnityEngine;

public class TrialEnemyPool : MonoBehaviour
{
    public static TrialEnemyPool Instance;

    public GameObject[] trialEnemyPrefabs;
    public int poolSizePerType = 10;

    private readonly Dictionary<GameObject, Queue<TrialEnemyMarker>> pools = new();
    private readonly Dictionary<GameObject, GameObject> enemyToPrefab = new();

    [HideInInspector] public HashSet<GameObject> activeEnemies = new();

    public Vector3 poolGraveyardPosition = new(0, -1000, 0);

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (GameObject prefab in trialEnemyPrefabs)
        {
            Queue<TrialEnemyMarker> pool = new();
            for (int i = 0; i < poolSizePerType; i++)
            {
                GameObject enemyPrefab = Instantiate(prefab);
                TrialEnemyMarker enemy = enemyPrefab.GetComponent<TrialEnemyMarker>();
                enemyPrefab.SetActive(false);
                pool.Enqueue(enemy);
            }
            pools[prefab] = pool;
        }
    }

    public void GetEnemy(GameObject prefab, Vector3 position)
    {
        if (!pools.ContainsKey(prefab)) return;
        
        Queue<TrialEnemyMarker> currentEnemyPool = pools[prefab];
        TrialEnemyMarker trialEnemy = currentEnemyPool.Dequeue();
        Enemy enemy = trialEnemy.GetComponent<Enemy>();

        trialEnemy.gameObject.SetActive(true);
        enemy.animator.enabled = false;
        trialEnemy.transform.SetPositionAndRotation(position, Quaternion.identity);
        enemy.Init(position);
        enemy.animator.enabled = true;
    }

    public void ReturnTrialEnemy(GameObject enemyObj)
    {
        if (!enemyObj.TryGetComponent(out Enemy enemy)) return;

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

        PrefabID id = enemyObj.GetComponent<PrefabID>();
        GameObject enemyPrefab = PrefabRegistry.GetPrefabByKey(id.prefabKey);

        Queue<TrialEnemyMarker> pool = pools[enemyPrefab];
        TrialEnemyMarker trialEnemy = enemyObj.GetComponent<TrialEnemyMarker>();
        pool.Enqueue(trialEnemy);

        activeEnemies.Remove(trialEnemy.gameObject);
    }
}
