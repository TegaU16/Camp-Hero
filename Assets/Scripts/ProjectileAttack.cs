using UnityEngine;

public class ProjectileAttack : MonoBehaviour, IRangedAttackBehavior
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    public string AttackName => "Projectile";

    public void ExecuteAttack(Transform attacker, Transform target, int damage)
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || target == null) return;

        Vector3 targetPoint = target.position;

        if (target.TryGetComponent(out CharacterController controller))
            targetPoint = target.position + controller.center;
        else if (target.TryGetComponent(out Collider col))
            targetPoint = col.bounds.center;
        
        Vector3 direction = (targetPoint - projectileSpawnPoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));
        if (projectile.TryGetComponent(out Projectile projectileScript))
            projectileScript.SetTarget(attacker, target, damage);
    }
}
