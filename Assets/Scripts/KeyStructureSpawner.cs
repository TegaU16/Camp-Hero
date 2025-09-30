using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyStructureSpawner : MonoBehaviour
{
    public static KeyStructureSpawner Instance;

    public GameObject[] trialStructurePrefabs;

    private readonly List<Vector3> keyStructurePositions = new();

    [HideInInspector] public HashSet<TrialAltar> activeTrialAltars = new();

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator SpawnKeyStructures(float worldSize, Vector3 worldCenter, Action<float> onProgress = null)
    {
        HashSet<Vector2Int> usedChunks = new();

        float chunkSize = VoxelGrid.Instance.chunkSize * VoxelGrid.Instance.voxelSize;
        int gridSize = VoxelGrid.Instance.gridSize;

        float minDistanceFromCenter = worldSize * 0.2f;
        float maxDistanceFromCenter = worldSize * 0.45f;

        activeTrialAltars.Clear();

        foreach (GameObject trialStructurePrefab in trialStructurePrefabs)
        {
            bool placed = false;
            const int maxAttempts = 100;

            for (int attempt = 0; attempt < maxAttempts && !placed; attempt++)
            {
                // Create a deterministic RNG for this prefab+attempt
                int hashSeed = VoxelGrid.Instance.seed ^ trialStructurePrefab.name.GetHashCode() ^ attempt;
                System.Random rng = new(hashSeed);

                int x = rng.Next(0, gridSize);
                int z = rng.Next(0, gridSize);
                Vector2Int chunkCoord = new(x, z);

                if (usedChunks.Contains(chunkCoord))
                    continue;

                Vector3 position = new(
                    x * chunkSize + chunkSize / 2f,
                    0f,
                    z * chunkSize + chunkSize / 2f
                );

                float dist = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(worldCenter.x, worldCenter.z)
                );

                if (dist < minDistanceFromCenter || dist > maxDistanceFromCenter)
                    continue;

                usedChunks.Add(chunkCoord);

                Vector3 groundPos = AdjustHeightToTerrain(position);
                keyStructurePositions.Add(groundPos);

                GameObject placedStructure = Instantiate(trialStructurePrefab, groundPos, Quaternion.identity);
                VoxelGrid.Instance.MarkAreaOccupied(placedStructure, true, true);

                TrialAltar trialAltar = placedStructure.GetComponentInChildren<TrialAltar>();
                if (trialAltar != null)
                    activeTrialAltars.Add(trialAltar);

                int index = Array.IndexOf(trialStructurePrefabs, trialStructurePrefab);
                onProgress?.Invoke((float)(index + 1) / trialStructurePrefabs.Length);

                KeyStructureCenter centerMarker = placedStructure.GetComponentInChildren<KeyStructureCenter>(true);
                if (centerMarker != null)
                    VoxelGrid.Instance.MarkAreaOccupied(centerMarker.gameObject, true, false);

                VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkCoord];

                placedStructure.transform.parent = chunk.chunkObject.transform;
                Vector3 spawnPos = placedStructure.transform.position;
                chunk.objects.Add(placedStructure);

                SpawnedObjectData data = new(spawnPos, placedStructure, trialStructurePrefab);

                chunk.savedObjects.Add(data);
                chunk.savedObjectPositions.Add(spawnPos);
                
                chunk.hasKeyStructure = true;

                placed = true;
            }

            if (!placed)
                Debug.LogWarning($"Failed to place structure {trialStructurePrefab.name} after {maxAttempts} attempts.");

            yield return null;
        }
    }

    private Vector3 AdjustHeightToTerrain(Vector3 position)
    {
        Vector3 rayStart = position + Vector3.up * 200f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 500f))
        {
            return hit.point;
        }
        else
        {
            Debug.LogWarning($"No terrain found below key structure position: {position}");
            return position;
        }
    }

    public bool IsNearKeyStructure(Vector3 pos, float radius = 20f)
    {
        foreach (Vector3 keyPos in keyStructurePositions)
        {
            if (Vector3.Distance(pos, keyPos) < radius)
                return true;
        }
        return false;
    }

    public List<Vector3> GetKeyStructurePositions()
    {
        return keyStructurePositions;
    }
}
