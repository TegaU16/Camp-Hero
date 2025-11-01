using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class SaveSystem
{
    private static string WorldsPath => Path.Combine(Application.persistentDataPath, "Worlds");

    private static string GetWorldPath(string worldName)
    {
        if (string.IsNullOrEmpty(worldName))
            throw new System.ArgumentException("worldName cannot be null or empty", nameof(worldName));

        return Path.Combine(WorldsPath, worldName);
    }

    private static string GetChunksPath(string worldName) =>
        Path.Combine(GetWorldPath(worldName), "chunks");

    private static string GetFilePath(string worldName, string fileName) =>
        Path.Combine(GetWorldPath(worldName), fileName);

    // ----- PLAYER -----
    public static void SavePlayer(string worldName, PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(worldName, "player.json"), json);
    }

    public static PlayerSaveData LoadPlayer(string worldName)
    {
        string path = GetFilePath(worldName, "player.json");
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(path));
    }

    // ----- WORLD METADATA -----
    public static void SaveWorldMeta(WorldMetaData data)
    {
        string worldPath = GetWorldPath(data.worldName);
        Directory.CreateDirectory(worldPath);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path.Combine(worldPath, "meta.json"), json);
    }

    public static WorldMetaData LoadWorldMeta(string worldName)
    {
        string path = GetFilePath(worldName, "meta.json");
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<WorldMetaData>(File.ReadAllText(path));
    }

    public static void DeleteWorldMeta(string worldName)
    {
        string worldDir = Path.Combine(WorldsPath, worldName);
        if (Directory.Exists(worldDir))
            Directory.Delete(worldDir, true);
    }

    // ----- CHUNKS -----
    private static string GetChunkPath(string worldName, Vector3 chunkPos) =>
        Path.Combine(GetChunksPath(worldName), $"chunk_{chunkPos.x}_{chunkPos.z}.json");

    public static void SaveChunk(string worldName, VoxelChunk chunk)
    {
        Directory.CreateDirectory(GetChunksPath(worldName));

        List<SpawnedObjectData> savedObjects = new();

        for (int i = 0; i < chunk.objects.Count; i++)
        {
            GameObject instance = chunk.objects[i];
            if (instance == null) continue;

            string prefabKey = instance.GetComponent<PrefabID>().prefabKey;

            if (prefabKey == null)
            {
                Debug.Log($"Tried saving {instance.name}");
                continue;
            }
            GameObject prefab = PrefabRegistry.GetPrefabByKey(prefabKey);

            savedObjects.Add(new SpawnedObjectData(
                instance.transform.position,
                instance,
                prefab
            ));
        }

        ChunkSaveData data = new()
        {
            chunkPosition = chunk.chunkPosition,
            spawnedObjects = savedObjects,
            hasNaturalObjects = chunk.hasNaturalObjects,
            hasKeyStructure = chunk.hasKeyStructure
        };

        File.WriteAllText(
            GetChunkPath(worldName, chunk.chunkPosition),
            JsonUtility.ToJson(data, true)
        );
    }

    public static ChunkSaveData LoadChunk(string worldName, Vector3 chunkPos)
    {
        string path = GetChunkPath(worldName, chunkPos);
        if (!File.Exists(path)) return null;
        ChunkSaveData data = JsonUtility.FromJson<ChunkSaveData>(File.ReadAllText(path));

        return data;
    }

    public static void ClearAllChunkSaves(string worldName)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");
        if (Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true); // true = delete all files and subdirectories
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to clear chunk saves for world '{worldName}': {ex.Message}");
            }
        }
    }

    // ----- CRAFTING -----
    private static string GetCraftingPath(string worldName) =>
        Path.Combine(GetWorldPath(worldName), "crafting.json");

    public static void SaveCrafting(string worldName, List<CraftingRecipe> unlockedRecipes)
    {
        CraftingSaveData data = new();
        foreach (CraftingRecipe recipe in unlockedRecipes)
            data.unlockedRecipeIDs.Add(recipe.resultItem.name);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetCraftingPath(worldName), json);
    }

    public static List<CraftingRecipe> LoadCrafting(string worldName, CraftingDatabase database)
    {
        string path = GetCraftingPath(worldName);
        if (!File.Exists(path)) return new List<CraftingRecipe>();

        string json = File.ReadAllText(path);
        CraftingSaveData data = JsonUtility.FromJson<CraftingSaveData>(json);

        List<CraftingRecipe> unlocked = new();
        foreach (string id in data.unlockedRecipeIDs)
        {
            CraftingRecipe recipe = database.GetRecipeByID(id);
            if (recipe != null) 
                unlocked.Add(recipe);
        }

        return unlocked;
    }

    // ----- SMELTING -----
    private static string GetSmeltingPath(string worldName) =>
        Path.Combine(GetWorldPath(worldName), "smelting.json");

    public static void SaveSmelting(string worldName, List<SmeltingRecipe> unlockedRecipes)
    {
        SmeltingSaveData data = new();
        foreach (SmeltingRecipe recipe in unlockedRecipes)
            data.unlockedRecipeIDs.Add(recipe.resultItem.name);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSmeltingPath(worldName), json);
    }

    public static List<SmeltingRecipe> LoadSmelting(string worldName, SmeltingDatabase database)
    {
        string path = GetSmeltingPath(worldName);
        if (!File.Exists(path)) return new List<SmeltingRecipe>();

        string json = File.ReadAllText(path);
        SmeltingSaveData data = JsonUtility.FromJson<SmeltingSaveData>(json);

        List<SmeltingRecipe> unlocked = new();
        foreach (string id in data.unlockedRecipeIDs)
        {
            SmeltingRecipe recipe = database.GetRecipeByID(id);
            if (recipe != null) 
                unlocked.Add(recipe);
        }

        return unlocked;
    }

    // ----- ANIMALS -----
    public static void SaveAnimals(string worldName, List<AnimalSaveData> data)
    {
        string json = JsonUtility.ToJson(new WorldAnimalData { animals = data }, true);
        string path = GetFilePath(worldName, "animals.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
    }

    public static List<AnimalSaveData> LoadAnimals(string worldName)
    {
        string path = GetFilePath(worldName, "animals.json");
        if (!File.Exists(path)) return null;

        WorldAnimalData worldData = JsonUtility.FromJson<WorldAnimalData>(File.ReadAllText(path));
        return worldData?.animals;
    }

    // ----- ENEMIES -----
    public static void SaveEnemies(string worldName, List<EnemySaveData> data)
    {
        string json = JsonUtility.ToJson(new WorldEnemyData { enemies = data }, true);
        string path = GetFilePath(worldName, "enemies.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
    }

    public static List<EnemySaveData> LoadEnemies(string worldName)
    {
        string path = GetFilePath(worldName, "enemies.json");
        if (!File.Exists(path)) return null;

        WorldEnemyData worldData = JsonUtility.FromJson<WorldEnemyData>(File.ReadAllText(path));
        return worldData?.enemies;
    }

    // ----- DAY NIGHT CYCLE -----
    public static void SaveDayNight(string worldName, DayNightSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetFilePath(worldName, "daynight.json");
        File.WriteAllText(path, json);
    }

    public static DayNightSaveData LoadDayNight(string worldName)
    {
        string path = GetFilePath(worldName, "daynight.json");
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        DayNightSaveData data = JsonUtility.FromJson<DayNightSaveData>(json);
        return data;
    }
}
