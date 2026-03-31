using Game.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Crafting
{
    public class CraftingItem : MonoBehaviour
    {
        [HideInInspector] public CraftingRecipe recipe;
        public Image itemImage;
        public GameObject requirementPrefab;

        [HideInInspector] public Image requirementBackground;

        void Start()
        {
            requirementBackground = requirementPrefab.GetComponent<Image>();

            if (recipe != null && itemImage != null)
                itemImage.sprite = recipe.resultItem.icon;
        }

        // Called by button
        public void SetRequirements()
        {
            CraftingUI.Instance.SetSelectedCraftingItem(this);
        }

        public bool HasItems()
        {
            foreach (CraftingRecipe.Requirement req in recipe.requirements)
            {
                int totalCount = 0;

                foreach (InventorySlot slot in InventoryManager.Instance.InventorySlots)
                {
                    InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
                    if (itemInSlot != null && itemInSlot.item == req.requiredItem)
                        totalCount += itemInSlot.count;
                }

                if (totalCount < req.count) return false;
            }

            return true;
        }
    }
}
