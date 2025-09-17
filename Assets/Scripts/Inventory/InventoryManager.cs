using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public static RectTransform InventoryUI { get; private set; }

    [SerializeField]
    private RectTransform inventoryUI;

    public Item[] startItems;
    public ItemEquip itemEquip;
    
    public List<Item> discoveredItems = new();

    private int numGold = 100;
    public const int numHotbarSlots = 7;

    public InventoryUIHandler inventoryUIHandler;

    private GameObject variableExtension;
    public List<GameObject> inventoryExtensions;
    public GameObject deleteSlot;

    public GameObject inventoryItemPrefab;
    public GameObject playerObject;

    public Player player;

    public PickupNotification pickupNotification;

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
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode itemDropKey = KeyCode.Q;
    public KeyCode itemStackDropKey = KeyCode.LeftControl;
    public KeyCode exitExtensionKey = KeyCode.Escape;

    public bool JustClosedExtension { get; set; }
    public int LastRemainingCount { get; private set; }

    private void Awake()
    {
        Instance = this;
        InventoryUI = inventoryUI;
    }

    private void Start()
    {
        foreach (Item item in startItems)
        {
            AddItem(item);
        }

        ChangeSelectedSlot(0);
        player = playerObject.GetComponentInChildren<Player>();

        itemEquip.EquipItem(GetSelectedItem(false));
        inventoryExtensions.Add(variableExtension);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        HandleSlotSelection();
        HandleInventoryToggle();
        HandleInteraction();
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

    public List<ItemData> GetSavedPlayerItems()
    {
        List<ItemData> items = new();

        for (int i = 0; i < inventoryUIHandler.inventorySlots.Count; i++)
        {
            InventorySlot currentSlot = inventoryUIHandler.inventorySlots[i];
            if (currentSlot != null && currentSlot.transform.childCount > 0)
            {
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
        }

        return items;
    }

    void ChangeSelectedSlot(int newValue)
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
        Item selectedItem = GetSelectedItem(false);
        if (selectedItem != null && selectedItem.equippedPrefab != null)
        {
            itemEquip.EquipItem(selectedItem);
        }
        else
        {
            itemEquip.EquipItem(null);
        }
    }

    public void UseSelectedItem()
    {
        Item selectedItem = GetSelectedItem(false);
        if (selectedItem != null)
        {
            if (selectedItem.actionType != ActionType.None)
            {
                selectedItem = GetSelectedItem(true);

                // Check if the item count is zero after using it
                InventoryItem itemInSlot = inventoryUIHandler.inventorySlots[selectedSlot].GetComponentInChildren<InventoryItem>();
                if (itemInSlot == null || itemInSlot.count <= 0)
                {
                    itemEquip.EquipItem(null);  // Unequip the item if it's no longer in the inventory
                }
                else if (selectedItem.equippedPrefab != null)
                {
                    itemEquip.EquipItem(selectedItem);
                }
            }
        }
        else
        {
            itemEquip.EquipItem(null);
        }
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
                if (itemInSlot.item == item && item.stackable)
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

                if (itemInSlot != null && itemInSlot.item == item && itemInSlot.count < item.maxStack && item.stackable)
                {
                    int availableSpace = item.maxStack - itemInSlot.count;
                    int itemsToAdd = Mathf.Min(count, availableSpace);

                    itemInSlot.count += itemsToAdd;
                    count -= itemsToAdd;

                    itemInSlot.RefreshCount();
                }
            }

            // Pass 2: If any left, put in empty slots
            for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && count > 0; i++)
            {
                InventorySlot slot = inventoryUIHandler.inventorySlots[i];
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot == null)
                {
                    int itemsToPlace = Mathf.Min(count, item.maxStack);
                    SpawnNewItem(item, slot, itemsToPlace);
                    count -= itemsToPlace;
                }
            }
        }

        int addedCount = initialCount - count;
        LastRemainingCount = count;

        if (addedCount > 0)
        {
            pickupNotification.ShowPickup(item, addedCount);
        }

        // Return true if all items were successfully added, otherwise false
        return count == 0;
    }

    public void TryDiscoverItem(Item item)
    {
        if (!discoveredItems.Contains(item))
        {
            discoveredItems.Add(item);

            CraftingManager.Instance.TryUnlockRecipes(discoveredItems);
            FurnaceManager.Instance.TryUnlockRecipes(discoveredItems);
        }
    }


    public void DropSelectedItem()
    {
        Item selectedItem = GetSelectedItem(true);
        DropItem(selectedItem, 1);
    }

    public void DropSelectedStack()
    {
        Item selectedItem = GetSelectedItem(false);
        InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

        if (itemInSlot != null)
        {
            DropItem(selectedItem, itemInSlot.count);
            itemInSlot.count = 0;
            GetSelectedItem(false);
        }
    }

    public void DropAllItems()
    {
        int numSlots = inventoryUIHandler.inventorySlots.Count;

        for (int i = 0; i < numSlots; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null)
            {
                DropItem(itemInSlot.item, itemInSlot.count);
                itemInSlot.count = 0;
                Destroy(itemInSlot.gameObject);
            }
        }

        EquipSelectedItem();
    }

    public void DropItem(Item item, int count)
    {
        Vector3 pos = playerObject.transform.position + playerObject.transform.forward * 1f + Vector3.up * 2f;

        if (item != null && item.itemDrop != null)
        {
            GameObject instance = Instantiate(item.itemDrop, pos, item.itemDrop.transform.rotation);
            
            if (instance.TryGetComponent(out InteractableItem interactable))
            {
                interactable.itemCount = count;
                interactable.EnablePickupAfterDelay(0.25f);
            }
        }

        EquipSelectedItem();
    }

    public void ClearItems()
    {
        int numSlots = inventoryUIHandler.inventorySlots.Count;

        for (int i = 0; i < numSlots; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null)
            {
                itemInSlot.count = 0;
                GetSelectedItem(true);
            }
        }
    }

    public void SpawnNewItem(Item item, InventorySlot slot, int count = 1)
    {
        GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
        InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
        inventoryItem.SetItem(item, count);  // Set the item and initialize it
    }

    public Item GetSelectedItem(bool delete)
    {
        InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

        if (itemInSlot != null)
        {
            if (itemInSlot.count <= 0)
            {
                Destroy(itemInSlot.gameObject);  // Remove the empty item from the slot
                return null;
            }

            Item item = itemInSlot.item;

            if (delete == true && itemInSlot.item.stackable == true)
            {
                itemInSlot.count--;
                if (itemInSlot.count <= 0)
                {
                    Destroy(itemInSlot.gameObject);
                }
                else
                {
                    itemInSlot.RefreshCount();
                }
            }

            return item;
        }

        return null;
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
            if (itemInSlot != null && itemInSlot.item == item)
            {
                return true;
            }
        }

        return false;
    }

    public void ConsumeItem(Item item, int consumeCount = 1)
    {
        foreach (InventorySlot slot in inventoryUIHandler.inventorySlots)
        {
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null && itemInSlot.item == item)
            {
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
            }

            if (consumeCount <= 0) break;
        }

        itemEquip.EquipItem(GetSelectedItem(false));
    }

    void HandleSlotSelection()
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

    void HandleInventoryToggle()
    {
        if (!Input.GetKeyDown(inventoryToggleKey)) return;

        if (IsExtensionOpen())
        {
            ResetExtensions();
        }
        else
        {
            if (mainInventory != null) mainInventory.SetActive(true);
            if (craftingMenuUI != null) craftingMenuUI.SetActive(true);
            OnInventoryOpen();
        }
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(interactionKey) && !IsExtensionOpen())
        {
            TryInteractWithObject();
        }
    }

    void HandleItemDropping()
    {
        if (!Input.GetKeyDown(itemDropKey) || IsExtensionOpen()) return;

        if (Input.GetKey(itemStackDropKey))
            DropSelectedStack();
        else
            DropSelectedItem();
    }

    void HandleExtensionExit()
    {
        if (IsExtensionOpen() && Input.GetKeyDown(exitExtensionKey))
        {
            ResetExtensions();
        }
    }

    public bool IsExtensionOpen()
    {
        for (int i = 0; i < inventoryExtensions.Count; i++)
        {
            if (inventoryExtensions[i] != null)
            {
                if (inventoryExtensions[i].activeSelf)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void OnInventoryOpen()
    {
        if (deleteSlot != null) deleteSlot.SetActive(true);
        if (darkBackground != null) darkBackground.SetActive(true);
        GameManager.Instance.ToggleCameraFollow(false);
    }

    void ResetExtensions()
    {
        if (darkBackground != null) darkBackground.SetActive(false);

        StorageUI storageUI = storageMenuUI.GetComponent<StorageUI>();
        if (storageUI != null && storageUI.gameObject.activeSelf)
        {
            storageUI.Close();
        }

        FurnaceUI furnaceUI = furnaceMenuUI.GetComponent<FurnaceUI>();
        if (furnaceUI != null && furnaceUI.gameObject.activeSelf)
        {
            furnaceUI.Close();
        }

        for (int i = 0; i < inventoryExtensions.Count; i++)
        {
            if (inventoryExtensions[i] != null)
            {
                inventoryExtensions[i].SetActive(false);
            }
        }

        deleteSlot.SetActive(false);
        JustClosedExtension = true;
        GameManager.Instance.ToggleCameraFollow(true);

        ItemTooltipUI.Instance.HideTooltip();
    }

    void TryInteractWithObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask layerOne = LayerMask.GetMask("Item");
        LayerMask layerTwo = LayerMask.GetMask("Pickable");

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, layerOne) || Physics.Raycast(ray, out hit, 5f, layerTwo))  // 5f is the interaction distance
        {
            if (hit.collider.TryGetComponent(out InteractableItem interactable))
            {
                interactable.Interact();
            }
        }
    }

    public bool IsInventoryFullForItem(Item item, int count = 1)
    {
        int remaining = count;

        // First, check space in existing stacks
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && remaining > 0; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null &&
                itemInSlot.item == item &&
                itemInSlot.count < item.maxStack &&
                item.stackable)
            {
                int space = item.maxStack - itemInSlot.count;
                remaining -= Mathf.Min(remaining, space);
            }
        }

        // Then, check for empty slots
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Count && remaining > 0; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null)
            {
                int space = item.stackable ? item.maxStack : 1;
                remaining -= Mathf.Min(remaining, space);
            }
        }

        return remaining > 0; // If there's still remaining, inventory is full
    }

    public bool AddGold(int count)
    {
        numGold += count;
        return true;
    }

    public bool RemoveGold(int count)
    {
        if (numGold < count)
        {
            return false;
        }

        numGold -= count;
        return true;
    }

    public void SetVariableExtension(GameObject extension)
    {
        variableExtension = extension;
    }

    public void SetPlayer(GameObject playerObject)
    {
        if (playerObject != null)
        {
            this.playerObject = playerObject;
            player = playerObject.GetComponent<Player>();
        }
        else
        {
            Debug.LogWarning("Player is null!");
        }
    }
}
