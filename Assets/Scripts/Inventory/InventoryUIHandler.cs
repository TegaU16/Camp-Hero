using System.Collections.Generic;
using UnityEngine;

public class InventoryUIHandler : MonoBehaviour
{
    public List<InventorySlot> inventorySlots;
    public RectTransform deleteSlot;

    private void Update()
    {
        if (!InventoryManager.Instance.mainInventory.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick(true);

        if (Input.GetMouseButtonDown(1))
            HandleClick(false);
    }

    private void HandleClick(bool isLeft)
    {
        if (InventoryManager.InventoryUI == null)
            return;

        Vector2 mousePosition = Input.mousePosition;

        if (RectTransformUtility.RectangleContainsScreenPoint(deleteSlot, mousePosition))
        {
            if (InventoryItem.selectedItem != null)
                DropSelectedItem();
            return;
        }

        foreach (InventorySlot slot in GetAllSlots())
        {
            if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(slot.GetComponent<RectTransform>(), mousePosition))
            {
                if (isLeft)
                    slot.HandleLeftClick();
                else
                    slot.HandleRightClick();
                return;
            }
        }
    }

    private IEnumerable<InventorySlot> GetAllSlots()
    {
        // Always include main inventory
        foreach (InventorySlot slot in inventorySlots)
            yield return slot;

        // Include chest slots if chest is open
        if (InventoryManager.Instance.activeChest != null)
        {
            foreach (InventorySlot slot in InventoryManager.Instance.activeChest.inventorySlots)
                yield return slot;
        }

        // Include furnace slots if furnace is open
        if (InventoryManager.Instance.activeFurnace != null)
        {
            foreach (InventorySlot slot in InventoryManager.Instance.activeFurnace.inventorySlots)
                yield return slot;
        }
    }

    private void DropSelectedItem()
    {
        InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
        Destroy(InventoryItem.selectedItem.gameObject);
    }
}
