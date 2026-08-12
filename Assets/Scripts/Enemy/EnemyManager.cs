using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.AI.Enemies
{
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
            if (activeTargets.Contains(target))
                activeTargets.Remove(target);
        }

        public List<Targetable> GetActiveTargets()
        {
            activeTargets.RemoveAll(IsInvalidTarget);
            return activeTargets;
        }

        private bool IsInvalidTarget(Targetable target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return true;

            if (target.TryGetComponent(out BreakableObject breakable)) return breakable.GetHealth() <= 0;

            if (target.TryGetComponent(out Health health)) return health.GetHealth() <= 0;

            return true;
        }

        private void Update()
        {
            if (Time.time >= nextGlobalScanTime)
            {
                StartCoroutine(RunGlobalScan());
                nextGlobalScanTime = Time.time + globalScanInterval;
            }
        }

        private IEnumerator RunGlobalScan()
        {
            int batchSize = 10;
            for (int i = 0; i < activeEnemies.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, activeEnemies.Count - i);
                for (int j = 0; j < count; j++)
                {
                    Enemy enemy = activeEnemies[i + j];
                    enemy.EnemyTargeting.AssignBestTarget(activeTargets);
                }
                yield return null; // wait for next frame
            }
        }

        public List<Enemy> GetActiveEnemies() => activeEnemies;
    }
}
