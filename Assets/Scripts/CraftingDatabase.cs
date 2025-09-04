using UnityEngine;
using System.Collections.Generic;

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
}
