using System.Collections.Generic;
using Game.Upgrades;
using TMPro;
using UnityEngine;

namespace Game.Players
{
    [System.Serializable]
    public class PlayerStats
    {
        [System.Serializable]
        public class Stat
        {
            public string Name;
            [HideInInspector] public int Value;
            public int MaxValue;
            public TextMeshProUGUI OuterLevelText;
            public Sprite Icon;
            public List<StatUpgrade> upgrades;
        }

        [System.Serializable]
        public class UpgradeBase<T> where T : UpgradeEffect
        {
            [HideInInspector] public bool purchased;
            public GameObject upgradeButtonObj;
            public T effect;

            public string UpgradeName => effect.upgradeName;
            public int Cost => effect.cost;
        }

        [System.Serializable]
        public class StatUpgrade : UpgradeBase<UpgradeEffect>
        {
        }

        [System.Serializable]
        public class CampfireUpgrade : UpgradeBase<CampfireUpgradeEffect>
        {
            public int LevelRequirement => effect.levelRequirement;
        }

        public Stat strength;
        public Stat vitality;
        public Stat endurance;
        public Stat stamina;
        public Stat luck;
        public Stat speed;

        public List<CampfireUpgrade> campfireUpgrades;

        [HideInInspector] public int availablePoints = 99;
        [HideInInspector] public int goldenPoints = 99;
    }
}
