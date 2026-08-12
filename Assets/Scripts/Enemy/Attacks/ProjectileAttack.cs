using Game.Defenses;
using UnityEngine;

namespace Game.AI.Enemies.Attacks
{
    public class ProjectileAttack : MonoBehaviour, IRangedAttackBehavior
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private float projectileYaw;

        [SerializeField] private AudioClip shotSound;

        public RangedAttackType AttackType => RangedAttackType.Projectile;

        public void ExecuteAttack(Transform attacker, Transform target, int damage)
        {
            if (projectilePrefab == null || projectileSpawnPoint == null || target == null) return;

            AudioManager.Instance.PlaySFX(shotSound, position: transform.position);

            Vector3 targetPoint = target.position;

            if (target.TryGetComponent(out CharacterController controller))
                targetPoint = target.position + controller.center;
            else if (target.TryGetComponent(out Collider col))
                targetPoint = col.bounds.center;

            Vector3 spawnPos = projectileSpawnPoint.position;
            Vector3 direction = (targetPoint - spawnPos).normalized;

            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            if (projectile.TryGetComponent(out Projectile projectileScript))
            {
                projectileScript.SetProjectileYaw(projectileYaw);
                projectileScript.SetTarget(attacker, target, damage);
            }
        }
    }
}
