using System.Collections;
using UnityEngine;

public class SimpleRagdollController : MonoBehaviour
{
    private Rigidbody[] allRigidbodies;
    private Collider[] allColliders;
    public Collider[] collidersToIgnore;

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
            {
                rb.isKinematic = false;
                rb.linearDamping = 2f;
                rb.angularDamping = 4f;
                rb.sleepThreshold = 0.5f;
            }

            StartCoroutine(FreezeAfterTime(3f));

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
            if (col.GetComponent<CharacterController>() == null && !ToIgnore(col))
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

    private bool ToIgnore(Collider colIgnore)
    {
        foreach(Collider col in collidersToIgnore)
        {
            if (col == colIgnore)
            {
                return true;
            }
        }

        return false;
    }

    IEnumerator FreezeAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var rb in allRigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
}
