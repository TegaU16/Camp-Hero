using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootEntry
{
    public Item item;
    public int minCount = 1;
    public int maxCount = 1;
    public float probability = 1f; // Chance to appear in loot
}

[CreateAssetMenu(menuName = "Loot/LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> lootEntries = new();

    public List<StoredItem> GetRandomLoot()
    {
        List<StoredItem> loot = new();

        foreach (LootEntry entry in lootEntries)
        {
            if (Random.value <= entry.probability)
            {
                StoredItem storedItem = new()
                {
                    item = entry.item,
                    count = Random.Range(entry.minCount, entry.maxCount + 1)
                };
                storedItem.SyncNameFromItem();
                loot.Add(storedItem);
            }
        }

        return loot;
    }
}
