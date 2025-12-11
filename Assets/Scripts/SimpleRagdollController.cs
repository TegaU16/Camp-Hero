using System.Collections;
using Game.AI;
using UnityEngine;

public class SimpleRagdollController : MonoBehaviour
{
    private Rigidbody[] allRigidbodies;
    private Collider[] allColliders;
    public Collider[] collidersToIgnore;

    [SerializeField] private Animator animator;
    [SerializeField] private VoxelAgent agent;

    [Header("For Procedural Animations")]
    [SerializeField] private ProceduralAnimator proceduralAnimator;

    public bool IsSetup { get; private set; } = false;

    void Awake()
    {
        allRigidbodies = GetComponentsInChildren<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>();

        if (TryGetComponent(out Collider mainCollider))
            allColliders = System.Array.FindAll(allColliders, col => col != mainCollider);

        if (TryGetComponent(out Rigidbody rootRigidbody))
            allRigidbodies = System.Array.FindAll(allRigidbodies, rb => rb != rootRigidbody);

        DisableRagdoll();
    }

    public void EnableRagdoll()
    {
        if (!IsSetup)
        {
            if (TryGetComponent(out CharacterController cc)) 
                cc.enabled = false;

            // Enable physics on all rigidbodies
            foreach (Rigidbody rb in allRigidbodies)
            {
                rb.isKinematic = false;
                rb.linearDamping = 2f;
                rb.angularDamping = 4f;
                rb.sleepThreshold = 0.5f;
            }

            StartCoroutine(FreezeAfterTime(3f));

            foreach (Collider col in allColliders)
            {
                if (col.GetComponent<CharacterController>() == null)
                    col.enabled = true;
            }

            if (animator != null)
                animator.enabled = false;

            if (proceduralAnimator != null)
                proceduralAnimator.enabled = true;

            if (agent != null)
                agent.enabled = false;

            IsSetup = true;
        }
    }

    public void DisableRagdoll()
    {
        foreach (Rigidbody rb in allRigidbodies)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            rb.isKinematic = true;
        }

        foreach (Collider col in allColliders)
        {
            if (col.GetComponent<CharacterController>() == null && !ToIgnore(col))
                col.enabled = false;
        }

        if (TryGetComponent(out CharacterController cc))
        {
            cc.enabled = false;   // force reset
            cc.enabled = true;    // toggle to clear internal physics
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();    // reset animator state
            animator.Update(0f);
        }

        if (proceduralAnimator != null)
            proceduralAnimator.enabled = true;

        if (agent != null)
        {
            agent.enabled = false; // force reset
            agent.enabled = true;
        }

        IsSetup = false;
    }

    private bool ToIgnore(Collider colIgnore)
    {
        foreach(Collider col in collidersToIgnore)
        {
            if (col == colIgnore) return true;
        }

        return false;
    }

    IEnumerator FreezeAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (Rigidbody rb in allRigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
}
