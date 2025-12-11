using Game.Smelting;
using UnityEngine;

namespace Game.Tutorial
{
    [CreateAssetMenu(menuName = "Tutorials/Smelting Recipe Highlight Tutorial")]
    public class SmeltingRecipeHighlightTutorial : ScriptableObject
    {
        [Header("Recipes to highlight")]
        public SmeltingRecipe[] targetRecipes;

        [Header("Timing")]
        public float highlightDuration = 5f;  // 0 = until manually cleared
        public float cooldown = 10f;

        [HideInInspector] public float lastTriggered = -999f;

        // Optional: trigger only if this condition is true
        public bool RequireSmeltable = true;

        // Optional: trigger only if player hasn't crafted the recipe before
        public bool RequireNotSmeltedBefore = true;
    }
}
