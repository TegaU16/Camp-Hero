using Game.AI.Enemies;
using UnityEngine;
using static BreakableObject;

namespace Game.Defenses
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float homingStrength = 2f;
        [SerializeField] private float maxLifetime = 5f;

        private Transform attacker;
        private Transform target;
        private int projectileDamage;
        private Vector3 currentDirection;
        private bool homing = true;
        private float lifetime;

        private float projectileYaw;

        public void SetTarget(Transform attacker, Transform t, int dmg, bool useHoming = true)
        {
            this.attacker = attacker;
            target = t;
            projectileDamage = dmg;
            homing = useHoming;

            float yaw = Random.Range(-projectileYaw, projectileYaw);
            float pitch = Random.Range(-projectileYaw, projectileYaw);

            Quaternion deviation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 baseDirection = (Utility.GetTargetPoint(target) - transform.position).normalized;
            currentDirection = deviation * baseDirection;
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;

            lifetime += Time.deltaTime;
            if (lifetime >= maxLifetime || target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = (Utility.GetTargetPoint(target) - transform.position).normalized;

            if (homing)
                currentDirection = Vector3.RotateTowards(currentDirection, toTarget, homingStrength * Time.deltaTime, 0f);

            float distanceThisFrame = speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, Utility.GetTargetPoint(target)) <= distanceThisFrame)
            {
                HitTarget();
                return;
            }

            transform.Translate(currentDirection * distanceThisFrame, Space.World);
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }

        private void HitTarget()
        {
            if (!target.TryGetComponent(out Collider collider)) return;

            Vector3 targetHitPoint;
            Vector3 targetHitNormal;

            targetHitPoint = collider.ClosestPoint(transform.position);
            targetHitNormal = (targetHitPoint - transform.position).normalized;

            if (target.TryGetComponent(out Targetable targetable) && targetable.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(projectileDamage, attacker);

                if (target.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 knockbackDir = (Utility.GetTargetPoint(target) - transform.position).normalized;
                    rb.AddForce(knockbackDir * 5f, ForceMode.Impulse);
                }
            }

            if (target.TryGetComponent(out BreakableObject breakable) && target.TryGetComponent(out Enemy enemy))
            {
                DamageInfo attackDamageInfo = new
                (
                    damage: projectileDamage,
                    hitPoint: targetHitPoint,
                    hitNormal: targetHitNormal
                );

                breakable.TakeDamage(attackDamageInfo);
                enemy.EnemyCombat.OnAttacked(attacker);
            }

            Destroy(gameObject);
        }

        public void SetProjectileYaw(float yaw) => projectileYaw = yaw;
    }
}
