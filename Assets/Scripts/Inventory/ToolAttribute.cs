using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(menuName = "Item/Tool Attribute")]
    public class ToolAttribute : ScriptableObject
    {
        public string attributeID;
        public string attributeName;
        public ToolType toolType;

        [Header("Multipliers")]
        public float damageMult;
        public float attackSpeedMult;
        public float knockbackForceMult;
        public float poiseDamageMult;
        public float critChanceMult;
        public float critFactorMult;
        public float resourceDropMult;

        private IEnumerable<(string, float)> Multipliers
        {
            get
            {
                yield return ("DMG", damageMult);
                yield return ("ATK SPD", attackSpeedMult);
                yield return ("KB FORCE", knockbackForceMult);
                yield return ("POISE DMG", poiseDamageMult);
                yield return ("CRIT %", critChanceMult);
                yield return ("CRIT DMG", critFactorMult);
                yield return ("DROP", resourceDropMult);
            }
        }

        public string GetAttributesText()
        {
            string attributeTexts = "";
            foreach ((string name, float value) in Multipliers)
            {
                if (value <= 0) continue;

                InventoryManager inventoryManager = InventoryManager.Instance;
                Color attributeColor = value > 1
                    ? inventoryManager.positiveAttributeColor
                    : inventoryManager.negativeAttributeColor;

                string attributeText = $"\n<color=#{attributeColor.ToHexString()}>{name}: {value}x</color>";
                attributeTexts += attributeText;
            }

            return attributeName + attributeTexts;
        }
    }
}
