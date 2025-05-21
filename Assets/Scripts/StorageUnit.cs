using UnityEngine;
using System.Collections.Generic;

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
        item = ItemRegistry.GetItemByName(itemName);
    }
}

public class StorageUnit : MonoBehaviour, IInteractable, ISaveableObject
{
    public string chestName = "Storage Chest";
    public int maxSlots = 16;

    [HideInInspector]
    public List<StoredItem> items = new();

    private StorageUI currentUI;

    public void Interact()
    {
        if (currentUI == null)
        {
            currentUI = InventoryManager.Instance.storageMenuUI.GetComponent<StorageUI>();
            currentUI.Open(this); // Pass this chest's reference

            InventoryManager.Instance.mainInventory.SetActive(true);
        }
        else
        {
            currentUI.Close();
            currentUI = null;

            InventoryManager.Instance.mainInventory.SetActive(false);
        }
    }

    public string GetInteractText() => $"Open {chestName}";

    public Transform GetTransform() => transform;

    public string SaveState()
    {
        foreach (var stored in items)
            stored.SyncNameFromItem();

        return JsonUtility.ToJson(this);
    }

    public void LoadState(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);

        foreach (var stored in items)
            stored.ResolveItemFromName();
    }
}
