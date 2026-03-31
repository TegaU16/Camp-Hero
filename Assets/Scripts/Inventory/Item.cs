using Game.Food;
using Game.Players;
using Game.StatusEffects;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(menuName = "Item/Item")]
    public class Item : ScriptableObject
    {
        [Header("Only Gameplay")]
        public ItemType itemTypes;
        public ActionType actionType;

        [Header("Only UI")]
        public int maxStack = 1;
        public Sprite icon;
        public string itemName;

        [Header("Both")]
        public GameObject equippedPrefab;
        public GameObject itemDrop;
        public Rarity rarity;

        [Header("Only Building")]
        public GameObject buildingGhost;
        public Vector2Int buildingSize = new(1, 1);

        [Header("Combat")]
        public AttackSet attackSet;
        public float attackDistance = 3f;
        public float[] attackDamage = new float[2];
        public float critChance = 2f;
        public float critFactor = 2f;

        [Header("Tool")]
        public int toolLevel;
        public ToolType toolType;
        [HideInInspector] public ToolAttribute toolAttribute;
        public Item[] itemsToPair;
        public StatusEffect statusEffect;

        [Header("Food")]
        public float foodValue;

        [Header("Farming")]
        public PlantData plantData;

        [Header("Fuel")]
        public float fuelValue;

        [Header("Smelting")]
        public float smeltRate = 0.01f;
        public Item output;
        public int outputCount = 1;
    }

    [System.Flags]
    public enum ItemType
    {
        General = 1 << 0,
        Building = 1 << 1,
        Food = 1 << 2,
        Fuel = 1 << 3,
        Smelting = 1 << 4,
        Reforging = 1 << 5
    }

    public enum ActionType
    {
        None,
        Action
    }

    [System.Flags]
    public enum ToolType
    {
        Axe = 1 << 0,
        Pickaxe = 1 << 1,
        Sword = 1 << 2
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }
}
