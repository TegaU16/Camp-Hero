using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    private Enemy enemyScript;

    void Awake()
    {
        // Cache the Enemy script from the parent
        enemyScript = GetComponentInParent<Enemy>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (enemyScript == null) return;

        if (other.GetComponent<Health>() != null || other.GetComponent<BreakableObject>() != null)
        {
            enemyScript.DealDamage();
        }
    }
}
