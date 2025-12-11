using Game.Inventory;
using UnityEngine;

namespace Game.Food
{
    [CreateAssetMenu(menuName = "Farming/PlantData")]
    public class PlantData : ScriptableObject
    {
        public Item plant;
        public int harvestAmount;
        public float totalGrowthTime; // in seconds
        public GameObject[] growthStages; // one prefab per stage
    }
}
