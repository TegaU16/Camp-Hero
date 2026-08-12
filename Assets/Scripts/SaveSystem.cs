using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Game.Terrain;
using Game.Registries;

namespace Game.Saving
{
    public static class SaveSystem
    {
        private static string WorldsPath => Path.Combine(Application.persistentDataPath, "Worlds");

        private static string GetWorldPath(string worldName)
        {
            if (string.IsNullOrEmpty(worldName))
                throw new System.ArgumentException("worldName cannot be null or empty", nameof(worldName));

            return Path.Combine(WorldsPath, worldName);
        }

        private static void EnsureWorldDirectoryExists(string worldName) => Directory.CreateDirectory(GetWorldPath(worldName));

        private static string GetChunksPath(string worldName) =>
            Path.Combine(GetWorldPath(worldName), "chunks");

        private static string GetFilePath(string worldName, string fileName) =>
            Path.Combine(GetWorldPath(worldName), fileName);

        // ----- PLAYER -----
        public static void SavePlayer(string worldName, PlayerSaveData data)
        {
            EnsureWorldDirectoryExists(worldName);

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
            EnsureWorldDirectoryExists(data.worldName);

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
            EnsureWorldDirectoryExists(worldName);
            Directory.CreateDirectory(GetChunksPath(worldName));

            foreach (SpawnedObjectData spawnedObjectData in chunk.savedObjects)
            {
                if (spawnedObjectData.instance == null) continue;
                spawnedObjectData.SaveState(spawnedObjectData.instance);
            }

            ChunkSaveData data = new()
            {
                chunkPosition = chunk.chunkPosition,
                spawnedObjects = chunk.savedObjects,
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
        private static string GetCraftingPath(string worldName) => Path.Combine(GetWorldPath(worldName), "crafting.json");

        public static void SaveCrafting(string worldName, CraftingSaveData data)
        {
            EnsureWorldDirectoryExists(worldName);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetCraftingPath(worldName), json);
        }

        public static CraftingSaveData LoadCrafting(string worldName)
        {
            string path = GetCraftingPath(worldName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            CraftingSaveData data = JsonUtility.FromJson<CraftingSaveData>(json);

            return data;
        }

        // ----- SMELTING -----
        private static string GetSmeltingPath(string worldName) =>
            Path.Combine(GetWorldPath(worldName), "smelting.json");

        public static void SaveSmelting(string worldName, SmeltingSaveData data)
        {
            EnsureWorldDirectoryExists(worldName);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSmeltingPath(worldName), json);
        }

        public static SmeltingSaveData LoadSmelting(string worldName)
        {
            string path = GetSmeltingPath(worldName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            SmeltingSaveData data = JsonUtility.FromJson<SmeltingSaveData>(json);

            return data;
        }

        // ----- ANIMALS -----
        public static void SaveAnimals(string worldName, List<AnimalSaveData> data)
        {
            EnsureWorldDirectoryExists(worldName);

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
            EnsureWorldDirectoryExists(worldName);

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
            EnsureWorldDirectoryExists(worldName);

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

        // ----- QUESTS -----
        public static void SaveQuestData(string worldName, WorldQuestSaveData data)
        {
            EnsureWorldDirectoryExists(worldName);

            string json = JsonUtility.ToJson(data, true);
            string path = GetFilePath(worldName, "quests.json");
            File.WriteAllText(path, json);
        }

        public static WorldQuestSaveData LoadQuestData(string worldName)
        {
            string path = GetFilePath(worldName, "quests.json");
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            WorldQuestSaveData data = JsonUtility.FromJson<WorldQuestSaveData>(json);
            return data;
        }
    }
}
