using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static StorageUnit;

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

        ClearSlots();
        gameObject.SetActive(true);
        InventoryManager.Instance.darkBackground.SetActive(true);
        BuildSlots();
    }

    public void Close()
    {
        SaveItemsToStorage();
        ClearSlots();
        gameObject.SetActive(false);
        InventoryManager.Instance.darkBackground.SetActive(false);
    }

    private void BuildSlots()
    {
        for (int i = 0; i < linkedStorage.maxSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slotInstances.Add(slot);

            if (i < linkedStorage.items.Count)
            {
                var storedItem = linkedStorage.items[i];
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
        foreach (var slot in slotInstances)
        {
            Destroy(slot);
        }
        slotInstances.Clear();
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
                    linkedStorage.items.Add(new StoredItem
                    {
                        item = inventoryItem.item,
                        count = Mathf.Max(1, inventoryItem.count)
                    });
                }
            }
        }

        Debug.Log($"Saved {linkedStorage.items.Count} items to storage.");
    }
}
