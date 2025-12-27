using Game.AI.Enemies;
using Game.StatusEffects;
using Game.Terrain.Structures;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Campfire/Flame Aura")]
    public class FlameAuraUpgrade : CampfireUpgradeEffect
    {
        public BurnEffect burnEffect;

        public override void OnEnemyEnterRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyEnterRange(campfire, enemy);

            if (burnEffect != null)
                burnEffect.Apply(enemy);
        }

        public override void OnEnemyExitRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyExitRange(campfire, enemy);

            if (burnEffect != null)
                burnEffect.ResetEffect(enemy);
        }
    }
}
