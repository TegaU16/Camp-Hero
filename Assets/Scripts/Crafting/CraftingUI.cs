using System;
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
        [Serializable]
        private struct CraftingTab
        {
            public Button tabButton;
            public CraftingCategory category;
        }

        public static CraftingUI Instance;

        [SerializeField] private GameObject craftingItemPrefab;
        public Transform craftingItemParent;
        [SerializeField] private TextMeshProUGUI craftingItemName;

        [SerializeField] private GameObject craftingItemIconBackground;
        [SerializeField] private Image craftingItemIcon;

        [SerializeField] private GameObject requirementPrefabParent;
        [SerializeField] private Button craftButton;

        [SerializeField] private List<CraftingTab> craftingTabs;
        private CraftingTab selectedTab;

        [HideInInspector] public CraftingItem selectedItem;

        private CraftingSource currentSource;

        private GameObject CraftingMenu => InventoryManager.Instance.craftingMenuUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            selectedItem = null;
            selectedTab = craftingTabs[0]; // First tab is ALL
            currentSource = CraftingSource.Base;
            ApplyFilters();
        }

        public void OpenCraftingMenu(CraftingSource menuCraftingSource)
        {
            if (InventoryManager.Instance.IsExtensionOpen()) return;
            if (!CraftingMenu.TryGetComponent(out CanvasGroup canvasGroup)) return;
            if (!CraftingMenu.TryGetComponent(out RectTransform rectTransform)) return;

            ClearRecipe();

            currentSource = menuCraftingSource;
            ApplyFilters();

            UITween.DefaultOpenMenu(canvasGroup, rectTransform);
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

                Image bg = reqGO.GetComponent<Image>();
                if (bg != null && craftingItem.requirementBackground != null)
                    bg.sprite = Instantiate(craftingItem.requirementBackground.sprite);

                Transform iconTransform = reqGO.transform.Find("Req. Icon");
                if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
                    iconImage.sprite = item.requiredItem.icon;

                Transform count = reqGO.transform.Find("Req. Count");
                if (count != null && count.TryGetComponent(out TextMeshProUGUI countText))
                    countText.text = item.count.ToString();

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

        public void FilterByCategory(CraftingCategory category, Button button)
        {
            selectedTab.category = category;
            selectedTab.tabButton = button;

            foreach (CraftingTab tab in craftingTabs)
            {
                if (!tab.tabButton.TryGetComponent(out InteractiveButton interactive)) continue;

                if (tab.tabButton == button)
                    interactive.Select();
                else
                    interactive.Deselect();
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            foreach (Transform child in craftingItemParent)
            {
                if (!child.TryGetComponent(out CraftingItem item)) continue;

                bool categoryPass =
                    selectedTab.category == CraftingCategory.All ||
                    item.recipe.category == selectedTab.category;

                bool sourcePass = (item.recipe.source & currentSource) != 0;

                child.gameObject.SetActive(categoryPass && sourcePass);
            }
        }

        public void SpawnCraftingItem(CraftingRecipe recipe)
        {
            GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
            CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();

            uiItem.recipe = recipe;
            uiItem.itemImage.sprite = recipe.resultItem.icon;

            ApplyFilters();
        }

        private void ClearRecipe()
        {
            craftingItemIconBackground.SetActive(false);
            craftingItemName.text = "Select an item to craft";
            craftButton.interactable = false;

            if (craftButton.TryGetComponent(out InteractiveButton interactiveButton))
                interactiveButton.Deselect();

            foreach (Transform child in requirementPrefabParent.transform)
                Destroy(child.gameObject);
        }
    }
}
