using Game.AI.Enemies;
using Game.Terrain.Structures;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Campfire/Flame Aura")]
    public class FlameAuraUpgrade : CampfireUpgradeEffect
    {
        public override void OnEnemyEnterRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyEnterRange(campfire, enemy);
        }

        public override void OnEnemyExitRange(Campfire campfire, Enemy enemy)
        {
            base.OnEnemyExitRange(campfire, enemy);
        }
    }
}
