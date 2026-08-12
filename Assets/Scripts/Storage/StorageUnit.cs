using UnityEngine;
using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using System.Linq;

namespace Game.Storage
{
    public class StorageUnit : StorageContainer, IInteractable, ISaveableObject
    {
        [SerializeField] private string chestName = "Storage Chest";
        public int maxSlots = 16;

        [HideInInspector] public List<InventorySlot> inventorySlots = new();

        public Sprite storageUnitIcon;
        public Sprite ObjectIcon => storageUnitIcon;

        private void Awake()
        {
            items = new ItemData[maxSlots];
        }

        public void Interact()
        {
            if (!InventoryManager.Instance.storageMenuUI.TryGetComponent(out StorageUI storageUI)) return;

            storageUI.Open(this);
        }

        public string GetInteractText() => $"Open {chestName}\n<color=#27ef60>\"E\"</color>";

        public Transform GetTransform() => transform;

        public string SaveState()
        {
            StorageSaveData data = new()
            {
                storedItems = items.ToList()
            };

            return JsonUtility.ToJson(data);
        }

        public void LoadState(string json)
        {
            StorageSaveData data = JsonUtility.FromJson<StorageSaveData>(json);
            if (data == null) return;

            items = data.storedItems.ToArray();
        }
    }
}
