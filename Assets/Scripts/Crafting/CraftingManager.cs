using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Inventory;
using Game.Saving;
using Game.Tutorial;
using UnityEngine;
using Worlds;

namespace Game.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance;

        [SerializeField] private GameObject fullInventoryNotification;

        [Header("State")]
        public CraftingDatabase craftingDatabase;
        private readonly List<CraftingRecipe> unlockedRecipes = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start() => DOTween.Init();

        public void TryUnlockRecipes(List<Item> discoveredItems)
        {
            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
            {
                if (recipe.ShouldUnlock(discoveredItems))
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
            CraftingUI.Instance.SpawnCraftingItem(recipe);

            string tutorialID = $"{recipe.resultItem.itemName}_crafting";
            TutorialData craftingRecipeTutorial = TutorialManager.Instance.GetTutorialData(tutorialID);
           
            if (craftingRecipeTutorial != null)
                TutorialManager.Instance.ActivateTutorial(craftingRecipeTutorial);
        }

        /// <summary>
        /// Crafts the currently selected item if requirements are met.
        /// </summary>
        public void Craft()
        {
            CraftingItem selectedItem = CraftingUI.Instance.selectedItem;
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

            if (InventoryManager.Instance.IsInventoryFullForItem(item))
            {
                TextNotification fullInv = TextNotificationPool.Instance.GetTextNotification(fullInventoryNotification);
                fullInv.gameObject.SetActive(true);
                fullInv.Setup();
                yield break;
            }

            Item itemWithAttribute = InventoryManager.Instance.SetAttributeForTool(item);
            InventoryManager.Instance.AddItem(itemWithAttribute);
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
                    if (itemUI.recipe != recipe) continue;
                    if (data.RequireCraftable && !itemUI.HasItems()) continue;
                    if (data.RequireNotCraftedBefore && itemUI.recipe.hasBeenCraftedBefore) continue;

                    matches.Add(itemUI);
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
            CraftingItem[] items = CraftingUI.Instance.craftingItemParent.transform.GetComponentsInChildren<CraftingItem>();
            return items.ToList();
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

        public void ClearHighlights(CraftingRecipeHighlightTutorial craftingRecipeHighlightTutorial)
        {
            CraftingRecipe[] tutorialRecipes = craftingRecipeHighlightTutorial.targetRecipes;
            foreach (CraftingItem itemUI in GetUnlockedCraftingItems())
            {
                if (tutorialRecipes.Contains(itemUI.recipe))
                    StopRecipeHighlight(itemUI);
            }
        }

        public void SaveCraftingProgress()
        {
            CraftingSaveData saveData = new();

            // Save unlocked recipes
            foreach (CraftingRecipe recipe in unlockedRecipes)
                saveData.unlockedRecipeIDs.Add(recipe.name);

            // Save which recipes were crafted before
            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
            {
                if (recipe.hasBeenCraftedBefore)
                    saveData.craftedBeforeIDs.Add(recipe.name);
            }

            SaveSystem.SaveCrafting(WorldSession.CurrentWorldName, saveData);
        }

        public void LoadCraftingProgress()
        {
            unlockedRecipes.Clear();
            foreach (Transform child in CraftingUI.Instance.craftingItemParent)
                Destroy(child.gameObject);

            CraftingSaveData saveData = SaveSystem.LoadCrafting(WorldSession.CurrentWorldName);
            if (saveData == null) return;

            foreach (string recipeName in saveData.unlockedRecipeIDs)
            {
                CraftingRecipe recipe = craftingDatabase.GetRecipeByID(recipeName);
                if (recipe == null) continue;

                unlockedRecipes.Add(recipe);
                CraftingUI.Instance.SpawnCraftingItem(recipe);
            }

            foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
                recipe.hasBeenCraftedBefore = saveData.craftedBeforeIDs.Contains(recipe.name);
        }
    }
}
