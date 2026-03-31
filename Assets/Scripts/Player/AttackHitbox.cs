using System.Collections.Generic;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Inventory;
using Game.StatusEffects;
using UnityEngine;
using static BreakableObject;

namespace Game.Players
{
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(Player))]
    public class AttackHitbox : MonoBehaviour
    {
        private readonly HashSet<BreakableObject> alreadyHit = new();

        public LayerMask breakableLayer;

        private Player player;
        private PlayerCombat playerCombat;
        private AttackData attackData;

        public AudioClip hitSound;

        private void Awake()
        {
            player = GetComponent<Player>();
            playerCombat = GetComponent<PlayerCombat>();
        }

        // Called by animation event
        private void PerformHit()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (!TryGetComponent(out BoxCollider box)) return;
            if (attackData == null) return;

            alreadyHit.Clear();

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
            Vector3 targetHitPoint;
            Vector3 targetHitNormal;

            Vector3 direction = (other.transform.position - boxCenter).normalized;

            if (other.Raycast(new Ray(boxCenter, direction), out RaycastHit hitInfo, 5f))
            {
                targetHitPoint = hitInfo.point;
                targetHitNormal = hitInfo.normal;
            }
            else
            {
                targetHitPoint = other.bounds.ClosestPoint(boxCenter);
                targetHitNormal = (targetHitPoint - boxCenter).normalized;
                if (targetHitNormal == Vector3.zero)
                    targetHitNormal = direction;
            }

            Vector3 knockbackDir = (other.transform.position - player.transform.position).normalized;

            // Calculate base damage (PlayerCombat still decides scaling)
            Item selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
            Utility.SetMultiplierSource(this, attackData.damageMultiplier, player.damageMultiplier);
            int itemDamage = playerCombat.ItemDamage(breakable, selectedItem);

            DamageInfo attackDamageInfo = new
            (
                damage: itemDamage,
                poiseDamage: attackData.poiseDamage,
                crit: playerCombat.isCritical,
                hitPoint: targetHitPoint,
                hitNormal: targetHitNormal
            );

            breakable.TakeDamage(attackDamageInfo);
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
                float finalKnockbackForce = attackData.knockbackForce * player.TotalKnockbackForceMultiplier;

                if (enemy != null)
                {
                    enemy.OnAttacked(transform);
                    enemy.ApplyKnockback(knockbackDir, finalKnockbackForce);
                }

                if (animal != null)
                    animal.ApplyKnockback(knockbackDir, finalKnockbackForce);
            }

            // VFX / SFX
            if (attackData.hitEffectPrefab != null)
                Instantiate(attackData.hitEffectPrefab, targetHitPoint, Quaternion.LookRotation(targetHitNormal));

            if (attackData.hitSound != null)
                AudioManager.Instance.PlaySFX(attackData.hitSound, position: targetHitPoint);
        }

        public void SetAttackData(AttackData data) => attackData = data;
    }
}
