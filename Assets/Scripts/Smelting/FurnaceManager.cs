using System.Collections;
using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using Game.Tutorial;
using UnityEngine;
using Worlds;

namespace Game.Smelting
{
    public class FurnaceManager : MonoBehaviour
    {
        public static FurnaceManager Instance;

        public FurnaceUI furnaceUI;
        public const float fuelDecreaseRate = -0.01f;

        [Header("State")]
        public SmeltingDatabase smeltingDatabase;
        private readonly List<SmeltingRecipe> unlockedRecipes = new();

        private void Awake()
        {
            Instance = this;
        }

        public void TryUnlockRecipes(List<Item> discoveredItems)
        {
            foreach (SmeltingRecipe recipe in smeltingDatabase.allRecipes)
            {
                if (!unlockedRecipes.Contains(recipe) && recipe.ShouldUnlock(discoveredItems))
                    UnlockRecipe(recipe);
            }
        }

        public void UnlockRecipe(SmeltingRecipe recipe)
        {
            if (unlockedRecipes.Contains(recipe)) return;

            unlockedRecipes.Add(recipe);
            furnaceUI.AddUnlockedRecipe(recipe);
        }

        public void HighlightSpecificRecipes(SmeltingRecipeHighlightTutorial data)
        {
            if (Time.time - data.lastTriggered < data.cooldown) return;

            data.lastTriggered = Time.time;

            List<FurnaceItem> matches = new();

            // Find UI items matching the recipe(s)
            foreach (FurnaceItem itemUI in GetUnlockedFurnaceItems())
            {
                foreach (SmeltingRecipe recipe in data.targetRecipes)
                {
                    if (itemUI.recipe != recipe) continue;
                    if (data.RequireNotSmeltedBefore && itemUI.recipe.hasBeenSmeltedBefore) continue;

                    matches.Add(itemUI);
                }
            }

            if (matches.Count == 0) return;

            // Highlight each target recipe
            foreach (FurnaceItem item in matches)
                AnimateRecipeHighlight(item);

            // Auto-stop after duration
            if (data.highlightDuration > 0)
                StartCoroutine(StopRecipeHighlightAfterDelay(matches, data.highlightDuration));
        }

        private IEnumerator StopRecipeHighlightAfterDelay(List<FurnaceItem> items, float delay)
        {
            yield return new WaitForSeconds(delay);

            foreach (FurnaceItem item in items)
                StopRecipeHighlight(item);
        }

        public void HighlightSuggestedRecipe()
        {
            // Find all craftable but NOT YET CRAFTED recipes
            List<FurnaceItem> possible = new();

            foreach (FurnaceItem itemUI in GetUnlockedFurnaceItems())
            {
                if (!itemUI.recipe.hasBeenSmeltedBefore)
                    possible.Add(itemUI);
            }

            if (possible.Count == 0) return;

            // Choose the best one (e.g., first unlocked or easiest to craft)
            FurnaceItem suggestion = possible[0];

            AnimateRecipeHighlight(suggestion);
        }

        private List<FurnaceItem> GetUnlockedFurnaceItems()
        {
            List<FurnaceItem> items = new();
            foreach (FurnaceItem item in furnaceUI.furnaceItemParent.transform.GetComponentsInChildren<FurnaceItem>())
                items.Add(item);

            return items;
        }

        private void AnimateRecipeHighlight(FurnaceItem item)
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

        private void StopRecipeHighlight(FurnaceItem item)
        {
            UIImageAnimator anim = item.GetComponentInChildren<UIImageAnimator>(true);
            if (anim == null) return;

            anim.Stop();
            anim.gameObject.SetActive(false);
        }

        public void SaveSmeltingProgress()
        {
            SmeltingSaveData saveData = new();

            // Save unlocked recipes
            foreach (SmeltingRecipe recipe in unlockedRecipes)
                saveData.unlockedRecipeIDs.Add(recipe.name);

            // Save which recipes were smelted before
            foreach (SmeltingRecipe recipe in smeltingDatabase.allRecipes)
            {
                if (recipe.hasBeenSmeltedBefore)
                    saveData.smeltedBeforeIDs.Add(recipe.name);
            }

            SaveSystem.SaveSmelting(WorldSession.CurrentWorldName, saveData);
        }

        public void LoadSmeltingProgress()
        {
            unlockedRecipes.Clear();
            foreach (Transform child in furnaceUI.furnaceItemParent)
                Destroy(child.gameObject);

            SmeltingSaveData saveData = SaveSystem.LoadSmelting(WorldSession.CurrentWorldName);

            if (saveData == null) return;

            // Load unlocked recipes
            foreach (string recipeName in saveData.unlockedRecipeIDs)
            {
                SmeltingRecipe recipe = smeltingDatabase.GetRecipeByID(recipeName);
                if (recipe == null) continue;

                unlockedRecipes.Add(recipe);
                furnaceUI.AddUnlockedRecipe(recipe);
            }

            // Load smelted-before flags
            foreach (SmeltingRecipe recipe in smeltingDatabase.allRecipes)
                recipe.hasBeenSmeltedBefore = saveData.smeltedBeforeIDs.Contains(recipe.name);
        }
    }
}
