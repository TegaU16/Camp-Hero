using Game.AI.Enemies;
using UnityEngine;

namespace Game.Defenses
{
    public class Defense : MonoBehaviour
    {
        public float range = 10f;
        public int damage = 20;
        public float fireRate = 1f;
        public float rotationSpeed = 5f;

        public Transform firePoint;
        public GameObject projectilePrefab;

        protected float fireCooldown;
        protected Transform currentTarget;

        public AudioClip shotSound;

        protected virtual void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;

            fireCooldown -= Time.deltaTime;

            if (currentTarget == null || !IsTargetAlive(currentTarget))
            {
                FindTarget();
            }
            else if (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown = 1f / fireRate;
            }
        }

        protected virtual void FindTarget()
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, hits);

            if (hitCount == hits.Length)
            {
                Collider[] expandedArray = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, range, expandedArray);
                hits = expandedArray;
            }

            float shortestDistance = Mathf.Infinity;
            Transform nearest = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];

                Enemy enemy = hit.GetComponent<Enemy>();
                BreakableObject breakable = hit.GetComponent<BreakableObject>();
                if (enemy == null || breakable == null || !IsTargetAlive(hit.transform)) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist >= shortestDistance) continue;

                shortestDistance = dist;
                nearest = hit.transform;
            }

            currentTarget = nearest;
        }

        protected virtual void Fire()
        {
            if (currentTarget == null) return;

            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            if (projectileObj.TryGetComponent(out Projectile projectile))
                projectile.SetTarget(transform, currentTarget, damage);

            AudioManager.Instance.PlaySFX(shotSound);
        }

        protected bool IsTargetAlive(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (!target.TryGetComponent(out Enemy enemy)) return false;

            Enemy.State enemyState = enemy.GetCurrentState();
            if (enemyState == Enemy.State.Dead) return false;

            float dist = Vector3.Distance(transform.position, target.position);
            return dist <= range;
        }
    }
}
