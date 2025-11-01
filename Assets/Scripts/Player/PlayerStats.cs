using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    [System.Serializable]
    public struct Stat
    {
        [HideInInspector] public int Value;
        public int MaxValue;
        public TextMeshProUGUI OuterLevelText;
        public List<StatUpgrade> upgrades;
    }

    [System.Serializable]
    public class StatUpgrade
    {
        [HideInInspector] public bool purchased;
        public GameObject upgradeButtonObj;
        public UpgradeEffect effect;

        public string UpgradeName => effect.upgradeName;
        public int Cost => effect.cost;
    }

    public Stat strength;
    public Stat vitality;
    public Stat endurance;
    public Stat stamina;
    public Stat luck;
    public Stat speed;

    [HideInInspector] public int availablePoints = 0;
    [HideInInspector] public int goldenPoints = 0;
}
