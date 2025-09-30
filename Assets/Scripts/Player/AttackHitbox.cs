using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private readonly HashSet<BreakableObject> alreadyHit = new();

    public LayerMask breakableLayer;

    public PlayerCombat playerCombat;

    private void Awake()
    {
        if (playerCombat == null)
        {
            playerCombat = GetComponentInParent<PlayerCombat>();
        }
    }

    public void PerformHit()
    {
        ClearHits();

        if (!TryGetComponent(out BoxCollider box)) return;

        Vector3 boxCenter = transform.TransformPoint(box.center);
        Vector3 boxHalfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation, breakableLayer);

        foreach (Collider hit in hits)
        {
            ProcessHit(hit, box);
        }
    }

    private void ProcessHit(Collider other, BoxCollider box)
    {
        if (GameManager.Instance.isPaused) return;

        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);

        BreakableObject breakable = other.GetComponentInParent<BreakableObject>();
        if (breakable == null) return;

        // Check if the other object is on the breakable layer
        if (((1 << other.gameObject.layer) & breakableLayer) == 0) return;

        // Only damage once per BreakableObject
        if (alreadyHit.Contains(breakable)) return;

        if (playerCombat == null)
        {
            Debug.LogWarning("playerCombat not assigned in AttackHitbox!");
            return;
        }

        Vector3 boxCenter = transform.TransformPoint(box.center);

        Vector3 hitPoint = other.bounds.ClosestPoint(boxCenter);
        Vector3 hitNormal = (hitPoint - boxCenter).normalized;

        int damage = playerCombat.ItemDamage(breakable, selectedItem);
        breakable.TakeDamage(damage, playerCombat.isCritical, hitPoint, hitNormal);
        alreadyHit.Add(breakable);

        Enemy enemy = breakable.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Transform playerTransform = playerCombat.gameObject.transform;
            enemy.GetComponent<Enemy>().OnAttacked(playerTransform);
        }
    }

    public void ClearHits()
    {
        alreadyHit.Clear();
    }
}
