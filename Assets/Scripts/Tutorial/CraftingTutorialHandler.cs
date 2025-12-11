using Game.Crafting;
using UnityEngine;

namespace Game.Tutorial
{
    public class CraftingTutorialHandler : MonoBehaviour
    {
        void OnEnable()
        {
            TutorialEventBus.OnTutorialTriggered += OnTutorialTriggered;
            TutorialEventBus.OnTutorialTimedOut += (_) => CraftingManager.Instance.ClearHighlights();
            TutorialEventBus.OnTutorialCompleted += (_) => CraftingManager.Instance.ClearHighlights();
        }

        void OnDisable() => TutorialEventBus.OnTutorialTriggered -= OnTutorialTriggered;

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
    }
}
