using Game.AI.Enemies;
using Game.Players;
using Game.Terrain.Structures;

namespace Game.Upgrades
{
    public class CampfireUpgradeEffect : UpgradeEffect
    {
        public int levelRequirement;
        public float effectRadius;

        // Optionally track whether it’s currently active
        protected bool isActive;

        public override void OnUnlocked(Player player) => isActive = true;

        public override void OnRemoved(Player player) => isActive = false;

        public virtual void OnPlayerEnterRange(Campfire campfire, Player player) { }
        public virtual void OnPlayerExitRange(Campfire campfire, Player player) { }

        public virtual void OnEnemyEnterRange(Campfire campfire, Enemy enemy) { }
        public virtual void OnEnemyExitRange(Campfire campfire, Enemy enemy) { }
    }
}
