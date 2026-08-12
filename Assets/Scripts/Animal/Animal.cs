using System.Collections;
using Game.Terrain;
using UnityEngine;
using Worlds;

namespace Game.AI.Animals
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SimpleRagdollController))]
    [RequireComponent(typeof(BreakableObject))]
    [RequireComponent(typeof(VoxelAgent))]
    [RequireComponent(typeof(Animator))]
    public class Animal : MonoBehaviour, ISimulatable
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly WaitForSeconds _waitForSeconds1 = new(1f);

        [Header("Components")]
        private Transform myTransform;  // Cached transform
        [HideInInspector] public Animator animator;
        private BreakableObject breakableObject;
        private SimpleRagdollController ragdollController;
        [HideInInspector] public VoxelAgent agent;
        private CharacterController characterController;

        [Header("Wander Settings")]
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float waitTimeMin = 2f;
        [SerializeField] private float waitTimeMax = 5f;

        [Header("Movement Settings")]
        [SerializeField] private float gravity = -9.8f;
        [SerializeField] private float moveSpeed = 2f;
        private float verticalVelocity = 0f;

        private bool queuedForWander = false;
        private Coroutine wanderCoroutine;

        [HideInInspector] public string worldName;

        // Movement state
        private Vector3Int currentGridPos;
        private Vector3 latestSpawnPos;
        private bool justSpawned = false;

        private Vector3 knockbackVelocity = Vector3.zero;

        [Header("Flee Settings")]
        [SerializeField] private float fleeDistance = 12f;
        [SerializeField] private float fleeSpeedMultiplier = 2f;

        private bool isFleeing = false;
        private Vector3 fleeDirection;

        public bool IsActiveAI { get; set; } = false;

        void Awake()
        {
            myTransform = transform;
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            agent = GetComponent<VoxelAgent>();
            breakableObject = GetComponent<BreakableObject>();
            ragdollController = GetComponent<SimpleRagdollController>();
        }

        void Start()
        {
            currentGridPos = WorldToGrid(myTransform.position);

            if (!IsActiveAI)
                Init(myTransform.position);
        }

        void FixedUpdate()
        {
            if (!GameManager.Instance.IsGameActive) return;

            if (!IsActiveAI)
            {
                Debug.LogWarning($"{name} is not active AI");
                return;
            }

            currentGridPos = WorldToGrid(myTransform.position);
            agent.UpdateAgent();

            if (!agent.WantsToMove())
            {
                verticalVelocity = 0f;
                knockbackVelocity = Vector3.zero;

                animator.SetFloat(SpeedHash, 0f, 0.2f, Time.fixedDeltaTime);
                return;
            }

            Vector3 move = agent.GetMovementThisFrame();

            if (!characterController.isGrounded)
                verticalVelocity += gravity * Time.fixedDeltaTime;
            else if (verticalVelocity < 0)
                verticalVelocity = -1f;

            move.y = verticalVelocity;

            move += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

            float finalSpeed = isFleeing ? moveSpeed * fleeSpeedMultiplier : moveSpeed;
            characterController.Move(finalSpeed * Time.fixedDeltaTime * move);

            float speed = agent.GetCurrentSpeedFraction();
            animator.SetFloat(SpeedHash, speed, 0.2f, Time.fixedDeltaTime);
        }

        void OnAnimatorMove()
        {
            if (!justSpawned) return;

            characterController.enabled = false;
            myTransform.position = latestSpawnPos;
            characterController.enabled = true;

            justSpawned = false;
        }

        public void Init(Vector3 spawnPosition)
        {
            latestSpawnPos = spawnPosition;

            agent.CancelPath();
            agent.Init(spawnPosition);
            characterController.enabled = false;
            myTransform.position = spawnPosition;
            characterController.enabled = true;

            ResetAnimatorPose();
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            justSpawned = true;
            IsActiveAI = true;

            if (wanderCoroutine != null)
                StopCoroutine(wanderCoroutine);

            wanderCoroutine = StartCoroutine(WanderRoutine());
        }

        public void Die()
        {
            IsActiveAI = false;
            animator.enabled = false;
            if (ragdollController != null) 
                ragdollController.EnableRagdoll();

            if (wanderCoroutine != null)
            {
                StopCoroutine(wanderCoroutine);
                wanderCoroutine = null;
            }

            WorldSession.CurrentRunStats.animalsKilled++;

            Invoke(nameof(Despawn), 5f);
        }

        private void Despawn()
        {
            if (breakableObject != null)
                breakableObject.DestroyObject();

            wanderCoroutine = null;
            AnimalPool.Instance.Return(this);
        }

        private IEnumerator WanderRoutine()
        {
            yield return _waitForSeconds1;

            while (true)
            {
                if (!queuedForWander)
                {
                    queuedForWander = true;
                    WanderManager.Instance.Enqueue(this);
                }

                while (agent.HasPath || agent.WantsToMove())
                    yield return null;

                yield return new WaitForSeconds(Random.Range(waitTimeMin, waitTimeMax));
            }
        }

        public void StartWanderStep()
        {
            queuedForWander = false;
            Vector3Int targetGrid = PickRandomNearbyGrid(currentGridPos, (int)wanderRadius);

            if (agent.CanWalkDirectly(currentGridPos, targetGrid))
                agent.SetDirectTarget(targetGrid);
            else
                agent.RequestPath(targetGrid);
        }

        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x);
            int z = Mathf.FloorToInt(worldPos.z);
            int y = Mathf.FloorToInt(Utility.GetHeightAt(x, z));

            return new Vector3Int(x, y, z);
        }

        private Vector3Int PickRandomNearbyGrid(Vector3Int center, int radius)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3Int offset = new(
                    Random.Range(-radius, radius + 1),
                    0,
                    Random.Range(-radius, radius + 1)
                );

                Vector3Int target = center + offset;

                target.x = Mathf.Clamp(target.x, 0, VoxelGrid.Instance.gridSize * VoxelGrid.Instance.chunkSize - 1);
                target.z = Mathf.Clamp(target.z, 0, VoxelGrid.Instance.gridSize * VoxelGrid.Instance.chunkSize - 1);

                target.y = Mathf.FloorToInt(Utility.GetHeightAt(target.x, target.z));

                if (TerrainGenerator.Instance.IsWalkable(target)) return target;
            }

            return center;
        }

        public void ApplyKnockback(Vector3 dir, float strength)
        {
            knockbackVelocity = dir.normalized * strength;
            StartFlee(dir);
        }

        private void StartFlee(Vector3 hitDirection)
        {
            isFleeing = true;

            fleeDirection = hitDirection.normalized;

            agent.CancelPath();

            Vector3Int fleeTarget = WorldToGrid(myTransform.position + fleeDirection * fleeDistance);

            if (agent.CanWalkDirectly(currentGridPos, fleeTarget))
                agent.SetDirectTarget(fleeTarget);
            else
                agent.RequestPath(fleeTarget);
        }

        public void ResetAnimatorPose()
        {
            animator.Rebind();
            animator.Update(0f);
        }

        public void OnSimulateStart() => enabled = true;
        public void OnSimulateStop() => enabled = false;
    }
}
