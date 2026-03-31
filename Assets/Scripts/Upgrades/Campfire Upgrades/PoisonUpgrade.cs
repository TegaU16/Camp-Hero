using Game.AI.Enemies;
using Game.StatusEffects;
using Game.Terrain.Structures;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Campfire/Poison")]
    public class PoisonUpgrade : CampfireUpgradeEffect
    {
        public PoisonEffect poisonEffect;

        public override void OnEnemyEnterRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyEnterRange(campfire, enemy);

            if (poisonEffect != null)
                poisonEffect.Apply(enemy);
        }

        public override void OnEnemyExitRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyExitRange(campfire, enemy);

            if (poisonEffect != null)
                poisonEffect.ResetEffect(enemy);
        }
    }
}
