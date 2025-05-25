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

    private void OnTriggerStay(Collider other)
    {
        ProcessHit(other);
    }

    private void ProcessHit(Collider other)
    {
        if (GameManager.Instance.isPaused) return;

        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
        if (selectedItem == null) return;

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

        int damage = playerCombat.ItemDamage(breakable, selectedItem);
        breakable.TakeDamage(damage, playerCombat.isCritical);
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
