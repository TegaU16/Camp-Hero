using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Smelting/Database")]
public class SmeltingDatabase : ScriptableObject
{
    public List<SmeltingRecipe> allRecipes;
}
