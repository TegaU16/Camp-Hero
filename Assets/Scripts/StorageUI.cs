using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StorageUI : MonoBehaviour
{
    public GameObject slotPrefab;             // Empty slot UI
    public GameObject inventoryItemPrefab;    // Item prefab
    public Transform slotParent;

    private StorageUnit linkedStorage;
    private readonly List<GameObject> slotInstances = new();

    public void Open(StorageUnit storage)
    {
        linkedStorage = storage;

        foreach (StoredItem storedItem in linkedStorage.items)
            storedItem.ResolveItemFromName();

        ClearSlots();
        gameObject.SetActive(true);
        InventoryManager.Instance.OnInventoryOpen();
        InventoryManager.Instance.activeChest = linkedStorage;
        BuildSlots();
    }

    public void Close()
    {
        linkedStorage.isOpen = false;

        SaveItemsToStorage();
        ClearSlots();
        gameObject.SetActive(false);
        InventoryManager.Instance.darkBackground.SetActive(false);

        linkedStorage = null;
    }

    private void BuildSlots()
    {
        for (int i = 0; i < linkedStorage.maxSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slotInstances.Add(slot);

            if (i < linkedStorage.items.Count)
            {
                StoredItem storedItem = linkedStorage.items[i];
                if (storedItem != null && storedItem.item != null) // Add null check
                {
                    GameObject itemGO = Instantiate(inventoryItemPrefab, slot.transform);
                    InventoryItem inventoryItem = itemGO.GetComponent<InventoryItem>();

                    inventoryItem.image = itemGO.GetComponent<Image>();
                    inventoryItem.canvasGroup = itemGO.GetComponent<CanvasGroup>();

                    inventoryItem.SetItem(storedItem.item, storedItem.count);
                    inventoryItem.PlaceInSlot(slot.transform);
                }
            }
        }
    }

    private void ClearSlots()
    {
        foreach (GameObject slot in slotInstances)
            Destroy(slot);

        slotInstances.Clear();
    }

    public List<InventorySlot> GetInventorySlots()
    {
        List<InventorySlot> slots = new();

        foreach (GameObject slot in slotInstances)
        {
            if (slot.TryGetComponent(out InventorySlot inventorySlot))
                slots.Add(inventorySlot);
        }

        return slots;
    }

    private void SaveItemsToStorage()
    {
        if (linkedStorage == null)
        {
            Debug.LogError("SaveItemsToStorage: linkedStorage is null!");
            return;
        }

        linkedStorage.items.Clear();

        foreach (GameObject slot in slotInstances)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null)
            {
                if (inventoryItem.item == null)
                {
                    Debug.LogWarning("SaveItemsToStorage: InventoryItem has no item assigned.");
                }
                else
                {
                    StoredItem stored = new()
                    {
                        item = inventoryItem.item,
                        count = Mathf.Max(1, inventoryItem.count)
                    };
                    stored.SyncNameFromItem();
                    linkedStorage.items.Add(stored);
                }
            }
        }
    }
}
