using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Inventory;
using Game.Crafting;
using Game.Smelting;

namespace Game.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance;
        private readonly Dictionary<string, TutorialData> tutorials = new();

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
            data.isActive = true;
            data.lastTriggeredTime = Time.time;

            TutorialEventBus.TriggerTutorial(data);

            if (data.duration > 0)
                Invoke(nameof(TimeoutTutorialInternal), data.duration);

            void TimeoutTutorialInternal()
            {
                if (!data.isActive) return;

                data.isActive = false;
                Debug.Log($"Tutorial timed out: {data.description}");
                TutorialEventBus.TimeoutTutorial(data);
            }
        }

        public void CompleteTutorial(TutorialData data)
        {
            data.isActive = false;
            TutorialEventBus.CompleteTutorial(data);
        }

        public List<TutorialData> CreatePickupCraftingRecipeTutorials(Item item)
        {
            // Pull recipes intentionally mapped to this item
            List<Item> discoveredItems = InventoryManager.Instance.discoveredItems;
            CraftingRecipe[] matchingRecipes = CraftingManager.Instance.craftingDatabase.GetRecipesUnlockedByItem(item, discoveredItems);

            if (matchingRecipes == null || matchingRecipes.Length == 0) return null;

            // Create ScriptableObject instance dynamically
            CraftingRecipeHighlightTutorial highlightSO =
                ScriptableObject.CreateInstance<CraftingRecipeHighlightTutorial>();

            highlightSO.targetRecipes = matchingRecipes;
            highlightSO.highlightDuration = 5f;
            highlightSO.cooldown = 9999f;
            highlightSO.RequireCraftable = false;
            highlightSO.RequireNotCraftedBefore = true;

            List<TutorialData> tutorials = new();

            // Create tutorial instances
            foreach (CraftingRecipe recipe in matchingRecipes) 
            {
                TutorialData tutorial = new()
                {
                    id = $"{recipe.resultItem.itemName}_crafting",
                    description = $"Highlight recipes unlocked by picking up {item.itemName}",
                    type = TutorialType.HighlightObject,
                    craftingRecipeHighlightData = highlightSO,
                    duration = 5f,
                    cooldown = 9999f,

                    triggerCondition = () => true,
                    completionCondition = null
                };

                tutorials.Add(tutorial);
            }

            return tutorials;
        }

        public List<TutorialData> CreatePickupSmeltingRecipeTutorials(Item item)
        {
            List<Item> discoveredItems = InventoryManager.Instance.discoveredItems;
            SmeltingRecipe[] matchingRecipes = FurnaceManager.Instance.smeltingDatabase.GetRecipesUnlockedByItem(item, discoveredItems);

            if (matchingRecipes == null || matchingRecipes.Length == 0) return null;

            SmeltingRecipeHighlightTutorial highlightSO =
                ScriptableObject.CreateInstance<SmeltingRecipeHighlightTutorial>();

            highlightSO.targetRecipes = matchingRecipes;
            highlightSO.highlightDuration = 5f;
            highlightSO.cooldown = 9999f;
            highlightSO.RequireSmeltable = false;
            highlightSO.RequireNotSmeltedBefore = true;

            List<TutorialData> tutorials = new();

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
                    cooldown = 9999f,

                    triggerCondition = () => true,
                    completionCondition = null
                };

                tutorials.Add(tutorial);
            }

            return tutorials;
        }

        public TutorialData GetTutorialData(string id)
        {
            tutorials.TryGetValue(id, out TutorialData data);
            return data;
        }
    }
}
