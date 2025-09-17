using System.Collections.Generic;
using UnityEngine;

public class StructureManager : MonoBehaviour
{
    public List<StructureTemplate> structureTemplates;

    public List<WorldStructure> activeStructures = new();
    public VoxelGrid voxelGrid;
    public LayerMask structureLayer;

    public BuildingManager buildingManager;

    int chunkSize;
    int gridSize;
    float voxelSize;

    public float minSpacing;

    private void Start()
    {
        chunkSize = voxelGrid.chunkSize;
        gridSize = voxelGrid.gridSize;
        voxelSize = voxelGrid.voxelSize;
    }

    public void SpawnStructuresInChunk(VoxelChunk chunk)
    {
        Vector3 chunkPosition = chunk.chunkObject.transform.position;

        Vector3 worldCenter = new((chunkSize * gridSize) * 0.5f * voxelSize, 0f, (chunkSize * gridSize) * 0.5f * voxelSize);

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                int hash = cx * 73856093 ^ cz * 19349663 ^ (voxelGrid.seed * 83492791);
                System.Random rng = new(hash);

                if (chunk.heightMap == null) continue;
                float y = chunk.heightMap[cx, cz];

                Vector3 basePosition = chunkPosition + new Vector3((cx + 0.5f) * voxelSize, y, (cz + 0.5f) * voxelSize);

                float offsetX = (float)(rng.NextDouble() - 0.5) * voxelSize;
                float offsetZ = (float)(rng.NextDouble() - 0.5) * voxelSize;
                Vector3 structureSpawnPos = basePosition + new Vector3(offsetX, 0, offsetZ);

                bool tooClose = false;
                foreach (WorldStructure existingStructure in activeStructures)
                {
                    if ((Vector3.Distance(existingStructure.worldOrigin, structureSpawnPos) < minSpacing) ||
                        (Vector3.Distance(worldCenter, structureSpawnPos) < minSpacing))
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                StructureTemplate template = SelectDeterministicStructure(cx, cz, voxelGrid.seed);
                if (template == null) continue;

                if (!IsValidPlacement(structureSpawnPos, template, chunkSize)) continue;

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

    WorldStructure GenerateStructure(StructureTemplate template, Vector3 origin, VoxelChunk chunk)
    {
        WorldStructure structure = new()
        {
            structureName = template.structureName,
            worldOrigin = origin
        };

        int hash = origin.GetHashCode() ^ voxelGrid.seed;
        int rotationIndex = Mathf.Abs(hash) % 4;
        Quaternion rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);

        for (int i = 0; i < template.prefabParts.Count; i++)
        {
            GameObject prefab = template.prefabParts[i];
            Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;

            Vector3 rotatedOffset = rotation * offset;
            Vector3 partPosition = origin + rotatedOffset;

            Vector3 rayOrigin = partPosition + Vector3.up * 50f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, voxelGrid.groundLayer))
            {
                partPosition.y = hit.point.y;
            }

            if (!buildingManager.IsAreaFree(prefab, partPosition)) continue;

            GameObject part = Instantiate(prefab, partPosition, rotation, chunk.chunkObject.transform);
            structure.structureObjects.Add(part);

            voxelGrid.MarkAreaOccupied(part);

            foreach (StorageUnit storage in part.GetComponentsInChildren<StorageUnit>())
            {
                if (storage.TryGetComponent(out LootTableReference lootRef) && lootRef.lootTable != null)
                    storage.items = lootRef.lootTable.GetRandomLoot();
            }
        }

        return structure;
    }

    bool IsValidPlacement(Vector3 origin, StructureTemplate template, float tolerance = 0.5f)
    {
        Bounds totalBounds = new(origin, Vector3.zero);

        for (int i = 0; i < template.prefabParts.Count; i++)
        {
            GameObject prefab = template.prefabParts[i];
            if (prefab == null) continue;

            Renderer rend = prefab.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            Bounds prefabBounds = rend.bounds;
            Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;

            prefabBounds.center = origin + offset;

            totalBounds.Encapsulate(prefabBounds);
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

                sampledHeights.Add(heightMap[localX, localZ]);
            }
        }

        if (sampledHeights.Count == 0) return false;

        float minH = Mathf.Min(sampledHeights.ToArray());
        float maxH = Mathf.Max(sampledHeights.ToArray());

        if (Mathf.Abs(maxH - minH) > tolerance) return false;

        return true;
    }

    float[,] GetHeightMapForChunk(int chunkX, int chunkZ)
    {
        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (voxelGrid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
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
                    Destroy(obj);
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
