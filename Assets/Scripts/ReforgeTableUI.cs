using Game.Inventory;
using UnityEngine;

namespace Game.Reforge
{
    public class ReforgeTableUI : MonoBehaviour
    {
        public InventorySlot toolSlot;
        public InventorySlot materialSlot;

        private bool isOpen;
        private ReforgeTable linkedTable;

        public void Open(ReforgeTable reforgeTable)
        {
            if (isOpen) return;

            isOpen = true;
            linkedTable = reforgeTable;
            gameObject.SetActive(true);
            InventoryManager.Instance.mainInventory.SetActive(true);
            InventoryManager.Instance.OnInventoryOpen();

            linkedTable.toolSlot = this.toolSlot;
            linkedTable.materialSlot = this.materialSlot;

            linkedTable.LoadUI();
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;

            if (linkedTable != null)
            {
                linkedTable.SaveUI(toolSlot); // Save the UI data back into furnace
                linkedTable.SaveUI(materialSlot);

                linkedTable.toolSlot = null;
                linkedTable.materialSlot = null;

                linkedTable = null;
            }

            gameObject.SetActive(false);
            InventoryManager.Instance.mainInventory.SetActive(false);
            InventoryManager.Instance.darkBackground.SetActive(false);
        }

        // Called by button
        public void GiveToolAttribute()
        {
            if (linkedTable == null) return;
            linkedTable.GiveToolAttribute();
        }
    }
}
