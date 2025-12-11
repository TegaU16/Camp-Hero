using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Inventory;
using Game.Saving;
using Game.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worlds;

namespace Game.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance;

        [Header("UI")]
        public GameObject craftingItemPrefab;
        public Transform craftingItemParent;
        public TextMeshProUGUI craftingItemName;
        public GameObject craftingItemIconBackground;
        public Image craftingItemIcon;
        public GameObject requirementPrefabParent;
        public Button craftButton;
        public List<Button> craftingTabs;

        [Header("State")]
        private CraftingItem selectedItem;
        public CraftingDatabase craftingDatabase;
        private readonly List<CraftingRecipe> unlockedRecipes = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            DOTween.Init();
            selectedItem = null;
        }

        public void ToggleCraftingMenu()
        {
            if (!InventoryManager.Instance.IsExtensionOpen())
            {
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

                if (InventoryManager.Instance.mainInventory != null)
                    InventoryManager.Instance.mainInventory.SetActive(true);

                if (InventoryManager.Instance.craftingMenuUI != null)
                    InventoryManager.Instance.craftingMenuUI.SetActive(true);

                InventoryManager.Instance.OnInventoryOpen();
            }
        }

        public void TryUnlockRecipes(List<Item> discoveredItems)
        {
            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
            {
                if (!unlockedRecipes.Contains(recipe) && recipe.ShouldUnlock(discoveredItems))
                    UnlockRecipe(recipe);
            }
        }

        /// <summary>
        /// Adds a new recipe to the list and instantiates its UI element.
        /// </summary>
        public void UnlockRecipe(CraftingRecipe recipe)
        {
            if (unlockedRecipes.Contains(recipe)) return;

            unlockedRecipes.Add(recipe);

            GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
            CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();

            uiItem.recipe = recipe;
            uiItem.recipe.category = recipe.category;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
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
                if (iconTransform != null)
                {
                    if (iconTransform.TryGetComponent(out Image iconImage))
                        iconImage.sprite = item.requiredItem.icon;
                }

                // Set requirement count
                Transform count = reqGO.transform.Find("Req. Count");
                if (count != null)
                {
                    if (count.TryGetComponent(out TextMeshProUGUI countText))
                        countText.text = item.count.ToString();
                }

                // Set requirement name
                Transform name = reqGO.transform.Find("Req. Name");
                if (name != null)
                {
                    if (name.TryGetComponent(out TextMeshProUGUI nameText))
                        nameText.text = item.requiredItem.name.ToString();
                }
            }

            // Update selected item
            selectedItem = craftingItem;

            craftingItemName.text = selectedItem.recipe.resultItem.itemName;
            craftingItemIconBackground.SetActive(true);
            craftingItemIcon.sprite = selectedItem.recipe.resultItem.icon;
            craftButton.interactable = true;

            if (craftButton.TryGetComponent(out InteractiveButton interactiveButton))
                interactiveButton.Select();
        }

        /// <summary>
        /// Crafts the currently selected item if requirements are met.
        /// </summary>
        public void Craft()
        {
            if (selectedItem == null || !selectedItem.HasItems()) return;

            selectedItem.recipe.hasBeenCraftedBefore = true;

            // Remove required items
            foreach (CraftingRecipe.Requirement requirement in selectedItem.recipe.requirements)
            {
                int toRemove = requirement.count;
                InventoryManager.Instance.ConsumeItem(requirement.requiredItem, toRemove);
            }

            StartCoroutine(DelayedAddCraftedItem(selectedItem.recipe.resultItem));
        }

        private IEnumerator DelayedAddCraftedItem(Item item)
        {
            yield return null; // wait 1 frame
            InventoryManager.Instance.AddItem(item);
            InventoryManager.Instance.EquipSelectedItem();
        }

        public void FilterByCategory(CraftingCategory category, Button button)
        {
            foreach (Transform child in craftingItemParent)
            {
                if (!child.TryGetComponent(out CraftingItem item)) continue;

                bool shouldShow = category == CraftingCategory.All || item.recipe.category == category;
                child.gameObject.SetActive(shouldShow);
            }

            foreach (Button b in craftingTabs)
            {
                if (b.TryGetComponent(out InteractiveButton interactive))
                {
                    if (b == button)
                        interactive.Select();
                    else
                        interactive.Deselect();
                }
            }
        }

        public void HighlightSpecificRecipes(CraftingRecipeHighlightTutorial data)
        {
            if (Time.time - data.lastTriggered < data.cooldown) return;

            data.lastTriggered = Time.time;

            List<CraftingItem> matches = new();

            // Find UI items matching the recipe(s)
            foreach (CraftingItem itemUI in GetUnlockedCraftingItems())
            {
                foreach (CraftingRecipe recipe in data.targetRecipes)
                {
                    if (itemUI.recipe == recipe)
                    {
                        // requirements
                        if (data.RequireCraftable && !itemUI.HasItems()) continue;

                        if (data.RequireNotCraftedBefore && itemUI.recipe.hasBeenCraftedBefore) continue;

                        matches.Add(itemUI);
                    }
                }
            }

            if (matches.Count == 0) return;

            // Highlight each target recipe
            foreach (CraftingItem item in matches)
                AnimateRecipeHighlight(item);

            // Auto-stop after duration
            if (data.highlightDuration > 0)
                StartCoroutine(StopRecipeHighlightAfterDelay(matches, data.highlightDuration));
        }

        private IEnumerator StopRecipeHighlightAfterDelay(List<CraftingItem> items, float delay)
        {
            yield return new WaitForSeconds(delay);

            foreach (CraftingItem item in items)
                StopRecipeHighlight(item);
        }

        public void HighlightSuggestedRecipe()
        {
            // Find all craftable but NOT YET CRAFTED recipes
            List<CraftingItem> possible = new();

            foreach (CraftingItem itemUI in GetUnlockedCraftingItems())
            {
                if (itemUI.HasItems() && !itemUI.recipe.hasBeenCraftedBefore)
                    possible.Add(itemUI);
            }

            if (possible.Count == 0) return;

            // Choose the best one (e.g., first unlocked or easiest to craft)
            CraftingItem suggestion = possible[0];

            AnimateRecipeHighlight(suggestion);
        }

        private List<CraftingItem> GetUnlockedCraftingItems()
        {
            List<CraftingItem> items = new();
            foreach (CraftingItem item in craftingItemParent.transform.GetComponentsInChildren<CraftingItem>())
                items.Add(item);

            return items;
        }

        private void AnimateRecipeHighlight(CraftingItem item)
        {
            UIImageAnimator anim = item.GetComponentInChildren<UIImageAnimator>(true);

            if (anim == null)
            {
                Debug.LogWarning("No UIImageAnimator found under " + item.name);
                return;
            }

            anim.gameObject.SetActive(true);
            anim.Play();
        }

        private void StopRecipeHighlight(CraftingItem item)
        {
            UIImageAnimator anim = item.GetComponentInChildren<UIImageAnimator>(true);

            if (anim == null) return;

            anim.Stop();
            anim.gameObject.SetActive(false);
        }

        public void ClearHighlights()
        {
            foreach (CraftingItem itemUI in GetUnlockedCraftingItems())
                StopRecipeHighlight(itemUI);
        }

        public void SaveCraftingProgress()
        {
            CraftingSaveData saveData = new();

            // Save unlocked recipes
            foreach (CraftingRecipe recipe in unlockedRecipes)
                saveData.unlockedRecipeIDs.Add(recipe.name);

            // Save which recipes were crafted before
            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
                if (recipe.hasBeenCraftedBefore)
                    saveData.craftedBeforeIDs.Add(recipe.name);

            SaveSystem.SaveCrafting(WorldSession.CurrentWorldName, saveData);
        }

        public void LoadCraftingProgress()
        {
            unlockedRecipes.Clear();
            foreach (Transform child in craftingItemParent)
                Destroy(child.gameObject);

            CraftingSaveData saveData = SaveSystem.LoadCrafting(WorldSession.CurrentWorldName);

            if (saveData == null) return;

            foreach (string recipeName in saveData.unlockedRecipeIDs)
            {
                CraftingRecipe recipe = craftingDatabase.GetRecipeByID(recipeName);
                if (recipe != null)
                {
                    unlockedRecipes.Add(recipe);

                    // Rebuild UI
                    GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
                    CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();
                    uiItem.recipe = recipe;
                    uiItem.itemImage.sprite = recipe.resultItem.icon;
                }
            }

            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
                recipe.hasBeenCraftedBefore = saveData.craftedBeforeIDs.Contains(recipe.name);
        }
    }
}
