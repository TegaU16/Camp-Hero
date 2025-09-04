using UnityEngine;

public class ProjectileAttack : MonoBehaviour, IRangedAttackBehavior
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    public string AttackName => "Projectile";

    public void ExecuteAttack(Transform attacker, Transform target, int damage)
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || target == null) return;

        Vector3 direction = (target.position - projectileSpawnPoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));
        if (projectile.TryGetComponent(out Projectile projectileScript))
        {
            projectileScript.SetTarget(target, damage);
        }
    }
}
