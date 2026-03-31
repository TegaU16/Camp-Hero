using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using UnityEngine;

namespace Game.Storage
{
    [System.Serializable]
    public class LootEntry
    {
        public Item item;
        public int minCount = 1;
        public int maxCount = 1;
    }

    [CreateAssetMenu(menuName = "Loot/LootTable")]
    public class LootTable : ScriptableObject
    {
        public WeightedTable<LootEntry> weightedTable = new();
        [SerializeField] private int minRolls = 6;
        [SerializeField] private int maxRolls = 8;

        public List<ItemData> GetRandomLoot()
        {
            int rolls = Random.Range(minRolls, maxRolls + 1);
            List<ItemData> loot = new();

            for (int i = 0; i < rolls; i++)
            {
                LootEntry entry = weightedTable.Roll();
                if (entry == null || entry.item == null) continue;

                ToolAttributeProbabilityTable table = InventoryManager.Instance.toolAttributeTable;
                ToolAttribute toolAttribute = table.GetRandomToolAttribute(entry.item.toolType);

                string attributeID = toolAttribute == null ? "" : toolAttribute.attributeID;

                loot.Add(new ItemData
                {
                    itemName = entry.item.itemName,
                    count = Random.Range(entry.minCount, entry.maxCount + 1),
                    toolAttribute = attributeID
                });
            }

            return loot;
        }
    }
}
