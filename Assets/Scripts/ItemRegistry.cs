using System.Collections.Generic;
using UnityEngine;

public class ItemRegistry : MonoBehaviour
{
    public Item[] allItems;

    private static Dictionary<string, Item> itemDict;

    private void Awake()
    {
        if (itemDict != null && itemDict.Count > 0) return;

        itemDict = new Dictionary<string, Item>();

        foreach (var item in allItems)
        {
            if (item != null && !itemDict.ContainsKey(item.name))
            {
                itemDict[item.name] = item;
            }
        }
    }

    public static Item GetItemByName(string name)
    {
        if (itemDict != null && itemDict.TryGetValue(name, out Item item))
            return item;

        Debug.LogWarning($"Item not found: {name}");
        return null;
    }
}
