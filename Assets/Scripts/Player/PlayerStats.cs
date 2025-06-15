using TMPro;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    [System.Serializable]
    public struct Stat
    {
        public int Value;
        public TextMeshProUGUI LevelText;
    }

    public Stat strength;
    public Stat vitality;
    public Stat endurance;
    public Stat stamina;
    public Stat luck;

    [Header("Points")]
    public int availablePoints = 0;
}
