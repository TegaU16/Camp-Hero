using UnityEngine;

namespace Game.Terrain
{
    [System.Serializable]
    public class OreBand
    {
        public OreType oreType;

        [Range(0f, 1f)]
        public float minRadiusPercent;

        [Range(0f, 1f)]
        public float maxRadiusPercent;

        [Range(0f, 1f)]
        public float depositChance = 0.5f;

        [Header("Per Deposit Settings")]
        public float minDepositRadius = 8;
        public float maxDepositRadius = 20;
        
        public int minOreSpacing = 2;
        public int maxOreSpacing = 4;

        public int minOreCount = 3;
        public int maxOreCount = 5;
    }
}
