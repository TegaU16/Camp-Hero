using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Chunky Crit")]
    public class ChunkyCritUpgrade : PassiveUpgradeEffect
    {
        public float chunkyMultiplier = 20f;
        public float chance = 0.1f;

        private PlayerCombat combat;
        private bool nextCritChunky = false;

        public override void OnUnlocked(Player player)
        {
            combat = player.GetComponent<PlayerCombat>();
            if (combat == null) return;

            combat.OnCriticalHit += TryMakeChunky;
            combat.OnModifyDamage += ApplyChunkyMultiplier;
        }

        public override void OnRemoved(Player player)
        {
            if (combat == null) return;

            combat.OnCriticalHit -= TryMakeChunky;
            combat.OnModifyDamage -= ApplyChunkyMultiplier;
        }

        private void TryMakeChunky()
        {
            if (Random.value <= chance)
                nextCritChunky = true;
        }

        private float ApplyChunkyMultiplier(int currentDamage, bool isCrit)
        {
            if (isCrit && nextCritChunky)
            {
                nextCritChunky = false;
                return currentDamage * chunkyMultiplier;
            }

            return currentDamage;
        }
    }
}
