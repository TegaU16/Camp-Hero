using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using Game.Terrain;
using NUnit.Framework.Interfaces;
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

        public ItemData[] GetRandomLoot(Vector3 position, int storageSlots)
        {
            List<ItemData> generatedItems = new();
            ItemData[] loot = new ItemData[storageSlots];

            int baseSeed = position.GetHashCode() ^ VoxelGrid.Instance.seed;
            System.Random rng = new(baseSeed);

            int rolls = rng.Next(minRolls, maxRolls + 1);

            // Step 1: Generate loot FIRST (without positions)
            for (int i = 0; i < rolls; i++)
            {
                int seed = baseSeed ^ (i * 73856093);

                LootEntry entry = weightedTable.Roll(seed);
                if (entry == null || entry.item == null) continue;

                ToolAttributeProbabilityTable table = InventoryManager.Instance.toolAttributeTable;
                ToolAttribute toolAttribute = table.GetRandomToolAttribute(entry.item.toolType);

                string attributeID = toolAttribute == null ? "" : toolAttribute.attributeID;

                generatedItems.Add(new ItemData
                {
                    itemName = entry.item.itemName,
                    count = rng.Next(entry.minCount, entry.maxCount + 1),
                    toolAttribute = attributeID
                });
            }

            // Shuffle slots
            List<int> availableSlots = new();
            for (int i = 0; i < storageSlots; i++)
                availableSlots.Add(i);

            for (int i = availableSlots.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (availableSlots[i], availableSlots[j]) = (availableSlots[j], availableSlots[i]);
            }

            // Place items INTO correct slots
            for (int i = 0; i < generatedItems.Count && i < availableSlots.Count; i++)
            {
                int slotIndex = availableSlots[i];
                ItemData item = generatedItems[i];

                item.position = slotIndex; // optional but good for debugging
                loot[slotIndex] = item;
            }

            return loot;
        }
    }
}
