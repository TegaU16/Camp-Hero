using System;
using System.IO;
using Game.Saving;
using Game.Terrain;
using UnityEngine;
using UnityEngine.UI;

public static class Utility
{
    public static Vector3Int WorldToVoxelCoord(Vector3 worldPos)
    {
        VoxelGrid voxelGrid = VoxelGrid.Instance;

        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / voxelGrid.voxelSize),
            Mathf.FloorToInt(worldPos.y / voxelGrid.voxelSize),
            Mathf.FloorToInt(worldPos.z / voxelGrid.voxelSize)
        );
    }

    public static Vector3 VoxelCoordToWorld(Vector3Int voxelCoord)
    {
        return new Vector3(
            voxelCoord.x + 0.5f,
            voxelCoord.y + 0.5f,
            voxelCoord.z + 0.5f);
    }

    public static bool IsAreaFree(GameObject prefab, Vector3 intendedPosition, Func<Vector3Int, bool> checkFunc)
    {
        Bounds bounds = prefab.GetComponentInChildren<Renderer>().bounds;

        // Move bounds to where it would be instantiated
        bounds.center = intendedPosition + (bounds.center - prefab.transform.position);

        Vector3Int min = WorldToVoxelCoord(bounds.min);
        Vector3Int max = WorldToVoxelCoord(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                Vector3Int voxelPos = new(x, 0, z);
                if (!checkFunc(voxelPos)) return false;
            }
        }

        return true;
    }

    public static float GetHeightAt(int x, int z)
    {
        VoxelGrid voxelGrid = VoxelGrid.Instance;

        int chunkX = Mathf.FloorToInt((float)x / voxelGrid.chunkSize);
        int chunkZ = Mathf.FloorToInt((float)z / voxelGrid.chunkSize);

        // Handle negative modulus properly
        int localX = x - chunkX * voxelGrid.chunkSize;
        int localZ = z - chunkZ * voxelGrid.chunkSize;

        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (!voxelGrid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) return 0f; // or some default height

        if (localX < 0 || localX >= voxelGrid.chunkSize || localZ < 0 || localZ >= voxelGrid.chunkSize) return 0f; // or default height

        return chunk.heightMap[localX, localZ];
    }

    public static void DisableButtonsOutside(Transform menuTransform, Button[] buttonsInScene, bool open)
    {
        foreach (Button button in buttonsInScene)
        {
            if (button.transform.parent != menuTransform)
            {
                button.interactable = !open;
                if (button.TryGetComponent(out InteractiveButton interactiveButton))
                    interactiveButton.isActive = !open;
            }
        }
    }

    public static int PositionHash(int x, int z, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 73856093 ^ x;
            hash = hash * 19349663 ^ z;
            return hash;
        }
    }

    public static int ConsistentHash(string input)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in input)
                hash = hash * 31 + c;

            return hash;
        }
    }

    public static bool WorldFirstGenerated(string worldName)
    {
        string worldDir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");

        bool hasChunks = false;
        if (Directory.Exists(worldDir))
        {
            // Look for any known chunk file types
            string[] chunkFiles = Directory.GetFiles(worldDir, "*.json");
            if (chunkFiles.Length == 0)
                chunkFiles = Directory.GetFiles(worldDir, "*.chunk");

            hasChunks = chunkFiles.Length > 0;
        }

        return !hasChunks;
    }

    public static Bounds GetObjectBounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);

        return b;
    }

    public static void AddObjectDataToChunk(SpawnedObjectData data, Vector3 spawnPosition, VoxelChunk chunk)
    {
        chunk.savedObjects.Add(data);
        chunk.savedObjectPositions.Add(spawnPosition);

        Vector3Int spawnKey = WorldToVoxelCoord(spawnPosition);
        chunk.savedObjectPositionsInt.Add(spawnKey);
    }
}
