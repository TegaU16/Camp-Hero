using System.Collections;
using System.Collections.Generic;
using Game.Crafting;
using Game.Inventory;
using Game.Smelting;
using UnityEngine;

namespace Game.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance;
        private readonly Dictionary<string, TutorialData> tutorials = new();

        public string recipeUnlockTag = "_unlock";

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void RegisterTutorial(TutorialData data) => tutorials[data.id] = data;

        public void ActivateTutorial(TutorialData data)
        {
            // Cancel other active popup tutorials
            if (data.type == TutorialType.PopupMessage)
            {
                foreach (TutorialData tutorial in tutorials.Values)
                {
                    if (!tutorial.isActive || tutorial.type != TutorialType.PopupMessage || tutorial == data) continue;

                    if (tutorial.id.Contains(recipeUnlockTag))
                        StartCoroutine(TimeoutRoutine(tutorial));
                    else
                        TimeoutTutorialInternal(tutorial);
                }
            }

            data.isActive = true;
            data.lastTriggeredTime = Time.time;

            TutorialEventBus.TriggerTutorial(data);

            if (data.duration > 0)
                StartCoroutine(TimeoutRoutine(data));
        }

        private IEnumerator TimeoutRoutine(TutorialData data)
        {
            yield return new WaitForSeconds(data.duration);

            if (!data.isActive) yield break;

            TimeoutTutorialInternal(data);
        }

        private void TimeoutTutorialInternal(TutorialData data)
        {
            data.isActive = false;
            TutorialEventBus.TimeoutTutorial(data);
        }

        public void CompleteTutorial(TutorialData data)
        {
            data.isActive = false;
            TutorialEventBus.CompleteTutorial(data);
        }

        public void CreatePickupCraftingRecipeTutorials(Item item)
        {
            // Pull recipes intentionally mapped to this item
            List<Item> discoveredItems = InventoryManager.Instance.discoveredItems;
            CraftingDatabase craftingDatabase = CraftingManager.Instance.craftingDatabase;
            CraftingRecipe[] matchingRecipes = craftingDatabase.GetRecipesUnlockedByItem(item, discoveredItems);

            if (matchingRecipes == null || matchingRecipes.Length == 0) return;

            // Create ScriptableObject instance dynamically
            CraftingRecipeHighlightTutorial highlightSO = ScriptableObject.CreateInstance<CraftingRecipeHighlightTutorial>();

            highlightSO.targetRecipes = matchingRecipes;
            highlightSO.highlightDuration = 5f;
            highlightSO.cooldown = 9999f;
            highlightSO.RequireCraftable = false;
            highlightSO.RequireNotCraftedBefore = true;

            // Create tutorial instances
            foreach (CraftingRecipe recipe in matchingRecipes) 
            {
                TutorialData highlightTutorial = new()
                {
                    id = $"{recipe.resultItem.itemName}_crafting",
                    description = $"Highlight recipes unlocked by picking up {item.itemName}",
                    type = TutorialType.HighlightObject,
                    craftingRecipeHighlightData = highlightSO,
                    duration = 5f,
                    cooldown = 9999f
                };
                RegisterTutorial(highlightTutorial);

                RecipeUnlockNotification notification = new()
                {
                    resultItem = recipe.resultItem,
                    icon = recipe.resultItem.icon,
                    duration = 2.4f
                };

                RecipeUnlockPopupQueue.Instance.Enqueue(notification);
            }
        }

        public void CreatePickupSmeltingRecipeTutorials(Item item)
        {
            List<Item> discoveredItems = InventoryManager.Instance.discoveredItems;
            SmeltingDatabase smeltingDatabase = FurnaceManager.Instance.smeltingDatabase;
            SmeltingRecipe[] matchingRecipes = smeltingDatabase.GetRecipesUnlockedByItem(item, discoveredItems);

            if (matchingRecipes == null || matchingRecipes.Length == 0) return;

            SmeltingRecipeHighlightTutorial highlightSO = ScriptableObject.CreateInstance<SmeltingRecipeHighlightTutorial>();

            highlightSO.targetRecipes = matchingRecipes;
            highlightSO.highlightDuration = 5f;
            highlightSO.cooldown = 9999f;
            highlightSO.RequireSmeltable = false;
            highlightSO.RequireNotSmeltedBefore = true;

            // Create tutorial instances
            foreach (SmeltingRecipe recipe in matchingRecipes)
            {
                TutorialData tutorial = new()
                {
                    id = $"{recipe.resultItem.itemName}_smelting",
                    description = $"Highlight recipes unlocked by picking up {item.itemName}",
                    type = TutorialType.HighlightObject,
                    smeltingRecipeHighlightData = highlightSO,
                    duration = 5f,
                    cooldown = 9999f
                };
                RegisterTutorial(tutorial);

                RecipeUnlockNotification notification = new()
                {
                    resultItem = recipe.resultItem,
                    icon = recipe.resultItem.icon,
                    duration = 2.4f
                };
                RecipeUnlockPopupQueue.Instance.Enqueue(notification);
            }
        }

        public TutorialData GetTutorialData(string id)
        {
            tutorials.TryGetValue(id, out TutorialData data);
            return data;
        }
    }
}
