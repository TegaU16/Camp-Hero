using UnityEngine;

public class SimpleRagdollController : MonoBehaviour
{
    private Rigidbody[] allRigidbodies;
    private Collider[] allColliders;

    [SerializeField] private Animator animator;
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;

    public bool IsSetup { get; private set; } = false;

    void Awake()
    {
        allRigidbodies = GetComponentsInChildren<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>();

        Collider mainCollider = GetComponent<Collider>();
        allColliders = System.Array.FindAll(allColliders, col => col != mainCollider);

        DisableRagdoll();
    }

    public void EnableRagdoll()
    {
        if (!IsSetup)
        {
            foreach (var rb in allRigidbodies)
                rb.isKinematic = false;

            foreach (var col in allColliders)
            {
                if (col.GetComponent<CharacterController>() == null)
                {
                    col.enabled = true;
                }
            }

            if (animator != null)
                animator.enabled = false;

            if (agent != null)
                agent.enabled = false;

            IsSetup = true;
        }
    }

    public void DisableRagdoll()
    {
        foreach (var rb in allRigidbodies)
            rb.isKinematic = true;

        foreach (var col in allColliders)
        {
            if (col.GetComponent<CharacterController>() == null)
            {
                col.enabled = false;
            }
        }

        if (animator != null)
            animator.enabled = true;

        if (agent != null)
            agent.enabled = true;

        IsSetup = false;
    }
}
