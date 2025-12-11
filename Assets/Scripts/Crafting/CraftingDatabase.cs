using UnityEngine;
using System.Collections.Generic;
using Game.Inventory;

namespace Game.Crafting
{
    [CreateAssetMenu(menuName = "Crafting/Database")]
    public class CraftingDatabase : ScriptableObject
    {
        public List<CraftingRecipe> allRecipes;
        private Dictionary<string, CraftingRecipe> recipeLookup;

        private void OnEnable()
        {
            recipeLookup = new Dictionary<string, CraftingRecipe>();
            foreach (CraftingRecipe recipe in allRecipes)
                recipeLookup[recipe.resultItem.name] = recipe;
        }

        public CraftingRecipe GetRecipeByID(string id)
        {
            recipeLookup.TryGetValue(id, out CraftingRecipe recipe);
            return recipe;
        }

        /// <summary>
        /// Returns recipes that become unlocked *specifically* because the player
        /// discovered `newItem`, given the set of items they had previously discovered.
        /// </summary>
        public CraftingRecipe[] GetRecipesUnlockedByItem(Item newItem, List<Item> discoveredBefore)
        {
            discoveredBefore ??= new List<Item>();

            // Create a copy of the list representing discovered items AFTER picking up the new item
            List<Item> discoveredAfter = new(discoveredBefore);
            if (!discoveredAfter.Contains(newItem))
                discoveredAfter.Add(newItem);

            List<CraftingRecipe> newlyUnlocked = new();

            foreach (CraftingRecipe recipe in allRecipes)
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
