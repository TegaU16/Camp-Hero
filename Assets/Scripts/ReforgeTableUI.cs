using Game.Inventory;
using UnityEngine;

namespace Game.Reforge
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class ReforgeTableUI : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        [SerializeField] private InventorySlot toolSlot;
        [SerializeField] private InventorySlot materialSlot;

        private bool isOpen;
        private ReforgeTable linkedTable;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Open(ReforgeTable reforgeTable)
        {
            if (isOpen) return;

            isOpen = true;
            linkedTable = reforgeTable;
            InventoryManager.Instance.OpenInventory();

            linkedTable.toolSlot = this.toolSlot;
            linkedTable.materialSlot = this.materialSlot;

            linkedTable.LoadUI();

            UITween.DefaultOpenMenu(canvasGroup, rectTransform);
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
        }

        // Called by button
        public void GiveToolAttribute()
        {
            if (linkedTable == null) return;
            linkedTable.GiveToolAttribute();
        }
    }
}
