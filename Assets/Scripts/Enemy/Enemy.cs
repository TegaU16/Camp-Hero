using System;
using System.Collections;
using System.Collections.Generic;
using Game.Players;
using Game.Registries;
using Game.StatusEffects;
using Game.Terrain.Structures.Trials;
using UnityEngine;
using Worlds;
using static BreakableObject;

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
        public enum State { Idle, Chasing, Attacking, RangedAttacking, Staggered, Dead }
        public enum AttackType { Melee, Ranged, Mixed }
        public enum EnemyType { Regular, Elite, Blight, Boss }

        // -------------------- STATE --------------------
        [Header("State")]
        private State currentState = State.Idle;
        [SerializeField] private AttackType attackType = AttackType.Melee;
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
        [SerializeField] private float rangedAttackRange = 10f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int damage = 10;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private int poise;

        private int poiseStack;
        private bool pendingTargetReassign;

        [Header("Combat References")]
        public Sprite enemyIcon;
        [HideInInspector] public BreakableObject breakableObject;
        [SerializeField] private List<MonoBehaviour> rangedAttackScripts = new();

        private IRangedAttackBehavior currentRangedAttack;
        private readonly List<IRangedAttackBehavior> rangedAttacks = new();

        // -------------------- MOVEMENT --------------------
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        private float cachedMoveSpeed;
        private float verticalVelocity = 0f;
        [SerializeField] private float gravity = -9.8f;
        private Vector3 knockbackVelocity = Vector3.zero;
        [SerializeField] private float knockbackDuration = 5f;

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
        [SerializeField] private float attackRangeBuffer = 1.5f;
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
        [SerializeField] private EnemyPersonality personality;

        // -------------------- REFERENCES --------------------
        private VoxelAgent agent;
        [HideInInspector] public Animator animator;
        private SimpleRagdollController ragdollController;
        private CharacterController characterController;
        private Transform myTransform;

        // -------------------- STATUS EFFECTS --------------------
        private readonly Dictionary<BurnEffect, Coroutine> activeBurns = new();
        private readonly Dictionary<SlowEffect, Coroutine> activeSlows = new();
        private readonly Dictionary<StunEffect, Coroutine> activeStuns = new();
        private readonly Dictionary<PoisonEffect, Coroutine> activePoisons = new();

        // -------------------- PARTICLE EFFECTS --------------------
        [Header("Effects")]
        [SerializeField] private ParticleEffectScaler particleEffectScaler;
        [SerializeField] private ParticleSystem deathEffect;

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
            }

            currentState = State.Chasing;
            cachedMoveSpeed = moveSpeed;

            Init(myTransform.position);
        }

        private void OnEnable() => EnemyManager.Instance.RegisterEnemy(this);

        private void OnDisable() => EnemyManager.Instance.UnregisterEnemy(this);

        private void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (currentState == State.Dead || currentState == State.Staggered) return;

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

            if (currentState != State.Idle) return;

            if (currentTarget == null)
                AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

            SetChasing();
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (!isActive || currentState == State.Dead) return;
            if (!characterController.enabled) return;

            if (currentState == State.Staggered)
            {
                if (!characterController.isGrounded)
                    verticalVelocity += gravity * Time.fixedDeltaTime;

                Vector3 enemyMove = Time.fixedDeltaTime * verticalVelocity * Vector3.up;
                characterController.Move(enemyMove);
                return;
            }

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
            if (!characterController.isGrounded)
                verticalVelocity += gravity * Time.fixedDeltaTime;
            else if (verticalVelocity < 0f)
                verticalVelocity = 0f;

            move.y = verticalVelocity;

            // Apply knockback
            move += knockbackVelocity;
            knockbackVelocity = -Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.fixedDeltaTime * knockbackDuration);

            // Finally move
            Vector3 displacement = moveSpeed * Time.fixedDeltaTime * move;
            characterController.Move(displacement);
        }

        #region Stagger

        // Called by animation event
        private void ExitStagger()
        {
            currentState = State.Idle;

            if (!pendingTargetReassign)
            {
                SetChasing();
                return;
            }

            currentTarget = null;
            AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

            pendingTargetReassign = false;

            SetChasing();
        }

        public void ApplyStagger(int poiseDamage)
        {
            if (currentState == State.Dead) return;

            poiseStack += poiseDamage;
            if (poiseStack < poise) return;

            poiseStack -= poise;
            currentState = State.Staggered;

            // Stop movement immediately
            agent.StopTracking();
            agent.CancelPath();
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            animator.SetTrigger("Stagger");

            pendingTargetReassign = true;
        }

        #endregion

        #region Chasing

        private void HandleChasing(float distanceToTarget)
        {
            if (activeStuns.Count > 0) return;

            if (Time.time - lastRepathTime > repathCooldown &&
                Vector3.Distance(currentTarget.position, lastTargetPosition) > repathDistanceThreshold)
            {
                lastTargetPosition = currentTarget.position;
                lastRepathTime = Time.time;

                float rangedAttackThreshold = rangedAttackRange * attackRangeBuffer;
                float meleeAttackThreshold = meleeAttackRange * attackRangeBuffer;

                bool shouldScanForTargets = attackType switch
                {
                    AttackType.Mixed => distanceToTarget > Mathf.Min(rangedAttackThreshold, meleeAttackThreshold),
                    AttackType.Melee => distanceToTarget > meleeAttackThreshold,
                    AttackType.Ranged => distanceToTarget > rangedAttackThreshold,
                    _ => false,
                };

                if (shouldScanForTargets)
                    AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

                Vector3Int to = GetTargetGridPosition(lastTargetPosition);
                TryRequestPath(to);
            }

            if (ShouldResetPath(distanceToTarget))
            {
                ResetFrozenPath();
                TryRequestPath(GetTargetGridPosition(currentTarget.position));
            }

            HandleAttackTrigger(distanceToTarget);
        }

        public void SetChasing() => currentState = State.Chasing;

        #endregion

        #region Combat

        private void HandleAttacking(float distanceToTarget)
        {
            if (activeStuns.Count > 0) return;

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

            if (distanceToTarget <= meleeAttackRange + 0.5f) return;

            ResetMovement();
            isAttacking = false;
            SetChasing();
            TryRequestPath(GetTargetGridPosition(currentTarget.position));
        }

        private void HandleRangedAttacking(float distanceToTarget)
        {
            if (activeStuns.Count > 0) return;

            myTransform.LookAt(new Vector3(currentTarget.position.x, myTransform.position.y, currentTarget.position.z));

            if (!isAttacking)
                attackTimer += Time.deltaTime;

            if (attackTimer >= attackCooldown)
            {
                if (rangedAttacks.Count == 0) return;

                int index = UnityEngine.Random.Range(0, rangedAttacks.Count);
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

            if (distanceToTarget <= rangedAttackRange + 1f) return;

            ResetMovement();
            isAttacking = false;
            SetChasing();
            TryRequestPath(GetTargetGridPosition(currentTarget.position));
        }

        private void HandleAttackTrigger(float distanceToTarget)
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
            animator.SetFloat("Speed", 0f);
            currentState = State.Attacking;

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        private void BeginRangedAttack()
        {
            agent.CancelPath();
            animator.SetFloat("Speed", 0f);
            currentState = State.RangedAttacking;

            if (!isAttacking)
                attackTimer = attackCooldown;
        }

        // Called by animation event
        private void RangedAttack()
        {
            if (currentRangedAttack == null || currentTarget == null) return;

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage;
            int finalDamage = (int)damageWithMultiplier;

            currentRangedAttack.ExecuteAttack(myTransform, currentTarget, finalDamage);
            currentRangedAttack = null;
        }

        // Called by animation event
        private void EndAttack()
        {
            isAttacking = false;
            ResetMovement();

            if (currentState != State.Dead)
                SetChasing();
        }

        public void ApplyKnockback(Vector3 dir, float strength) => knockbackVelocity = dir.normalized * strength;

        public void OnAttacked(Transform attacker)
        {
            recentAttacker = attacker;
            lastAttackedTime = Time.time;

            nextTargetSwitchTime = 0f;
            AssignBestTarget(EnemyManager.Instance.GetActiveTargets());
        }

        public void DealDamage()
        {
            if (currentTarget == null) return;

            float distance = Vector3.Distance(myTransform.position, currentTarget.position);
            float meleeAttackRangeBuffer = 0.5f;
            if (distance > meleeAttackRange + meleeAttackRangeBuffer) return;

            float poisonMult = GetPoisonDamageMult();

            float damageWithMultiplier = DifficultyManager.Instance.GetDamageMultiplier() * damage * poisonMult;
            int finalDamage = (int)damageWithMultiplier;

            if (currentTarget.TryGetComponent(out Health targetHealth))
            {
                targetHealth.TakeDamage(finalDamage);
                Vector3 knockbackDir = (currentTarget.position - myTransform.position).normalized;

                if (currentTarget.TryGetComponent(out CharacterController targetCC))
                {
                    knockbackDir.y = 0.5f;

                    if (targetCC.enabled)
                        targetCC.Move(knockbackForce * Time.deltaTime * knockbackDir);
                }
                else if (currentTarget.TryGetComponent(out Rigidbody targetRb))
                {
                    targetRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }

                return;
            }

            BreakableObject targetBreakable = currentTarget.GetComponentInParent<BreakableObject>();
            if (targetBreakable == null) return;

            Vector3 targetHitPoint = targetBreakable.GetComponent<Collider>().ClosestPoint(transform.position);
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
            if (activePoisons.Count == 0) return 1f;

            float finalMult = 1f;
            foreach (PoisonEffect effect in activePoisons.Keys)
                finalMult *= effect.attackDamageMult;

            return finalMult;
        }

        #endregion

        #region Pathfinding

        private void TryRequestPath(Vector3Int targetGrid)
        {
            Vector3Int from = GetTargetGridPosition(myTransform.position);

            if (agent.CanWalkDirectly(from, targetGrid))
            {
                ResetFrozenPath();
                agent.StartTracking(currentTarget);
                return;
            }

            float timeSinceLast = Time.time - lastPathRequestTime;
            float distToFrozen = Vector3Int.Distance(targetGrid, frozenPathTarget);

            if (frozenPathTarget == new Vector3Int(-999, 0, -999) ||
                distToFrozen >= minTargetMovementThreshold ||
                timeSinceLast >= pathRequestCooldown)
            {
                frozenPathTarget = targetGrid;
                agent.StopTracking();
                agent.RequestPath(frozenPathTarget);
                lastPathRequestTime = Time.time;
            }
        }

        private Vector3Int GetTargetGridPosition(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x);
            int z = Mathf.FloorToInt(worldPos.z);
            float y = Utility.GetHeightAt(x, z);

            return new Vector3Int(x, Mathf.RoundToInt(y), z);
        }

        private void ResetFrozenPath() => frozenPathTarget = new(-999, 0, -999);

        private bool ShouldResetPath(float distanceToTarget)
        {
            return distanceToTarget > Mathf.Max(meleeAttackRange, rangedAttackRange)
                    && currentTarget != null
                    && !agent.HasPath
                    && Time.time - lastPathRequestTime > 1f;
        }

        #endregion

        #region Targeting

        private void ValidateTarget(List<Targetable> potentialTargets)
        {
            if (Time.time >= nextTargetSwitchTime)
            {
                nextTargetSwitchTime = 0f;
                AssignBestTarget(potentialTargets);
                return;
            }

            if (currentTarget != null && currentTarget.gameObject.activeInHierarchy) return;

            nextTargetSwitchTime = 0f;
            AssignBestTarget(potentialTargets);

            ResetFrozenPath();
            SetChasing();
            TryRequestPath(GetTargetGridPosition(currentTarget.position));
        }

        public void AssignBestTarget(List<Targetable> potentialTargets)
        {
            if (currentState == State.Attacking || currentState == State.RangedAttacking) return;
            if (Time.time < nextTargetSwitchTime) return;

            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Targetable target in potentialTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                if (blockedTargets.Contains(target.transform)) continue;

                TargetScore ts = CreateTargetScore(target.transform);
                float score = personality.CalculateScore(ts);

                if (score <= bestScore) continue;

                bestScore = score;
                bestTarget = target.transform;
            }

            if (bestTarget != null)
                currentTarget = bestTarget;
            else if (campfireTarget != null)
                currentTarget = campfireTarget;

            ResetFrozenPath();
            SetChasing();
            agent.StartTracking(currentTarget);
            nextTargetSwitchTime = Time.time + targetStickTime;
            TryRequestPath(GetTargetGridPosition(currentTarget.position));
        }

        private int GetTargetPriority(Targetable.TargetType type)
        {
            if (preferredTargets == null || preferredTargets.Count <= 0) return int.MinValue;

            int index = preferredTargets.IndexOf(type);
            if (index >= 0) return index;

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

        #endregion

        #region Init

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
            poiseStack = 0;

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

        #endregion

        private void OnAnimatorMove()
        {
            if (!justSpawned) return;

            characterController.enabled = false;
            myTransform.position = latestSpawnPos;
            characterController.enabled = true;
            justSpawned = false;
        }

        public void ResetMovement() => moveSpeed = cachedMoveSpeed;

        #region Status Effects

        public void ModifySpeed(float factor)
        {
            if (factor == 1f)
                ResetMovement();
            else
                moveSpeed *= factor;

            if (animator != null && animator.enabled)
                animator.speed = factor;
        }

        private void ApplyStatusEffect<T>(
            T statusEffect,
            Dictionary<T, Coroutine> effectsDict,
            Func<T, IEnumerator> enumerator)
            where T : StatusEffect
        {
            if (statusEffect == null) return;
            if (effectsDict.ContainsKey(statusEffect)) return;

            Coroutine routine = StartCoroutine(enumerator(statusEffect));
            effectsDict.Add(statusEffect, routine);
        }

        private void StopStatusEffect<T>(
            T statusEffect,
            Dictionary<T, Coroutine> effectsDict)
            where T : StatusEffect
        {
            if (statusEffect == null) return;
            if (!effectsDict.TryGetValue(statusEffect, out Coroutine routine)) return;

            StopCoroutine(routine);
            EffectsPool.Instance.ReturnParticleSystem(statusEffect.particleSystem);
            effectsDict.Remove(statusEffect);
        }

        private IEnumerator ApplyDamageOverTime<T>(
            T statusEffect,
            Dictionary<T, Coroutine> effectsDict,
            float damagePerTick,
            float tickSpeed)
            where T : StatusEffect
        {
            float elapsed = 0f;
            if (breakableObject.GetHealth() <= 0) yield break;

            PlayEffect(statusEffect.particleSystem);

            while (elapsed < statusEffect.duration)
            {
                DamageInfo effectDamageInfo = new
                (
                    damage: Mathf.RoundToInt(damagePerTick),
                    hitPoint: transform.position,
                    hitNormal: Vector3.zero
                );

                breakableObject.TakeDamage(effectDamageInfo);

                yield return new WaitForSeconds(tickSpeed);

                if (!statusEffect.noTimer)
                    elapsed += tickSpeed;
            }

            StopStatusEffect(statusEffect, effectsDict);
        }

        private IEnumerator ModifySpeedOverTime<T>(
            T statusEffect,
            Dictionary<T, Coroutine> effectsDict,
            float speedFactor)
            where T : StatusEffect
        {
            ModifySpeed(speedFactor);
            PlayEffect(statusEffect.particleSystem);

            if (statusEffect.noTimer) yield break;

            yield return new WaitForSeconds(statusEffect.duration);

            StopStatusEffect(statusEffect, effectsDict);
        }

        public void ApplySlow(SlowEffect slowEffect) => ApplyStatusEffect(slowEffect, activeSlows, SlowRoutine);

        public void StopSlow(SlowEffect slowEffect)
        {
            StopStatusEffect(slowEffect, activeSlows);
            ModifySpeed(1f);
        }

        private IEnumerator SlowRoutine(SlowEffect slowEffect)
        {
            yield return ModifySpeedOverTime(slowEffect, activeSlows, slowEffect.slowFactor);
        }

        public void ApplyBurn(BurnEffect burnEffect) => ApplyStatusEffect(burnEffect, activeBurns, BurnRoutine);

        public void StopBurn(BurnEffect burnEffect) => StopStatusEffect(burnEffect, activeBurns);

        private IEnumerator BurnRoutine(BurnEffect burnEffect)
        {
            yield return ApplyDamageOverTime(burnEffect, activeBurns, burnEffect.damagePerTick, burnEffect.tickSpeed);
        }

        public void ApplyStun(StunEffect stunEffect) => ApplyStatusEffect(stunEffect, activeStuns, StunRoutine);

        public void StopStun(StunEffect stunEffect) => StopStatusEffect(stunEffect, activeStuns);

        private IEnumerator StunRoutine(StunEffect stunEffect)
        {
            yield return ModifySpeedOverTime(stunEffect, activeStuns, speedFactor: 0f);
        }

        public void ApplyPoison(PoisonEffect poisonEffect) => ApplyStatusEffect(poisonEffect, activePoisons, PoisonRoutine);

        public void StopPoison(PoisonEffect poisonEffect) => StopStatusEffect(poisonEffect, activePoisons);

        private IEnumerator PoisonRoutine(PoisonEffect poisonEffect)
        {
            yield return ApplyDamageOverTime(poisonEffect, activePoisons, poisonEffect.damagePerTick, poisonEffect.tickSpeed);
        }

        private void PlayEffect(ParticleSystem effect, bool persistent = false)
        {
            if (effect == null) return;
            if (particleEffectScaler == null) return;
            if (!effect.TryGetComponent(out PrefabID prefabID)) return;

            GameObject effectPrefab = PrefabRegistry.GetPrefabByKey(prefabID.prefabKey);
            ParticleSystem particleSystem = EffectsPool.Instance.GetParticleSystem(effectPrefab);
            if (particleSystem == null) return;

            if (persistent)
            {
                particleSystem.transform.SetParent(null);
                StartCoroutine(ReturnEffectAfterDelay(particleSystem, particleSystem.totalTime));
            }

            particleEffectScaler.ApplyTo(particleSystem);
            particleSystem.Play();
        }

        private IEnumerator ReturnEffectAfterDelay(ParticleSystem effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            EffectsPool.Instance.ReturnParticleSystem(effect);
        }

        #endregion

        #region Death

        public void Die()
        {
            if (currentState == State.Dead) return;

            StopAllCoroutines();

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

            Invoke(nameof(Despawn), 5f);
        }

        private void Despawn()
        {
            EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null && !TryGetComponent<TrialEnemyMarker>(out _))
                spawner.DecreaseEnemyCount();

            PlayEffect(deathEffect, persistent: true);

            if (breakableObject != null)
                breakableObject.DestroyObject();

            EnemyPool.Instance.ReturnEnemy(this);
            gameObject.SetActive(false);
        }

        #endregion

        public State GetCurrentState() => currentState;

        public void ResetAnimatorPose()
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
