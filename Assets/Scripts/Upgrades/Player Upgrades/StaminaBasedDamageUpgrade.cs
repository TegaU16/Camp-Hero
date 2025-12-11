using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Stamina Based Crit Damage Boost")]
    public class StaminaBasedDamageUpgrade : PassiveUpgradeEffect
    {
        public float staminaMultiplier = 0.5f;

        private Player player;
        private PlayerCombat combat;
        private PlayerCombat.ModifyDamageDelegate damageHandler;

        public override void OnUnlocked(Player player)
        {
            this.player = player;
            combat = player.GetComponent<PlayerCombat>();

            if (combat == null) return;

            // Create a delegate instance
            damageHandler = OnCritDamage;

            // Subscribe to the damage modification event
            combat.OnModifyDamage += damageHandler;
        }

        public override void OnRemoved(Player player)
        {
            if (combat != null && damageHandler != null)
                combat.OnModifyDamage -= damageHandler;
        }

        private float OnCritDamage(int currentDamage, bool isCrit)
        {
            float currentStamina = player.CurrentStamina;
            float staminaRatio = Mathf.Clamp01(currentStamina / player.staminaBar.maxStamina);

            float bonusMultiplier = 1f + staminaRatio * staminaMultiplier;

            if (isCrit) return Mathf.RoundToInt(currentDamage * bonusMultiplier);
            return currentDamage;
        }
    }
}
