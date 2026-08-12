using Game.AI.Enemies;
using UnityEngine;
using static BreakableObject;

namespace Game.Defenses
{
    public class DefenderTotem : Defense
    {
        [SerializeField] private float pulseRadius = 5f;
        [SerializeField] private int pulseDamage = 15;
        [SerializeField] private float pulseInterval = 2f; // Time between pulses
        [SerializeField] private ParticleSystem pulseEffect;

        private float pulseTimer = 0f;

        protected override void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
            pulseTimer -= Time.deltaTime;

            if (pulseTimer <= 0f && HasEnemiesInRange())
            {
                Fire();
                pulseTimer = pulseInterval;
            }
        }

        protected override void Fire()
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, hits);

            if (hitCount == hits.Length)
            {
                Collider[] expandedArray = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, expandedArray);
                hits = expandedArray;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (!hit.TryGetComponent(out Enemy enemy)) continue;

                Vector3 targetHitPoint = hit.ClosestPoint(transform.position);
                Vector3 targetHitNormal = (targetHitPoint - transform.position).normalized;

                if (enemy.breakableObject != null)
                {
                    DamageInfo attackDamageInfo = new
                    (
                        damage: pulseDamage,
                        hitPoint: targetHitPoint,
                        hitNormal: targetHitNormal
                    );

                    enemy.breakableObject.TakeDamage(attackDamageInfo);
                }

                enemy.EnemyCombat.OnAttacked(transform);
            }

            if (pulseEffect != null)
                pulseEffect.Play();

            AudioManager.Instance.PlaySFX(shotSound, position: transform.position);
        }

        private bool HasEnemiesInRange()
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pulseRadius, hits);

            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].TryGetComponent(out Enemy enemy) && IsTargetAlive(enemy.gameObject.transform)) return true;
            }

            return false;
        }

        protected override void FindTarget() { } // Not used
    }
}
