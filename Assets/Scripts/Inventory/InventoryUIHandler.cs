using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Inventory
{
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
            if (InventoryManager.InventoryUI == null) return;

            Vector2 mousePosition = Input.mousePosition;
            if (RectTransformUtility.RectangleContainsScreenPoint(deleteSlot, mousePosition))
            {
                if (InventoryItem.selectedItem != null)
                {
                    InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
                    Destroy(InventoryItem.selectedItem.gameObject);
                }

                return;
            }

            foreach (InventorySlot slot in GetAllSlots())
            {
                if (slot == null) continue;

                RectTransform slotTransform = slot.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(slotTransform, mousePosition)) continue;

                if (isLeft)
                    slot.HandleLeftClick();
                else
                    slot.HandleRightClick();
                return;
            }
        }

        private List<InventorySlot> GetAllSlots()
        {
            InventorySlot[] slots = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None);
            return slots.Where(slot => slot.gameObject.activeInHierarchy).ToList();
        }
    }
}

