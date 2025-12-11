using System.Collections.Generic;
using Game.Players;
using Game.Terrain.Structures.Trials;
using UnityEngine;
using Worlds;

namespace Game.AI.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SimpleRagdollController))]
    [RequireComponent(typeof(BreakableObject))]
    [RequireComponent(typeof(VoxelAgent))]
    [RequireComponent(typeof(Animator))]
    public class Enemy : MonoBehaviour
    {
        // -------------------- ENUMS --------------------
        public enum State { Idle, Chasing, Attacking, RangedAttacking, Dead }
        public enum AttackType { Melee, Ranged, Mixed }
        public enum EnemyType { Regular, Elite, Blight, Boss }

        // -------------------- STATE --------------------
        [Header("State")]
        private State currentState = State.Idle;
        public AttackType attackType = AttackType.Melee;
        public EnemyType enemyType = EnemyType.Regular;

        private bool isActive;
        private bool isAttacking = false;

        private Transform currentTarget;
        private Transform recentAttacker;
        private Transform campfireTarget;

        private float attackTimer;
        private float nextTargetSwitchTime;

        // -------------------- COMBAT --------------------
        [Header("Combat Settings")]
        public float meleeAttackRange = 2f;
        public float rangedAttackRange = 10f;
        public float attackCooldown = 2f;
        public int damage = 10;
        public float knockbackForce = 5f;

        [Header("Combat References")]
        public Sprite enemyIcon;
        public BreakableObject breakableObject;
        [SerializeField] private List<MonoBehaviour> rangedAttackScripts = new();

        private IRangedAttackBehavior currentRangedAttack;
        private readonly List<IRangedAttackBehavior> rangedAttacks = new();

        // -------------------- MOVEMENT --------------------
        [Header("Movement")]
        public float moveSpeed = 3.5f;
        private float cachedMoveSpeed;
        private Vector3 knockbackVelocity = Vector3.zero;
        private float verticalVelocity = 0f;
        [SerializeField] private float gravity = -9.8f;

        private Vector3 latestSpawnPos;
        private bool justSpawned;

        // -------------------- AI TARGETING --------------------
        [Header("Targeting")]
        [SerializeField] private List<Targetable.TargetType> preferredTargets = new();
        private readonly HashSet<Transform> blockedTargets = new();

        [SerializeField] private float targetStickTime = 3f;
        [SerializeField] private float minTargetMovementThreshold = 1f;
        [SerializeField] private float repathCooldown = 0.5f;
        [SerializeField] private float repathDistanceThreshold = 1f;
        private Vector3 lastTargetPosition;
        private float lastRepathTime = -999f;
        private readonly float retaliateMemoryDuration = 5f;
        private float lastAttackedTime = -999f;

        // -------------------- PATHFINDING --------------------
        [Header("Pathfinding")]
        private Vector3Int frozenPathTarget = new(-999, 0, -999);
        private float lastPathRequestTime = -999f;
        [SerializeField] private float pathRequestCooldown = 0.2f;

        // -------------------- SCORING --------------------
        [Header("Scoring Weights")]
        public EnemyPersonality personality;

        // -------------------- REFERENCES --------------------
        private VoxelAgent agent;
        [HideInInspector] public Animator animator;
        private SimpleRagdollController ragdollController;
        private CharacterController characterController;
        private Transform myTransform;

        private void Awake()
        {
            myTransform = transform;
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            agent = GetComponent<VoxelAgent>();
            breakableObject = GetComponent<BreakableObject>();
            ragdollController = GetComponent<SimpleRagdollController>();

            foreach (MonoBehaviour script in rangedAttackScripts)
            {
                if (script is IRangedAttackBehavior attack)
                    rangedAttacks.Add(attack);
                else
                    Debug.LogWarning($"{script.name} does not implement IRangedAttackBehavior!");
            }
        }

        private void Start()
        {
            if (GetComponent<TrialEnemyMarker>() != null)
            {
                Transform playerTarget = FindAnyObjectByType<Player>().transform;
                currentTarget = playerTarget;
            }
            else
            {
                campfireTarget = GameObject.FindGameObjectWithTag("Campfire").transform;
                currentTarget = campfireTarget;
            }

            currentState = State.Chasing;
            cachedMoveSpeed = moveSpeed;

            Init(myTransform.position);
        }

        private void OnEnable()
        {
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.RegisterEnemy(this);
        }

        private void OnDisable()
        {
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.UnregisterEnemy(this);
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (currentState == State.Dead) return;

            ValidateTarget(EnemyManager.Instance.GetActiveTargets());
            float distanceToTarget = Vector3.Distance(myTransform.position, currentTarget.position);

            switch (currentState)
            {
                case State.Chasing:
                    HandleChasing(distanceToTarget);
                    break;
                case State.Attacking:
                    HandleAttacking(distanceToTarget);
                    break;
                case State.RangedAttacking:
                    HandleRangedAttacking(distanceToTarget);
                    break;
            }

            if (currentState == State.Idle)
            {
                if (currentTarget == null)
                {
                    AssignBestTarget(EnemyManager.Instance.GetActiveTargets());
                    if (currentTarget == null)
                        currentTarget = campfireTarget;
                }

                SetChasing();
            }
        }

        void FixedUpdate()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (!isActive || currentState == State.Dead) return;
            if (!characterController.enabled) return;

            agent.UpdateAgent();

            Vector3 move = Vector3.zero;

            if (agent.WantsToMove() && (currentState == State.Idle || currentState == State.Chasing))
            {
                // Only horizontal motion from the agent
                Vector3 agentMove = agent.GetMovementThisFrame();
                move.x = agentMove.x;
                move.z = agentMove.z;
            }

            // Apply vertical forces regardless of agent movement
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0)
                    verticalVelocity = 0f;
            }
            else
            {
                verticalVelocity += gravity * Time.fixedDeltaTime;
            }

            move.y = verticalVelocity;

            // Apply knockback
            move += knockbackVelocity;
            knockbackVelocity = -Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

            // Finally move
            Vector3 displacement = moveSpeed * Time.fixedDeltaTime * move;
            characterController.Move(displacement);
        }

        private void HandleChasing(float distanceToTarget)
        {
            if (Time.time - lastRepathTime > repathCooldown &&
                Vector3.Distance(currentTarget.position, lastTargetPosition) > repathDistanceThreshold)
            {
                lastTargetPosition = currentTarget.position;
                lastRepathTime = Time.time;

                if (distanceToTarget > rangedAttackRange * 1.5f || distanceToTarget > meleeAttackRange * 1.5f)
                    AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

                Vector3Int to = GetTargetGridPosition(lastTargetPosition);

                TryRequestPath(to);
            }

            if (ShouldResetPath(distanceToTarget))
            {
                frozenPathTarget = new(-999, 0, -999); // reset frozen path
                TryRequestPath(GetTargetGridPosition(currentTarget.position));
            }

            HandleAttackTrigger(distanceToTarget);
        }

        private void HandleAttacking(float distanceToTarget)
        {
            Vector3 dir = currentTarget.position - myTransform.position;
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
                animator.SetTrigger("Attack");
                isAttacking = true;
                attackTimer = 0f;
            }

            if (distanceToTarget > meleeAttackRange + 0.5f)
            {
                ResetMovement();
                isAttacking = false;
                SetChasing();
                TryRequestPath(GetTargetGridPosition(currentTarget.position));
            }
        }

        private void HandleRangedAttacking(float distanceToTarget)
        {
            myTransform.LookAt(new Vector3(currentTarget.position.x, myTransform.position.y, currentTarget.position.z));

            if (!isAttacking)
                attackTimer += Time.deltaTime;

            if (attackTimer >= attackCooldown)
            {
                if (rangedAttacks.Count == 0) return;

                int index = Random.Range(0, rangedAttacks.Count);
                currentRangedAttack = rangedAttacks[index];

                switch (currentRangedAttack.AttackName)
                {
                    case "Laser":
                        animator.SetTrigger("LaserAttack");
                        break;
                    case "Projectile":
                        animator.SetTrigger("ProjectileAttack");
                        break;
                    case "SpawnOnTarget":
                        animator.SetTrigger("SpawnAttack");
                        break;
                    default:
                        animator.SetTrigger("RangedAttack");
                        break;
                }

                isAttacking = true;
                attackTimer = 0f;
            }

            if (distanceToTarget > rangedAttackRange + 1f)
            {
                ResetMovement();
                isAttacking = false;
                SetChasing();
                TryRequestPath(GetTargetGridPosition(currentTarget.position));
            }
        }

        private void HandleAttackTrigger(float distanceToTarget)
        {
            if (attackType == AttackType.Melee && distanceToTarget <= meleeAttackRange)
                BeginMeleeAttack();
            else if (attackType == AttackType.Ranged && distanceToTarget <= rangedAttackRange)
                BeginRangedAttack();
            else if (attackType == AttackType.Mixed)
            {
                if (distanceToTarget <= meleeAttackRange)
                    BeginMeleeAttack();
                else if (distanceToTarget <= rangedAttackRange)
                    BeginRangedAttack();
            }
        }

        private void TryRequestPath(Vector3Int targetGrid)
        {
            Vector3Int from = GetTargetGridPosition(myTransform.position);

            if (agent.CanWalkDirectly(from, targetGrid))
            {
                frozenPathTarget = new(-999, 0, -999);
                agent.StartTracking(currentTarget);
                return;
            }

            float timeSinceLast = Time.time - lastPathRequestTime;
            float distToFrozen = Vector3Int.Distance(targetGrid, frozenPathTarget);

            if (frozenPathTarget == new Vector3Int(-999, 0, -999) || distToFrozen >= minTargetMovementThreshold || timeSinceLast >= pathRequestCooldown)
            {
                frozenPathTarget = targetGrid;
                agent.StopTracking();
                agent.RequestPath(frozenPathTarget);
                lastPathRequestTime = Time.time;
            }
        }

        private Vector3Int GetTargetGridPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x);
            int z = Mathf.RoundToInt(worldPos.z);
            float y = Utility.GetHeightAt(x, z);

            return new Vector3Int(x, Mathf.RoundToInt(y), z);
        }

        private void ValidateTarget(List<Targetable> potentialTargets)
        {
            bool targetIsInvalid = currentTarget == null || !currentTarget.gameObject.activeInHierarchy;

            if (targetIsInvalid)
            {
                nextTargetSwitchTime = 0f;
                AssignBestTarget(potentialTargets);

                if (campfireTarget == null)
                {
                    GameObject campfire = GameObject.FindGameObjectWithTag("Campfire");
                    if (campfire != null)
                        campfireTarget = campfire.transform;
                }

                if (currentTarget == null)
                    currentTarget = campfireTarget;

                frozenPathTarget = new(-999, 0, -999);

                SetChasing();

                TryRequestPath(GetTargetGridPosition(currentTarget.position));
            }
            else if (Time.time >= nextTargetSwitchTime)
            {
                nextTargetSwitchTime = 0f;
                AssignBestTarget(potentialTargets);
            }
        }

        public void Init(Vector3 spawnPos)
        {
            // Guard against reinitialization
            if (isActive) return;

            if (agent == null || characterController == null) return;

            latestSpawnPos = spawnPos;

            // Reset pathfinding state (no exceptions expected in normal conditions)
            agent.CancelPath();
            agent.Init(spawnPos);

            // Temporarily disable CharacterController for safe repositioning
            bool wasEnabled = characterController.enabled;
            if (wasEnabled)
                characterController.enabled = false;

            myTransform.position = spawnPos;

            if (wasEnabled)
                characterController.enabled = true;

            // Reset motion and animation state
            ResetAnimatorPose();
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            isActive = true;
            justSpawned = true;
            isAttacking = false;

            // Target assignment
            List<Targetable> activeTargets = EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveTargets() : null;
            if (activeTargets != null && activeTargets.Count > 0)
                AssignBestTarget(activeTargets);
            else
                currentTarget = campfireTarget; // fallback

            if (currentTarget == null) return; // No valid target found — don't chase or path

            // Begin chasing and request initial path
            SetChasing();
            Vector3Int targetPos = GetTargetGridPosition(currentTarget.position);
            TryRequestPath(targetPos);
        }

        public void ApplyKnockback(Vector3 dir, float strength)
        {
            knockbackVelocity = dir.normalized * strength;
        }

        public void OnAttacked(Transform attacker)
        {
            recentAttacker = attacker;
            lastAttackedTime = Time.time;

            nextTargetSwitchTime = 0f;
            AssignBestTarget(EnemyManager.Instance.GetActiveTargets());
        }

        void OnAnimatorMove()
        {
            if (justSpawned)
            {
                characterController.enabled = false;
                myTransform.position = latestSpawnPos;
                characterController.enabled = true;
                justSpawned = false;
            }
        }

        void BeginMeleeAttack()
        {
            agent.StopPath();
            animator.SetFloat("Speed", 0f);
            currentState = State.Attacking;

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        void BeginRangedAttack()
        {
            agent.StopPath();
            animator.SetFloat("Speed", 0f);
            currentState = State.RangedAttacking;

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        // Called by animation event
        void RangedAttack()
        {
            if (currentRangedAttack == null || currentTarget == null) return;

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage;
            int finalDamage = (int)damageWithMultiplier;

            currentRangedAttack.ExecuteAttack(myTransform, currentTarget, finalDamage);
            currentRangedAttack = null;
        }

        // Called by animation event
        void EndAttack()
        {
            isAttacking = false;
            ResetMovement();

            if (currentState != State.Dead)
                SetChasing();
        }

        public void ResetMovement()
        {
            moveSpeed = cachedMoveSpeed;
        }

        public void ModifySpeed(float factor)
        {
            if (factor == 1f)
                ResetMovement();
            else
                moveSpeed *= factor;

            if (animator != null && animator.enabled)
                animator.speed = factor;
        }

        public void DealDamage()
        {
            if (currentTarget == null) return;

            float distance = Vector3.Distance(myTransform.position, currentTarget.position);
            if (distance > meleeAttackRange + 0.5f) return;

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage;
            int finalDamage = (int)damageWithMultiplier;

            if (currentTarget.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(finalDamage);

                if (currentTarget.TryGetComponent(out CharacterController targetCC))
                {
                    Vector3 knockbackDir = (currentTarget.position - myTransform.position).normalized;
                    knockbackDir.y = 0.5f;

                    if (targetCC.enabled)
                        targetCC.Move(knockbackForce * Time.deltaTime * knockbackDir);
                }
                else if (currentTarget.TryGetComponent(out Rigidbody targetRb))
                {
                    Vector3 knockbackDir = (currentTarget.position - myTransform.position).normalized;
                    targetRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }
            }
            else if (currentTarget.GetComponentInParent<BreakableObject>() is BreakableObject targetBreakable)
            {
                Vector3 hitPoint = targetBreakable.GetComponent<Collider>().ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;

                targetBreakable.TakeDamage(finalDamage, false, hitPoint, hitNormal, fromEnemy: true);
            }
        }

        public void Die()
        {
            if (currentState == State.Dead) return;

            currentState = State.Dead;
            isActive = false;
            animator.enabled = false;

            if (ragdollController != null)
                ragdollController.EnableRagdoll();

            WorldSession.CurrentRunStats.totalEnemiesDefeated++;

            if (enemyType == EnemyType.Elite)
                WorldSession.CurrentRunStats.eliteEnemiesDefeated++;

            if (enemyType == EnemyType.Blight)
                WorldSession.CurrentRunStats.blightEnemiesDefeated++;

            // Play a sound or spawn a death effect here

            Invoke(nameof(Despawn), 5f);
        }

        private void Despawn()
        {
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                if (!TryGetComponent<TrialEnemyMarker>(out _))
                    spawner.DecreaseEnemyCount();
            }

            if (breakableObject != null)
                breakableObject.DestroyObject();

            EnemyPool.Instance.ReturnEnemy(this);
            gameObject.SetActive(false);
        }

        public void SetChasing()
        {
            currentState = State.Chasing;
        }

        public void AssignBestTarget(List<Targetable> potentialTargets)
        {
            // Debug.Log($"{name} AssignBestTarget() called. CurrentState={currentState}, CurrentTarget={(currentTarget != null ? currentTarget.name : null)}");

            if (currentState == State.Attacking || currentState == State.RangedAttacking) return;

            if (Time.time < nextTargetSwitchTime) return;

            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Targetable t in potentialTargets)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                if (blockedTargets.Contains(t.transform)) continue;

                TargetScore ts = CreateTargetScore(t.transform);
                float score = personality.CalculateScore(ts);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = t.transform;
                }
            }

            /*if (bestTarget != null)
                Debug.Log($"{name} picked BEST target: {bestTarget.name} with score {bestScore}");
            else
                Debug.Log($"{name} found no valid targets. Falling back to campfire: {(campfireTarget != null ? campfireTarget.name : null)}");*/

            if (bestTarget != null)
            {
                if (bestTarget != currentTarget)
                {
                    frozenPathTarget = new(-999, 0, -999);
                    currentTarget = bestTarget;
                    SetChasing();
                    agent.StartTracking(currentTarget);
                    TryRequestPath(GetTargetGridPosition(currentTarget.position));
                    nextTargetSwitchTime = Time.time + targetStickTime;
                }
            }
            else
            {
                if (currentTarget != campfireTarget)
                {
                    frozenPathTarget = new(-999, 0, -999);
                    currentTarget = campfireTarget;
                    SetChasing();
                    agent.StartTracking(currentTarget);
                    nextTargetSwitchTime = Time.time + targetStickTime;
                    TryRequestPath(GetTargetGridPosition(currentTarget.position));
                }
            }

            // Debug.Log($"{name} chasing target: {(currentTarget != null ? currentTarget.name : null)} at {(currentTarget != null ? currentTarget.position : null)}");
        }

        private int GetTargetPriority(Targetable.TargetType type)
        {
            if (preferredTargets != null && preferredTargets.Count > 0)
            {
                int index = preferredTargets.IndexOf(type);
                if (index >= 0) return index;
            }

            return int.MinValue;
        }

        private TargetScore CreateTargetScore(Transform target)
        {
            if (target == null) return null;

            float distance = Vector3.Distance(myTransform.position, target.position);

            Targetable targetable = target.GetComponent<Targetable>();
            int priority = targetable != null ? GetTargetPriority(targetable.targetType) : int.MaxValue;

            float retaliationBias = 0f;
            if (target == recentAttacker && Time.time - lastAttackedTime <= retaliateMemoryDuration)
            {
                float freshness = 1f - ((Time.time - lastAttackedTime) / retaliateMemoryDuration);
                retaliationBias = personality.lastDamageWeight * freshness;
            }

            float objectiveScore = 0f;
            if (targetable != null)
                objectiveScore = GetObjectiveThreat(targetable, personality);

            return new TargetScore(target, priority, distance, retaliationBias, objectiveScore);
        }

        private float GetObjectiveThreat(Targetable targetable, EnemyPersonality personality)
        {
            float objWeight = personality.objectiveThreatWeight;

            return targetable.targetType switch
            {
                Targetable.TargetType.Campfire => objWeight * 4,
                Targetable.TargetType.Player => objWeight * 5,
                Targetable.TargetType.Defense => objWeight * 6,
                Targetable.TargetType.Wall => objWeight * 3,
                Targetable.TargetType.Structure => objWeight * 2,
                _ => objWeight,
            };
        }

        public State GetCurrentState()
        {
            return currentState;
        }

        public void ResetAnimatorPose()
        {
            animator.Rebind();
            animator.Update(0f);
        }

        private bool ShouldResetPath(float distanceToTarget)
        {
            return distanceToTarget > Mathf.Max(meleeAttackRange, rangedAttackRange)
                    && currentTarget != null
                    && !agent.HasPath
                    && Time.time - lastPathRequestTime > 1f;
        }
    }
}
