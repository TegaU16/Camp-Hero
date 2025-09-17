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

    // Called by animation event
    void PerformHit()
    {
        ClearHits();

        if (!TryGetComponent(out BoxCollider box)) return;

        Vector3 boxCenter = transform.TransformPoint(box.center);
        Vector3 boxHalfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation, targetableLayer);

        foreach (Collider hit in hits)
        {
            ProcessHit(hit);
        }
    }

    void ProcessHit(Collider other)
    {
        if (GameManager.Instance.isPaused) return;

        if (enemyScript == null) return;

        Targetable target = other.GetComponentInParent<Targetable>();

        if (alreadyHit.Contains(target)) return;

        if (target != null)
            enemyScript.DealDamage();
    }

    public void ClearHits()
    {
        alreadyHit.Clear();
    }
}
