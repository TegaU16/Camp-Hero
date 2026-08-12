using Game.AI.Enemies;
using UnityEngine;
using static BreakableObject;

namespace Game.Defenses
{
    [RequireComponent(typeof(Rigidbody))]
    public class MortarProjectile : MonoBehaviour
    {
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private float arcHeight = 5f;

        [SerializeField] private float turnSpeed = 5f;
        Transform target;
        Transform mortar;

        private Rigidbody rb;
        private int projectileDamage;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position).normalized;

            Vector3 norm = rb.linearVelocity.normalized;
            float magnitude = rb.linearVelocity.magnitude;
            Vector3 newVelocity = Vector3.Lerp(norm, direction, turnSpeed * Time.fixedDeltaTime) * magnitude;

            rb.linearVelocity = newVelocity;
            transform.forward = rb.linearVelocity.normalized;
        }

        public void Launch(Vector3 targetPosition, int projectileDamageAmount)
        {
            projectileDamage = projectileDamageAmount;

            Vector3 start = transform.position;
            Vector3 end = targetPosition;

            Vector3 direction = end - start;
            Vector3 horizontal = new(direction.x, 0f, direction.z);
            float heightDifference = direction.y;

            float gravity = -Physics.gravity.y;

            float initialYVelocity = Mathf.Sqrt(2 * gravity * arcHeight);
            float timeToApex = initialYVelocity / gravity;

            float totalTime = timeToApex + Mathf.Sqrt(2 * (arcHeight - heightDifference) / gravity);
            Vector3 initialXZVelocity = horizontal / totalTime;

            Vector3 launchVelocity = initialXZVelocity + Vector3.up * initialYVelocity;

            rb.linearVelocity = launchVelocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, hits);

            if (hitCount == hits.Length)
            {
                Collider[] expandedArray = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, expandedArray);
                hits = expandedArray;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];

                if (!hit.TryGetComponent(out Enemy enemy)) continue;
                if (!hit.TryGetComponent(out BreakableObject breakable)) continue;

                Vector3 targetHitPoint = hit.GetComponent<CharacterController>().ClosestPoint(transform.position);
                Vector3 targetHitNormal = (targetHitPoint - transform.position).normalized;

                DamageInfo attackDamageInfo = new
                (
                    damage: projectileDamage,
                    hitPoint: targetHitPoint,
                    hitNormal: targetHitNormal
                );

                breakable.TakeDamage(attackDamageInfo);

                enemy.EnemyCombat.OnAttacked(mortar);
            }

            if (explosionEffect != null)
                Instantiate(explosionEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        public void SetTarget(Transform currentTarget)
        {
            target = currentTarget;
        }

        public void SetMortar(Transform currentMortar)
        {
            mortar = currentMortar;
        }
    }
}
