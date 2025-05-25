using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [HideInInspector] public bool isCritical = false;
    public AttackHitbox playerAttackHitbox;
    private const float reductionFactor = 2f;

    private static readonly Dictionary<ToolType, HashSet<BreakableObject.ObjectType>> toolToObjectMap = new()
    {
        { ToolType.Axe, new() { BreakableObject.ObjectType.Wood } },
        { ToolType.Pickaxe, new() { BreakableObject.ObjectType.Stone } },
        { ToolType.Sword, new() { BreakableObject.ObjectType.Flesh } }
    };

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        AttackHitbox hitbox = playerAttackHitbox;
        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);

        if (hitbox != null && selectedItem != null)
        {
            if (hitbox.TryGetComponent(out BoxCollider box))
            {
                float attackDistance = selectedItem.attackDistance;

                box.size = new Vector3(0.2f, 0.2f, attackDistance);
                box.center = new Vector3(0f, 0f, -attackDistance / 2f);
            }

            hitbox.ClearHits();
            hitbox.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.3f);
            hitbox.gameObject.SetActive(true);
        }
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

        bool isCrit = Random.Range(0, 100) < selectedItem.critChance;

        bool isTypeMatched = IsTypeMatched(hitObject, selectedItem);
        bool isToolLevelSufficient = selectedItem.toolLevel >= hitObject.objectLevel;

        return CalculateDamage(damage, isCrit, isTypeMatched && isToolLevelSufficient, selectedItem);
    }

    private int CalculateDamage(float damage, bool isCrit, bool isToolValid, Item selectedItem)
    {
        if (isToolValid)
        {
            isCritical = isCrit;
            return isCrit ? (int)(damage * selectedItem.critFactor) : (int)damage;
        }
        else
        {
            isCritical = isCrit;
            float damageModifier = isCrit ? selectedItem.critFactor / (reductionFactor * 2) : 1 / (reductionFactor * 2);
            return (int)(damage * damageModifier);
        }
    }

    public bool IsTypeMatched(BreakableObject hitObject, Item selectedItem)
    {
        if (selectedItem == null) return false;

        if (selectedItem.toolType == ToolType.None || hitObject.type == BreakableObject.ObjectType.None)
            return true;

        return toolToObjectMap.TryGetValue(selectedItem.toolType, out var breakableTypes)
            && breakableTypes.Contains(hitObject.type);
    }
}
