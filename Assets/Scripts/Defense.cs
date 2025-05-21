using UnityEngine;

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

    protected virtual void Update()
    {
        if (GameManager.Instance.isPaused) return;

        fireCooldown -= Time.deltaTime;

        FindTarget();
        if (currentTarget != null && fireCooldown <= 0f)
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
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    nearest = hit.transform;
                }
            }
        }

        currentTarget = nearest;
    }

    protected virtual void Fire()
    {
        if (currentTarget == null) return;

        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        if (projectileObj.TryGetComponent(out Projectile projectile))
            projectile.SetTarget(currentTarget, damage);
    }
}
