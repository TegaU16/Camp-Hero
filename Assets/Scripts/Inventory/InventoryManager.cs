using System;
using System.Collections.Generic;
using System.Linq;
using Game.Crafting;
using Game.Players;
using Game.Reforge;
using Game.Registries;
using Game.Saving;
using Game.Smelting;
using Game.Storage;
using Game.Terrain;
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
        public GameObject reforgeMenuUI;
        public GameObject campfireMenuUI;
        public GameObject trialMenuUI;

        public GameObject darkBackground;

        [Header("Keys")]
        public KeyCode inventoryToggleKey = KeyCode.Tab;
        public KeyCode itemDropKey = KeyCode.Q;
        public KeyCode itemStackDropKey = KeyCode.LeftControl;
        public KeyCode exitExtensionKey = KeyCode.Escape;
        public KeyCode tooltipExpansionKey = KeyCode.LeftControl;

        [Header("Attribute Tables")]
        public ToolAttributeProbabilityTable toolAttributeTable;
        [SerializeField] private ToolAttributeProbabilityTable reforgedToolAttributeTable;

        [Header("Attribute Colors")]
        public Color positiveAttributeColor;
        public Color negativeAttributeColor;

        public bool JustClosedExtension { get; set; }
        public int LastRemainingCount { get; private set; }

        public Action<InventorySlot> OnInventoryItemChanged;

        public List<InventorySlot> InventorySlots => inventoryUIHandler.inventorySlots;

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
            OnInventoryItemChanged += EquipSelectedItem;

            foreach (Item item in startItems)
                AddItem(item);

            ChangeSelectedSlot(0);

            ItemEquip.Instance.EquipItem(GetSelectedItem(delete: false));
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;

            HandleSlotSelection();
            HandleInventoryToggle();
            HandleItemDropping();
            HandleExtensionExit();
            HandleTooltipExpansion();
        }

        void LateUpdate()
        {
            JustClosedExtension = false;
        }

        #region Item Save/Load

        public void AddSavedPlayerItems(List<ItemData> items)
        {
            foreach (ItemData itemData in items)
            {
                Item item = ItemRegistry.GetItemByName(itemData.itemName);
                item.toolAttribute = ToolAtributeRegistry.GetToolAttributeByName(itemData.toolAttribute);
                AddItem(item, itemData.count, itemData.position);
            }
        }

        public InventorySaveData GetSavedInventoryData()
        {
            List<ItemData> items = new();

            for (int i = 0; i < InventorySlots.Count; i++)
            {
                InventorySlot currentSlot = InventorySlots[i];
                if (currentSlot == null || currentSlot.transform.childCount <= 0) continue;

                InventoryItem invItem = currentSlot.GetComponentInChildren<InventoryItem>();
                Item item = invItem.item;

                bool hasToolattribute = item.toolAttribute != null;

                ItemData itemData = new()
                {
                    itemName = item.name,
                    toolAttribute = hasToolattribute ? item.toolAttribute.attributeID : "",
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

        #endregion

        #region Item Equip

        public void EquipSelectedItem(InventorySlot slot)
        {
            if (slot == null || slot != InventorySlots[selectedSlot]) return;

            Item selectedItem = GetSelectedItem(delete: false);

            if (selectedItem != null && selectedItem.equippedPrefab != null)
                ItemEquip.Instance.EquipItem(selectedItem);
            else
                ItemEquip.Instance.EquipItem(null);
        }

        #endregion

        #region Use Item

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
            InventoryItem itemInSlot = InventorySlots[selectedSlot].GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null || itemInSlot.count <= 0)
                ItemEquip.Instance.EquipItem(null);  // Unequip the item if it's no longer in the inventory
            else if (selectedItem.equippedPrefab != null)
                ItemEquip.Instance.EquipItem(selectedItem);
        }

        public void ConsumeItem(Item item, int consumeCount = 1)
        {
            foreach (InventorySlot slot in InventorySlots)
            {
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot == null || itemInSlot.item != item) continue;

                if (itemInSlot.count > consumeCount)
                {
                    itemInSlot.count -= consumeCount;
                    itemInSlot.RefreshCount();
                    break;
                }

                consumeCount -= itemInSlot.count;
                Destroy(itemInSlot.gameObject);

                if (consumeCount <= 0) break;
            }

            ItemEquip.Instance.EquipItem(GetSelectedItem(delete: false));
        }

        #endregion

        #region Adding Items

        public bool AddItem(Item item, int count = 1, int? targetSlotIndex = null)
        {
            int initialCount = count; // Store the initial count for later comparison

            if (targetSlotIndex.HasValue) // Case 1: Add to a specific slot
            {
                int index = targetSlotIndex.Value;

                if (index < 0 || index >= InventorySlots.Count)
                {
                    Debug.LogWarning("Invalid slot index!");
                    return false;
                }

                InventorySlot slot = InventorySlots[index];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot != null)
                {
                    if (itemInSlot.item != item)
                    {
                        Debug.LogWarning("Slot already contains a different item.");
                        return false;
                    }

                    int availableSpace = item.maxStack - itemInSlot.count;
                    int itemsToAdd = Mathf.Min(count, availableSpace);
                    itemInSlot.count += itemsToAdd;
                    count -= itemsToAdd;
                    itemInSlot.RefreshCount();
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
                for (int i = 0; i < InventorySlots.Count && count > 0; i++)
                {
                    InventorySlot slot = InventorySlots[i];
                    InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                    if (itemInSlot == null || itemInSlot.item != item || itemInSlot.count >= item.maxStack) continue;

                    int availableSpace = item.maxStack - itemInSlot.count;
                    int itemsToAdd = Mathf.Min(count, availableSpace);

                    itemInSlot.count += itemsToAdd;
                    count -= itemsToAdd;

                    itemInSlot.RefreshCount();
                }

                // Pass 2: If any left, put in empty slots
                for (int i = 0; i < InventorySlots.Count && count > 0; i++)
                {
                    InventorySlot slot = InventorySlots[i];
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
            {
                PickupNotificationManager.Instance.ShowPickup(item, addedCount);

                TutorialData pickStoneTutorial = VoxelGrid.Instance.pickStoneTutorial;
                if (pickStoneTutorial != null && pickStoneTutorial.isActive && pickStoneTutorial.id.Contains(item.itemName))
                    TutorialManager.Instance.CompleteTutorial(pickStoneTutorial);
            }

            // Return true if all items were successfully added, otherwise false
            return count == 0;
        }

        public void SpawnNewItem(Item item, InventorySlot slot, int count = 1)
        {
            if (slot == null || item == null) return;

            GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
            InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
            inventoryItem.SetItem(item, count);  // Set the item and initialize it
            EquipSelectedItem(slot);

            if (!InventorySlots.Contains(slot)) return;

            SetTutorialForItem(item);
            TryDiscoverItem(item);
        }

        #endregion

        #region Item Discovery

        public void TryDiscoverItem(Item item)
        {
            if (discoveredItems.Exists(x => x.itemName == item.itemName)) return;

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

        #endregion

        #region Dropping Items

        private void HandleItemDropping()
        {
            if (!Input.GetKeyDown(itemDropKey) || IsExtensionOpen()) return;

            if (Input.GetKey(itemStackDropKey))
                DropSelectedStack();
            else
                DropSelectedItem();
        }

        public void DropSelectedItem()
        {
            Item selectedItem = GetSelectedItem(delete: true);
            DropItem(selectedItem, 1);
            EquipSelectedItem(InventorySlots[selectedSlot]);
        }

        public void DropSelectedStack()
        {
            Item selectedItem = GetSelectedItem(delete: false);
            InventorySlot slot = InventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null) return;

            DropItem(selectedItem, itemInSlot.count);
            itemInSlot.count = 0;
            GetSelectedItem(delete: false);
            EquipSelectedItem(InventorySlots[selectedSlot]);
        }

        public void DropAllItems()
        {
            int numSlots = InventorySlots.Count;

            for (int i = 0; i < numSlots; i++)
            {
                InventorySlot slot = InventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot == null) continue;

                DropItem(itemInSlot.item, itemInSlot.count, scatter: true);
                itemInSlot.count = 0;
                Destroy(itemInSlot.gameObject);
            }

            EquipSelectedItem(InventorySlots[selectedSlot]);
        }

        public void DropItem(Item item, int count, bool scatter = false)
        {
            if (item == null || item.itemDrop == null) return;

            Vector3 pos = playerObject.transform.position + Vector3.up * 1.5f;

            float scatterDistance = 0.5f;
            float scatterForce = 2f;

            ItemSpawner.Spawn(item, pos, count, torsoBone: null, scatter, scatterDistance, scatterForce);
        }

        #endregion

        #region Get Item

        public Item GetSelectedItem(bool delete)
        {
            InventorySlot slot = InventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null) return null;

            if (itemInSlot.count <= 0)
            {
                Destroy(itemInSlot.gameObject);  // Remove the empty item from the slot
                return null;
            }

            if (!delete) return itemInSlot.item;

            itemInSlot.count--;

            if (itemInSlot.count <= 0)
                Destroy(itemInSlot.gameObject);
            else
                itemInSlot.RefreshCount();

            return itemInSlot.item;
        }

        public InventoryItem GetSelectedInventoryItem()
        {
            InventorySlot slot = InventorySlots[selectedSlot];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            return itemInSlot;
        }

        #endregion

        #region Slot Selection

        private void HandleSlotSelection()
        {
            if (IsExtensionOpen()) return;

            // Handle number keys
            for (int i = 1; i <= numHotbarSlots; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + (i - 1))) continue;

                ChangeSelectedSlot(i - 1);
                return;
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

        private void ChangeSelectedSlot(int newValue)
        {
            if (selectedSlot >= 0)
            {
                InventorySlot selected = InventorySlots[selectedSlot];
                if (selected.TryGetComponent(out SelectableImage selectable))
                    selectable.Deselect();
            }

            InventorySlot newSlot = InventorySlots[newValue];
            if (newSlot.TryGetComponent(out SelectableImage selectableImage))
                selectableImage.Select();

            selectedSlot = newValue;

            EquipSelectedItem(InventorySlots[selectedSlot]);
        }

        #endregion

        #region Inventory Toggle

        private void HandleInventoryToggle()
        {
            if (!Input.GetKeyDown(inventoryToggleKey)) return;

            if (IsExtensionOpen())
                ResetExtensions();
            else if (CraftingManager.Instance != null)
                CraftingUI.Instance.ToggleCraftingMenu(CraftingSource.Base);
        }

        public void OnInventoryOpen()
        {
            if (deleteSlot != null)
                deleteSlot.SetActive(true);

            if (darkBackground != null)
                darkBackground.SetActive(true);

            CameraControlToggle.Instance.SetCameraControl(false);
        }

        #endregion

        #region Inventory Extension Handling

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

        public void ResetExtensions()
        {
            if (darkBackground != null)
                darkBackground.SetActive(false);

            if (StorageUI.Instance.gameObject.activeSelf)
                StorageUI.Instance.Close();

            FurnaceUI furnaceUI = furnaceMenuUI.GetComponent<FurnaceUI>();
            if (furnaceUI != null && furnaceUI.gameObject.activeSelf)
                furnaceUI.Close();

            ReforgeTableUI reforgeUI = reforgeMenuUI.GetComponent<ReforgeTableUI>();
            if (reforgeUI != null && reforgeUI.gameObject.activeSelf)
                reforgeUI.Close();

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

        #endregion

        #region Item Tooltip

        private void HandleTooltipExpansion()
        {
            if (!ItemTooltipUI.Instance.IsOpen || !IsExtensionOpen()) return;

            Item hoveredItem = ItemTooltipUI.Instance.HoveredItem;
            if (hoveredItem == null || hoveredItem.toolAttribute == null) return;

            if (Input.GetKeyDown(tooltipExpansionKey))
                ItemTooltipUI.Instance.ExpandTooltip(expand: true);
        }

        #endregion

        #region Check For Item

        public bool IsInventoryFullForItem(Item item, int count = 1)
        {
            int remaining = count;

            // First, check space in existing stacks
            for (int i = 0; i < InventorySlots.Count && remaining > 0; i++)
            {
                InventorySlot slot = InventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot == null || itemInSlot.item != item || itemInSlot.count >= item.maxStack) continue;

                int space = item.maxStack - itemInSlot.count;
                remaining -= Mathf.Min(remaining, space);
            }

            // Then, check for empty slots
            for (int i = 0; i < InventorySlots.Count && remaining > 0; i++)
            {
                InventorySlot slot = InventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot != null) continue;

                int space = item.maxStack;
                remaining -= Mathf.Min(remaining, space);
            }

            return remaining > 0; // If there's still remaining, inventory is full
        }

        public bool HasItem(Item item)
        {
            foreach (InventorySlot slot in InventorySlots)
            {
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                if (itemInSlot != null && itemInSlot.item == item) return true;
            }

            return false;
        }

        #endregion

        public void SetTutorialForItem(Item item)
        {
            TutorialManager.Instance.CreatePickupCraftingRecipeTutorials(item);
            TutorialManager.Instance.CreatePickupSmeltingRecipeTutorials(item);
        }

        public Item SetAttributeForTool(Item item, InventoryItem invItem = null)
        {
            if (item == null || item.toolType == 0) return item;

            ToolAttribute attribute = reforgedToolAttributeTable.GetRandomToolAttribute(item.toolType);
            item.toolAttribute = attribute;

            if (invItem != null)
                invItem.item = item;

            return item;
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
