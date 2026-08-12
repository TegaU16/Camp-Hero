using System.Collections.Generic;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Inventory;
using Game.StatusEffects;
using Game.Terrain;
using UnityEngine;
using Worlds;
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

            WorldSession.CurrentRunStats.totalDamageDealt += itemDamage;
            if (breakable.TryGetComponent(out ObjectCategory _))
                WorldSession.CurrentRunStats.damageDealtToResources += itemDamage;

            float finalKnockbackForce = attackData.knockbackForce * player.TotalKnockbackForceMultiplier;

            DamageInfo attackDamageInfo = new
            (
                damage: itemDamage,
                poiseDamage: attackData.poiseDamage,
                crit: playerCombat.isCritical,
                hitPoint: targetHitPoint,
                hitNormal: targetHitNormal,
                knockbackDirection: knockbackDir,
                knockbackForce: finalKnockbackForce
            );

            if (breakable.TryGetComponent(out HitStopController hitStop))
            {
                float duration = attackData.hitStopDuration;

                if (playerCombat.isCritical)
                    duration *= 1.5f;

                hitStop.ApplyHitStop(duration);
            }

            breakable.TakeDamage(attackDamageInfo);
            alreadyHit.Add(breakable);

            AudioManager.Instance.PlaySFX(hitSound);

            Enemy enemy = breakable.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.EnemyCombat.OnAttacked(transform);
                WorldSession.CurrentRunStats.damageDealtToEnemies += itemDamage;

                if (attackData.statusEffects != null)
                {
                    foreach (StatusEffect effect in attackData.statusEffects)
                    {
                        if (effect != null)
                            effect.Apply(enemy);
                    }
                }
            }

            Animal animal = breakable.GetComponentInParent<Animal>();
            if (animal != null && attackData.applyKnockback)
                animal.ApplyKnockback(knockbackDir, finalKnockbackForce);

            DamageFlash damageFlash = breakable.GetComponentInParent<DamageFlash>();
            if (damageFlash != null)
                damageFlash.Flash();

            // VFX / SFX
            if (attackData.hitEffectPrefab != null)
                Instantiate(attackData.hitEffectPrefab, targetHitPoint, Quaternion.LookRotation(targetHitNormal));

            if (attackData.hitSound != null)
                AudioManager.Instance.PlaySFX(attackData.hitSound, position: targetHitPoint);
        }

        public void SetAttackData(AttackData data) => attackData = data;
    }
}
