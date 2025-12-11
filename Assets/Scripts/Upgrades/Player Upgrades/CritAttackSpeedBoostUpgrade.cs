using UnityEngine;
using System.Collections;
using Game.Players;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Passive/Crit Attack Speed Boost")]
    public class CritAttackSpeedBoostUpgrade : PassiveUpgradeEffect
    {
        [Tooltip("How much faster the attack animations become.")]
        public float speedMultiplier = 1.5f;

        [Tooltip("How long the boost lasts (in seconds).")]
        public float duration = 3f;

        private PlayerCombat combat;
        private Animator animator;
        private Player player;
        private Coroutine activeRoutine;

        public override void OnUnlocked(Player player)
        {
            this.player = player;
            combat = player.GetComponent<PlayerCombat>();
            animator = player.GetComponent<Animator>();

            if (combat == null || animator == null) return;

            // Subscribe to critical hit event
            combat.OnCriticalHit += HandleCrit;
        }

        public override void OnRemoved(Player player)
        {
            if (combat != null)
                combat.OnCriticalHit -= HandleCrit;

            if (activeRoutine != null)
                player.StopCoroutine(activeRoutine);

            if (animator != null)
                animator.speed = 1f; // Restore normal speed
        }

        private void HandleCrit()
        {
            // Restart the timer if another crit happens mid-boost
            if (activeRoutine != null)
                player.StopCoroutine(activeRoutine);

            activeRoutine = player.StartCoroutine(BoostRoutine());
        }

        private IEnumerator BoostRoutine()
        {
            float originalSpeed = animator.speed;
            animator.speed = originalSpeed * speedMultiplier;

            yield return new WaitForSeconds(duration);

            animator.speed = originalSpeed;
            activeRoutine = null;
        }
    }
}
