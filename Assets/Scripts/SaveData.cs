using System.Collections.Generic;
using Game.Storage;
using Game.Terrain.Structures;
using UnityEngine;
using Worlds;

namespace Game.Saving
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public int maxHealth;
        public int currentHealth;
        public float maxStamina;
        public float currentStamina;
        public PlayerAttributesData attributes;
        public InventorySaveData inventory;
        public LevelData levelData;
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public List<ItemData> savedItems;
        public List<string> discoveredItems;
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
        public WorldType worldType;
        public RunStats worldStats;
    }

    [System.Serializable]
    public class ChunkSaveData
    {
        public Vector3 chunkPosition;
        public List<SpawnedObjectData> spawnedObjects;

        public List<string> furnaceStates = new();
        public List<string> storageStates = new();
    }

    [System.Serializable]
    public class SpawnedObjectData
    {
        public Vector3 position;
        public string prefabID;
        public string savedStateJson;

        [System.NonSerialized] public GameObject instance;

        public WorldStructure structureRef;
        public int structurePartIndex;

        public SpawnedObjectData(Vector3 pos, GameObject instanceObj, GameObject prefabObj)
        {
            position = pos;
            prefabID = prefabObj.GetComponent<PrefabID>().prefabKey;
            savedStateJson = null;
            instance = instanceObj;
            structureRef = null;
            structurePartIndex = -1;
        }

        public void SaveState(GameObject instanceObj)
        {
            if (instanceObj == null)
            {
                Debug.LogWarning($"{prefabID} instance is null");
                savedStateJson = null;
                return;
            }

            StorageUnit storage = instanceObj.GetComponentInChildren<StorageUnit>();
            if (storage != null)
            {
                savedStateJson = storage.SaveState();
                return;
            }

            ISaveableObject[] saveables = instanceObj.GetComponentsInChildren<ISaveableObject>();
            List<string> allStates = new();

            foreach (ISaveableObject saveable in saveables)
            {
                string state = saveable.SaveState();
                if (!string.IsNullOrEmpty(state))
                    allStates.Add(state);
            }

            savedStateJson = JsonUtility.ToJson(new MultiSaveData { states = allStates });
        }
    }

    [System.Serializable]
    public class MultiSaveData
    {
        public List<string> states;
    }

    [System.Serializable]
    public class InteractableItemData
    {
        public string itemName;
        public string toolAttribute;
        public int count;
    }

    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public string toolAttribute;
        public int count;
        public int position;
    }

    [System.Serializable]
    public class RockSaveData
    {
        public List<int> removedRocks;
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

        public HashSet<string> unlockedUpgrades = new();
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
        public List<string> craftedBeforeIDs = new();
    }

    [System.Serializable]
    public class SmeltingSaveData
    {
        public List<string> unlockedRecipeIDs = new(); // result item names
        public List<string> smeltedBeforeIDs = new();
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
        public ItemData input;
        public ItemData output;
        public ItemData fuel;
        public float smeltProgress;
        public float currentFuel;
    }

    [System.Serializable]
    public class ReforgeTableSaveData
    {
        public ItemData tool;
        public ItemData material;
    }

    [System.Serializable]
    public class StorageSaveData
    {
        public List<ItemData> storedItems;
    }

    [System.Serializable]
    public class WorldQuestSaveData
    {
        public List<QuestSaveData> questSaveDatas = new();
        public bool hasOpenedTrialMenu;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public string questID;
        public string questTitle;
        public bool isCompleted;
        public List<string> requiredItems;
        public int requiredCount;
        public int currentCount;
    }
}
