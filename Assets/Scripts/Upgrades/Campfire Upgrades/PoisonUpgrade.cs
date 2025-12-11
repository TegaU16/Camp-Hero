using System.Collections;
using Game.AI.Enemies;
using Game.StatusEffects;
using Game.Terrain.Structures;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Campfire/Poison")]
    public class PoisonUpgrade : CampfireUpgradeEffect
    {
        [Tooltip("Damage per tick")]
        public float damageAmountPerTick = 5f;

        [Tooltip("Seconds between ticks")]
        public float tickRate = 1f;

        public SlowEffect slowEffect;

        public override void OnEnemyEnterRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyEnterRange(campfire, enemy);

            campfire.StartUpgradeCoroutine(this, enemy, DamageEnemyOverTime(enemy));

            if (slowEffect != null)
                slowEffect.Apply(enemy);
        }

        public override void OnEnemyExitRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyExitRange(campfire, enemy);

            campfire.StopUpgradeCoroutine(this, enemy);
            if (slowEffect != null)
                slowEffect.ResetEffect(enemy);
        }

        private IEnumerator DamageEnemyOverTime(Enemy enemy)
        {
            if (!enemy.TryGetComponent(out BreakableObject breakable)) yield break;

            while (true)
            {
                if (breakable.GetHealth() > 0)
                    breakable.TakeDamage(Mathf.RoundToInt(damageAmountPerTick), crit: false);

                yield return new WaitForSeconds(tickRate);
            }
        }
    }
}
