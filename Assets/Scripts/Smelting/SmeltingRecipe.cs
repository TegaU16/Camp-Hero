using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Smelting
{
    [CreateAssetMenu(menuName = "Smelting/Recipe")]
    public class SmeltingRecipe : ScriptableObject
    {
        public Item resultItem;
        public Item requiredItem;
        public SmeltingCategory category;

        [HideInInspector] public bool hasBeenSmeltedBefore;

        public bool ShouldUnlock(List<Item> discoveredItems) => discoveredItems.Contains(requiredItem);
    }

    public enum SmeltingCategory
    {
        All,
        Resources,
        Food,
    }
}
