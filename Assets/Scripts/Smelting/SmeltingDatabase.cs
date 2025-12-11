using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Smelting
{
    [CreateAssetMenu(menuName = "Smelting/Database")]
    public class SmeltingDatabase : ScriptableObject
    {
        public List<SmeltingRecipe> allRecipes;
        private Dictionary<string, SmeltingRecipe> recipeLookup;

        private void OnEnable()
        {
            recipeLookup = new Dictionary<string, SmeltingRecipe>();
            foreach (SmeltingRecipe recipe in allRecipes)
                recipeLookup[recipe.resultItem.name] = recipe;
        }

        public SmeltingRecipe GetRecipeByID(string id)
        {
            recipeLookup.TryGetValue(id, out SmeltingRecipe recipe);
            return recipe;
        }

        public SmeltingRecipe GetRecipeByInput(Item input)
        {
            foreach (SmeltingRecipe recipe in allRecipes)
            {
                if (recipe.requiredItem == input) return recipe;
            }

            return null;
        }

        /// <summary>
        /// Returns recipes that become unlocked *specifically* because the player
        /// discovered `newItem`, given the set of items they had previously discovered.
        /// </summary>
        public SmeltingRecipe[] GetRecipesUnlockedByItem(Item newItem, List<Item> discoveredBefore)
        {
            discoveredBefore ??= new List<Item>();

            // Create a copy of the list representing discovered items AFTER picking up the new item
            List<Item> discoveredAfter = new(discoveredBefore);
            if (!discoveredAfter.Contains(newItem))
                discoveredAfter.Add(newItem);

            List<SmeltingRecipe> newlyUnlocked = new();

            foreach (SmeltingRecipe recipe in allRecipes)
            {
                bool unlockedBefore = recipe.ShouldUnlock(discoveredBefore);
                bool unlockedAfter = recipe.ShouldUnlock(discoveredAfter);

                // The recipe becomes unlocked *only* at this moment
                if (!unlockedBefore && unlockedAfter)
                    newlyUnlocked.Add(recipe);
            }

            return newlyUnlocked.ToArray();
        }
    }
}
