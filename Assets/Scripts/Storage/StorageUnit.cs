using UnityEngine;
using System.Collections.Generic;
using Game.Inventory;
using Game.Registries;

namespace Game.Storage
{
    [System.Serializable]
    public class StoredItem
    {
        [System.NonSerialized] public Item item;
        public string itemName;
        public int count;

        public void SyncNameFromItem()
        {
            if (item != null)
                itemName = item.name;
        }

        public void ResolveItemFromName()
        {
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogWarning("StoredItem has no itemName to resolve.");
                return;
            }

            item = ItemRegistry.GetItemByName(itemName);
        }
    }

    public class StorageUnit : MonoBehaviour, IInteractable, ISaveableObject
    {
        [SerializeField] private string chestName = "Storage Chest";
        public int maxSlots = 16;
        [HideInInspector] public bool isOpen = false;

        [HideInInspector] public List<StoredItem> items = new();
        [HideInInspector] public List<InventorySlot> inventorySlots = new();

        public void Interact()
        {
            StorageUI storageUI = InventoryManager.Instance.storageMenuUI.GetComponent<StorageUI>();

            if (!isOpen)
            {
                storageUI.Open(this);
                isOpen = true;
                inventorySlots = storageUI.GetInventorySlots();
                InventoryManager.Instance.mainInventory.SetActive(true);
            }
            else
            {
                storageUI.Close();
                isOpen = false;
                inventorySlots = null;
                InventoryManager.Instance.mainInventory.SetActive(false);
            }
        }

        public string GetInteractText() => $"Open {chestName}";

        public Transform GetTransform() => transform;

        public string SaveState()
        {
            foreach (StoredItem stored in items)
                stored.SyncNameFromItem();

            string json = JsonUtility.ToJson(this);
            return json;
        }

        public void LoadState(string json)
        {
            items = new List<StoredItem>();
            JsonUtility.FromJsonOverwrite(json, this);

            foreach (StoredItem stored in items)
                stored.ResolveItemFromName();
        }
    }
}
