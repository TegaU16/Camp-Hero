using System;
using System.Collections;
using System.Collections.Generic;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures.Trials
{
    public class TrialAltarSpawner : MonoBehaviour
    {
        public static TrialAltarSpawner Instance;

        public StructureTemplate[] trialStructures;

        private readonly List<Vector3> keyStructurePositions = new();

        [HideInInspector] public HashSet<TrialAltar> activeTrialAltars = new();

        [Range(0f, 0.5f)] public float innerRadius;
        [Range(0f, 0.5f)] public float outerRadius;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public IEnumerator SpawnTrialAltars(float worldSize, Vector3 worldCenter, Action<float> onProgress = null)
        {
            HashSet<Vector2Int> usedChunks = new();

            float chunkSize = VoxelGrid.Instance.chunkSize * VoxelGrid.Instance.voxelSize;
            int gridSize = VoxelGrid.Instance.gridSize;

            float minDistanceFromCenter = worldSize * innerRadius;
            float maxDistanceFromCenter = worldSize * outerRadius;

            activeTrialAltars.Clear();

            foreach (StructureTemplate trialStructure in trialStructures)
            {
                bool placed = false;
                const int maxAttempts = 100;

                for (int attempt = 0; attempt < maxAttempts && !placed; attempt++)
                {
                    int hashSeed = VoxelGrid.Instance.seed ^ trialStructure.name.GetHashCode() ^ attempt;
                    System.Random rng = new(hashSeed);

                    int x = rng.Next(0, gridSize);
                    int z = rng.Next(0, gridSize);
                    Vector2Int chunkCoord = new(x, z);

                    if (usedChunks.Contains(chunkCoord)) continue;

                    Vector3 position = new(
                        x * chunkSize + chunkSize / 2f + 0.5f,
                        0f,
                        z * chunkSize + chunkSize / 2f + 0.5f
                    );

                    float dist = Vector2.Distance(
                        new Vector2(position.x, position.z),
                        new Vector2(worldCenter.x, worldCenter.z)
                    );

                    if (dist < minDistanceFromCenter || dist > maxDistanceFromCenter) continue;

                    usedChunks.Add(chunkCoord);

                    keyStructurePositions.Add(position);

                    WorldStructure structure = new()
                    {
                        structureName = trialStructure.structureName,
                        worldOrigin = position
                    };

                    for (int i = 0; i < trialStructure.prefabParts.Count; i++)
                    {
                        PrefabPart prefabPart = trialStructure.prefabParts[i];
                        Vector3 prefabPos = position + prefabPart.localOffset;

                        int partPosX = Mathf.FloorToInt(prefabPos.x);
                        int partPosZ = Mathf.FloorToInt(prefabPos.z);
                        prefabPos.y = Utility.GetHeightAt(partPosX, partPosZ);

                        GameObject prefab = prefabPart.prefab;

                        Vector3Int voxelPos = Utility.WorldToVoxelCoord(prefabPos);

                        int chunkX = Mathf.FloorToInt((float)voxelPos.x / VoxelGrid.Instance.chunkSize);
                        int chunkZ = Mathf.FloorToInt((float)voxelPos.z / VoxelGrid.Instance.chunkSize);

                        Vector2Int chunkKey = new(chunkX, chunkZ);

                        if (!VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) continue;

                        SpawnedObjectData data = new(prefabPos, prefab, prefab)
                        {
                            structureRef = structure,
                            structurePartIndex = i
                        };
                        Utility.AddObjectDataToChunk(data, prefabPos, chunk);
                    }

                    int index = Array.IndexOf(trialStructures, trialStructure);
                    onProgress?.Invoke((float)(index + 1) / trialStructures.Length);

                    placed = true;
                }

                yield return null;
            }
        }

        public void RegisterAltar(TrialAltar trialAltar)
        {
            if (activeTrialAltars.Contains(trialAltar)) return;
            activeTrialAltars.Add(trialAltar);
        }
    }
}
