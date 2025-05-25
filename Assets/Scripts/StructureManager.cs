using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StructureManager : MonoBehaviour
{
    public List<StructureTemplate> structureTemplates;
    public Dictionary<Vector2Int, VoxelChunk> chunkMap;

    public List<WorldStructure> activeStructures = new();
    public VoxelGrid voxelGrid;
    public LayerMask structureLayer;

    public BuildingManager buildingManager;

    public float minSpacing;

    void Start()
    {
        chunkMap = new Dictionary<Vector2Int, VoxelChunk>();
    }

    public void SpawnStructuresInChunk(VoxelChunk chunk)
    {
        Vector3 chunkPosition = chunk.chunkObject.transform.position;
        int chunkSize = voxelGrid.chunkSize;

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                Vector3 basePosition = chunkPosition + new Vector3((cx + 0.5f) * voxelGrid.voxelSize, 100f, (cz + 0.5f) * voxelGrid.voxelSize);

                if (Physics.Raycast(basePosition, Vector3.down, out RaycastHit hit, 200f, voxelGrid.groundLayer))
                {
                    Vector3 groundPosition = hit.point;

                    Vector3 offset = voxelGrid.CalculatePositionOffset(cx, cz, voxelGrid.seed);
                    Vector3 structureSpawnPos = groundPosition + offset;

                    bool tooClose = false;
                    foreach (WorldStructure existingStructure in activeStructures)
                    {
                        if (Vector3.Distance(existingStructure.worldOrigin, structureSpawnPos) < minSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    if (!IsValidPlacement(structureSpawnPos, chunkSize)) continue;

                    StructureTemplate template = SelectDeterministicStructure(cx, cz, voxelGrid.seed);
                    WorldStructure structure = GenerateStructure(template, structureSpawnPos, chunk);

                    structure.worldOrigin = structureSpawnPos;
                    activeStructures.Add(structure);

                    foreach (GameObject obj in structure.structureObjects)
                    {
                        chunk.savedObjectPositions.Add(obj.transform.position);
                        chunk.objects.Add(obj);
                        chunk.savedObjects.Add(new SpawnedObjectData(obj.transform.position, obj));
                    }

                    return;
                }
            }
        }
    }

    WorldStructure GenerateStructure(StructureTemplate template, Vector3 origin, VoxelChunk chunk)
    {
        WorldStructure structure = new()
        {
            structureName = template.structureName,
            worldOrigin = origin
        };

        for (int i = 0; i < template.prefabParts.Count; i++)
        {
            GameObject prefab = template.prefabParts[i];
            Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;

            Vector3 partPosition = origin + offset;

            Vector3 rayOrigin = partPosition + Vector3.up * 50f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, voxelGrid.groundLayer))
            {
                partPosition.y = hit.point.y;
            }

            if (!buildingManager.IsAreaFree(prefab, partPosition)) continue;

            GameObject part = Instantiate(prefab, partPosition, Quaternion.identity, chunk.chunkObject.transform);
            structure.structureObjects.Add(part);

            voxelGrid.MarkAreaOccupied(part);

            foreach (var storage in part.GetComponentsInChildren<StorageUnit>())
            {
                LootTableReference lootRef = storage.GetComponent<LootTableReference>();
                if (lootRef != null && lootRef.lootTable != null)
                {
                    storage.items = lootRef.lootTable.GetRandomLoot();
                }
            }
        }

        return structure;
    }

    bool IsValidPlacement(Vector3 position, int chunkSize, float radius = 5f, float tolerance = 0.5f)
    {
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(position.z / chunkSize);

        float[,] heightMap = GetHeightMapForChunk(chunkX, chunkZ);

        if (heightMap == null)
        {
            return false;
        }

        int centerX = Mathf.FloorToInt(position.x) - chunkX * chunkSize;
        int centerZ = Mathf.FloorToInt(position.z) - chunkZ * chunkSize;

        if (centerX < 0 || centerX >= chunkSize || centerZ < 0 || centerZ >= chunkSize)
        {
            return false;
        }

        float centerHeight = heightMap[centerX, centerZ];

        for (int x = -(int)radius; x <= radius; x++)
        {
            for (int z = -(int)radius; z <= radius; z++)
            {
                if (x == 0 && z == 0) continue;

                int neighborX = centerX + x;
                int neighborZ = centerZ + z;

                if (neighborX >= 0 && neighborX < chunkSize && neighborZ >= 0 && neighborZ < chunkSize)
                {
                    float neighborHeight = heightMap[neighborX, neighborZ];
                    if (Mathf.Abs(centerHeight - neighborHeight) > tolerance)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    float[,] GetHeightMapForChunk(int chunkX, int chunkZ)
    {
        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
        {
            return chunk.heightMap;
        }

        return null;
    }

    StructureTemplate SelectDeterministicStructure(int x, int z, int seed)
    {
        if (structureTemplates.Count == 0) return null;

        int hash = x * 73856093 ^ z * 19349663 ^ (seed * 83492791);
        int randomIndex = Mathf.Abs(hash) % structureTemplates.Count;

        return structureTemplates[randomIndex];
    }

    public void ClearStructures()
    {
        foreach (WorldStructure structure in activeStructures)
        {
            foreach (GameObject obj in structure.structureObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        activeStructures.Clear();
    }
}

public class WorldStructure
{
    public string structureName;
    public Vector3 worldOrigin;
    public List<GameObject> structureObjects = new();
}
