using Game.Crafting;
using UnityEngine;

namespace Game.Tutorial
{
    public class CraftingTutorialHandler : MonoBehaviour
    {
        void OnEnable()
        {
            TutorialEventBus.OnTutorialTriggered += OnTutorialTriggered;
            TutorialEventBus.OnTutorialTimedOut += OnTutorialClosed;
            TutorialEventBus.OnTutorialCompleted += OnTutorialClosed;
        }

        void OnDisable()
        {
            TutorialEventBus.OnTutorialTriggered -= OnTutorialTriggered;
            TutorialEventBus.OnTutorialTimedOut -= OnTutorialClosed;
            TutorialEventBus.OnTutorialCompleted -= OnTutorialClosed;
        }

        private void OnTutorialTriggered(TutorialData data)
        {
            if (data.craftingRecipeHighlightData != null)
            {
                CraftingManager.Instance.HighlightSpecificRecipes(data.craftingRecipeHighlightData);
                return;
            }

            if (data.id == "suggest_crafting_recipe")
                CraftingManager.Instance.HighlightSuggestedRecipe();
        }

        private void OnTutorialClosed(TutorialData data)
        {
            if (data.craftingRecipeHighlightData != null)
                CraftingManager.Instance.ClearHighlights(data.craftingRecipeHighlightData);
        }
    }
}
