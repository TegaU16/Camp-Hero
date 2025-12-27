using Game.AI.Enemies;
using UnityEngine;

namespace Game.Defenses
{
    [RequireComponent(typeof(Rigidbody))]
    public class MortarProjectile : MonoBehaviour
    {
        public float explosionRadius = 3f;
        public LayerMask damageMask;
        public GameObject explosionEffect;
        public float arcHeight = 5f;

        [SerializeField] float turnSpeed = 5f;
        Transform target;
        Transform mortar;

        private Rigidbody rb;
        private int damage;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position).normalized;
            Vector3 newVelocity = Vector3.Lerp(rb.linearVelocity.normalized, direction, turnSpeed * Time.fixedDeltaTime) * rb.linearVelocity.magnitude;

            rb.linearVelocity = newVelocity;
            transform.forward = rb.linearVelocity.normalized;
        }

        public void Launch(Vector3 targetPosition, int damageAmount)
        {
            damage = damageAmount;

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

                Vector3 hitPoint = hit.GetComponent<CharacterController>().ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;

                breakable.TakeDamage(damage, crit: false, hitPoint, hitNormal);

                enemy.OnAttacked(mortar);
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
