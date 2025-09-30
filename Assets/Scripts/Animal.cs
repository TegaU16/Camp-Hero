using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Animal : MonoBehaviour, ISimulatable
{
    [Header("Components")]
    private Transform myTransform;  // Cached transform
    public Animator animator;
    private BreakableObject breakableObject;
    private SimpleRagdollController ragdollController;
    public VoxelAgent agent;
    private CharacterController characterController;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;

    [Header("Movement Settings")]
    [SerializeField] private float gravity = -9.8f;
    private float verticalVelocity = 0f;

    private Coroutine wanderCoroutine;

    // Movement state
    private Vector3Int currentGridPos;
    private Vector3 latestSpawnPos;
    private bool justSpawned = false;

    private Vector3 knockbackVelocity = Vector3.zero;

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
        {
            Init(myTransform.position);
        }
    }

    void FixedUpdate()
    {
        if (!IsActiveAI)
        {
            Debug.LogWarning($"{name} is not active AI");
            return;
        }

        currentGridPos = WorldToGrid(myTransform.position);

        agent.UpdateAgent();

        Vector3 move;

        if (agent.WantsToMove())
        {
            move = agent.GetMovementThisFrame();

            float targetY = agent.GetTargetY();
            float yDiff = targetY - myTransform.position.y;

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0)
                    verticalVelocity = -1f;
            }
            else
            {
                verticalVelocity += gravity * Time.fixedDeltaTime;
            }

            move.y = verticalVelocity + yDiff * 10f;

            move += knockbackVelocity;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

            characterController.Move(move * Time.fixedDeltaTime);

            float speed = agent.GetCurrentSpeedFraction();
            animator.SetFloat("Speed", speed, 0.2f, Time.fixedDeltaTime);
        }
        else
        {
            verticalVelocity = 0f;
            knockbackVelocity = Vector3.zero;

            animator.SetFloat("Speed", 0f, 0.2f, Time.fixedDeltaTime);
        }
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
        if (ragdollController != null) ragdollController.EnableRagdoll();

        if (wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }

        Invoke(nameof(Despawn), 5f);
    }

    private void Despawn()
    {
        if (breakableObject != null) breakableObject.DestroyObject();
        wanderCoroutine = null;
        AnimalPool.Instance.ReturnAnimal(this);
    }

    IEnumerator WanderRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            Vector3Int targetGrid = PickRandomNearbyGrid(currentGridPos, (int)wanderRadius);

            agent.CancelPath();
            if (agent.CanWalkDirectly(currentGridPos, targetGrid))
            {
                agent.SetDirectTarget(targetGrid);
            }
            else
            {
                agent.RequestPath(targetGrid);
            }

            while (agent.HasPath)
                yield return null;

            yield return new WaitForSeconds(Random.Range(waitTimeMin, waitTimeMax));
        }
    }

    public List<Vector3Int> ComputeWanderPathAsync(Vector3Int targetGrid)
    {
        PathfinderManager pfm = PathfinderManager.Instance;

        if (!pfm.IsWalkable(targetGrid))
        {
            Vector3Int fallback = pfm.FindNearestUnblocked(targetGrid);
            targetGrid = fallback;
        }

        GridAStar pathfinder = pfm.BuildLocalPathfinder();
        return pathfinder.FindPath(currentGridPos, targetGrid);
    }

    public void ApplyWanderPath(List<Vector3Int> newPath)
    {
        if (newPath == null || newPath.Count == 0)
        {
            Vector3Int newTarget = PickRandomNearbyGrid(currentGridPos, (int)wanderRadius);
            WanderManager.Instance.Enqueue(new WanderRequest(this, newTarget, () => { }));
            return;
        }

        agent.OnPathResult(newPath);
    }

    private Vector3Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x),
            0,
            Mathf.RoundToInt(worldPos.z)
        );
    }

    private Vector3Int PickRandomNearbyGrid(Vector3Int center, int radius)
    {
        Vector3Int offset = new(
            Random.Range(-radius, radius + 1),
            0,
            Random.Range(-radius, radius + 1)
        );

        Vector3Int target = center + offset;

        target.x = Mathf.Clamp(target.x, 0, VoxelGrid.Instance.gridSize * VoxelGrid.Instance.chunkSize - 1);
        target.z = Mathf.Clamp(target.z, 0, VoxelGrid.Instance.gridSize * VoxelGrid.Instance.chunkSize - 1);

        float height = VoxelGrid.Instance.GetHeightAt(target.x, target.z);
        target.y = Mathf.RoundToInt(height);

        return target;
    }

    public void ApplyKnockback(Vector3 dir, float strength)
    {
        knockbackVelocity = dir.normalized * strength;
    }

    public void ResetAnimatorPose()
    {
        animator.Rebind();
        animator.Update(0f);
    }

    public void OnSimulateStart() => enabled = true;
    public void OnSimulateStop() => enabled = false;
}
