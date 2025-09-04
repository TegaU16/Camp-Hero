using System.Collections.Generic;
using UnityEngine;

public class InventoryUIHandler : MonoBehaviour
{
    public List<InventorySlot> inventorySlots;
    public RectTransform inventoryPanel;
    public RectTransform deleteSlot;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && InventoryManager.Instance.mainInventory.activeSelf)
        {
            HandleClick(true);
        }

        if (Input.GetMouseButtonDown(1) && InventoryManager.Instance.mainInventory.activeSelf)
        {
            HandleClick(false);
        }
    }

    private void HandleClick(bool isLeft)
    {
        if (InventoryManager.InventoryUI == null) return;

        Vector2 mousePosition = Input.mousePosition;

        if (RectTransformUtility.RectangleContainsScreenPoint(deleteSlot, mousePosition))
        {
            if (InventoryItem.selectedItem != null)
            {
                DropSelectedItem();
            }
            return;
        }

        foreach (InventorySlot slot in inventorySlots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(slot.GetComponent<RectTransform>(), mousePosition))
            {
                if (isLeft)
                    slot.HandleLeftClick();
                else
                    //slot.HandleRightClick();
                return;
            }
        }
    }

    private void DropSelectedItem()
    {
        InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
        Destroy(InventoryItem.selectedItem.gameObject);
    }

    public int GetSlotIndex(InventorySlot slot)
    {
        return inventorySlots.IndexOf(slot);
    }
}
