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

    // ----- CHUNKS -----
    private static string GetChunkPath(string worldName, Vector3 chunkPos) =>
        Path.Combine(GetChunksPath(worldName), $"chunk_{chunkPos.x}_{chunkPos.z}.json");

    public static void SaveChunk(string worldName, VoxelChunk chunk)
    {
        Directory.CreateDirectory(GetChunksPath(worldName));
        ChunkSaveData data = new()
        {
            chunkPosition = chunk.chunkPosition,
            spawnedObjects = chunk.savedObjects != null
                ? new List<SpawnedObjectData>(chunk.savedObjects)
                : new List<SpawnedObjectData>(),
            hasNaturalObjects = chunk.hasNaturalObjects,
            hasKeyStructure = chunk.hasKeyStructure
        };

        chunk.SaveChunkFurnaces(data);
        chunk.SaveChunkStorages(data);

        File.WriteAllText(GetChunkPath(worldName, chunk.chunkPosition), JsonUtility.ToJson(data, true));
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

    // ----- TRIAL ALTARS -----
    private static string GetTrialAltarsPath(string worldName) =>
    Path.Combine(GetWorldPath(worldName), "trial_altars");

    private static string GetTrialAltarPath(string worldName, string altarID) =>
        Path.Combine(GetTrialAltarsPath(worldName), $"trial_altar_{altarID}.json");

    public static void SaveTrialAltarState(string worldName, string altarID, TrialAltarSaveData data)
    {
        Directory.CreateDirectory(GetTrialAltarsPath(worldName)); // ensure dir exists
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetTrialAltarPath(worldName, altarID), json);
    }

    public static TrialAltarSaveData LoadTrialAltarState(string worldName, string altarID)
    {
        string path = GetTrialAltarPath(worldName, altarID);
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<TrialAltarSaveData>(File.ReadAllText(path));
    }

    public static void ClearAllTrialAltarSaves(string worldName)
    {
        string dir = GetTrialAltarsPath(worldName);
        if (Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to clear trial altar saves for world '{worldName}': {ex.Message}");
            }
        }
    }

    // ----- GEM ALTARS -----
    private static string GetGemAltarsPath(string worldName) =>
    Path.Combine(GetWorldPath(worldName), "gem_altars");

    private static string GetGemAltarPath(string worldName, string altarID) =>
        Path.Combine(GetGemAltarsPath(worldName), $"gem_altar_{altarID}.json");

    public static void SaveGemAltarState(string worldName, string altarID, GemAltarSaveData data)
    {
        Directory.CreateDirectory(GetGemAltarsPath(worldName)); // ensure dir exists
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetGemAltarPath(worldName, altarID), json);
    }

    public static GemAltarSaveData LoadGemAltarState(string worldName, string altarID)
    {
        string path = GetGemAltarPath(worldName, altarID);
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<GemAltarSaveData>(File.ReadAllText(path));
    }

    public static void ClearAllGemAltarSaves(string worldName)
    {
        string dir = GetGemAltarsPath(worldName);
        if (Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to clear gem altar saves for world '{worldName}': {ex.Message}");
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
        {
            data.unlockedRecipeIDs.Add(recipe.resultItem.name);
        }

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
            if (recipe != null) unlocked.Add(recipe);
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
        {
            data.unlockedRecipeIDs.Add(recipe.resultItem.name);
        }

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
            if (recipe != null) unlocked.Add(recipe);
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

    // ----- CAMPFIRE -----
    private static string GetCampfirePath(string worldName) =>
    Path.Combine(GetWorldPath(worldName), "campfire.json");

    public static void SaveCampfire(string worldName, CampfireSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetCampfirePath(worldName);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
    }

    public static CampfireSaveData LoadCampfire(string worldName)
    {
        string path = GetCampfirePath(worldName);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<CampfireSaveData>(json);
    }
}
