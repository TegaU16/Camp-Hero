using UnityEngine;

public class InventoryUIHandler : MonoBehaviour
{
    public InventorySlot[] inventorySlots; // Reference to all inventory slots
    public RectTransform inventoryPanel;  // Reference to the entire inventory UI panel

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
        Vector2 mousePosition = Input.mousePosition;

        if (RectTransformUtility.RectangleContainsScreenPoint(inventoryPanel, mousePosition))
        {
            foreach (var slot in inventorySlots)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slot.GetComponent<RectTransform>(), mousePosition))
                {
                    if (isLeft)
                        slot.HandleLeftClick();
                    else
                        slot.HandleRightClick();
                    return;
                }
            }
        }
        else
        {
            if (InventoryItem.selectedItem != null)
            {
                DropSelectedItem();
            }
        }
    }

    private void DropSelectedItem()
    {
        InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
        Destroy(InventoryItem.selectedItem.gameObject);
    }
}
