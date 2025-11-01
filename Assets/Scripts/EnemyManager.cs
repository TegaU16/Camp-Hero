using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly List<Enemy> activeEnemies = new();
    private readonly List<Targetable> activeTargets = new();

    [SerializeField] private float globalScanInterval = 1f;
    private float nextGlobalScanTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public void RegisterTarget(Targetable target)
    {
        if (!activeTargets.Contains(target))
            activeTargets.Add(target);
    }

    public void UnregisterTarget(Targetable target)
    {
        activeTargets.Remove(target);
    }

    public List<Targetable> GetActiveTargets()
    {
        activeTargets.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);
        return activeTargets;
    }

    private void Update()
    {
        if (Time.time >= nextGlobalScanTime)
        {
            StartCoroutine(RunGlobalScan());
            nextGlobalScanTime = Time.time + globalScanInterval;
        }
    }

    IEnumerator RunGlobalScan()
    {
        int batchSize = 10;
        for (int i = 0; i < activeEnemies.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, activeEnemies.Count - i);
            for (int j = 0; j < count; j++)
            {
                Enemy enemy = activeEnemies[i + j];

                if (enemy.GetCurrentState() == Enemy.State.Idle || enemy.GetCurrentState() == Enemy.State.Chasing)
                    enemy.AssignBestTarget(activeTargets);
            }
            yield return null; // wait for next frame
        }
    }

    public List<Enemy> GetActiveEnemies() => activeEnemies;
}
