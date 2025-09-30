using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StructureManager : MonoBehaviour
{
    public List<StructureTemplate> structureTemplates;

    public List<WorldStructure> activeStructures = new();
    public LayerMask structureLayer;

    public BuildingManager buildingManager;

    int chunkSize;
    int gridSize;
    float voxelSize;

    int seed;

    public float minSpacing;

    private void Start()
    {
        chunkSize = VoxelGrid.Instance.chunkSize;
        gridSize = VoxelGrid.Instance.gridSize;
        voxelSize = VoxelGrid.Instance.voxelSize;

        seed = VoxelGrid.Instance.seed;
    }

    public void SpawnStructuresInChunk(VoxelChunk chunk)
    {
        Vector3 chunkPosition = chunk.chunkPosition;
        Vector3 worldCenter = new((chunkSize * gridSize) * 0.5f * voxelSize, 0f, (chunkSize * gridSize) * 0.5f * voxelSize);

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

                StructureTemplate template = SelectDeterministicStructure(globalX, globalZ, seed);
                if (template == null) continue;

                if (!IsValidPlacement(structureSpawnPos, template)) continue;

                int targetChunkX = Mathf.FloorToInt(structureSpawnPos.x / (chunkSize * voxelSize));
                int targetChunkZ = Mathf.FloorToInt(structureSpawnPos.z / (chunkSize * voxelSize));
                Vector2Int chunkKey = new(targetChunkX, targetChunkZ);

                VoxelChunk targetChunk = VoxelGrid.Instance.chunkMap[chunkKey];

                WorldStructure structure = GenerateStructure(template, structureSpawnPos, targetChunk);
                structure.worldOrigin = structureSpawnPos;
                activeStructures.Add(structure);

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

        int hash = origin.GetHashCode() ^ seed;
        int rotationIndex = Mathf.Abs(hash) % 4;
        Quaternion rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);

        for (int i = 0; i < template.prefabParts.Count; i++)
        {
            GameObject prefab = template.prefabParts[i];
            Vector3 offset = i < template.localOffsets.Count ? template.localOffsets[i] : Vector3.zero;

            Vector3 rotatedOffset = rotation * offset;
            Vector3 partPosition = origin + rotatedOffset;

            Vector3 rayOrigin = partPosition + Vector3.up * 50f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, VoxelGrid.Instance.groundLayer))
                partPosition.y = hit.point.y;

            if (!buildingManager.IsAreaFree(prefab, partPosition)) continue;

            GameObject part = Instantiate(prefab, partPosition, rotation, chunk.chunkObject.transform);
            structure.structureObjects.Add(part);

            chunk.objects.Add(part);

            SpawnedObjectData data = new(part.transform.position, part, prefab);

            chunk.savedObjects.Add(data);
            chunk.savedObjectPositions.Add(part.transform.position);

            VoxelGrid.Instance.MarkAreaOccupied(part);

            foreach (StorageUnit storage in part.GetComponentsInChildren<StorageUnit>())
            {
                if (storage.TryGetComponent(out LootTableReference lootRef) && lootRef.lootTable != null)
                    storage.items = lootRef.lootTable.GetRandomLoot();
            }
        }

        return structure;
    }

    bool IsValidPlacement(Vector3 origin, StructureTemplate template, float tolerance = 0.1f, float maxSlope = 5f)
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
                    if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope)
                        return false;
                }
            }
        }

        if (sampledHeights.Count == 0) return false;

        float minH = sampledHeights.Min();
        float maxH = sampledHeights.Max();

        if (maxH - minH > tolerance)
            return false;

        foreach (float h in sampledHeights)
        {
            if (Mathf.Abs(origin.y - h) > tolerance)
                return false;
        }

        return true;
    }

    float[,] GetHeightMapForChunk(int chunkX, int chunkZ)
    {
        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
            return chunk.heightMap;

        return null;
    }

    StructureTemplate SelectDeterministicStructure(int globalX, int globalZ, int seed)
    {
        if (structureTemplates.Count == 0) return null;

        int hash = globalX * 73856093 ^ globalZ * 19349663 ^ (seed * 83492791);
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
