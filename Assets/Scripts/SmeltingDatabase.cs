using System.Collections.Generic;
using UnityEngine;

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
}
