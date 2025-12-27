using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Crafting
{
    [CreateAssetMenu(menuName = "Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [System.Serializable]
        public struct Requirement
        {
            public Item requiredItem;
            public int count;
        }

        public Item resultItem;
        public Requirement[] requirements;
        public CraftingCategory category;
        public CraftingSource source;
        [HideInInspector] public bool hasBeenCraftedBefore;

        [Header("Unlocking Logic")]
        [Tooltip("Leave empty to unlock this recipe when any requirement item is discovered.")]
        public Item[] itemsRequiredToUnlock;

        private void OnValidate()
        {
            if (source == 0)
                source = CraftingSource.Base;
        }

        /// <summary>
        /// Determines if this recipe should be unlocked given the player's discovered items.
        /// </summary>
        public bool ShouldUnlock(List<Item> discoveredItems)
        {
            if (itemsRequiredToUnlock == null || itemsRequiredToUnlock.Length == 0)
            {
                // Default: unlock if ANY requirement is discovered
                foreach (Requirement req in requirements)
                {
                    if (discoveredItems.Contains(req.requiredItem)) return true;
                }

                return false;
            }

            // Custom: unlock only if ALL listed unlock items have been discovered
            foreach (Item unlockItem in itemsRequiredToUnlock)
            {
                if (!discoveredItems.Contains(unlockItem)) return false;
            }

            return true;
        }
    }

    public enum CraftingCategory
    {
        All,
        Tools,
        Stations,
        Defenses,
        Consumables,
        Resources
    }

    [System.Flags]
    public enum CraftingSource
    {
        Base = 1 << 0,
        Workbench = 1 << 1
    }
}
