using System.Collections.Generic;
using Game.Inventory;
using Game.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        public GameObject craftingItemPrefab;
        public Transform craftingItemParent;
        public TextMeshProUGUI craftingItemName;
        public GameObject craftingItemIconBackground;
        public Image craftingItemIcon;
        public GameObject requirementPrefabParent;
        public Button craftButton;
        public List<Button> craftingTabs;

        [HideInInspector] public CraftingItem selectedItem;

        private void Start()
        {
            selectedItem = null;
        }

        public void ToggleCraftingMenu(CraftingSource menuCraftingSource)
        {
            if (InventoryManager.Instance.IsExtensionOpen()) return;

            if (selectedItem == null)
            {
                craftingItemIconBackground.SetActive(false);
                craftingItemName.text = "Select an item to craft";
                craftButton.interactable = false;

                if (craftButton.TryGetComponent(out InteractiveButton interactiveButton))
                    interactiveButton.Deselect();

                foreach (Transform child in requirementPrefabParent.transform)
                    Destroy(child.gameObject);
            }

            FilterBySource(menuCraftingSource);

            if (InventoryManager.Instance.mainInventory != null)
                InventoryManager.Instance.mainInventory.SetActive(true);

            if (InventoryManager.Instance.craftingMenuUI != null)
                InventoryManager.Instance.craftingMenuUI.SetActive(true);

            InventoryManager.Instance.OnInventoryOpen();
        }

        /// <summary>
        /// Updates the crafting item UI and shows its requirements.
        /// </summary>
        public void SetSelectedCraftingItem(CraftingItem craftingItem)
        {
            // Clear all existing requirement entries
            foreach (Transform child in requirementPrefabParent.transform)
                Destroy(child.gameObject);

            foreach (CraftingRecipe.Requirement item in craftingItem.recipe.requirements)
            {
                if (item.requiredItem == null) continue;

                GameObject reqGO = Instantiate(craftingItem.requirementPrefab, requirementPrefabParent.transform);

                // Set requirement background
                Image bg = reqGO.GetComponent<Image>();
                if (bg != null && craftingItem.requirementBackground != null)
                    bg.sprite = Instantiate(craftingItem.requirementBackground.sprite);

                // Set requirement icon
                Transform iconTransform = reqGO.transform.Find("Req. Icon");
                if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
                    iconImage.sprite = item.requiredItem.icon;

                // Set requirement count
                Transform count = reqGO.transform.Find("Req. Count");
                if (count != null && count.TryGetComponent(out TextMeshProUGUI countText))
                    countText.text = item.count.ToString();

                // Set requirement name
                Transform name = reqGO.transform.Find("Req. Name");
                if (name != null && name.TryGetComponent(out TextMeshProUGUI nameText))
                    nameText.text = item.requiredItem.name.ToString();
            }

            craftingItemName.text = craftingItem.recipe.resultItem.itemName;
            craftingItemIconBackground.SetActive(true);
            craftingItemIcon.sprite = craftingItem.recipe.resultItem.icon;
            craftButton.interactable = true;

            if (craftButton.TryGetComponent(out InteractiveButton interactiveButton))
                interactiveButton.Select();

            selectedItem = craftingItem;

            string id = craftingItem.recipe.resultItem.itemName;
            TutorialData craftingTutorial = TutorialManager.Instance.GetTutorialData(id);

            if (craftingTutorial != null)
                TutorialManager.Instance.CompleteTutorial(craftingTutorial);
        }

        private void FilterBySource(CraftingSource menuCraftingSource)
        {
            foreach (Transform child in craftingItemParent.transform)
            {
                if (!child.TryGetComponent(out CraftingItem craftingItem)) continue;

                CraftingSource itemCraftingSource = craftingItem.recipe.source;
                bool shouldShow = (itemCraftingSource & menuCraftingSource) != 0;
                child.gameObject.SetActive(shouldShow);
            }
        }

        public void FilterByCategory(CraftingCategory category, Button button)
        {
            foreach (Transform child in craftingItemParent)
            {
                if (!child.TryGetComponent(out CraftingItem item)) continue;

                bool shouldShow = category == CraftingCategory.All || item.recipe.category == category;
                child.gameObject.SetActive(shouldShow);
            }

            foreach (Button tab in craftingTabs)
            {
                if (!tab.TryGetComponent(out InteractiveButton interactive)) continue;

                if (tab == button)
                    interactive.Select();
                else
                    interactive.Deselect();
            }
        }

        public void SpawnCraftingItem(CraftingRecipe recipe)
        {
            GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
            CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();

            uiItem.recipe = recipe;
            uiItem.recipe.category = recipe.category;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
        }
    }
}
