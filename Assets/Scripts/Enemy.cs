using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum State { Idle, Chasing, Attacking, Dead }
    private State currentState = State.Idle;

    public float detectionRange = 15f;
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

    private BreakableObject breakableObject;

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
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 0.2f;
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (currentState == State.Dead) return;

        if (currentTarget == null) currentTarget = campfireTarget;

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        ScanForTargets();

        switch (currentState)
        {
            case State.Idle:
                if (distance <= detectionRange)
                {
                    currentState = State.Chasing;
                }
                break;

            case State.Chasing:
                if (!isAttacking)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentTarget.position);
                }

                if (currentTarget.CompareTag("Player") && distance > detectionRange + 2f)
                {
                    Debug.Log("[Enemy] Player is too far, reverting to campfire");
                    currentTarget = campfireTarget;
                }

                if (distance <= attackRange)
                {
                    isAttacking = true;
                    agent.isStopped = true;
                    agent.ResetPath();
                    currentState = State.Attacking;
                    Attack();
                    attackTimer = 0f;
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
        float currentSpeed = agent.velocity.magnitude;
        float normalizedSpeed = currentSpeed / moveSpeed;
        animator.SetFloat("Speed", normalizedSpeed, 0.2f, Time.deltaTime);
    }

    void OnAnimatorMove()
    {
        if (currentState == State.Chasing || currentState == State.Attacking)
        {
            // Use deltaPosition to move manually
            transform.position += animator.deltaPosition;

            // Optional: Match NavMeshAgent position to prevent drifting
            agent.nextPosition = transform.position;
        }
    }

    void Attack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void DealDamage()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange + 0.5f) return;

        if (currentTarget.TryGetComponent(out Health targetHealth))
        {
            HealthBar healthBar = currentTarget.GetComponent<HealthBar>();
            targetHealth.TakeDamage(damage, healthBar);

            if (currentTarget.TryGetComponent(out Rigidbody targetRb))
            {
                Vector3 knockbackDir = (currentTarget.position - transform.position).normalized;
                targetRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }
        }
        else if (currentTarget.TryGetComponent(out BreakableObject targetBreakable))
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
        if (ragdollController.IsSetup)
        {
            ragdollController.DisableRagdoll();
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind(); // <---- very important to reset animator properly
            animator.Update(0f); // <---- immediately update the animator
        }

        currentState = State.Idle;

        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.Warp(spawnPosition);
            }
            else
            {
                // If not on navmesh yet, manually move and then try to place
                transform.position = spawnPosition;
                if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    Debug.LogWarning("Couldn't find valid NavMesh position for respawning enemy.");
                }
            }

            agent.isStopped = false;
        }

        if (breakableObject != null)
        {
            breakableObject.ResetObject();
        }

        gameObject.SetActive(true);
    }

    public void EnableAttackHitbox()
    {
        EnemyAttackHitbox hitbox = GetComponentInChildren<EnemyAttackHitbox>(true);
        if (hitbox != null)
        {
            hitbox.gameObject.SetActive(true);
        }
    }

    public void DisableAttackHitbox()
    {
        EnemyAttackHitbox hitbox = GetComponentInChildren<EnemyAttackHitbox>(true);
        if (hitbox != null)
        {
            hitbox.gameObject.SetActive(false);
        }
    }

    void ScanForTargets()
    {
        // Retaliation check
        if (recentAttacker != null && Time.time - lastAttackedTime <= retaliateDuration)
        {
            if (currentTarget != recentAttacker)
            {
                Debug.Log($"[Scan] Retaliating against: {recentAttacker.name}");
                lastTarget = recentAttacker;
                lastTargetChangeTime = Time.time;
                currentTarget = recentAttacker;
            }
            return;
        }

        Transform bestTarget = campfireTarget;
        float closestDistance = Mathf.Infinity;

        Collider[] hits = new Collider[20];
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, hits);

        if (hitCount == hits.Length)
        {
            Collider[] expandedArray = new Collider[hitCount * 2];
            hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, expandedArray);
            hits = expandedArray;
        }

        bool foundHighPriorityPlayer = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            if (hit.TryGetComponent(out Targetable targetable))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                Debug.Log($"[Scan] Found target: {hit.name}, Type: {targetable.targetType}, Distance: {dist}");

                if (targetable.targetType == Targetable.TargetType.Player && dist <= 5f)
                {
                    bestTarget = hit.transform;
                    closestDistance = dist;
                    foundHighPriorityPlayer = true;
                    Debug.Log($"[Scan] Prioritizing nearby PLAYER: {hit.name}");
                    break;  // Immediate priority
                }

                if (!foundHighPriorityPlayer && dist < closestDistance)
                {
                    bestTarget = hit.transform;
                    closestDistance = dist;
                }
            }
        }

        if (foundHighPriorityPlayer || Time.time - lastTargetChangeTime >= targetMemoryTime)
        {
            if (bestTarget != currentTarget)
            {
                Debug.Log($"[Scan] Switching target to: {bestTarget.name} (distance: {closestDistance})");
                lastTarget = bestTarget;
                lastTargetChangeTime = Time.time;
                currentTarget = bestTarget;
            }
        }
        else
        {
            Debug.Log($"[Scan] Retaining current target: {(currentTarget != null ? currentTarget.name : null ?? "None")} due to target memory.");
        }
    }

    public void OnAttacked(Transform attacker)
    {
        recentAttacker = attacker;
        lastAttackedTime = Time.time;
    }
}
