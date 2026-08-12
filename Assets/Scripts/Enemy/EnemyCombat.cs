using System.Collections.Generic;
using Game.StatusEffects;
using Unity.Services.Analytics;
using UnityEngine;
using static BreakableObject;

namespace Game.AI.Enemies
{
    public enum RangedAttackType
    { 
        Laser,
        Projectile,
        Spawn
    }

    public enum AttackType 
    {
        Melee,
        Ranged,
        Mixed
    }
[RequireComponent(typeof(BreakableObject), typeof(Enemy), typeof(Animator))]

    [RequireComponent(typeof(VoxelAgent))]
    public class EnemyCombat : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int RangedAttackHash = Animator.StringToHash("RangedAttack");
        private static readonly int SpawnAttackHash = Animator.StringToHash("SpawnAttack");
        private static readonly int ProjectileAttackHash = Animator.StringToHash("ProjectileAttack");
        private static readonly int LaserAttackHash = Animator.StringToHash("LaserAttack");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [Header("Combat Settings")]
        public float meleeAttackRange = 2f;
        [SerializeField] private float rangedAttackRange = 10f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private int poise;

        [SerializeField] private float attackRangeBuffer = 1.5f;

        private int poiseStack;
        private bool isAttacking = false;
        private float attackTimer;

        [Header("Combat References")]
        [SerializeField] private AttackType attackType = AttackType.Melee;
        public Sprite enemyIcon;
        [SerializeField] private List<MonoBehaviour> rangedAttackScripts = new();
        [SerializeField] private AudioClip attackSwingSound;

        [Header("Laser")]
        [SerializeField] private AudioClip chargingSound;

        private IRangedAttackBehavior currentRangedAttack;
        private readonly List<IRangedAttackBehavior> rangedAttacks = new();

        private Transform myTransform;
        private Animator animator;
        private BreakableObject breakableObject;
        private VoxelAgent agent;
        private Enemy enemy;

        public float RangedAttackThreshold => rangedAttackRange * attackRangeBuffer;
        public float MeleeAttackThreshold => meleeAttackRange * attackRangeBuffer;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            breakableObject = GetComponent<BreakableObject>();
            animator = GetComponent<Animator>();
            agent = GetComponent<VoxelAgent>();

