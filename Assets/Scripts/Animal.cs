using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Animal : MonoBehaviour
{
    [HideInInspector] public VoxelChunk ownerChunk;

    private NavMeshAgent agent;
    private Animator animator;
    private BreakableObject breakableObject;
    private SimpleRagdollController ragdollController;

    Vector3 wanderCenter;
    [SerializeField] float wanderRadius = 5f;
    [SerializeField] float wanderInterval = 2f;

    float wanderTimer;

    public bool IsActiveAI { get; private set; } = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        breakableObject = GetComponent<BreakableObject>();
        ragdollController = GetComponent<SimpleRagdollController>();

        agent.updatePosition = true;
        agent.updateRotation = false;
    }

    void Update()
    {
        if (!IsActiveAI) return;

        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            PickNewDestination();
            wanderTimer = 0f;
        }

        float moveSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", moveSpeed);
    }

    void OnAnimatorMove()
    {
        // Usually empty or optional — just disables Unity’s auto-motion
    }

    private void PickNewDestination()
    {
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        Vector3 newTarget = wanderCenter + new Vector3(rand.x, 0, rand.y);

        if (NavMesh.SamplePosition(newTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void Init(Vector3 spawnPosition, VoxelChunk ownerChunk)
    {
        this.ownerChunk = ownerChunk;

        PooledAIUtility.ResetAI(
            this,
            agent,
            animator,
            spawnPosition,
            ragdollController
        );

        wanderCenter = spawnPosition;
        wanderTimer = wanderInterval;
        IsActiveAI = true;

        StartCoroutine(DelayedPickNewDestination());
    }

    private IEnumerator DelayedPickNewDestination()
    {
        // Wait 1 frame to guarantee agent is valid
        yield return null;

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        PickNewDestination();
    }

    public void Die()
    {
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
        if (breakableObject != null)
        {
            breakableObject.DestroyObject();
        }

        AnimalPool.Instance.ReturnAnimal(this);
    }
}
