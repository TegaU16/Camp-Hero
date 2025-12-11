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
        private readonly List<TutorialData> tutorials = new();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Update()
        {
            foreach (TutorialData tutorial in tutorials)
            {
                if (tutorial.isActive || Time.time - tutorial.lastTriggeredTime < tutorial.cooldown) continue;

                if (tutorial.triggerCondition != null && tutorial.triggerCondition())
                    ActivateTutorial(tutorial);
            }

            foreach (TutorialData tutorial in tutorials.Where(t => t.isActive))
            {
                if (tutorial.completionCondition != null && tutorial.completionCondition())
                    CompleteTutorial(tutorial);
            }
        }

        private void RegisterTutorial(TutorialData data) => tutorials.Add(data);

        public void RegisterAndActivate(TutorialData data)
        {
            RegisterTutorial(data);
            ActivateTutorial(data);
        }

        private void ActivateTutorial(TutorialData data)
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

        private void CompleteTutorial(TutorialData data)
        {
            data.isActive = false;
            TutorialEventBus.CompleteTutorial(data);
        }

        public TutorialData CreatePickupCraftingRecipeTutorial(Item item)
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

            // Create tutorial instance
            TutorialData tutorial = new()
            {
                id = $"pickup_{item.itemName}_recipe_tutorial",
                description = $"Highlight recipes unlocked by picking up {item.itemName}",
                type = TutorialType.HighlightObject,
                craftingRecipeHighlightData = highlightSO,
                duration = 5f,
                cooldown = 9999f,

                triggerCondition = () => true,
                completionCondition = null
            };

            return tutorial;
        }

        public TutorialData CreatePickupSmeltingRecipeTutorial(Item item)
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

            TutorialData tutorial = new()
            {
                id = $"pickup_{item.itemName}_recipe_tutorial",
                description = $"Highlight smelting recipes unlocked by picking up {item.itemName}",
                type = TutorialType.HighlightObject,
                smeltingRecipeHighlightData = highlightSO,
                duration = 5f,
                cooldown = 9999f
            };

            return tutorial;
        }
    }
}
