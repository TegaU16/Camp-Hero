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
            playerCombat = GetComponentInParent<PlayerCombat>();
    }

    public void PerformHit(AttackData attackData)
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        alreadyHit.Clear();

        if (!TryGetComponent(out BoxCollider box)) return;

        // Use AttackData override hitbox if provided
        Vector3 boxCenter = attackData.overrideHitbox
            ? transform.TransformPoint(attackData.hitboxCenter)
            : transform.TransformPoint(box.center);

        Vector3 boxHalfExtents = attackData.overrideHitbox
            ? Vector3.Scale(attackData.hitboxSize * 0.5f, transform.lossyScale)
            : Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation, breakableLayer);

        foreach (Collider hit in hits)
            ProcessHit(hit, box, attackData);
    }

    private void ProcessHit(Collider other, BoxCollider box, AttackData attackData)
    {
        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);

        BreakableObject breakable = other.GetComponentInParent<BreakableObject>();
        if (breakable == null) return;

        if (((1 << other.gameObject.layer) & breakableLayer) == 0) return;

        if (alreadyHit.Contains(breakable)) return;

        if (playerCombat == null)
        {
            Debug.LogWarning("playerCombat not assigned in AttackHitbox!");
            return;
        }

        Vector3 boxCenter = transform.TransformPoint(box.center);
        Vector3 hitPoint = other.bounds.ClosestPoint(boxCenter);
        Vector3 hitNormal = (hitPoint - boxCenter).normalized;

        Vector3 knockbackDir = (other.transform.position - playerCombat.transform.position).normalized;

        // Calculate base damage (PlayerCombat still decides scaling)
        int damage = playerCombat.ItemDamage(breakable, selectedItem, attackData);
        breakable.TakeDamage(damage, playerCombat.isCritical, hitPoint, hitNormal);
        alreadyHit.Add(breakable);

        Enemy enemy = breakable.GetComponentInParent<Enemy>();
        if (enemy != null && attackData.statusEffects != null)
        {
            foreach (StatusEffect effect in attackData.statusEffects)
            {
                if (effect != null)
                    effect.Apply(enemy);
            }
        }

        Animal animal = breakable.GetComponentInParent<Animal>();

        // Extra effects
        if (attackData.applyKnockback)
        {
            if (enemy != null)
            {
                enemy.OnAttacked(transform);
                enemy.ApplyKnockback(knockbackDir, attackData.knockbackForce);
            }

            if (animal != null)
                animal.ApplyKnockback(knockbackDir, attackData.knockbackForce);
        }

        // VFX / SFX
        if (attackData.hitEffectPrefab != null)
            Instantiate(attackData.hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));

        if (attackData.hitSound != null)
            AudioManager.Instance.PlaySFX(attackData.hitSound, position: hitPoint);
    }
}
