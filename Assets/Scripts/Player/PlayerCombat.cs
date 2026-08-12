using System;
using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Inventory;
using UnityEngine;

namespace Game.Players
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Player))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private int staminaCost = 10;

        [SerializeField] private GameObject lowStaminaNotification;

        [HideInInspector] public bool canAttack = true;

        private Player player;
        private Animator animator;
        private Health health;

        [SerializeField] private AttackSet defaultAttackSet;
        private AttackSet currentAttackSet;

        [HideInInspector] public bool isCritical = false;

        public AttackHitbox playerAttackHitbox;
        private const float reductionFactor = 4f;

        public AudioClip swordWhoosh;

        private static readonly Dictionary<ToolType, HashSet<ObjectType>> toolToObjectMap = new()
        {
            { ToolType.Axe, new() { ObjectType.Wood } },
            { ToolType.Pickaxe, new() { ObjectType.Stone } },
            { ToolType.Sword, new() { ObjectType.Flesh } }
        };

        public delegate float ModifyDamageDelegate(int currentDamage, bool isCrit);
        public event ModifyDamageDelegate OnModifyDamage;

        public delegate int LifestealDelegate(int damageDealt);
        public event LifestealDelegate OnLifesteal;

        public event Action OnCriticalHit;

        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

        [SerializeField] private string attackLayerName = "Upper Body";
        private int attackLayerIndex = -1;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            player = GetComponent<Player>();
            health = GetComponent<Health>();

            attackLayerIndex = animator.GetLayerIndex(attackLayerName);

            if (attackLayerIndex >= 0)
                animator.SetLayerWeight(attackLayerIndex, 1f);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleComboAttack();
        }

        private void HandleComboAttack()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (InventoryManager.Instance.IsExtensionOpen()) return;
            if (!canAttack) return;

            if (player.CurrentStamina < staminaCost)
            {
                TextNotification lowStamina = TextNotificationPool.Instance.Get(lowStaminaNotification, isPoolStatic: false);
                lowStamina.Setup();

                return;
            }

            Item selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
            AttackSet set = (selectedItem != null && selectedItem.attackSet != null)
                ? selectedItem.attackSet
                : defaultAttackSet;

            if (set.comboAttacks == null || set.comboAttacks.Length == 0) return;

            currentAttackSet = set;

            animator.speed = player.TotalAttackSpeedMultiplier;

            if (attackLayerIndex >= 0)
                animator.SetLayerWeight(attackLayerIndex, 1f);

            animator.SetTrigger(AttackTriggerHash);
        }

        // Optional animation event at end of combo / recovery
        private void ResetCombo()
        {
            animator.applyRootMotion = false;
            animator.speed = 1f;

            animator.ResetTrigger(AttackTriggerHash);
        }

        private void SetAttackByIndex(int index)
        {
            if (index < 0 || index >= currentAttackSet.comboAttacks.Length) return;

            playerAttackHitbox.SetAttackData(currentAttackSet.comboAttacks[index]);
        }

        public int ItemDamage(BreakableObject hitObject, Item selectedItem)
        {
            if (selectedItem == null)
            {
                isCritical = false;
                return (int)(baseDamage * player.TotalDamageMultiplier);
            }

            float minDamage = selectedItem.attackDamage[0];
            float maxDamage = selectedItem.attackDamage[1];
            float damage = UnityEngine.Random.Range(minDamage, maxDamage);

            float critChanceWithLuck = selectedItem.critChance * player.TotalCritChanceMultiplier;
            critChanceWithLuck = Mathf.Clamp(critChanceWithLuck, 0f, 100f);

            bool isCrit = UnityEngine.Random.Range(0f, 100f) < critChanceWithLuck;

            bool isTypeMatched = IsTypeMatched(hitObject, selectedItem);
            bool isToolLevelSufficient = selectedItem.toolLevel >= hitObject.objectLevel;
            bool isToolValid = isTypeMatched && isToolLevelSufficient;

            int damageResult = CalculateDamage(damage, isCrit, isToolValid, selectedItem);

            if (OnLifesteal != null && hitObject.GetComponent<Enemy>())
            {
                int healthGain = OnLifesteal.Invoke(damageResult);
                health.AddHealth(healthGain);
            }

            return hitObject.PlacedByPlayer ? Mathf.Min(10, damageResult) : damageResult;
        }

        private int CalculateDamage(float baseDamage, bool isCrit, bool isToolValid, Item selectedItem)
        {
            isCritical = isCrit;

            float critFactor = selectedItem.critFactor * player.TotalCritFactorMultiplier;
            float damage = baseDamage;

            if (isCritical)
            {
                damage *= critFactor;
                OnCriticalHit?.Invoke();
            }

            if (!isToolValid)
            {
                float reduction = isCritical ? critFactor / reductionFactor : 1f / reductionFactor;
                damage *= reduction;
            }

            if (OnModifyDamage != null)
                damage = OnModifyDamage.Invoke((int)damage, isCritical);

            return Mathf.RoundToInt(damage);
        }

        public bool IsTypeMatched(BreakableObject hitObject, Item selectedItem)
        {
            if (selectedItem == null) return false;
            if (selectedItem.toolType == 0 || hitObject.objectType == ObjectType.None) return true;

            return CanBreak(selectedItem, hitObject);
        }

        private bool CanBreak(Item selectedItem, BreakableObject hitObject)
        {
            foreach ((ToolType tool, HashSet<ObjectType> breakables) in toolToObjectMap)
            {
                if ((selectedItem.toolType & tool) != 0 &&
                    breakables.Contains(hitObject.objectType))
                    return true;
            }

            return false;
        }

        private void PlayWhooshSound()
        {
            float pitch = 1f + UnityEngine.Random.Range(-0.1f, 0.1f);
            AudioManager.Instance.PlaySFX(swordWhoosh, pitch: pitch);
        }

        private void StartWeaponSlash()
        {
            GameObject equippedItem = ItemEquip.Instance.HeldItem;
            if (equippedItem == null) return;

            WeaponSlash slash = equippedItem.GetComponentInChildren<WeaponSlash>();
            if (slash != null)
                slash.StartSlash();
        }

        private void EndWeaponSlash()
        {
            GameObject equippedItem = ItemEquip.Instance.HeldItem;
            if (equippedItem == null) return;

            WeaponSlash slash = equippedItem.GetComponentInChildren<WeaponSlash>();
            if (slash != null)
                slash.EndSlash();
        }
    }
}
