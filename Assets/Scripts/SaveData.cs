using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public int maxHealth;
    public int currentHealth;
    public float maxStamina;
    public float currentStamina;
    public PlayerAttributesData attributes;
    public List<ItemData> inventory;
    public LevelData levelData;
}

[System.Serializable]
public class AnimalSaveData
{
    public string prefabID;
    public Vector3 position;
    public int currentHealth;
}

[System.Serializable]
public class WorldAnimalData
{
    public List<AnimalSaveData> animals = new();
}

[System.Serializable]
public class EnemySaveData
{
    public string prefabName;
    public Vector3 position;
    public int currentHealth;
}

[System.Serializable]
public class WorldEnemyData
{
    public List<EnemySaveData> enemies = new();
}

[System.Serializable]
public class WorldMetaData
{
    public string worldName;
    public string seed;
    public string createdDate;
    public string lastPlayedDate;
    public Difficulty difficulty;
    public WorldState worldState = WorldState.Active;
    public RunStats worldStats;
}

[System.Serializable]
public class ChunkSaveData
{
    public Vector3 chunkPosition;
    public List<SpawnedObjectData> spawnedObjects;
    public bool hasNaturalObjects;
    public bool hasKeyStructure;

    public List<string> furnaceStates = new();
    public List<string> storageStates = new();
}

[System.Serializable]
public class SpawnedObjectData
{
    public Vector3 position;
    public string prefabID;
    public string savedStateJson;

    public SpawnedObjectData(Vector3 pos, GameObject instanceObj, GameObject prefabObj)
    {
        position = pos;

        prefabID = prefabObj.GetComponent<PrefabID>().prefabKey;

        if (instanceObj != null && instanceObj.TryGetComponent(out ISaveableObject saveable))
            savedStateJson = saveable.SaveState();
        else
            savedStateJson = null;
    }

    public SpawnedObjectData() { }
}

[System.Serializable]
public class InteractableItemData
{
    public string itemName;
    public int count;
}

[System.Serializable]
public class ItemData
{
    public string itemName;
    public int count;
    public int position;
}

[System.Serializable]
public class PlayerAttributesData
{
    public int strength;
    public int vitality;
    public int endurance;
    public int stamina;
    public int luck;

    public int availablePoints;
    public int goldenPoints;

    public List<string> unlockedUpgrades = new();
}

[System.Serializable]
public class LevelData
{
    public int level;
    public int maxExp;
    public int currentExp;
}

[System.Serializable]
public class TrialAltarSaveData
{
    public int currentWave;
    public bool trialCompleted;
    public bool keyAvailable;
}

[System.Serializable]
public class GemAltarSaveData
{
    public bool bossDefeated;
    public bool isActivated;
}

[System.Serializable]
public class CraftingSaveData
{
    public List<string> unlockedRecipeIDs = new(); // result item names
}

[System.Serializable]
public class SmeltingSaveData
{
    public List<string> unlockedRecipeIDs = new(); // result item names
}

[System.Serializable]
public class DayNightSaveData
{
    public float timeOfDay;
    public int currentDay;
    public bool hasAdvancedDayToday;
}

[System.Serializable]
public class CampfireSaveData
{
    public int currentHealth;
    public List<GemColor> unlockedGems;
}

[System.Serializable]
public class BreakableObjectData
{
    public int currentHealth;
}

[System.Serializable]
public class FarmPlotData
{
    public string plantName;
    public float growthTimer;
    public int currentStage;
    public bool isPlanted;
}

[System.Serializable]
public class FurnaceSaveData
{
    public string inputJson;
    public string outputJson;
    public string fuelDataJson;
    public float currentFuel;
}