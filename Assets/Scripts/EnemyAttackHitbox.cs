using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    private Enemy enemyScript;
    private readonly HashSet<Targetable> alreadyHit = new();

    public LayerMask targetableLayer;

    void Awake()
    {
        enemyScript = GetComponentInParent<Enemy>();
    }

    private void Start()
    {
        if (enemyScript != null && TryGetComponent(out BoxCollider box))
        {
            box.size = new Vector3(box.size.x, box.size.y, enemyScript.meleeAttackRange);
            box.center = new Vector3(0, box.center.y, enemyScript.meleeAttackRange / 2f);
        }
    }

    // Called by animation event
    void PerformHit()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (enemyScript == null) return;

        alreadyHit.Clear();

        if (!TryGetComponent(out BoxCollider box)) return;

        Vector3 boxCenter = transform.TransformPoint(box.center);
        Vector3 boxHalfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation, targetableLayer);

        foreach (Collider hit in hits)
            ProcessHit(hit);
    }

    void ProcessHit(Collider other)
    {
        Targetable target = other.GetComponentInParent<Targetable>();
        if (target == null) return;

        if (alreadyHit.Contains(target)) return;

        alreadyHit.Add(target);
        enemyScript.DealDamage();
    }
}
