using System;
using System.Collections.Generic;
using System.Linq;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public class StructureManager : MonoBehaviour
    {
        public static StructureManager Instance;

        public List<StructureTemplate> structureTemplates;

        public List<WorldStructure> activeStructures = new();
        public LayerMask structureLayer;

        [Range(0f, 0.5f)] public float outerRadius;

        private int chunkSize;
        private int gridSize;
        private float voxelSize;

        private int seed;

        public float minSpacing;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            chunkSize = VoxelGrid.Instance.chunkSize;
            gridSize = VoxelGrid.Instance.gridSize;
            voxelSize = VoxelGrid.Instance.voxelSize;

            seed = VoxelGrid.Instance.seed;
        }

        public void SpawnStructuresInChunk(VoxelChunk chunk)
        {
            float worldSideLength = chunkSize * gridSize;
            float maxDistanceFromCenter = worldSideLength * outerRadius;

            Vector3 chunkPosition = chunk.chunkPosition;
            Vector3 worldCenter = new(worldSideLength * 0.5f * voxelSize, 0f, worldSideLength * 0.5f * voxelSize);

            System.Random worldRng = new(seed);

            for (int cx = 0; cx < chunkSize; cx++)
            {
                for (int cz = 0; cz < chunkSize; cz++)
                {
                    int chunkX = (int)(chunkPosition.x / chunkSize);
                    int chunkZ = (int)(chunkPosition.z / chunkSize);

                    int globalX = chunkX * chunkSize + cx;
                    int globalZ = chunkZ * chunkSize + cz;

                    if (chunk.heightMap == null) continue;
                    float y = chunk.heightMap[cx, cz];

                    Vector3 basePosition = chunkPosition + new Vector3((cx + 0.5f) * voxelSize, y, (cz + 0.5f) * voxelSize);

                    float maxOffset = chunkSize * gridSize * voxelSize * 0.5f; // half the world width
                    float offsetX = (float)(worldRng.NextDouble() - 0.5) * maxOffset;
                    float offsetZ = (float)(worldRng.NextDouble() - 0.5) * maxOffset;

                    Vector3 structureSpawnPos = basePosition + new Vector3(offsetX, 0, offsetZ);
                    if (!VoxelGrid.Instance.IsWithinBorders(structureSpawnPos)) continue;

                    float distanceFromCenter = Vector2.Distance(
                        new Vector2(structureSpawnPos.x, structureSpawnPos.z),
                        new Vector2(worldCenter.x, worldCenter.z)
                    );

                    bool tooClose = false;
                    foreach (WorldStructure existingStructure in activeStructures)
                    {
                        if (Vector3.Distance(existingStructure.worldOrigin, structureSpawnPos) < minSpacing ||
                            distanceFromCenter < minSpacing || distanceFromCenter > maxDistanceFromCenter)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose) continue;

                    StructureTemplate template = SelectDeterministicStructure(globalX, globalZ, seed);
                    if (template == null) continue;
                    if (!IsValidPlacement(structureSpawnPos, template)) continue;

                    int targetChunkX = Mathf.FloorToInt(structureSpawnPos.x / (chunkSize * voxelSize));
                    int targetChunkZ = Mathf.FloorToInt(structureSpawnPos.z / (chunkSize * voxelSize));
                    targetChunkX = Mathf.Max(0, targetChunkX);
                    targetChunkZ = Mathf.Max(0, targetChunkZ);

                    Vector2Int chunkKey = new(targetChunkX, targetChunkZ);

                    VoxelChunk targetChunk = VoxelGrid.Instance.chunkMap[chunkKey];

                    WorldStructure structure = GenerateStructure(template, structureSpawnPos, targetChunk);
                    structure.worldOrigin = structureSpawnPos;
                    activeStructures.Add(structure);

                    return;
                }
            }
        }

        private WorldStructure GenerateStructure(StructureTemplate template, Vector3 origin, VoxelChunk chunk)
        {
            WorldStructure structure = new()
            {
                structureName = template.structureName,
                worldOrigin = origin
            };

            int hash = origin.GetHashCode() ^ seed;
            int rotationIndex = Mathf.Abs(hash) % 4;
            Quaternion rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);

            for (int i = 0; i < template.prefabParts.Count; i++)
            {
                GameObject prefab = template.prefabParts[i];
                if (prefab == null) continue;

                Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;
                Vector3 rotatedOffset = rotation * offset;
                Vector3 partPosition = origin + rotatedOffset;

                int partPosX = Mathf.RoundToInt(partPosition.x);
                int partPosZ = Mathf.RoundToInt(partPosition.z);

                float height = Utility.GetHeightAt(partPosX, partPosZ);
                partPosition.y = height;

                // Only keep valid placements
                if (Utility.IsAreaFree(prefab, partPosition, VoxelGrid.Instance.IsOccupied)) continue;

                // Add to saved objects WITHOUT instantiating
                SpawnedObjectData data = new(partPosition, prefab, prefab)
                {
                    structureRef = structure,
                    structurePartIndex = i
                };
                Utility.AddObjectDataToChunk(data, partPosition, chunk);
            }

            return structure;
        }

        private bool IsValidPlacement(Vector3 origin, StructureTemplate template, float tolerance = 0.1f, float maxSlope = 5f)
        {
            // Approximate structure footprint
            Bounds totalBounds = new(origin, Vector3.zero);
            for (int i = 0; i < template.prefabParts.Count; i++)
            {
                if (template.prefabParts[i] == null) continue;

                Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;
                totalBounds.Encapsulate(new Bounds(origin + offset, Vector3.one * 2f));
            }

            int minX = Mathf.FloorToInt(totalBounds.min.x / voxelSize);
            int maxX = Mathf.FloorToInt(totalBounds.max.x / voxelSize);
            int minZ = Mathf.FloorToInt(totalBounds.min.z / voxelSize);
            int maxZ = Mathf.FloorToInt(totalBounds.max.z / voxelSize);

            List<float> sampledHeights = new();

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    float[,] heightMap = GetHeightMapForChunk(x / chunkSize, z / chunkSize);
                    if (heightMap == null) return false;

                    int localX = x % chunkSize;
                    int localZ = z % chunkSize;
                    if (localX < 0 || localZ < 0 || localX >= chunkSize || localZ >= chunkSize) continue;

                    float h = heightMap[localX, localZ];
                    sampledHeights.Add(h);

                    Vector3 worldPos = new(x * voxelSize, h + 10f, z * voxelSize);
                    if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 20f, VoxelGrid.Instance.groundLayer))
                    {
                        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) return false;
                    }
                }
            }

            if (sampledHeights.Count == 0) return false;

            float minH = sampledHeights.Min();
            float maxH = sampledHeights.Max();

            if (maxH - minH > tolerance) return false;

            foreach (float h in sampledHeights)
            {
                if (Mathf.Abs(origin.y - h) > tolerance) return false;
            }

            return true;
        }

        private float[,] GetHeightMapForChunk(int chunkX, int chunkZ)
        {
            Vector2Int chunkKey = new(chunkX, chunkZ);
            if (VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) return chunk.heightMap;

            return null;
        }

        private StructureTemplate SelectDeterministicStructure(int globalX, int globalZ, int seed)
        {
            if (structureTemplates.Count == 0) return null;

            int hash = globalX * 73856093 ^ globalZ * 19349663 ^ (seed * 83492791);
            int randomIndex = Mathf.Abs(hash) % structureTemplates.Count;

            return structureTemplates[randomIndex];
        }
    }

    [Serializable]
    public class WorldStructure
    {
        public string structureName;
        public Vector3 worldOrigin;
    }
}
