using Game.Smelting;
using UnityEngine;

namespace Game.Tutorial
{
    public class SmeltingTutorialHandler : MonoBehaviour
    {
        void OnEnable() => TutorialEventBus.OnTutorialTriggered += OnTutorialTriggered;
        void OnDisable() => TutorialEventBus.OnTutorialTriggered -= OnTutorialTriggered;

        private void OnTutorialTriggered(TutorialData data)
        {
            if (data.smeltingRecipeHighlightData != null)
            {
                FurnaceManager.Instance.HighlightSpecificRecipes(data.smeltingRecipeHighlightData);
                return;
            }

            if (data.id == "suggest_smelting_recipe")
                FurnaceManager.Instance.HighlightSuggestedRecipe();
        }
    }
}
