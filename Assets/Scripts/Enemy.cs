using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum State { Idle, Chasing, Attacking, RangedAttacking, Dead }
    private State currentState = State.Idle;

    public enum AttackType { Melee, Ranged, Mixed }
    public AttackType attackType = AttackType.Melee;

    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float rangedAttackRange = 10f;

    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public int damage = 10;
    public float knockbackForce = 5f;
    private bool isAttacking = false;

    private Transform currentTarget;
    private Transform campfireTarget;

    private Transform lastTarget;
    private float lastTargetChangeTime;
    private readonly float targetMemoryTime = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private SimpleRagdollController ragdollController;
    private float attackTimer;
    public float moveSpeed = 3.5f;

    private Transform recentAttacker;
    private float lastAttackedTime;
    [SerializeField] private float retaliateDuration = 4f; // How long to remember attacker

    private float stuckTimer = 0f;
    private const float stuckThreshold = 1.5f;

    private BreakableObject breakableObject;

    [SerializeField] private List<Targetable.TargetType> preferredTargets = new();

    [Header("Scoring Weights")]
    public float distanceWeight = 1f;
    public float damageMemoryWeight = -5f;
    public float objectiveThreatWeight = -3f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = true;
        agent.updateRotation = false;
        animator = GetComponent<Animator>();
        ragdollController = GetComponent<SimpleRagdollController>();
        breakableObject = GetComponent<BreakableObject>();
    }

    void Start()
    {
        campfireTarget = GameObject.FindGameObjectWithTag("Campfire").transform;
        currentTarget = campfireTarget;
        currentState = State.Chasing;

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 0.2f;
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (currentState == State.Dead) return;

        ValidateTarget();

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        ScanForTargets();

        switch (currentState)
        {
            case State.Chasing:
                if (!isAttacking)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentTarget.position);
                }

                // Re-issue destination if something is clearly wrong
                if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
                {
                    stuckTimer += Time.deltaTime;

                    if (stuckTimer >= stuckThreshold)
                    {
                        if (agent.isOnNavMesh && agent.enabled)
                        {
                            agent.SetDestination(currentTarget.position);
                            Debug.LogWarning($"{name} stuck for {stuckTimer:F1}s, reissuing destination");
                            stuckTimer = 0f;
                        }
                    }
                }
                else
                {
                    stuckTimer = 0f; // Reset if moving normally
                }

                if (attackType == AttackType.Melee && distance <= attackRange)
                {
                    BeginMeleeAttack();
                }
                else if (attackType == AttackType.Ranged && distance <= rangedAttackRange)
                {
                    BeginRangedAttack();
                }
                else if (attackType == AttackType.Mixed)
                {
                    if (distance <= attackRange)
                        BeginMeleeAttack();
                    else if (distance <= rangedAttackRange)
                        BeginRangedAttack();
                }

                break;

            case State.Attacking:
                agent.isStopped = true;
                transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

                attackTimer += Time.deltaTime;
                if (attackTimer >= attackCooldown)
                {
                    Attack();
                    attackTimer = 0f;
                }

                if (distance > attackRange + 0.5f)
                {
                    isAttacking = false;
                    currentState = State.Chasing;
                    if (agent.isOnNavMesh && agent.enabled && currentTarget != null)
                        agent.SetDestination(currentTarget.position);
                }

                break;

            case State.RangedAttacking:
                transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

                if (distance > rangedAttackRange + 1f)
                {
                    currentState = State.Chasing;
                    isAttacking = false;
                }
                break;
        }

        if (agent.enabled && agent.remainingDistance > agent.stoppingDistance)
        {
            Vector3 move = agent.desiredVelocity;
            if (move != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), Time.deltaTime * 10f);
            }
        }

        // Update animator speed parameter (for blending idle/walk nicely)
        if (!agent.isStopped && agent.remainingDistance > agent.stoppingDistance)
        {
            float currentSpeed = agent.velocity.magnitude;
            float normalizedSpeed = currentSpeed / moveSpeed;
            animator.SetFloat("Speed", normalizedSpeed, 0.2f, Time.deltaTime);
        }
        else if (currentState == State.Chasing)
        {
            // Force walk animation if chasing but agent not moving due to close target
            animator.SetFloat("Speed", 1f, 0.2f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);
        }

        if (agent.velocity == Vector3.zero && currentState == State.Chasing)
        {
            //Debug.LogWarning($"{name} is in Chasing state but velocity is 0. Remaining distance: {agent.remainingDistance}, stopping distance: {agent.stoppingDistance}, has path: {agent.hasPath}");
        }
    }

    void OnAnimatorMove()
    {
        if (currentState == State.Chasing || currentState == State.Attacking)
        {
            /*// Use deltaPosition to move manually
            transform.position += animator.deltaPosition;

            // Optional: Match NavMeshAgent position to prevent drifting
            agent.nextPosition = transform.position;*/
        }
    }

    void Attack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
    }

    void BeginMeleeAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.ResetPath();
        currentState = State.Attacking;
        Attack();
        attackTimer = 0f;
    }

    void BeginRangedAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.ResetPath();
        currentState = State.RangedAttacking;
        animator.SetTrigger("RangedAttack");
        attackTimer = 0f;
    }

    // Called by animation event
    void RangedAttack()
    {
        if (projectilePrefab != null && projectileSpawnPoint != null && currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - projectileSpawnPoint.position).normalized;

            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(direction));
            if (projectile.TryGetComponent(out Projectile projectileScript))
            {
                projectileScript.SetTarget(currentTarget, damage);
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void DealDamage(Targetable targetable)
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange + 0.5f) return;

        if (currentTarget.TryGetComponent(out Health targetHealth))
        {
            HealthBar healthBar;
            if (targetable.targetType == Targetable.TargetType.Player)
            {
                healthBar = currentTarget.GetComponent<Player>().healthBar;
            } 
            else
            {
                healthBar = currentTarget.GetComponent<HealthBar>();
            }
            targetHealth.TakeDamage(damage, healthBar);

            if (currentTarget.TryGetComponent(out Rigidbody targetRb))
            {
                Vector3 knockbackDir = (currentTarget.position - transform.position).normalized;
                targetRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }
        }
        else if (currentTarget.GetComponentInParent<BreakableObject>() is BreakableObject targetBreakable)
        {
            targetBreakable.TakeDamage(damage, false);
        }
    }

    public void Die()
    {
        if (currentState == State.Dead) return;

        currentState = State.Dead;
        animator.enabled = false;

        if (ragdollController != null)
        {
            ragdollController.EnableRagdoll();
        }
        else
        {
            Debug.LogWarning("No SimpleRagdollCreator found!");
        }

        // Optionally: Play a sound or spawn a death effect here

        Invoke(nameof(Despawn), 5f);
    }

    private void Despawn()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.DecreaseEnemyCount();
        }

        if (breakableObject != null)
        {
            breakableObject.DestroyObject();
        }

        gameObject.SetActive(false);
    }

    public void ResetEnemy(Vector3 spawnPosition)
    {
        PooledAIUtility.ResetAI(
            this,
            agent,
            animator,
            spawnPosition,
            ragdollController
        );

        currentState = State.Chasing;
        currentTarget = campfireTarget;

        if (breakableObject != null)
        {
            breakableObject.ResetObject();
        }
    }

    void ScanForTargets()
    {
        if (currentState == State.Attacking || currentState == State.RangedAttacking)
            return;

        // Retaliation still overrides for now
        if (recentAttacker != null && Time.time - lastAttackedTime <= retaliateDuration)
        {
            if (currentTarget != recentAttacker)
            {
                lastTarget = recentAttacker;
                lastTargetChangeTime = Time.time;
                currentTarget = recentAttacker;
            }
            return;
        }

        Targetable[] allTargets = FindObjectsByType<Targetable>(FindObjectsSortMode.None);

        TargetScore bestScore = null;

        foreach (Targetable t in allTargets)
        {
            if (t == null || !t.gameObject.activeInHierarchy)
                continue;

            float distance = Vector3.Distance(transform.position, t.transform.position);
            int priority = GetTargetPriority(t.targetType);
            float damageScore = (t.transform == recentAttacker) ? damageMemoryWeight : 0f;
            float objectiveThreat = EvaluateObjectiveThreat(t);

            TargetScore score = new(t.transform, priority, distance * distanceWeight, damageScore, objectiveThreat);

            if (bestScore == null || score.finalScore < bestScore.finalScore)
            {
                bestScore = score;
            }
        }

        if (bestScore != null && bestScore.target != currentTarget &&
            (Time.time - lastTargetChangeTime >= targetMemoryTime || bestScore.finalScore < GetScoreForTarget(currentTarget)))
        {
            Debug.Log($"{name} switching to {bestScore.target.name} with score {bestScore.finalScore:F2}");
            lastTarget = currentTarget;
            currentTarget = bestScore.target;
            lastTargetChangeTime = Time.time;

            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.SetDestination(currentTarget.position);
            }
        }
    }

    private int GetTargetPriority(Targetable.TargetType type)
    {
        if (preferredTargets != null && preferredTargets.Count > 0)
        {
            int index = preferredTargets.IndexOf(type);
            if (index >= 0)
                return index;
        }

        return int.MaxValue;
    }

    private float GetScoreForTarget(Transform target)
    {
        if (target == null) return float.MaxValue;

        float distance = Vector3.Distance(transform.position, target.position);

        Targetable targetable = target.GetComponent<Targetable>();
        int priority = targetable != null ? GetTargetPriority(targetable.targetType) : int.MaxValue;

        float damageScore = (target == recentAttacker) ? damageMemoryWeight : 0f;
        float objectiveScore = EvaluateObjectiveThreat(targetable);

        return priority + (distance * distanceWeight) + damageScore + objectiveScore;
    }

    private float EvaluateObjectiveThreat(Targetable targetable)
    {
        if (targetable == null) return 0f;

        // You can adjust this logic for smarter threat detection
        if (targetable.targetType == Targetable.TargetType.Campfire)
            return -5f; // Strong incentive to destroy the objective

        return 0f;
    }

    public void OnAttacked(Transform attacker)
    {
        recentAttacker = attacker;
        lastAttackedTime = Time.time;
    }

    public State GetCurrentState()
    {
        return currentState;
    }

    private void ValidateTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = campfireTarget;
            if (agent.isOnNavMesh && agent.enabled)
                agent.SetDestination(campfireTarget.position);
        }
    }
}
