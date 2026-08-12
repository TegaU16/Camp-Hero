using Game.Inventory;
using Game.Registries;
using Game.Saving;
using UnityEngine;

namespace Game.Storage
{
    public abstract class StorageContainer : MonoBehaviour
    {
        [HideInInspector] public ItemData[] items;

        protected Item GetItemFromSlot(int index)
        {
            if (index < 0 || index >= items.Length) return null;
            if (items[index] == null || string.IsNullOrEmpty(items[index].itemName)) return null;

            return ItemRegistry.Instance.GetByKey(items[index].itemName);
        }

        protected void SaveSlot(InventorySlot slot, int index)
        {
            InventoryItem invItem = slot.GetComponentInChildren<InventoryItem>();

            if (invItem == null || invItem.item == null)
            {
                items[index] = null;
                return;
            }

            items[index] ??= new ItemData();

            items[index].itemName = invItem.item.itemName;
            items[index].count = invItem.count;
            items[index].position = index;

            if (invItem.item.toolAttribute != null)
                items[index].toolAttribute = invItem.item.toolAttribute.attributeID;
        }

        protected void RefreshSlot(InventorySlot slot, ItemData data)
        {
            if (slot == null) return;

            InventoryItem existing = slot.GetComponentInChildren<InventoryItem>();

            if (data == null)
            {
                if (existing != null)
                    Destroy(existing.gameObject);
                return;
            }

            Item item = ItemRegistry.Instance.GetByKey(data.itemName);
            if (item == null) return;

            if (existing != null && existing.item == item)
            {
                existing.count = data.count;
                StartCoroutine(existing.RefreshCount());
                return;
            }

            slot.ClearSlot();
            InventoryManager.Instance.SpawnNewItem(item, slot, data.count);

            InventoryItem newItem = slot.GetComponentInChildren<InventoryItem>();
            if (newItem != null)
            {
                newItem.count = data.count;
                StartCoroutine(newItem.RefreshCount());
            }
        }
    }
}