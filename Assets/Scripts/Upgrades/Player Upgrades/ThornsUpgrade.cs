using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Thorns")]
    public class ThornsUpgrade : PassiveUpgradeEffect
    {
        [Range(0f, 1f)] public float thornsDamageRatio;
        private Health.ThornsDamageDelegate thornsDamageHandler;
        private Health health;

        public override void OnUnlocked(Player player)
        {
            health = player.GetComponent<Health>();

            if (health == null) return;

            // Create a delegate instance
            thornsDamageHandler = OnThornsHit;

            // Subscribe to the damage modification event
            health.OnHit += thornsDamageHandler;
        }

        public override void OnRemoved(Player player)
        {
            if (health != null && thornsDamageHandler != null)
                health.OnHit -= thornsDamageHandler;
        }

        private void OnThornsHit(int currentDamage, BreakableObject breakable)
        {
            float thornsDamage = currentDamage * thornsDamageRatio;
            breakable.TakeDamage((int)thornsDamage, false);
        }
    }
}
