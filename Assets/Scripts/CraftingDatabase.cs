using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Crafting/Database")]
public class CraftingDatabase : ScriptableObject
{
    public List<CraftingRecipe> allRecipes;
}
