using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;

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
    public string chestName = "Storage Chest";
    public int maxSlots = 16;
    [HideInInspector] public bool isOpen = false;

    [HideInInspector]
    public List<StoredItem> items = new();

    public void Interact()
    {
        StorageUI ui = InventoryManager.Instance.storageMenuUI.GetComponent<StorageUI>();

        if (!isOpen)
        {
            ui.Open(this);
            isOpen = true;
            InventoryManager.Instance.mainInventory.SetActive(true);
        }
        else
        {
            ui.Close();
            isOpen = false;
            InventoryManager.Instance.mainInventory.SetActive(false);
        }
    }

    public string GetInteractText() => $"Open {chestName}";

    public Transform GetTransform() => transform;

    public string SaveState()
    {
        foreach (StoredItem stored in items)
            stored.SyncNameFromItem();

        return JsonUtility.ToJson(this);
    }

    public void LoadState(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);

        foreach (StoredItem stored in items)
            stored.ResolveItemFromName();
    }
}
