using UnityEngine;
using System.Collections;
using Game.Players;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Speed Based Damage Boost")]
    public class SpeedBasedDamageUpgrade : PassiveUpgradeEffect
    {
        [Tooltip("Multiplier per unit of normalized speed (0–1). For example, 0.5 means 50% bonus at max speed.")]
        public float speedMultiplier = 0.5f;

        private Player player;
        private PlayerCombat combat;
        private Coroutine updateRoutine;

        public override void OnUnlocked(Player player)
        {
            this.player = player;
            combat = player.GetComponent<PlayerCombat>();
            if (combat == null) return;

            updateRoutine = player.StartCoroutine(UpdateDamageBonus());
        }

        public override void OnRemoved(Player player)
        {
            if (updateRoutine != null)
                player.StopCoroutine(updateRoutine);

            Utility.RemoveMultiplierSource(this, player.damageMultiplier);
        }

        private IEnumerator UpdateDamageBonus()
        {
            while (true)
            {
                float currentSpeed = player.CurrentSpeed;
                float maxSpeed = 10f;
                float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

                float bonusMultiplier = 1f + speedRatio * speedMultiplier;

                // Register or update this upgrade’s contribution
                Utility.SetMultiplierSource(this, bonusMultiplier, player.damageMultiplier);

                yield return null;
            }
        }
    }
}
