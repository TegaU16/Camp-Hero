using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Smelting/Recipe")]
public class SmeltingRecipe : ScriptableObject
{
    public Item resultItem;
    public Item requiredItem;

    public bool ShouldUnlock(List<Item> discoveredItems)
    {
        return discoveredItems.Contains(requiredItem);
    }
}