            foreach (MonoBehaviour script in rangedAttackScripts)
            {
                if (script is IRangedAttackBehavior attack)
                    rangedAttacks.Add(attack);
                else
                    Debug.LogWarning($"{script.name} does not implement IRangedAttackBehavior!");
            }
        }

        public void Init()
        {
            isAttacking = false;
            poiseStack = 0;
        }

        public bool AddPoise(int poiseDamage)
        {
            poiseStack += poiseDamage;
            bool shouldStagger = poiseStack >= poise;
            
            if (shouldStagger)
                poiseStack -= poise;

            return shouldStagger;
        }

        public bool ShouldScanForTargets(float distanceToTarget)
        {
            bool shouldScanForTargets = attackType switch
            {
                AttackType.Mixed => distanceToTarget > Mathf.Min(MeleeAttackThreshold, RangedAttackThreshold),
                AttackType.Melee => distanceToTarget > MeleeAttackThreshold,
                AttackType.Ranged => distanceToTarget > RangedAttackThreshold,
                _ => false,
            };

            return shouldScanForTargets;
        }

        public bool OutOfAttackRange(float distanceToTarget) => distanceToTarget > Mathf.Max(meleeAttackRange, rangedAttackRange);

        public void HandleAttacking(float distanceToTarget)
        {
            if (enemy.activeStuns.Count > 0) return;

            Vector3 dir = enemy.EnemyTargeting.CurrentTarget.position - myTransform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRot, Time.deltaTime * 10f);
            }

            if (!isAttacking)
                attackTimer += Time.deltaTime;

            if (!isAttacking && attackTimer >= attackCooldown)
            {
                animator.SetTrigger(AttackHash);
                isAttacking = true;
                attackTimer = 0f;
            }

            if (distanceToTarget <= meleeAttackRange + 0.5f) return;
            
            isAttacking = false;
            enemy.ResetAttack();
        }

        public void HandleRangedAttacking(float distanceToTarget)
        {
            if (enemy.activeStuns.Count > 0) return;

            myTransform.LookAt(new Vector3(
                enemy.EnemyTargeting.CurrentTarget.position.x,
                myTransform.position.y,
                enemy.EnemyTargeting.CurrentTarget.position.z
            ));

            if (!isAttacking)
                attackTimer += Time.deltaTime;

            if (attackTimer >= attackCooldown)
            {
                if (rangedAttacks.Count == 0) return;

                int index = UnityEngine.Random.Range(0, rangedAttacks.Count);
                currentRangedAttack = rangedAttacks[index];

                switch (currentRangedAttack.AttackType)
                {
                    case RangedAttackType.Laser:
                        animator.SetTrigger(LaserAttackHash);
                        break;
                    case RangedAttackType.Projectile:
                        animator.SetTrigger(ProjectileAttackHash);
                        break;
                    case RangedAttackType.Spawn:
                        animator.SetTrigger(SpawnAttackHash);
                        break;
                    default:
                        animator.SetTrigger(RangedAttackHash);
                        break;
                }

                isAttacking = true;
                attackTimer = 0f;
            }

            if (distanceToTarget <= rangedAttackRange + 1f) return;

            isAttacking = false;
            enemy.ResetAttack();
        }

        public void HandleAttackTrigger(float distanceToTarget)
        {
            if (distanceToTarget <= meleeAttackRange && (attackType == AttackType.Melee || attackType == AttackType.Mixed))
            {
                BeginMeleeAttack();
                return;
            }

            if (distanceToTarget <= rangedAttackRange && (attackType == AttackType.Ranged || attackType == AttackType.Mixed))
            {
                BeginRangedAttack();
                return;
            }
        }

        private void BeginMeleeAttack()
        {
            agent.CancelPath();
            animator.SetFloat(SpeedHash, 0f);
            enemy.SetCurrentState(Enemy.State.Attacking);

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        private void BeginRangedAttack()
        {
            agent.CancelPath();
            animator.SetFloat(SpeedHash, 0f);
            enemy.SetCurrentState(Enemy.State.RangedAttacking);

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        // Called by animation event
        private void RangedAttack()
        {
            if (currentRangedAttack == null || enemy.EnemyTargeting.CurrentTarget == null) return;

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage;
            int finalDamage = (int)damageWithMultiplier;

            currentRangedAttack.ExecuteAttack(myTransform, enemy.EnemyTargeting.CurrentTarget, finalDamage);
            currentRangedAttack = null;
        }

        // Called by animation event
        private void PlayChargingSound() => AudioManager.Instance.PlaySFX(chargingSound, position: transform.position);

        // Called by animation event
        private void PlaySwingSound() => AudioManager.Instance.PlaySFX(attackSwingSound, position: transform.position);

        // Called by animation event
        private void EndAttack()
        {
            isAttacking = false;
            enemy.ResetMovement();
        }

        public void OnAttacked(Transform attacker)
        {
            enemy.EnemyTargeting.SetRecentAttacker(attacker);
            enemy.EnemyTargeting.AssignBestTarget(EnemyManager.Instance.GetActiveTargets());
        }

        public void DealDamage()
        {
            if (enemy.EnemyTargeting.CurrentTarget == null) return;

            float distance = Vector3.Distance(myTransform.position, enemy.EnemyTargeting.CurrentTarget.position);
            float meleeAttackRangeBuffer = 0.5f;
            if (distance > meleeAttackRange + meleeAttackRangeBuffer) return;

            float poisonMult = GetPoisonDamageMult();

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage * poisonMult;
            int finalDamage = (int)damageWithMultiplier;

            if (enemy.EnemyTargeting.CurrentTarget.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(finalDamage);
                Vector3 knockbackDir = (enemy.EnemyTargeting.CurrentTarget.position - myTransform.position).normalized;

                if (enemy.EnemyTargeting.CurrentTarget.TryGetComponent(out CharacterController targetCC))
                {
                    knockbackDir.y = 0.5f;

                    if (targetCC.enabled)
                        targetCC.Move(knockbackForce * Time.deltaTime * knockbackDir);
                }

                return;
            }

            BreakableObject targetBreakable = enemy.EnemyTargeting.CurrentTarget.GetComponentInParent<BreakableObject>();
            if (targetBreakable == null) return;

            Vector3 targetHitPoint = targetBreakable.GetComponentInChildren<Collider>().ClosestPoint(transform.position);
            Vector3 targetHitNormal = (targetHitPoint - transform.position).normalized;

            DamageInfo attackDamageInfo = new
            (
                damage: finalDamage,
                hitPoint: targetHitPoint,
                hitNormal: targetHitNormal,
                fromEnemy: true
            );

            targetBreakable.TakeDamage(attackDamageInfo);
        }

        private float GetPoisonDamageMult()
        {
            if (enemy.activePoisons.Count == 0) return 1f;

            float finalMult = 1f;
            foreach (PoisonEffect effect in enemy.activePoisons.Keys)
                finalMult *= effect.attackDamageMult;

            return finalMult;
        }

        public void SetTransform(Transform transform) => myTransform = transform;
    }
}
