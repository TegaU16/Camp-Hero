using System;
using System.Collections;
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
        float offset = VoxelGrid.Instance.voxelSize / 2f;

        return new Vector3(
            voxelCoord.x + offset,
            voxelCoord.y + offset,
            voxelCoord.z + offset);
    }

    public static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dz = Mathf.Abs(a.z - b.z);

        return dx + dz;
    }

    public static bool AreaCheck(GameObject prefab, Vector3 intendedPosition, Func<Vector3Int, bool> checkFunc)
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
        int chunkSize = voxelGrid.chunkSize;

        int chunkX = Mathf.FloorToInt((float)x / chunkSize);
        int chunkZ = Mathf.FloorToInt((float)z / chunkSize);

        int localX = x - chunkX * chunkSize;
        int localZ = z - chunkZ * chunkSize;

        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (!voxelGrid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk) || 
            localX < 0 || 
            localX >= chunkSize || 
            localZ < 0 || 
            localZ >= chunkSize) return 0f;

        return chunk.heightMap[localX * chunkSize + localZ];
    }

    public static void DisableButtonsOutside(Transform menuTransform, Button[] buttonsInScene, bool open)
    {
        foreach (Button button in buttonsInScene)
        {
            if (button.transform.IsChildOf(menuTransform)) continue;

            button.interactable = !open;
            if (button.TryGetComponent(out InteractiveButton interactiveButton))
                interactiveButton.isActive = !open;
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

    public static Bounds GetObjectBounds(Transform objTransform)
    {
        Renderer[] renderers = objTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(objTransform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        return bounds;
    }

    public static void AddObjectDataToChunk(SpawnedObjectData data, Vector3 spawnPosition, VoxelChunk chunk)
    {
        chunk.savedObjects.Add(data);
        chunk.savedObjectPositions.Add(spawnPosition);

        Vector3Int spawnKey = WorldToVoxelCoord(spawnPosition);
        chunk.savedObjectPositionsInt.Add(spawnKey);
    }

    public static void SetMultiplierSource(object source, float multiplier, MultiplierStat multiplierStat)
        => multiplierStat.SetSource(source, multiplier);

    public static void RemoveMultiplierSource(object source, MultiplierStat multiplierStat)
        => multiplierStat.RemoveSource(source);

    public static Vector3 GetTargetPoint(Transform target)
    {
        if (target.TryGetComponent(out Collider col)) return col.bounds.center; // best default

        return target.position + Vector3.up * 1.5f; // fallback
    }
}
