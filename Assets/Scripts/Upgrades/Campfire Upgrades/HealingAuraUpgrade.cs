using System.Collections;
using Game.Players;
using Game.Terrain.Structures;
using UnityEngine;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Campfire/Healing Aura")]
    public class HealingAuraUpgrade : CampfireUpgradeEffect
    {
        [Tooltip("Heal per tick")]
        public float healAmountPerTick = 5f;

        [Tooltip("Seconds between heals")]
        public float tickRate = 1f;

        public override void OnPlayerEnterRange(Campfire campfire, Player player)
        {
            base.OnPlayerEnterRange(campfire, player);

            campfire.StartUpgradeCoroutine(this, player, HealPlayerOverTime(player));
        }

        public override void OnPlayerExitRange(Campfire campfire, Player player)
        {
            base.OnPlayerExitRange(campfire, player);

            campfire.StopUpgradeCoroutine(this, player);
        }

        private IEnumerator HealPlayerOverTime(Player player)
        {
            if (!player.TryGetComponent(out Health playerHealth)) yield break;

            while (true)
            {
                if (playerHealth.GetHealth() < playerHealth.maxHealth)
                    playerHealth.AddHealth(Mathf.RoundToInt(healAmountPerTick));

                yield return new WaitForSeconds(tickRate);
            }
        }
    }
}
