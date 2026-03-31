using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Inventory;
using UnityEngine;

namespace Game.Players
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(ProceduralAnimator))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Player))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float comboResetTime = 1f;
        [SerializeField] private int baseDamage = 1;

        private int comboStep = 0;
        private float lastAttackTime;
        private bool canChain = false;
        private bool attackQueued;
        [HideInInspector] public bool canAttack = true;

        private Player player;
        private Animator animator;
        private ProceduralAnimator proceduralAnimator;
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

        public event System.Action OnCriticalHit;

        void Start()
        {
            player = GetComponent<Player>();
            animator = GetComponent<Animator>();
            proceduralAnimator = GetComponent<ProceduralAnimator>();
            health = GetComponent<Health>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleComboAttack();

            if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
                ResetCombo();
        }

        private void HandleComboAttack()
        {
			if (!GameManager.Instance.IsGameActive) return;
			if (InventoryManager.Instance.IsExtensionOpen()) return;
            if (!canAttack) return;

            Item selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
            AttackSet set = (selectedItem != null && selectedItem.attackSet != null)
                ? selectedItem.attackSet
                : defaultAttackSet;

            if (set.comboAttacks == null || set.comboAttacks.Length == 0) return;

            if (currentAttackSet != set)
            {
                comboStep = 0;
                canChain = true;
                attackQueued = false;
                currentAttackSet = set;
            }

            if (!canChain && comboStep != 0)
            {
                attackQueued = true;
                return;
            }

            attackQueued = false;
            comboStep++;

            int attackIndex = Mathf.Min(comboStep - 1, set.comboAttacks.Length - 1);
            AttackData attack = set.comboAttacks[attackIndex];

            if (attack.staminaCost > player.staminaBar.GetStamina())
            {
                comboStep = 0;
                return;
            }

            proceduralAnimator.SetAttacking(true);
            proceduralAnimator.SetProceduralOverrides(
                legs: attack.overrideLegs,
                arms: attack.overrideArms,
                torso: attack.overrideTorso
            );

            playerAttackHitbox.SetAttackData(attack);

            animator.applyRootMotion = attack.useRootMotion;
            animator.speed = player.TotalAttackSpeedMultiplier;

            // Determine if this is the first attack in the combo
            bool firstAttack = comboStep == 1;

            if (firstAttack && !string.IsNullOrEmpty(attack.animationTrigger))
                animator.SetTrigger(attack.animationTrigger); // start combo
            else
                animator.SetInteger("Combo Step", comboStep); // chain rest

            // 6️⃣ Track timing for combo chaining
            lastAttackTime = Time.time;
            canChain = false; // will be re-enabled by animation event
        }

        // animation event calls this
        private void EnableNextComboWindow()
        {
            canChain = true;

            if (attackQueued)
                HandleComboAttack();
        }

        private void ResetCombo()
        {
            animator.applyRootMotion = false;

            comboStep = 0;
            attackQueued = false;
            animator.SetInteger("Combo Step", 0);
            canChain = false;

            proceduralAnimator.SetAttacking(false);
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
            float damage = Random.Range(minDamage, maxDamage);

            float critChanceWithLuck = selectedItem.critChance * player.TotalCritChanceMultiplier;
            critChanceWithLuck = Mathf.Clamp(critChanceWithLuck, 0f, 100f);
            bool isCrit = Random.Range(0f, 100f) < critChanceWithLuck;

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

            float damageMultiplier = player.TotalDamageMultiplier;
            float damage = baseDamage;
            float critFactor = selectedItem.critFactor * player.TotalCritFactorMultiplier;

            // Critical scaling from AttackData
            if (isCritical)
            {
                damage *= critFactor;
                OnCriticalHit?.Invoke(); // Notify listeners of a crit
            }

            // Apply tool reduction if invalid
            if (!isToolValid)
            {
                float reduction = isCritical ? critFactor / reductionFactor : 1 / reductionFactor;
                damage *= reduction;
            }

            // Event-driven passive damage modifiers
            if (OnModifyDamage != null)
                damage = OnModifyDamage.Invoke((int)damage, isCritical);
            // Pass base damage and crit flag; subscribers return new modified damage

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
                if ((selectedItem.toolType & tool) != 0 && breakables.Contains(hitObject.objectType)) return true;
            }

            return false;
        }

        // Called by animation event
        private void PlayWhooshSound()
        {
            float pitchVariance = 0.1f;
            float pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);

            AudioManager.Instance.PlaySFX(swordWhoosh, pitch: pitch);
        }

        // Called by animation event
        private void StartWeaponSlash()
        {
            GameObject equippedItem = ItemEquip.Instance.HeldItem;
            if (equippedItem == null) return;

            SlashEffect slashEffect = equippedItem.GetComponentInChildren<SlashEffect>();
            if (slashEffect == null) return;

            slashEffect.StartSlash();
        }

        // Called by animation event
        private void EndWeaponSlash()
        {
            GameObject equippedItem = ItemEquip.Instance.HeldItem;
            if (equippedItem == null) return;

            SlashEffect slashEffect = equippedItem.GetComponentInChildren<SlashEffect>();
            if (slashEffect == null) return;

            slashEffect.StopSlash();
        }
    }
}
