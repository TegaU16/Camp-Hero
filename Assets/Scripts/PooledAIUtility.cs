using UnityEngine;
using UnityEngine.AI;

public static class PooledAIUtility
{
    /// <summary>
    /// Resets a pooled AI agent correctly.
    /// Handles NavMeshAgent, Animator, and optional ragdoll.
    /// </summary>
    public static void ResetAI(
    MonoBehaviour agentBehaviour,
    Animator animator,
    Vector3 spawnPosition,
    SimpleRagdollController ragdollController = null)
    {
        if (ragdollController != null && ragdollController.IsSetup)
            ragdollController.DisableRagdoll();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }

        agentBehaviour.transform.position = spawnPosition;

        agentBehaviour.gameObject.SetActive(true);
    }
}
