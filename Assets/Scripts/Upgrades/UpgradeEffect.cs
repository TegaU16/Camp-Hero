using Game.Players;
using UnityEngine;

namespace Game.Upgrades
{
    public abstract class UpgradeEffect : ScriptableObject
    {
        public string upgradeName;
        [TextArea] public string description;
        public int cost;

        public abstract void OnUnlocked(Player player);
        public abstract void OnRemoved(Player player); // Optional for reversible effects
    }
}
