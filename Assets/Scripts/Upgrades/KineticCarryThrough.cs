using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Passive/Kinetic Carry-Through")]
public class KineticCarryThrough : PassiveUpgradeEffect
{
    [Tooltip("Radius around the killed enemy to deal carry-through damage.")]
    public float radius = 5f;

    [Tooltip("Percentage of overflow damage applied to surrounding enemies.")]
    [Range(0f, 1f)]
    public float overflowMultiplier = 0.5f;

    private void OnEnemyKilled(Enemy killedEnemy, int damageDealt, int damageRequired, BreakableObject breakable)
    {
        int overflow = damageDealt - damageRequired;
        int baseCarryDamage = damageRequired;

        // Start coroutine instead of applying damage instantly
        killedEnemy.StartCoroutine(CarryThroughCoroutine(killedEnemy.transform.position, baseCarryDamage, overflow, breakable));
    }

    private IEnumerator CarryThroughCoroutine(Vector3 center, int baseDamage, int overflow, BreakableObject breakable)
    {
        List<Enemy> targets = EnemyManager.Instance.GetActiveEnemies();
        List<Enemy> killedThisChain = new();

        foreach (Enemy enemy in targets)
        {
            if (enemy == null || enemy.GetCurrentState() == Enemy.State.Dead || Vector3.Distance(center, enemy.transform.position) > radius) continue;

            int damage = baseDamage;
            if (overflow > 0)
                damage += Mathf.RoundToInt(overflow * overflowMultiplier);

            if (breakable != null)
                breakable.TakeDamage(damage, crit: false);

            if (breakable != null && breakable.GetHealth() == 0)
                killedThisChain.Add(enemy);

            yield return null; // wait 1 frame before applying damage to next target
        }

        // Chain carry-through for newly killed enemies
        foreach (Enemy killed in killedThisChain)
        {
            BreakableObject killedBreakable = killed.GetComponent<BreakableObject>();
            int carriedOverflow = Mathf.Max(0, overflow);
            yield return CarryThroughCoroutine(killed.transform.position, baseDamage, carriedOverflow, killedBreakable);
        }
    }

    public override void OnUnlocked(Player player)
    {
        base.OnUnlocked(player);

        // Subscribe to every enemy's OnEnemyKilled event
        foreach (Enemy enemy in EnemyManager.Instance.GetActiveEnemies())
        {
            if (enemy.TryGetComponent(out BreakableObject bo))
                bo.OnEnemyKilled += OnEnemyKilled;
        }
    }

    public override void OnRemoved(Player player)
    {
        base.OnRemoved(player);

        // Unsubscribe
        foreach (Enemy enemy in EnemyManager.Instance.GetActiveEnemies())
        {
            if (enemy.TryGetComponent(out BreakableObject bo))
                bo.OnEnemyKilled -= OnEnemyKilled;
        }
    }
}
