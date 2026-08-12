using UnityEngine;
using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using Game.Registries;

namespace Game.Storage
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class StorageUI : MonoBehaviour
    {
        public static StorageUI Instance;

        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotParent;

        private StorageUnit linkedStorage;
        private readonly List<InventorySlot> slotInstances = new();

        private bool isOpen;
        private float menuHeight;
        private readonly float slotHeight = 36f;

        private CanvasGroup canvasGroup;
        private RectTransform storageRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();
            storageRect = GetComponent<RectTransform>();
        }

        public void Open(StorageUnit storage)
        {
            if (isOpen) return;

            isOpen = true;
            linkedStorage = storage;

            if (linkedStorage is Chest chest)
                chest.ToggleChest();
            
            BuildSlots();
            InventoryManager.Instance.OpenInventory();

            linkedStorage.inventorySlots = slotInstances;
            UITween.DefaultOpenMenu(canvasGroup, storageRect);
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            SaveItemsToStorage();
            ClearSlots();

            if (linkedStorage is Chest chest)
                chest.ToggleChest();

            linkedStorage.inventorySlots = null;
            linkedStorage = null;
        }

        private void BuildSlots()
        {
            ClearSlots();

            for (int i = 0; i < linkedStorage.maxSlots; i++)
            {
                GameObject slot = Instantiate(slotPrefab, slotParent);
                if (!slot.TryGetComponent(out InventorySlot inventorySlot)) continue;

                slotInstances.Add(inventorySlot);

                if (i >= linkedStorage.items.Length) continue;

                ItemData storedItem = linkedStorage.items[i];
                if (storedItem == null) continue;

                Item baseItem = ItemRegistry.Instance.GetByKey(storedItem.itemName);
                if (baseItem == null) continue;

                Item item = baseItem.maxStack > 1 ? baseItem : Instantiate(baseItem);
                if (storedItem.toolAttribute != null)
                {
                    ToolAttribute toolAttribute = ToolAttributeRegistry.Instance.GetByKey(storedItem.toolAttribute);
                    item.toolAttribute = toolAttribute;
                }
                
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

            int slotCount = linkedStorage.maxSlots;
            linkedStorage.items = new ItemData[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                InventorySlot slot = slotInstances[i];
                if (slot == null) return;

                InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();
                if (inventoryItem == null || inventoryItem.item == null) continue;

                bool hasToolAttribute = inventoryItem.item.toolAttribute != null;

                ItemData stored = new()
                {
                    itemName = inventoryItem.item.itemName,
                    count = Mathf.Max(1, inventoryItem.count),
                    toolAttribute =  hasToolAttribute ? inventoryItem.item.toolAttribute.attributeID : "",
                    position = i
                };

                linkedStorage.items[i] = stored;
            }
        }
    }
}
