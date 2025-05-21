using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static string GetChunkPath(string worldName, Vector3 chunkPos)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"chunk_{chunkPos.x}_{chunkPos.z}.json");
    }

    public static void SaveChunk(string worldName, VoxelChunk chunk)
    {
        ChunkSaveData data = new()
        {
            chunkPosition = chunk.chunkPosition,
            spawnedObjects = chunk.savedObjects
        };

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(GetChunkPath(worldName, chunk.chunkPosition), json);
    }

    public static ChunkSaveData LoadChunk(string worldName, Vector3 chunkPos)
    {
        string path = GetChunkPath(worldName, chunkPos);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<ChunkSaveData>(json);
    }

    public static void ClearAllChunkSaves(string worldName)
    {
        string dir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");
        if (Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true); // true = delete all files and subdirectories
                Debug.Log($"Cleared all chunk saves for world '{worldName}'.");
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to clear chunk saves for world '{worldName}': {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"No chunk save directory found for world '{worldName}'.");
        }
    }
}
