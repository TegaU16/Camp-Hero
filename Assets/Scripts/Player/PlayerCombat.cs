using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboResetTime = 1f;
    public int maxCombo = 3;

    private int comboStep = 0;
    private float lastAttackTime;
    private bool canChain = false;

    private Animator animator;
    private Player player;

    [HideInInspector] public float damageMultiplier;
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

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleComboAttack();
        }

        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            ResetCombo();
        }
    }

    private void HandleComboAttack()
    {
        if (InventoryManager.Instance.IsExtensionOpen()) return;

        if (GameManager.Instance.isPaused) return;

        if (canChain || comboStep == 0)
        {
            comboStep++;
            if (comboStep > maxCombo) comboStep = 1;

            AttackHitbox hitbox = playerAttackHitbox;
            Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);

            if (hitbox != null && selectedItem != null)
            {
                if (hitbox.TryGetComponent(out BoxCollider box))
                {
                    float attackDistance = selectedItem.attackDistance;

                    box.size = new Vector3(0.5f, 1f, attackDistance);
                    box.center = new Vector3(0f, box.size.y / 2f, attackDistance / 2f);
                }
            }

            player.FreezeMovement();
            animator.SetTrigger("Attack");
            animator.SetInteger("Combo Step", comboStep);
            lastAttackTime = Time.time;

            canChain = false; // wait for animation to re-enable this
        }
    }

    // Called via animation event during each attack animation
    void EnableNextComboWindow()
    {
        canChain = true;
    }

    // Called via animation event at the end of the final attack
    void EndCombo()
    {
        ResetCombo();
    }

    private void ResetCombo()
    {
        player.ResetMovement();
        comboStep = 0;
        animator.SetInteger("Combo Step", 0);
        canChain = false;
    }

    public int ItemDamage(BreakableObject hitObject, Item selectedItem)
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

        int damageResult = CalculateDamage(damage, isCrit, isTypeMatched && isToolLevelSufficient, selectedItem);

        return hitObject.PlacedByPlayer ? Mathf.Min(10, damageResult) : damageResult;
    }

    private int CalculateDamage(float damage, bool isCrit, bool isToolValid, Item selectedItem)
    {
        isCritical = isCrit;

        if (isToolValid)
        {
            return isCrit ? (int)(damage * selectedItem.critFactor * damageMultiplier) : (int)(damage * damageMultiplier);
        }
        else
        {
            float damageReduction = isCrit ? selectedItem.critFactor / reductionFactor : 1 / reductionFactor;
            return (int)(damage * damageReduction * damageMultiplier);
        }
    }

    public bool IsTypeMatched(BreakableObject hitObject, Item selectedItem)
    {
        if (selectedItem == null) return false;

        if (selectedItem.toolType == ToolType.None || hitObject.objectType == BreakableObject.ObjectType.None)
            return true;

        return toolToObjectMap.TryGetValue(selectedItem.toolType, out HashSet<BreakableObject.ObjectType> breakableTypes)
            && breakableTypes.Contains(hitObject.objectType);
    }
}
