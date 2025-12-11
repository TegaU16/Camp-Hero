using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Lifesteal")]
    public class LifestealUpgrade : PassiveUpgradeEffect
    {

        private PlayerCombat combat;
        private PlayerCombat.LifestealDelegate lifestealHandler;

        public override void OnUnlocked(Player player)
        {
            combat = player.GetComponent<PlayerCombat>();

            if (combat == null) return;

            // Create a delegate instance
            lifestealHandler = OnHitHealthGain;

            combat.OnLifesteal += lifestealHandler;
        }

        public override void OnRemoved(Player player)
        {
            if (combat != null && lifestealHandler != null)
                combat.OnLifesteal -= lifestealHandler;
        }

        private int OnHitHealthGain(int damageDealt)
        {
            int healthGain = (int)(damageDealt / 10f);
            return healthGain;
        }
    }
}
