using UnityEngine;
using System.Collections;
using Game.Players;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Active/Dash")]
    public class DashUpgrade : ActiveUpgradeEffect
    {
        [Header("Dash Settings")]
        public float dashForce = 20f;
        public float dashDuration = 0.2f;
        public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public GameObject dashStreakPrefab;
        public string dashAnimationTrigger = "Dash";

        public override void Activate(Player player)
        {
            // Don’t allow activation if on cooldown or not enough stamina
            if (isOnCooldown) return;
            if (player.CurrentStamina < staminaCost) return;

            player.StartCoroutine(DashRoutine(player));

            if (player.TryGetComponent(out Health health))
                health.isImmune = true;

            if (player.TryGetComponent(out PlayerCombat playerCombat))
                playerCombat.canAttack = false;

            // Trigger the slam animation
            if (player.TryGetComponent(out Animator animator))
                animator.SetTrigger(dashAnimationTrigger);

            if (player.TryGetComponent(out ProceduralAnimator proceduralAnimator))
                proceduralAnimator.enabled = false;

            player.StartCoroutine(CooldownRoutine());
        }

        private IEnumerator DashRoutine(Player player)
        {
            // Deduct stamina
            player.UseStamina(staminaCost);

            if (dashStreakPrefab != null)
                Instantiate(dashStreakPrefab, player.speedLinesSpawn);

            if (!player.TryGetComponent(out CharacterController cc)) yield break;

            Vector3 dashDirection = player.transform.forward.normalized;
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                float t = elapsed / dashDuration;
                float speedMultiplier = dashSpeedCurve.Evaluate(t);

                // Move the player
                cc.Move(dashForce * speedMultiplier * Time.deltaTime * dashDirection);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
