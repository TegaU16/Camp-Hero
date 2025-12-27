using System.Collections.Generic;
using System.Linq;
using Game.Crafting;
using Game.Players;
using Game.Registries;
using Game.Saving;
using Game.Smelting;
using Game.Storage;
using Game.Tutorial;
using UnityEngine;

namespace Game.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;
        public static RectTransform InventoryUI { get; private set; }

        [SerializeField]
        private RectTransform inventoryUI;

        public Item[] startItems;

        public List<Item> discoveredItems = new();

        public const int numHotbarSlots = 7;

        public InventoryUIHandler inventoryUIHandler;

        public List<GameObject> inventoryExtensions;
        public GameObject deleteSlot;

        public GameObject inventoryItemPrefab;
        private GameObject playerObject;

        int selectedSlot = 0;

        [Header("Menus")]
        public GameObject mainInventory;
        public GameObject craftingMenuUI;
        public GameObject storageMenuUI;
        public GameObject furnaceMenuUI;
        public GameObject campfireMenuUI;
        public GameObject trialMenuUI;

        public GameObject darkBackground;

        [Header("Keys")]
        public KeyCode inventoryToggleKey = KeyCode.Tab;
        public KeyCode itemDropKey = KeyCode.Q;
        public KeyCode itemStackDropKey = KeyCode.LeftControl;
        public KeyCode exitExtensionKey = KeyCode.Escape;

        [HideInInspector] public StorageUnit activeChest;
        [HideInInspector] public FurnaceUnit activeFurnace;

        public bool JustClosedExtension { get; set; }
        public int LastRemainingCount { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            InventoryUI = inventoryUI;
        }

        private void Start()
        {
            foreach (Item item in startItems)
                AddItem(item);

            ChangeSelectedSlot(0);

            ItemEquip.Instance.EquipItem(GetSelectedItem(delete: false));
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;

            HandleSlotSelection();
            HandleInventoryToggle();
            HandleItemDropping();
            HandleExtensionExit();
        }

        void LateUpdate()
        {
            JustClosedExtension = false;
        }

        public void AddSavedPlayerItems(List<ItemData> items)
        {
            foreach (ItemData itemData in items)
            {
                Item item = ItemRegistry.GetItemByName(itemData.itemName);
                AddItem(item, itemData.count, itemData.position);
            }
        }

        public InventorySaveData GetSavedInventoryData()
        {
            List<ItemData> items = new();

            for (int i = 0; i < inventoryUIHandler.inventorySlots.Count; i++)
            {
                InventorySlot currentSlot = inventoryUIHandler.inventorySlots[i];
                if (currentSlot == null || currentSlot.transform.childCount <= 0) continue;

                InventoryItem invItem = currentSlot.GetComponentInChildren<InventoryItem>();
                Item item = invItem.item;

                ItemData itemData = new()
                {
                    itemName = item.name,
                    count = invItem.count,
                    position = i
                };

                items.Add(itemData);
            }

            List<string> discoveredItemNames = discoveredItems.Where(x => x != null).Select(x => x.itemName).ToList();

            InventorySaveData inventorySaveData = new()
            {
                savedItems = items,
                discoveredItems = discoveredItemNames
            };

            return inventorySaveData;
        }

        private void ChangeSelectedSlot(int newValue)
        {
            if (selectedSlot >= 0)
            {
                InventorySlot selected = inventoryUIHandler.inventorySlots[selectedSlot];
                if (selected.TryGetComponent(out SelectableImage selectable))
                    selectable.Deselect();
            }

            InventorySlot newSlot = inventoryUIHandler.inventorySlots[newValue];
            if (newSlot.TryGetComponent(out SelectableImage selectableImage))
                selectableImage.Select();

            selectedSlot = newValue;

            EquipSelectedItem();
        }

        public void EquipSelectedItem()
        {
            Item selectedItem = GetSelectedItem(delete: false);

            if (selectedItem != null && selectedItem.equippedPrefab != null)
                ItemEquip.Instance.EquipItem(selectedItem);
            else
                ItemEquip.Instance.EquipItem(null);
        }

        public void UseSelectedItem()
        {
            Item selectedItem = GetSelectedItem(delete: false);
            if (selectedItem == null)
            {
                ItemEquip.Instance.EquipItem(null);
                return;
            }

            if (selectedItem.actionType == ActionType.None) return;

            selectedItem = GetSelectedItem(delete: true);

            // Check if the item count is zero after using it
            InventoryItem itemInSlot = inventoryUIHandler.inventorySlots[selectedSlot].GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null || itemInSlot.count <= 0)
                ItemEquip.Instance.EquipItem(null);  // Unequip the item if it's no longer in the inventory
            else if (selectedItem.equippedPrefab != null)
                ItemEquip.Instance.EquipItem(selectedItem);
        }

        public bool AddItem(Item item, int count = 1, int? targetSlotIndex = null)
        {
            int initialCount = count; // Store the initial count for later comparison

            if (targetSlotIndex.HasValue) // Case 1: Add to a specific slot
            {
                int index = targetSlotIndex.Value;

                if (index < 0 || index >= inventoryUIHandler.inventorySlots.Count)
                {
                    Debug.LogWarning("Invalid slot index!");
                    return false;
                }

                InventorySlot slot = inventoryUIHandler.inventorySlots[index];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot != null)
                {
                    if (itemInSlot.item == item)
                    {
                        int availableSpace = item.maxStack - itemInSlot.count;
                        int itemsToAdd = Mathf.Min(count, availableSpace);
                        itemInSlot.count += itemsToAdd;
                        count -= itemsToAdd;
                        itemInSlot.RefreshCount();
                    }
                    else
                    {
                        Debug.LogWarning("Slot already contains a different item.");
                        return false;
                    }
                }
                else
                {
                    int itemsToPlace = Mathf.Min(count, item.maxStack);
                    SpawnNewItem(item, slot, itemsToPlace);
                    count -= itemsToPlace;
                }
            }
            else // Case 2: Prioritize stacking, then fill empty slots
            {
                // Pass 1: Try stacking
                for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && count > 0; i++)
                {
                    InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                    InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                    if (itemInSlot == null || itemInSlot.item != item || itemInSlot.count >= item.maxStack) continue;

                    int availableSpace = item.maxStack - itemInSlot.count;
                    int itemsToAdd = Mathf.Min(count, availableSpace);

                    itemInSlot.count += itemsToAdd;
                    count -= itemsToAdd;

                    itemInSlot.RefreshCount();
                }

                // Pass 2: If any left, put in empty slots
                for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && count > 0; i++)
                {
                    InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                    InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                    if (itemInSlot != null) continue;

                    int itemsToPlace = Mathf.Min(count, item.maxStack);
                    SpawnNewItem(item, slot, itemsToPlace);
                    count -= itemsToPlace;
                }
            }

            int addedCount = initialCount - count;
            LastRemainingCount = count;

            if (addedCount > 0)
                PickupNotificationManager.Instance.ShowPickup(item, addedCount);

            // Return true if all items were successfully added, otherwise false
            return count == 0;
        }

        public void TryDiscoverItem(Item item)
        {
            if (discoveredItems.Contains(item)) return;

            discoveredItems.Add(item);
            CraftingManager.Instance.TryUnlockRecipes(discoveredItems);
            FurnaceManager.Instance.TryUnlockRecipes(discoveredItems);
        }

        public void LoadDiscoveredItems(List<string> itemNames)
        {
            List<Item> items = ItemRegistry.GetItemsByName(itemNames);
            foreach (Item item in items)
                TryDiscoverItem(item);
        }

        public void DropSelectedItem()
        {
            Item selectedItem = GetSelectedItem(delete: true);
            DropItem(selectedItem, 1);
        }

        public void DropSelectedStack()
        {
            Item selectedItem = GetSelectedItem(delete: false);
            InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null) return;

            DropItem(selectedItem, itemInSlot.count);
            itemInSlot.count = 0;
            GetSelectedItem(delete: false);
        }

        public void DropAllItems()
        {
            int numSlots = inventoryUIHandler.inventorySlots.Count;

            for (int i = 0; i < numSlots; i++)
            {
                InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot == null) continue;

                DropItem(itemInSlot.item, itemInSlot.count, scatter: true);
                itemInSlot.count = 0;
                Destroy(itemInSlot.gameObject);
            }

            EquipSelectedItem();
        }

        public void DropItem(Item item, int count, bool scatter = false)
        {
            if (item == null || item.itemDrop == null) return;

            Vector3 pos = playerObject.transform.position + Vector3.up * 1.5f;

            float scatterDistance = 0.5f;
            float scatterForce = 2f;

            ItemSpawner.Spawn(item, pos, count, torsoBone: null, scatter, scatterDistance, scatterForce);

            EquipSelectedItem();
        }

        public void SpawnNewItem(Item item, InventorySlot slot, int count = 1)
        {
            GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
            InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
            inventoryItem.SetItem(item, count);  // Set the item and initialize it

            SetTutorialForItem(slot, item);
        }

        public void SetTutorialForItem(InventorySlot slot, Item item)
        {
            if (slot == null ||
                item == null ||
                discoveredItems.Contains(item) ||
                !inventoryUIHandler.inventorySlots.Contains(slot)) return;

            List<TutorialData> craftingTutorials = TutorialManager.Instance.CreatePickupCraftingRecipeTutorials(item);
            if (craftingTutorials != null)
            {
                foreach (TutorialData tutorialData in craftingTutorials)
                {
                    if (tutorialData != null)
                        TutorialManager.Instance.RegisterTutorial(tutorialData);
                }
            }

            List<TutorialData> smeltingTutorials = TutorialManager.Instance.CreatePickupSmeltingRecipeTutorials(item);
            if (smeltingTutorials != null)
            {
                foreach (TutorialData tutorialData in smeltingTutorials)
                {
                    if (tutorialData != null)
                        TutorialManager.Instance.RegisterTutorial(tutorialData);
                }
            }

            TryDiscoverItem(item);
        }

        public Item GetSelectedItem(bool delete)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null) return null;

            if (itemInSlot.count <= 0)
            {
                Destroy(itemInSlot.gameObject);  // Remove the empty item from the slot
                return null;
            }

            if (delete)
            {
                itemInSlot.count--;

                if (itemInSlot.count <= 0)
                    Destroy(itemInSlot.gameObject);
                else
                    itemInSlot.RefreshCount();
            }

            return itemInSlot.item;
        }

        public InventoryItem GetSelectedInventoryItem()
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            return itemInSlot;
        }

        public bool HasItem(Item item)
        {
            foreach (InventorySlot slot in inventoryUIHandler.inventorySlots)
            {
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot != null && itemInSlot.item == item) return true;
            }

            return false;
        }

        public void ConsumeItem(Item item, int consumeCount = 1)
        {
            foreach (InventorySlot slot in inventoryUIHandler.inventorySlots)
            {
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot == null || itemInSlot.item != item) continue;

                if (itemInSlot.count <= consumeCount)
                {
                    consumeCount -= itemInSlot.count;
                    Destroy(itemInSlot.gameObject);
                }
                else
                {
                    itemInSlot.count -= consumeCount;
                    itemInSlot.RefreshCount();
                    break;
                }

                if (consumeCount <= 0) break;
            }

            ItemEquip.Instance.EquipItem(GetSelectedItem(delete: false));
        }

        private void HandleSlotSelection()
        {
            if (IsExtensionOpen()) return;

            // Handle number keys
            for (int i = 1; i <= numHotbarSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
                {
                    ChangeSelectedSlot(i - 1);
                    return;
                }
            }

            // Handle scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                int nextSlot = (selectedSlot + 1) % numHotbarSlots;
                ChangeSelectedSlot(nextSlot);
            }
            else if (scroll < 0f)
            {
                int prevSlot = (selectedSlot - 1 + numHotbarSlots) % numHotbarSlots;
                ChangeSelectedSlot(prevSlot);
            }
        }

        private void HandleInventoryToggle()
        {
            if (!Input.GetKeyDown(inventoryToggleKey)) return;

            if (IsExtensionOpen())
                ResetExtensions();
            else if (CraftingManager.Instance != null)
                CraftingManager.Instance.craftingUI.ToggleCraftingMenu(CraftingSource.Base);
        }

        private void HandleItemDropping()
        {
            if (!Input.GetKeyDown(itemDropKey) || IsExtensionOpen()) return;

            if (Input.GetKey(itemStackDropKey))
                DropSelectedStack();
            else
                DropSelectedItem();
        }

        private void HandleExtensionExit()
        {
            if (IsExtensionOpen() && Input.GetKeyDown(exitExtensionKey))
                ResetExtensions();
        }

        public bool IsExtensionOpen()
        {
            for (int i = 0; i < inventoryExtensions.Count; i++)
            {
                if (inventoryExtensions[i] != null && inventoryExtensions[i].activeSelf) return true;
            }

            return false;
        }

        public void OnInventoryOpen()
        {
            if (deleteSlot != null)
                deleteSlot.SetActive(true);

            if (darkBackground != null)
                darkBackground.SetActive(true);

            CameraControlToggle.Instance.SetCameraControl(false);
        }

        public void ResetExtensions()
        {
            if (darkBackground != null)
                darkBackground.SetActive(false);

            StorageUI storageUI = storageMenuUI.GetComponent<StorageUI>();
            if (storageUI != null && storageUI.gameObject.activeSelf)
                storageUI.Close();

            FurnaceUI furnaceUI = furnaceMenuUI.GetComponent<FurnaceUI>();
            if (furnaceUI != null && furnaceUI.gameObject.activeSelf)
                furnaceUI.Close();

            if (campfireMenuUI != null && campfireMenuUI.activeSelf)
                PlayerStatsManager.Instance.CloseStatsMenu();

            for (int i = 0; i < inventoryExtensions.Count; i++)
            {
                if (inventoryExtensions[i] != null)
                    inventoryExtensions[i].SetActive(false);
            }

            deleteSlot.SetActive(false);
            JustClosedExtension = true;
            CameraControlToggle.Instance.SetCameraControl(true);

            ItemTooltipUI.Instance.HideTooltip();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public bool IsInventoryFullForItem(Item item, int count = 1)
        {
            int remaining = count;

            // First, check space in existing stacks
            for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && remaining > 0; i++)
            {
                InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot == null || itemInSlot.item != item || itemInSlot.count >= item.maxStack) continue;

                int space = item.maxStack - itemInSlot.count;
                remaining -= Mathf.Min(remaining, space);
            }

            // Then, check for empty slots
            for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && remaining > 0; i++)
            {
                InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot != null) continue;

                int space = item.maxStack;
                remaining -= Mathf.Min(remaining, space);
            }

            return remaining > 0; // If there's still remaining, inventory is full
        }

        public void SetPlayer(GameObject playerObject)
        {
            if (playerObject != null)
                this.playerObject = playerObject;
            else
                Debug.LogWarning("Player is null!");
        }
    }
}
