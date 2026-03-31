using UnityEngine;
using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using Game.Registries;

namespace Game.Storage
{
    public class StorageUI : MonoBehaviour
    {
        public static StorageUI Instance;

        public GameObject slotPrefab;
        public Transform slotParent;

        private StorageUnit linkedStorage;
        private readonly List<InventorySlot> slotInstances = new();

        private bool isOpen;
        private float menuHeight;
        public float slotHeight = 36f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Open(StorageUnit storage)
        {
            if (isOpen) return;

            isOpen = true;
            linkedStorage = storage;

            if (linkedStorage.TryGetComponent(out Animator animator))
                animator.SetTrigger("Open");

            ClearSlots();

            InventoryManager.Instance.mainInventory.SetActive(true);
            InventoryManager.Instance.OnInventoryOpen();
            BuildSlots();

            linkedStorage.inventorySlots = slotInstances;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            SaveItemsToStorage();
            ClearSlots();

            if (linkedStorage.TryGetComponent(out Animator animator))
                animator.SetTrigger("Close");

            InventoryManager.Instance.mainInventory.SetActive(false);
            InventoryManager.Instance.darkBackground.SetActive(false);

            linkedStorage.inventorySlots = null;
            linkedStorage = null;
            gameObject.SetActive(false);
        }

        private void BuildSlots()
        {
            for (int i = 0; i < linkedStorage.maxSlots; i++)
            {
                GameObject slot = Instantiate(slotPrefab, slotParent);
                if (!slot.TryGetComponent(out InventorySlot inventorySlot)) continue;

                slotInstances.Add(inventorySlot);

                if (i >= linkedStorage.items.Length) continue;

                ItemData storedItem = linkedStorage.items[i];
                if (storedItem == null) continue;

                Item baseItem = ItemRegistry.GetItemByName(storedItem.itemName);
                if (baseItem == null) continue;

                Item item = baseItem.maxStack > 1 ? baseItem : Instantiate(baseItem);
                ToolAttribute toolAttribute = ToolAtributeRegistry.GetToolAttributeByName(storedItem.toolAttribute);
                item.toolAttribute = toolAttribute;

                InventoryManager.Instance.SpawnNewItem(item, inventorySlot, storedItem.count);
            }

            // Resize the menu based on number of slots
            int slotsPerRow = InventoryManager.numHotbarSlots;
            int modulo = linkedStorage.maxSlots % slotsPerRow != 0 ? 1 : 0;
            int numRows = Mathf.FloorToInt(linkedStorage.maxSlots / slotsPerRow) + modulo;

            float spacing = 12f;
            menuHeight = spacing + slotHeight * numRows;

            if (!TryGetComponent(out RectTransform rectTransform)) return;

            Vector2 currentSize = rectTransform.sizeDelta;
            rectTransform.sizeDelta = new Vector2(currentSize.x, menuHeight);

            float baseY = -142f;
            Vector2 anchoredPos = rectTransform.anchoredPosition;
            anchoredPos.y = numRows * slotHeight / 2f + baseY;

            rectTransform.anchoredPosition = anchoredPos;
        }

        private void ClearSlots()
        {
            foreach (InventorySlot slot in slotInstances)
                Destroy(slot.gameObject);

            slotInstances.Clear();
        }

        private void SaveItemsToStorage()
        {
            if (linkedStorage == null)
            {
                Debug.LogError("SaveItemsToStorage: linkedStorage is null!");
                return;
            }

            int itemCount = linkedStorage.items.Length;
            linkedStorage.items = new ItemData[itemCount];

            for (int i = 0; i < itemCount; i++)
            {
                InventorySlot slot = slotInstances[i];
                if (slot == null) return;

                InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();
                if (inventoryItem == null || inventoryItem.item == null) continue;

                ItemData stored = new()
                {
                    itemName = inventoryItem.item.itemName,
                    count = Mathf.Max(1, inventoryItem.count)
                };

                linkedStorage.items[i] = stored;
            }
        }
    }
}
