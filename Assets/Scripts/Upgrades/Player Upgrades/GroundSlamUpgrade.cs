using Game.AI.Enemies;
using Game.Players;
using Game.StatusEffects;
using UnityEngine;
using static BreakableObject;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Active/Ground Slam")]
    public class GroundSlamUpgrade : ActiveUpgradeEffect
    {
        [Header("Slam Settings")]
        public int slamDamage = 50;
        public float radius = 5f;
        public StunEffect stun;
        public string slamAnimationTrigger = "Ground Slam";

        public LayerMask enemyLayer;

        private Player player;

        public override void Activate(Player playerScript)
        {
            if (isOnCooldown) return;
            if (player.CurrentStamina < staminaCost) return;

            player = playerScript;
            player.UseStamina(staminaCost);

            if (player.TryGetComponent(out Health health))
                health.isImmune = true;

            if (player.TryGetComponent(out PlayerCombat playerCombat))
                playerCombat.canAttack = false;

            // Trigger the slam animation
            if (player.TryGetComponent(out Animator animator))
                animator.SetTrigger(slamAnimationTrigger);

            if (player.TryGetComponent(out ProceduralAnimator proceduralAnimator))
                proceduralAnimator.enabled = false;

            player.StartCoroutine(CooldownRoutine()); // begins the cooldown timer
        }

        // Called by animation event
        public void OnSlamImpact()
        {
            Vector3 origin = player.transform.position;

            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, hits, enemyLayer);

            if (hitCount == hits.Length)
            {
                Collider[] expandedArray = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(origin, radius, expandedArray, enemyLayer);
                hits = expandedArray;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (!hit.TryGetComponent(out Enemy enemy) || !hit.TryGetComponent(out BreakableObject breakable)) continue;

                DamageInfo slamDamageInfo = new
                (
                    damage: slamDamage
                );

                breakable.TakeDamage(slamDamageInfo);

                if (stun != null)
                    stun.Apply(enemy);
            }

            // Optionally add visual or sound effects here
            // e.g. Instantiate(slamEffectPrefab, origin, Quaternion.identity);
        }
    }
}
