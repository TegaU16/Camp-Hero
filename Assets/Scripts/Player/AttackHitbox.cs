using System.Collections.Generic;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Inventory;
using Game.StatusEffects;
using UnityEngine;

namespace Game.Players
{
    public class AttackHitbox : MonoBehaviour
    {
        private readonly HashSet<BreakableObject> alreadyHit = new();

        public LayerMask breakableLayer;

        public PlayerCombat playerCombat;
        private AttackData attackData;

        public AudioClip hitSound;

        private void Awake()
        {
            if (playerCombat == null)
                playerCombat = GetComponentInParent<PlayerCombat>();
        }

        // Called by animation event
        private void PerformHit()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;

            alreadyHit.Clear();

            if (!TryGetComponent(out BoxCollider box)) return;

            if (attackData == null) return;

            // Compute world center before OverlapBox
            Vector3 worldCenter = transform.TransformPoint(box.center);
            Vector3 boxHalfExtents = attackData.overrideHitbox
                ? Vector3.Scale(attackData.hitboxSize * 0.5f, transform.lossyScale)
                : Vector3.Scale(box.size * 0.5f, transform.lossyScale);

            Collider[] hits = Physics.OverlapBox(worldCenter, boxHalfExtents, transform.rotation, breakableLayer);

            foreach (Collider hit in hits)
                ProcessHit(hit, box);
        }

        private void ProcessHit(Collider other, BoxCollider box)
        {
            BreakableObject breakable = other.GetComponentInParent<BreakableObject>();
            if (breakable == null) return;

            if (((1 << other.gameObject.layer) & breakableLayer) == 0) return;

            if (alreadyHit.Contains(breakable)) return;

            if (playerCombat == null) return;

            Vector3 boxCenter = transform.TransformPoint(box.center);
            Vector3 hitPoint;
            Vector3 hitNormal;

            Vector3 direction = (other.transform.position - boxCenter).normalized;

            if (other.Raycast(new Ray(boxCenter, direction), out RaycastHit hitInfo, 5f))
            {
                hitPoint = hitInfo.point;
                hitNormal = hitInfo.normal;
            }
            else
            {
                hitPoint = other.bounds.ClosestPoint(boxCenter);
                hitNormal = (hitPoint - boxCenter).normalized;
                if (hitNormal == Vector3.zero)
                    hitNormal = direction;
            }

            Vector3 knockbackDir = (other.transform.position - playerCombat.transform.position).normalized;

            // Calculate base damage (PlayerCombat still decides scaling)
            Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
            int damage = playerCombat.ItemDamage(breakable, selectedItem, attackData);

            breakable.TakeDamage(damage, playerCombat.isCritical, hitPoint, hitNormal);
            alreadyHit.Add(breakable);

            AudioManager.Instance.PlaySFX(hitSound);

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

        public void SetAttackData(AttackData data)
        {
            if (data == null) return;
            attackData = data;
        }
    }
}
