using Game.AI.Enemies;
using UnityEngine;
using static BreakableObject;

namespace Game.Defenses
{
    public class Projectile : MonoBehaviour
    {
        public float speed = 20f;
        public float homingStrength = 2f;
        public float maxLifetime = 5f;

        private Transform attacker;
        private Transform target;
        private int projectileDamage;
        private Vector3 currentDirection;
        private bool homing = true;
        private float lifetime;

        public void SetTarget(Transform attacker, Transform t, int dmg, bool useHoming = true)
        {
            this.attacker = attacker;
            target = t;
            projectileDamage = dmg;
            homing = useHoming;

            if (target != null)
                currentDirection = (target.position - transform.position).normalized;
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

            Vector3 toTarget = (target.position - transform.position).normalized;

            if (homing)
                currentDirection = Vector3.RotateTowards(currentDirection, toTarget, homingStrength * Time.deltaTime, 0f);

            float distanceThisFrame = speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, target.position) <= distanceThisFrame)
            {
                HitTarget();
                return;
            }

            transform.Translate(currentDirection * distanceThisFrame, Space.World);
            transform.rotation = Quaternion.LookRotation(currentDirection);
        }

        private void HitTarget()
        {
            Vector3 targetHitPoint;
            Vector3 targetHitNormal;

            if (target.TryGetComponent(out Collider collider))
                targetHitPoint = collider.ClosestPoint(transform.position);
            else if (target.TryGetComponent(out CharacterController controller))
                targetHitPoint = controller.ClosestPoint(transform.position);
            else
                return;

            targetHitNormal = (targetHitPoint - transform.position).normalized;

            if (target.TryGetComponent(out Targetable targetable) && targetable.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(projectileDamage, attacker);

                if (target.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 knockbackDir = (target.position - transform.position).normalized;
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
                enemy.OnAttacked(attacker);
            }

            Destroy(gameObject);
        }
    }
}
