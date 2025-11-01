using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboResetTime = 1f;

    private int comboStep = 0;
    private float lastAttackTime;
    private bool canChain = false;
    private bool attackQueued;

    private Animator animator;
    private ProceduralAnimator proceduralAnimator;
    private Health health;

    public AttackSet defaultAttackSet;
    private AttackSet currentAttackSet;

    [HideInInspector] public float critMultiplier;
    [HideInInspector] public bool isCritical = false;

    public AttackHitbox playerAttackHitbox;
    private const float reductionFactor = 2f;

    private static readonly Dictionary<ToolType, HashSet<BreakableObject.ObjectType>> toolToObjectMap = new()
    {
        { ToolType.Axe, new() { BreakableObject.ObjectType.Wood } },
        { ToolType.Pickaxe, new() { BreakableObject.ObjectType.Stone } },
        { ToolType.Sword, new() { BreakableObject.ObjectType.Flesh } }
    };

    private readonly Dictionary<object, float> damageMultiplierSources = new();
    private readonly float baseDamageMultiplier = 1f;

    public delegate float ModifyDamageDelegate(int currentDamage, bool isCrit);
    public event ModifyDamageDelegate OnModifyDamage;

    public delegate int LifestealDelegate(int damageDealt);
    public event LifestealDelegate OnLifesteal;

    public event System.Action OnCriticalHit;

    void Start()
    {
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
        if (InventoryManager.Instance.IsExtensionOpen()) return;
        if (!GameManager.Instance.IsGameManagerReady()) return;

        // 1️⃣ Get current AttackSet
        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
        AttackSet set = (selectedItem != null && selectedItem.attackSet != null)
            ? selectedItem.attackSet
            : defaultAttackSet;

        if (set.comboAttacks == null || set.comboAttacks.Length == 0) return;

        // 2️⃣ Reset combo if weapon changed
        if (currentAttackSet != set)
        {
            comboStep = 0;
            canChain = true;
            attackQueued = false;
            currentAttackSet = set;
        }

        // 3️⃣ Only start next attack if allowed
        if (!canChain && comboStep != 0)
        {
            attackQueued = true;
            return;
        }

        attackQueued = false;
        comboStep++;

        int attackIndex = Mathf.Min(comboStep - 1, set.comboAttacks.Length - 1);
        AttackData attack = set.comboAttacks[attackIndex];

        proceduralAnimator.SetAttacking(true);
        proceduralAnimator.SetProceduralOverrides(
            legs: attack.overrideLegs,
            arms: attack.overrideArms,
            torso: attack.overrideTorso
        );

        // 4️⃣ Setup hitbox
        if (playerAttackHitbox.TryGetComponent(out BoxCollider box))
        {
            if (attack.overrideHitbox)
            {
                box.size = attack.hitboxSize;
                box.center = attack.hitboxCenter;
            }
            else
            {
                // default hitbox in front of player
                float attackDistance = attack.attackDistance;
                box.size = new Vector3(0.5f, 1f, attackDistance);
                box.center = new Vector3(0f, box.size.y / 2f, attackDistance / 2f);
            }
        }

        // 5️⃣ Apply root motion and animator parameters
        animator.applyRootMotion = attack.useRootMotion;

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

    // animation events call these
    void EnableNextComboWindow()
    {
        canChain = true;

        if (attackQueued)
            HandleComboAttack();
    }
    void EndCombo() => ResetCombo();

    private void ResetCombo()
    {
        animator.applyRootMotion = false;

        comboStep = 0;
        attackQueued = false;
        animator.SetInteger("Combo Step", 0);
        canChain = false;

        proceduralAnimator.SetAttacking(false);
    }

    public int ItemDamage(BreakableObject hitObject, Item selectedItem, AttackData attackData)
    {
        if (selectedItem == null)
        {
            isCritical = false;
            return 0;
        }

        float minDamage = selectedItem.attackDamage[0];
        float maxDamage = selectedItem.attackDamage[1];
        float damage = Random.Range(minDamage, maxDamage);

        float critChanceWithLuck = selectedItem.critChance * critMultiplier;
        critChanceWithLuck = Mathf.Clamp(critChanceWithLuck, 0f, 100f);
        bool isCrit = Random.Range(0f, 100f) < critChanceWithLuck;

        bool isTypeMatched = IsTypeMatched(hitObject, selectedItem);
        bool isToolLevelSufficient = selectedItem.toolLevel >= hitObject.objectLevel;

        int damageResult = CalculateDamage(damage, isCrit, isTypeMatched && isToolLevelSufficient, selectedItem, attackData);

        if (OnLifesteal != null && hitObject.GetComponent<Enemy>())
        {
            int healthGain = OnLifesteal.Invoke(damageResult);
            health.AddHealth(healthGain);
        }

        return hitObject.PlacedByPlayer ? Mathf.Min(10, damageResult) : damageResult;
    }

    private int CalculateDamage(float baseDamage, bool isCrit, bool isToolValid, Item selectedItem, AttackData attackData)
    {
        isCritical = isCrit;

        float damageMultiplier = TotalDamageMultiplier; // Base multiplier from upgrades

        // Start with item damage range
        float damage = baseDamage;

        // Apply AttackData scaling
        damage *= attackData.damageMultiplier;

        // Critical scaling from AttackData
        if (isCritical)
        {
            damage *= attackData.critMultiplier;
            OnCriticalHit?.Invoke(); // Notify listeners of a crit
        }

        // Apply tool reduction if invalid
        if (!isToolValid)
        {
            float reduction = isCritical ? selectedItem.critFactor / reductionFactor : 1 / reductionFactor;
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

        if (selectedItem.toolType == ToolType.None || hitObject.objectType == BreakableObject.ObjectType.None)
            return true;

        return toolToObjectMap.TryGetValue(selectedItem.toolType, out HashSet<BreakableObject.ObjectType> breakableTypes)
            && breakableTypes.Contains(hitObject.objectType);
    }

    public float TotalDamageMultiplier
    {
        get
        {
            if (damageMultiplierSources.Count == 0) return baseDamageMultiplier;

            // Multiply all bonuses together
            float total = baseDamageMultiplier;
            foreach (float mult in damageMultiplierSources.Values)
                total *= mult;

            return total;
        }
    }

    public void SetDamageMultiplierSource(object source, float multiplier)
    {
        if (source == null) return;
        damageMultiplierSources[source] = multiplier;
    }

    public void RemoveDamageMultiplierSource(object source)
    {
        if (source == null) return;
        damageMultiplierSources.Remove(source);
    }
}
