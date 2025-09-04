using UnityEngine;

public class TrialEnemyMarker : MonoBehaviour
{
    private Animator animator;
    private SimpleRagdollController ragdollController;
    private BreakableObject breakableObject;

    private void Start()
    {
        animator = GetComponent<Animator>();
        ragdollController = GetComponent<SimpleRagdollController>();
        breakableObject = GetComponent<BreakableObject>();
    }

    public void OnTrialSpawn()
    {
        ResetTrialEnemy(transform.position);
    }

    private void ResetTrialEnemy(Vector3 spawnPosition)
    {
        PooledAIUtility.ResetAI(
            this,
            animator,
            spawnPosition,
            ragdollController
        );

        Enemy enemy = GetComponent<Enemy>();
        enemy.SetChasing();

        if (breakableObject != null)
        {
            breakableObject.ResetObject();
        }
    }
}
