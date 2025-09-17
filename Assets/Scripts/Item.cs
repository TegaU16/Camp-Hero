using UnityEngine;

[CreateAssetMenu(menuName = "Item/Item")]
public class Item : ScriptableObject
{
    [Header("Only Gameplay")]
    public ItemType itemType;
    public ActionType actionType;

    [Header("Only UI")]
    public bool stackable = true;
    public int maxStack = 1;
    public Sprite icon;
    public string itemName;

    [Header("Both")]
    public GameObject equippedPrefab;
    public GameObject itemDrop;
    public AnimatorOverrideController animatorController;

    [Header("Only Building")]
    public GameObject buildingGhost;
    public float height;
    public Vector2Int buildingSize = new(1, 1);

    [Header("For Melee")]
    public float attackDistance = 3f;
    public float attackSpeed;
    public float attackDelay;
    public float[] attackDamage = new float[2];
    public float critChance = 2f;
    public float critFactor = 2f;
    public LayerMask attackLayer;

    public GameObject hitEffect;
    public AudioClip swingSound;
    public AudioClip hitSound;

    [Header("Tool")]
    public int toolLevel;
    public ToolType toolType;

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

    [HideInInspector]
    public bool attacking = false;
    [HideInInspector]
    public bool readyToAttack = true;
}

public enum ItemType
{
    Potion,
    Weapon,
    Tool,
    Building,
    Armor,
    Food,
    Crafting,
    Fuel,
    Smelting,
    General
}

public enum ActionType
{
    Throw,
    Drink,
    Eat,
    Hit,
    Shoot,
    Place,
    None
}

public enum ToolType
{
    Axe,
    Pickaxe,
    Sword,
    None
}