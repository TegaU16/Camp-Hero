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
        NavMeshAgent navAgent,
        Animator animator,
        Vector3 spawnPosition,
        SimpleRagdollController ragdollController = null)
    {
        // 1) Disable ragdoll if applicable
        if (ragdollController != null && ragdollController.IsSetup)
        {
            ragdollController.DisableRagdoll();
        }

        // 2) Reset Animator safely
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f); // force re-evaluate pose immediately
        }

        // 3) Ensure NavMeshAgent is enabled
        if (navAgent != null)
        {
            navAgent.enabled = true;

            // Warp agent directly if possible
            if (navAgent.isOnNavMesh)
            {
                if (!navAgent.Warp(spawnPosition))
                {
                    Debug.LogWarning($"{agentBehaviour.name} Warp failed. Trying fallback.");
                    agentBehaviour.transform.position = spawnPosition;
                }
            }
            else
            {
                // Not on navmesh yet? Fallback: force transform, sample navmesh
                agentBehaviour.transform.position = spawnPosition;
                if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                }
                else
                {
                    Debug.LogWarning($"{agentBehaviour.name} could not find valid NavMesh position at {spawnPosition}.");
                }
            }

            navAgent.ResetPath();
            navAgent.isStopped = false;
        }
        else
        {
            // No agent? Fallback: set transform only.
            agentBehaviour.transform.position = spawnPosition;
        }

        // 4) Optional: any custom logic can go here

        // 5) Finally: re-activate (optional if not already)
        agentBehaviour.gameObject.SetActive(true);
    }
}
