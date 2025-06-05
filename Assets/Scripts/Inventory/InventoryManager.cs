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

    public GameObject darkBackground;

    [Header("Keys")]
    public KeyCode inventoryToggleKey = KeyCode.Tab;
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode itemDropKey = KeyCode.Q;
    public KeyCode itemStackDropKey = KeyCode.LeftControl;
    public KeyCode exitExtensionKey = KeyCode.Escape;

    private void Awake()
    {
        Instance = this;
        InventoryUI = inventoryUI;
    }

    private void Start()
    {
        foreach (var item in startItems)
        {
            AddItem(item);
        }

        ChangeSelectedSlot(0);
        player = playerObject.GetComponentInChildren<Player>();

        inventoryExtensions.Add(variableExtension);
    }

    void ChangeSelectedSlot(int newValue)
    {
        if (selectedSlot >= 0)
        {
            inventoryUIHandler.inventorySlots[selectedSlot].Deselect();
        }

        inventoryUIHandler.inventorySlots[newValue].Select();
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

    public int LastRemainingCount { get; private set; }

    public bool AddItem(Item item, int count = 1)
    {
        int initialCount = count; // Store the initial count for later comparison

        // First, try to add the items to existing stacks
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Length && count > 0; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null &&
                itemInSlot.item == item &&
                itemInSlot.count < item.maxStack &&
                itemInSlot.item.stackable == true)
            {
                // Calculate how many items can be added to this stack
                int availableSpace = item.maxStack - itemInSlot.count;
                int itemsToAdd = Mathf.Min(count, availableSpace);

                // Add items to the stack
                itemInSlot.count += itemsToAdd;
                count -= itemsToAdd;

                itemInSlot.RefreshCount();
            }
        }

        // Then, spawn new stacks in empty slots if there are still items left
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Length && count > 0; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot == null)
            {
                // Calculate how many items to place in this new stack
                int itemsToPlace = Mathf.Min(count, item.maxStack);

                // Spawn the item with the specified count
                SpawnNewItem(item, slot, itemsToPlace);
                count -= itemsToPlace;
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
        int numSlots = inventoryUIHandler.inventorySlots.Length;

        for (int i = 0; i < numSlots; i++)
        {
            InventorySlot slot = inventoryUIHandler.inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null)
            {
                DropItem(itemInSlot.item, itemInSlot.count);
                itemInSlot.count = 0;
                GetSelectedItem(false);
            }
        }
    }

    public void DropItem(Item item, int count)
    {
        Vector3 pos = playerObject.transform.position + playerObject.transform.forward * 2f;

        Debug.Log(playerObject.transform.position.ToString());

        if (item != null && item.itemDrop != null)
        {
            GameObject instance = Instantiate(item.itemDrop, pos, item.itemDrop.transform.rotation);
            
            if (instance.TryGetComponent<InteractableItem>(out var interactable))
            {
                interactable.itemCount = count;
            }
        }

        // Ensure the inventory slot is updated before re-equipping
        if (GetSelectedItem(false) == null)  // Check if slot is empty
        {
            itemEquip.EquipItem(null);  // Unequip the item if the slot is empty
        }
        else
        {
            EquipSelectedItem();  // Equip the current item in the slot if it still exists
        }
    }

    public void ClearItems()
    {
        int numSlots = inventoryUIHandler.inventorySlots.Length;

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

    public InventoryItem GetInventoryItem()
    {
        InventorySlot slot = inventoryUIHandler.inventorySlots[selectedSlot];
        InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

        return itemInSlot;
    }

    // Update is called once per frame
    void Update()
    {
        HandleSlotSelection();
        HandleInventoryToggle();
        HandleInteraction();
        HandleItemDropping();
        HandleExtensionExit();
    }

    void HandleSlotSelection()
    {
        if (int.TryParse(Input.inputString, out int number) && number is > 0 and <= numHotbarSlots)
        {
            ChangeSelectedSlot(number - 1);
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
            if (darkBackground != null) darkBackground.SetActive(true);
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
    }

    void TryInteractWithObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask layerOne = LayerMask.GetMask("Item");
        LayerMask layerTwo = LayerMask.GetMask("Pickable");

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, layerOne) || Physics.Raycast(ray, out hit, 5f, layerTwo))  // 5f is the interaction distance
        {
            if (hit.collider.TryGetComponent<InteractableItem>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }

    public bool IsInventoryFullForItem(Item item, int count = 1)
    {
        int remaining = count;

        // First, check space in existing stacks
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Length && remaining > 0; i++)
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
        for (int i = 0; i < inventoryUIHandler.inventorySlots.Length && remaining > 0; i++)
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
