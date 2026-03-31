using System.Collections;
using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Players;
using UnityEngine;
using static BreakableObject;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Kinetic Carry-Through")]
    public class KineticCarryThrough : PassiveUpgradeEffect
    {
        private static readonly WaitForSeconds _waitForSeconds0_05 = new(0.05f);

        [Tooltip("Radius around the killed enemy to deal carry-through damage.")]
        public float radius = 5f;

        [Tooltip("Percentage of overflow damage applied to surrounding enemies.")]
        [Range(0f, 1f)]
        public float overflowMultiplier = 0.5f;

        private void OnEnemyKilled(Enemy killedEnemy, int damageDealt, int damageRequired)
        {
            int overflow = damageDealt - damageRequired;
            int baseCarryDamage = damageRequired;

            // Start coroutine with new visited set
            killedEnemy.StartCoroutine(CarryThroughCoroutine(
                killedEnemy.transform.position,
                baseCarryDamage,
                overflow,
                new HashSet<Enemy>()
            ));
        }

        private IEnumerator CarryThroughCoroutine(Vector3 center, int baseDamage, int overflow, HashSet<Enemy> visited)
        {
            List<Enemy> targets = EnemyManager.Instance.GetActiveEnemies();
            List<Enemy> killedThisChain = new();

            foreach (Enemy enemy in targets)
            {
                if (enemy == null ||
                    enemy.GetCurrentState() == Enemy.State.Dead ||
                    Vector3.Distance(center, enemy.transform.position) > radius ||
                    visited.Contains(enemy)) continue;

                visited.Add(enemy); // mark as hit by this chain

                int carryThroughDamage = baseDamage;
                if (overflow > 0)
                    carryThroughDamage += Mathf.RoundToInt(overflow * overflowMultiplier);

                if (enemy.breakableObject != null)
                {
                    DamageInfo carryThroughDamageInfo = new
                    (
                        damage: carryThroughDamage
                    );

                    enemy.breakableObject.TakeDamage(carryThroughDamageInfo);

                    if (enemy.breakableObject.GetHealth() <= 0)
                        killedThisChain.Add(enemy);
                }

                yield return _waitForSeconds0_05; // short delay between hits
            }

            // Chain carry-through for newly killed enemies
            foreach (Enemy killed in killedThisChain)
            {
                int carriedOverflow = Mathf.Max(0, overflow);

                yield return CarryThroughCoroutine(
                    killed.transform.position,
                    baseDamage,
                    carriedOverflow,
                    visited // pass visited set so we don’t double-hit enemies
                );
            }
        }

        public override void OnUnlocked(Player player)
        {
            base.OnUnlocked(player);

            // Subscribe to every enemy's OnEnemyKilled event
            foreach (Enemy enemy in EnemyManager.Instance.GetActiveEnemies())
            {
                if (enemy.breakableObject != null)
                    enemy.breakableObject.OnEnemyKilled += OnEnemyKilled;
            }
        }

        public override void OnRemoved(Player player)
        {
            base.OnRemoved(player);

            // Unsubscribe
            foreach (Enemy enemy in EnemyManager.Instance.GetActiveEnemies())
            {
                if (enemy.breakableObject != null)
                    enemy.breakableObject.OnEnemyKilled -= OnEnemyKilled;
            }
        }
    }
}
