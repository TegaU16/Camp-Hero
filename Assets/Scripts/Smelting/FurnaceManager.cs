using System.Collections;
using System.Collections.Generic;
using Game.Inventory;
using Game.Saving;
using Game.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worlds;

namespace Game.Smelting
{
    public class FurnaceManager : MonoBehaviour
    {
        public static FurnaceManager Instance;

        [Header("General UI")]
        public Transform furnaceItemParent;
        public GameObject furnaceItemPrefab;
        public FurnaceUI furnaceUI;
        public GameObject recipeSection;
        public GameObject nullItemSelectText;
        public List<Button> smeltingTabs;

        [Header("Process Section")]
        public ArrowFillController progressArrow;

        [Header("Info Section")]
        public Image resultImage;
        public TextMeshProUGUI resultName;
        public Image requiredImage;
        public TextMeshProUGUI requiredName;

        public const float fuelDecreaseRate = -0.01f;

        [Header("State")]
        public SmeltingDatabase smeltingDatabase;
        private readonly List<SmeltingRecipe> unlockedRecipes = new();

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            progressArrow.fillAmount = 0;

            recipeSection.SetActive(false);
            nullItemSelectText.SetActive(true);
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

            GameObject itemGO = Instantiate(furnaceItemPrefab, furnaceItemParent);
            FurnaceItem uiItem = itemGO.GetComponent<FurnaceItem>();
            uiItem.recipe = recipe;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
        }

        public void SetSelectedFurnaceItem(FurnaceItem furnaceItem = null, bool onOpened = false)
        {
            bool itemSelected = furnaceItem != null;

            if (itemSelected)
            {
                resultImage.sprite = furnaceItem.recipe.resultItem.icon;
                requiredImage.sprite = furnaceItem.recipe.requiredItem.icon;

                resultName.text = furnaceItem.recipe.resultItem.name;
                requiredName.text = furnaceItem.recipe.requiredItem.name;
            }

            recipeSection.SetActive(itemSelected);
            nullItemSelectText.SetActive(!itemSelected);

            if (!onOpened)
            {
                FurnaceUnit furnaceUnit = furnaceUI.linkedFurnace;
                if (furnaceUnit != null)
                    furnaceUnit.selectedItem = furnaceItem;
            }
        }

        public void Open(FurnaceUnit unit)
        {
            furnaceUI.Open(unit);
        }

        public void FilterByCategory(SmeltingCategory category, Button button)
        {
            foreach (Transform child in furnaceItemParent)
            {
                if (!child.TryGetComponent(out FurnaceItem item)) continue;

                bool shouldShow = category == SmeltingCategory.All || item.recipe.category == category;
                child.gameObject.SetActive(shouldShow);
            }

            foreach (Button b in smeltingTabs)
            {
                if (b.TryGetComponent(out InteractiveButton interactiveButton))
                {
                    if (b == button)
                        interactiveButton.Select();
                    else
                        interactiveButton.Deselect();
                }
            }
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
                    if (itemUI.recipe == recipe)
                    {
                        // requirements
                        if (data.RequireNotSmeltedBefore && itemUI.recipe.hasBeenSmeltedBefore) continue;

                        matches.Add(itemUI);
                    }
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
            foreach (FurnaceItem item in furnaceItemParent.transform.GetComponentsInChildren<FurnaceItem>())
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
                if (recipe.hasBeenSmeltedBefore)
                    saveData.smeltedBeforeIDs.Add(recipe.name);

            SaveSystem.SaveSmelting(WorldSession.CurrentWorldName, saveData);
        }

        public void LoadSmeltingProgress()
        {
            unlockedRecipes.Clear();
            foreach (Transform child in furnaceItemParent)
                Destroy(child.gameObject);

            SmeltingSaveData saveData = SaveSystem.LoadSmelting(WorldSession.CurrentWorldName);

            if (saveData == null) return;

            // Load unlocked recipes
            foreach (string recipeName in saveData.unlockedRecipeIDs)
            {
                SmeltingRecipe recipe = smeltingDatabase.GetRecipeByID(recipeName);
                if (recipe != null)
                {
                    unlockedRecipes.Add(recipe);

                    // Rebuild UI
                    GameObject itemGO = Instantiate(furnaceItemPrefab, furnaceItemParent);
                    FurnaceItem uiItem = itemGO.GetComponent<FurnaceItem>();
                    uiItem.recipe = recipe;
                    uiItem.itemImage.sprite = recipe.resultItem.icon;
                }
            }

            // Load smelted-before flags
            foreach (SmeltingRecipe recipe in smeltingDatabase.allRecipes)
                recipe.hasBeenSmeltedBefore = saveData.smeltedBeforeIDs.Contains(recipe.name);
        }
    }
}
