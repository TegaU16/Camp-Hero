using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Active/Stamina To Health")]
    public class StaminaToHealthUpgrade : ActiveUpgradeEffect
    {
        public float minStaminaRequired;

        public override void Activate(Player player)
        {
            if (isOnCooldown) return;
            if (player.CurrentStamina < minStaminaRequired) return;

            ConvertStaminaToHealth(player);
            player.StartCoroutine(CooldownRoutine());
        }

        private void ConvertStaminaToHealth(Player player)
        {
            float halfStamina = player.CurrentStamina / 2f;
            player.UseStamina(halfStamina);
            player.health.AddHealth((int)halfStamina);
        }
    }
}
