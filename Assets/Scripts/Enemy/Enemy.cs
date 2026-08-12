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
    [RequireComponent(typeof(CharacterController), typeof(SimpleRagdollController), typeof(BreakableObject))]
    [RequireComponent(typeof(VoxelAgent), typeof(Animator), typeof(PrefabID))]
    [RequireComponent(typeof(EnemyTargeting), typeof(EnemyCombat))]
    public class Enemy : MonoBehaviour, IHitStopListener
    {
        private static readonly int StaggerHash = Animator.StringToHash("Stagger");

        // -------------------- ENUMS --------------------
        public enum State { Idle, Chasing, Attacking, RangedAttacking, Staggered, Dead }
        public enum EnemyType { Regular, Elite, Blight, Boss }

        // -------------------- STATE --------------------
        [Header("State")]
        private State currentState = State.Idle;
        
        public EnemyType enemyType = EnemyType.Regular;

        private bool isActive;

        // -------------------- MOVEMENT --------------------
        [Header("Movement")]
        [SerializeField] private float gravity = -9.8f;
        [SerializeField] private float moveSpeed = 3.5f;

        private float cachedMoveSpeed;
        private float verticalVelocity = 0f;
        
        [SerializeField] private float knockbackDamping = 8f;
        [SerializeField] private float knockbackStopThreshold = 0.05f;

        private Vector3 knockbackVelocity = Vector3.zero;
        private bool IsBeingKnockedBack => knockbackVelocity.sqrMagnitude > knockbackStopThreshold * knockbackStopThreshold;
        private bool wasBeingKnockedBack;

        private Vector3 latestSpawnPos;
        private bool justSpawned;

        // -------------------- PATHFINDING --------------------
        [Header("Pathfinding")]
        private Vector3Int frozenPathTarget = new(-999, 0, -999);
        private float lastPathRequestTime = -999f;
        [SerializeField] private float pathRequestCooldown = 0.2f;
        [SerializeField] private float repathCooldown = 0.5f;
        [SerializeField] private float repathDistanceThreshold = 1f;
        [SerializeField] private float minTargetMovementThreshold = 1f;
        private Vector3 lastTargetPosition;
        private float lastRepathTime = -999f;

        // -------------------- REFERENCES --------------------
        private VoxelAgent agent;
        [HideInInspector] public Animator animator;
        [HideInInspector] public BreakableObject breakableObject;
        private SimpleRagdollController ragdollController;
        private CharacterController characterController;
        private Transform myTransform;

        // -------------------- STATUS EFFECTS --------------------
        private readonly Dictionary<BurnEffect, Coroutine> activeBurns = new();
        private readonly Dictionary<SlowEffect, Coroutine> activeSlows = new();
        public readonly Dictionary<StunEffect, Coroutine> activeStuns = new();
        public readonly Dictionary<PoisonEffect, Coroutine> activePoisons = new();

        // -------------------- PARTICLE EFFECTS --------------------
        [Header("Effects")]
        [SerializeField] private ParticleEffectScaler particleEffectScaler;
        [SerializeField] private ParticleSystem deathEffect;

        public EnemyTargeting EnemyTargeting { get; private set; }
        public EnemyCombat EnemyCombat { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            agent = GetComponent<VoxelAgent>();
            ragdollController = GetComponent<SimpleRagdollController>();
            breakableObject = GetComponent<BreakableObject>();

            EnemyTargeting = GetComponent<EnemyTargeting>();
            EnemyCombat = GetComponent<EnemyCombat>();

            myTransform = transform;
            EnemyTargeting.SetTransform(myTransform);
            EnemyCombat.SetTransform(myTransform);
        }

        private void Start()
        {
            if (GetComponent<TrialEnemyMarker>() != null)
            {
                Transform playerTarget = FindAnyObjectByType<Player>().transform;
                EnemyTargeting.SetCurrentTarget(playerTarget);
            }
            else
            {
                Transform campfireTarget = GameObject.FindGameObjectWithTag("Campfire").transform;
                EnemyTargeting.SetCampfireTarget(campfireTarget);
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
            if (IsCurrentState(State.Dead) || IsCurrentState(State.Staggered)) return;

            EnemyTargeting.ValidateTarget(EnemyManager.Instance.GetActiveTargets());
            float distanceToTarget = Vector3.Distance(myTransform.position, EnemyTargeting.CurrentTarget.position);

            switch (currentState)
            {
                case State.Chasing:
                    HandleChasing(distanceToTarget);
                    break;
                case State.Attacking:
                    EnemyCombat.HandleAttacking(distanceToTarget);
                    break;
                case State.RangedAttacking:
                    EnemyCombat.HandleRangedAttacking(distanceToTarget);
                    break;
            }

            if (!IsCurrentState(State.Idle)) return;

            if (EnemyTargeting.CurrentTarget == null)
                EnemyTargeting.AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

            SetCurrentState(State.Chasing);
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (!isActive || IsCurrentState(State.Dead)) return;
            if (!characterController.enabled) return;

            float deltaTime = Time.fixedDeltaTime;

            if (currentState == State.Staggered)
            {
                ApplyVerticalMovement(deltaTime);
                return;
            }

            Vector3 navigationVelocity = Vector3.zero;

            bool allowNavigation = !IsBeingKnockedBack;
            agent.UpdateAgent(allowNavigation);

            if (!IsBeingKnockedBack && agent.WantsToMove() && (IsCurrentState(State.Idle) || IsCurrentState(State.Chasing)))
            {
                Vector3 agentDirection = agent.GetMovementThisFrame();
                navigationVelocity = new Vector3(
                    agentDirection.x,
                    0f,
                    agentDirection.z
                ) * moveSpeed;
            }

            UpdateVerticalVelocity(deltaTime);

            Vector3 totalVelocity = navigationVelocity + Vector3.up * verticalVelocity + knockbackVelocity;

            characterController.Move(totalVelocity * deltaTime);

            knockbackVelocity = Vector3.MoveTowards(
                knockbackVelocity,
                Vector3.zero,
                knockbackDamping * deltaTime
            );

            if (knockbackVelocity.sqrMagnitude <= knockbackStopThreshold * knockbackStopThreshold)
                knockbackVelocity = Vector3.zero;

            bool beingKnockedBack = IsBeingKnockedBack;

            if (wasBeingKnockedBack && !beingKnockedBack)
                ResumeNavigationAfterKnockback();

            wasBeingKnockedBack = beingKnockedBack;
        }

        private void ResumeNavigationAfterKnockback()
        {
            if (EnemyTargeting.CurrentTarget == null) return;

            ResetFrozenPath();
            TryRequestPath(GetTargetGridPosition(EnemyTargeting.CurrentTarget.position));
        }

        private void UpdateVerticalVelocity(float deltaTime)
        {
            if (!characterController.isGrounded)
                verticalVelocity += gravity * deltaTime;
            else if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }

        private void ApplyVerticalMovement(float deltaTime)
        {
            UpdateVerticalVelocity(deltaTime);
            characterController.Move(verticalVelocity * deltaTime * Vector3.up);
        }

        public bool IsCurrentState(State state) => currentState == state;

        public void ApplyKnockback(Vector3 direction, float speed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;

            knockbackVelocity = direction.normalized * speed;
        }

        #region Stagger

        // Called by animation event
        private void ExitStagger()
        {
            currentState = State.Idle;
            EnemyTargeting.ReassignTarget();
            SetCurrentState(State.Chasing);
        }

        public void ApplyStagger(int poiseDamage, Vector3 knockbackDirection, float knockbackForce)
        {
            if (currentState == State.Dead) return;

            bool shouldStagger = EnemyCombat.AddPoise(poiseDamage);
            if (!shouldStagger) return;

            currentState = State.Staggered;

            ApplyKnockback(knockbackDirection, knockbackForce);

            // Stop movement immediately
            agent.StopTracking();
            agent.CancelPath();
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            animator.SetTrigger(StaggerHash);

            EnemyTargeting.pendingTargetReassign = true;
        }

        #endregion

        #region Chasing

        private void HandleChasing(float distanceToTarget)
        {
            if (activeStuns.Count > 0) return;

            if (Time.time - lastRepathTime > repathCooldown &&
                Vector3.Distance(EnemyTargeting.CurrentTarget.position, lastTargetPosition) > repathDistanceThreshold)
            {
                lastTargetPosition = EnemyTargeting.CurrentTarget.position;
                lastRepathTime = Time.time;

                if (EnemyCombat.ShouldScanForTargets(distanceToTarget))
                    EnemyTargeting.AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

                Vector3Int to = GetTargetGridPosition(lastTargetPosition);
                TryRequestPath(to);
            }

            if (ShouldResetPath(distanceToTarget))
                ResetPath();

            EnemyCombat.HandleAttackTrigger(distanceToTarget);
        }

        public void SetCurrentState(State state) => currentState = state;

        #endregion

        #region Pathfinding

        private void TryRequestPath(Vector3Int targetGrid)
        {
            Vector3Int from = GetTargetGridPosition(myTransform.position);

            if (agent.CanWalkDirectly(from, targetGrid))
            {
                ResetFrozenPath();
                agent.StartTracking(EnemyTargeting.CurrentTarget);
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
            return EnemyCombat.OutOfAttackRange(distanceToTarget)
                    && EnemyTargeting.CurrentTarget != null
                    && !agent.HasPath
                    && Time.time - lastPathRequestTime > 1f;
        }

        public void ResetPath()
        {
            ResetFrozenPath();
            TryRequestPath(GetTargetGridPosition(EnemyTargeting.CurrentTarget.position));
        }

        #endregion

        #region Init

        public void Init(Vector3 spawnPos)
        {
            if (isActive) return;
            if (agent == null || characterController == null) return;

            latestSpawnPos = spawnPos;

            agent.CancelPath();
            agent.Init(spawnPos);

            EnemyCombat.Init();

            bool wasEnabled = characterController.enabled;
            if (wasEnabled)
                characterController.enabled = false;

            myTransform.position = spawnPos;

            if (wasEnabled)
                characterController.enabled = true;

            ResetAnimatorPose();
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            isActive = true;
            justSpawned = true;

            List<Targetable> activeTargets = EnemyManager.Instance != null ? EnemyManager.Instance.GetActiveTargets() : null;
            if (activeTargets != null && activeTargets.Count > 0)
                EnemyTargeting.AssignBestTarget(activeTargets);
            else
                EnemyTargeting.FallbackToCampfire();

            if (EnemyTargeting.CurrentTarget == null) return;

            SetCurrentState(State.Chasing);
            Vector3Int targetPos = GetTargetGridPosition(EnemyTargeting.CurrentTarget.position);
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

        public void ResetAttack()
        {
            ResetMovement();
            SetCurrentState(State.Chasing);
            TryRequestPath(GetTargetGridPosition(EnemyTargeting.CurrentTarget.position));
        }

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

            GameObject effectPrefab = PrefabRegistry.Instance.GetByKey(prefabID.prefabKey);
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

            PrefabID prefabID = GetComponent<PrefabID>();
            GameObject prefab = PrefabRegistry.Instance.GetByKey(prefabID.prefabKey);

            EnemyPool.Instance.Return(this, prefab);
            gameObject.SetActive(false);
        }

        #endregion

        public State GetCurrentState() => currentState;

        public void ResetAnimatorPose()
        {
            animator.Rebind();
            animator.Update(0f);
        }

        public void OnHitStopStart() => isActive = false;

        public void OnHitStopEnd() => isActive = true;
    }
}
