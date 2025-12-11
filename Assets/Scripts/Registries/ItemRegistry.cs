using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Registries
{
    public class ItemRegistry : MonoBehaviour
    {
        public Item[] allItems;

        private static Dictionary<string, Item> itemDict;

        private void Awake()
        {
            if (itemDict != null && itemDict.Count > 0) return;

            itemDict = new Dictionary<string, Item>();

            foreach (Item item in allItems)
            {
                if (item != null && !itemDict.ContainsKey(item.name))
                    itemDict[item.name] = item;
            }
        }

        public static Item GetItemByName(string name)
        {
            if (itemDict != null && itemDict.TryGetValue(name, out Item item)) return item;

            Debug.LogWarning($"Item not found: {name}");
            return null;
        }

        public static List<Item> GetItemsByName(List<string> names)
        {
            List<Item> items = new();
            foreach (string name in names)
            {
                Item item = GetItemByName(name);
                if (item != null)
                    items.Add(item);
            }

            return items;
        }
    }
}
