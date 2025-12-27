using System;
using System.Collections;
using System.Collections.Generic;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures.Trials
{
    public class KeyStructureSpawner : MonoBehaviour
    {
        public static KeyStructureSpawner Instance;

        public GameObject[] trialStructurePrefabs;

        private readonly List<Vector3> keyStructurePositions = new();

        [HideInInspector] public HashSet<TrialAltar> activeTrialAltars = new();

        [Range(0f, 0.5f)] public float innerRadius;
        [Range(0f, 0.5f)] public float outerRadius;

        private void Awake()
        {
            Instance = this;
        }

        public IEnumerator SpawnKeyStructures(float worldSize, Vector3 worldCenter, Action<float> onProgress = null)
        {
            HashSet<Vector2Int> usedChunks = new();

            float chunkSize = VoxelGrid.Instance.chunkSize * VoxelGrid.Instance.voxelSize;
            int gridSize = VoxelGrid.Instance.gridSize;

            float minDistanceFromCenter = worldSize * innerRadius;
            float maxDistanceFromCenter = worldSize * outerRadius;

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

                    if (usedChunks.Contains(chunkCoord)) continue;

                    Vector3 position = new(
                        x * chunkSize + chunkSize / 2f,
                        0f,
                        z * chunkSize + chunkSize / 2f
                    );

                    float dist = Vector2.Distance(
                        new Vector2(position.x, position.z),
                        new Vector2(worldCenter.x, worldCenter.z)
                    );

                    if (dist < minDistanceFromCenter || dist > maxDistanceFromCenter) continue;

                    usedChunks.Add(chunkCoord);

                    Vector3 groundPos = AdjustHeightToTerrain(position);
                    keyStructurePositions.Add(groundPos);

                    GameObject placedStructure = Instantiate(trialStructurePrefab, groundPos, Quaternion.identity);
                    VoxelGrid.Instance.MarkVoxelArea(placedStructure, walkable: true, buildable: false);

                    TrialAltar trialAltar = placedStructure.GetComponentInChildren<TrialAltar>();
                    if (trialAltar != null)
                        activeTrialAltars.Add(trialAltar);

                    int index = Array.IndexOf(trialStructurePrefabs, trialStructurePrefab);
                    onProgress?.Invoke((float)(index + 1) / trialStructurePrefabs.Length);

                    KeyStructureCenter centerMarker = placedStructure.GetComponentInChildren<KeyStructureCenter>(true);
                    if (centerMarker != null)
                        VoxelGrid.Instance.MarkVoxelArea(centerMarker.gameObject, buildable: false);

                    VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkCoord];

                    placedStructure.transform.parent = chunk.chunkObject.transform;
                    Vector3 spawnPos = placedStructure.transform.position;

                    chunk.objects.Add(placedStructure);

                    SpawnedObjectData data = new(spawnPos, placedStructure, trialStructurePrefab);
                    Utility.AddObjectDataToChunk(data, spawnPos, chunk);

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
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 500f)) return hit.point;

            Debug.LogWarning($"No terrain found below key structure position: {position}");
            return position;
        }
    }
}
