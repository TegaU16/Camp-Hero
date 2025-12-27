using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Cheat Death")]
    public class CheatDeathUpgrade : PassiveUpgradeEffect
    {
        [Tooltip("The minimum HP percentage above which this effect can trigger.")]
        public float minHPPercentage = 0.2f;

        private Health health;

        public override void OnUnlocked(Player player)
        {
            health = player.GetComponent<Health>();
            if (health == null) return;

            // Subscribe to pre-damage event
            health.OnPreDamage += ModifyDamage;
        }

        public override void OnRemoved(Player player)
        {
            if (health != null)
                health.OnPreDamage -= ModifyDamage;
        }

        private int ModifyDamage(int incomingDamage)
        {
            if (health.GetHealth() / (float)health.maxHealth > minHPPercentage
                && incomingDamage >= health.GetHealth()) return health.GetHealth() - 1;

            return incomingDamage;
        }
    }
}
