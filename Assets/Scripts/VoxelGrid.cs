using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelGrid : MonoBehaviour
{
    public Material voxelAtlasMaterial;

    public int chunkSize = 16;
    public int gridSize = 10;
    public float voxelSize = 1f;
    public float maxHeight = 5f;
    public float heightScale = 0.5f;

    public float simulationDistance = 10f;
    public float viewDistance = 2f; // For object spawning/visibility
    [SerializeField] private ColliderPool colliderPool;
    public StructureManager structureManager;

    public int objectSpawnFrequency = 5;
    public float minSpacing = 2.0f;

    [HideInInspector] public int seed = 12345;
    private string worldName;

    public GameObject borderWallPrefab;

    [HideInInspector]
    public bool worldGenerated = false;

    public LayerMask groundLayer;
    private GameObject player;
    private Vector3 playerPosition;
    private readonly List<VoxelChunk> chunks = new();

    public AnimalSpawner animalSpawner;

    public List<BiomeData> biomes;
    public NoiseSettings biomeNoiseSettings;

    private readonly HashSet<Vector3Int> voxelOccupancy = new();

    public DayNightCycle dayNightCycle;

    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            this.player = player;
            playerPosition = player.transform.position;
        }
        else
        {
            Debug.LogWarning("Player is null!");
        }
    }

    public void SetWorld(int worldSeed, string name)
    {
        if (worldGenerated)
        {
            Debug.LogWarning("SetWorld called more than once. Ignored.");
            return;
        }

        animalSpawner.groundLayer = groundLayer;
        seed = worldSeed;
        worldName = name;
        GenerateTerrain();
        CreateWorldBorders();
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        ManageChunks();

        float sunIntensity = dayNightCycle.GetSunlightIntensity();
        voxelAtlasMaterial.SetFloat("_SunlightIntensity", sunIntensity);
    }

    void GenerateTerrain()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 chunkPosition = new(x * chunkSize * voxelSize, 0, z * chunkSize * voxelSize);
                GameObject chunkObject = new("VoxelChunk " + x + "," + z);
                chunkObject.transform.position = chunkPosition;

                VoxelChunk chunk = new(chunkObject, chunkSize);
                chunks.Add(chunk);

                Vector2Int chunkKey = new(x, z);
                structureManager.chunkMap[chunkKey] = chunk;

                ChunkSaveData loadedData = SaveSystem.LoadChunk(worldName, chunk.chunkPosition);

                if (loadedData != null)
                {
                    LoadChunkObjects(chunk, loadedData);
                }
                else
                {
                    Debug.Log($"No save data for chunk at: {chunk.chunkPosition}");
                }

                animalSpawner.chunks.Add(chunk);

                GenerateChunkTerrain(chunk, chunkPosition);
                SpawnObjectsInChunk(chunk);

                SaveSystem.SaveChunk(worldName, chunk);
            }
        }

        worldGenerated = true;
    }

    void GenerateChunkTerrain(VoxelChunk chunk, Vector3 chunkPosition)
    {
        BiomeData biome = SelectBiome(chunkPosition);
        chunk.biome = biome;

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();
        List<Color> vertexColors = new();
        int faceCount = 0;

        chunk.heightMap = new float[chunkSize, chunkSize];

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                float worldX = chunkPosition.x + cx;
                float worldZ = chunkPosition.z + cz;

                float height = GenerateBiomeHeight(worldX, worldZ, biome.noiseSettings);
                height = Mathf.Floor(height / heightScale) * heightScale;
                height = Mathf.Clamp(height, 0, maxHeight);

                chunk.heightMap[cx, cz] = height;
            }
        }

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                float height = chunk.heightMap[cx, cz];
                Vector3 voxelPosition = new(cx * voxelSize, height, cz * voxelSize);

                Voxel voxel = new(voxelPosition)
                {
                    lightLevel = 15
                };
                chunk.voxels[cx, cz] = voxel;

                float sunMultiplier = dayNightCycle.GetSunlightIntensity();
                Color lightColor = LightToColor(voxel.lightLevel, sunMultiplier);

                AddFace(vertices, triangles, uvs, vertexColors, voxelPosition, Vector3.up, ref faceCount, height, lightColor, biome);

                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, voxelPosition, Vector3.left, -1, 0, ref faceCount, vertices, triangles, uvs, vertexColors, height, lightColor, biome);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, voxelPosition, Vector3.right, 1, 0, ref faceCount, vertices, triangles, uvs, vertexColors, height, lightColor, biome);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, voxelPosition, Vector3.back, 0, -1, ref faceCount, vertices, triangles, uvs, vertexColors, height, lightColor, biome);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, voxelPosition, Vector3.forward, 0, 1, ref faceCount, vertices, triangles, uvs, vertexColors, height, lightColor, biome);
            }
        }

        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            colors = vertexColors.ToArray()
        };
        mesh.RecalculateNormals();

        MeshFilter filter = chunk.chunkObject.GetComponent<MeshFilter>();
        if (!filter) filter = chunk.chunkObject.AddComponent<MeshFilter>();

        MeshRenderer renderer = chunk.chunkObject.GetComponent<MeshRenderer>();
        if (!renderer) renderer = chunk.chunkObject.AddComponent<MeshRenderer>();

        filter.mesh = mesh;
        renderer.material = voxelAtlasMaterial;

        MeshCollider meshCollider = chunk.chunkObject.GetComponent<MeshCollider>();
        if (!meshCollider) meshCollider = chunk.chunkObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;

        chunk.chunkObject.layer = LayerMask.NameToLayer("Ground");
        chunk.generatedMesh = mesh;
    }

    void TryAddSideIfNeighborLower(float[,] heightMap, int x, int z, int size, Vector3 pos, Vector3 dir, int dx, int dz,
    ref int faceCount, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> vertexColors,
    float height, Color vertexColor, BiomeData biome)
    {
        int nx = x + dx;
        int nz = z + dz;

        float currentHeight = heightMap[x, z];
        float neighborHeight = (nx < 0 || nz < 0 || nx >= size || nz >= size) ? -1 : heightMap[nx, nz];

        if (neighborHeight < currentHeight)
        {
            AddFace(vertices, triangles, uvs, vertexColors, pos, dir, ref faceCount, height, vertexColor, biome);
        }
    }

    void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors, Vector3 position, Vector3 normal, ref int faceCount, float height, Color vertexColor, BiomeData biome)
    {
        Vector3[] faceVerts = new Vector3[4];
        Vector2[] faceUVs = GetUVsForHeight(biome, height);

        if (normal == Vector3.up) // Top
        {
            faceVerts[0] = position + new Vector3(0, voxelSize, 0);
            faceVerts[1] = position + new Vector3(0, voxelSize, voxelSize);
            faceVerts[2] = position + new Vector3(voxelSize, voxelSize, voxelSize);
            faceVerts[3] = position + new Vector3(voxelSize, voxelSize, 0);
        }
        else if (normal == Vector3.forward) // Z+
        {
            faceVerts[0] = position + new Vector3(0, 0, voxelSize);
            faceVerts[1] = position + new Vector3(voxelSize, 0, voxelSize);
            faceVerts[2] = position + new Vector3(voxelSize, voxelSize, voxelSize);
            faceVerts[3] = position + new Vector3(0, voxelSize, voxelSize);
        }
        else if (normal == Vector3.back) // Z-
        {
            faceVerts[0] = position + new Vector3(voxelSize, 0, 0);
            faceVerts[1] = position + new Vector3(0, 0, 0);
            faceVerts[2] = position + new Vector3(0, voxelSize, 0);
            faceVerts[3] = position + new Vector3(voxelSize, voxelSize, 0);
        }
        else if (normal == Vector3.left) // X-
        {
            faceVerts[0] = position + new Vector3(0, 0, 0);
            faceVerts[1] = position + new Vector3(0, 0, voxelSize);
            faceVerts[2] = position + new Vector3(0, voxelSize, voxelSize);
            faceVerts[3] = position + new Vector3(0, voxelSize, 0);
        }
        else if (normal == Vector3.right) // X+
        {
            faceVerts[0] = position + new Vector3(voxelSize, 0, voxelSize);
            faceVerts[1] = position + new Vector3(voxelSize, 0, 0);
            faceVerts[2] = position + new Vector3(voxelSize, voxelSize, 0);
            faceVerts[3] = position + new Vector3(voxelSize, voxelSize, voxelSize);
        }

        int vertStart = vertices.Count;
        vertices.AddRange(faceVerts);

        
        triangles.Add(vertStart);
        triangles.Add(vertStart + 1);
        triangles.Add(vertStart + 2);

        triangles.Add(vertStart);
        triangles.Add(vertStart + 2);
        triangles.Add(vertStart + 3);

        for (int i = 0; i < 4; i++)
        {
            uvs.Add(faceUVs[i]);
            colors.Add(vertexColor);
        }

        faceCount++;
    }

    void SpawnObjectsInChunk(VoxelChunk chunk)
    {
        Vector3 chunkPosition = chunk.chunkObject.transform.position;

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                Vector3 position = chunkPosition + new Vector3((cx + 0.5f) * voxelSize, maxHeight * voxelSize, (cz + 0.5f) * voxelSize);

                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    if (ShouldSpawnObject(cx, cz, seed, chunkPosition))
                    {
                        Vector3 offset = CalculatePositionOffset(cx, cz, seed);
                        Vector3 spawnPosition = hit.point + offset;

                        bool tooClose = false;
                        foreach (Vector3 spawnedPosition in chunk.savedObjectPositions)
                        {
                            if (Vector3.Distance(hit.point, spawnedPosition) < minSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (tooClose) continue;

                        chunk.savedObjectPositions.Add(spawnPosition);
                        GameObject prefab = SelectDeterministicObjectPrefab(cx, cz, seed, chunk);
                        if (prefab != null)
                        {
                            chunk.savedObjects.Add(new SpawnedObjectData(spawnPosition, prefab));

                            GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
                            obj.transform.parent = chunk.chunkObject.transform;
                            chunk.objects.Add(obj);

                            MarkAreaOccupied(obj);

                            if (obj.TryGetComponent(out BreakableObject breakable))
                            {
                                breakable.owningChunk = chunk;
                                breakable.savedObjectIndex = chunk.savedObjects.Count - 1;
                            }

                            SpawnClusterAround(spawnPosition, chunk, obj);
                        }
                    }
                }
            }
        }

        if (!chunk.structureSpawned)
        {
            structureManager.SpawnStructuresInChunk(chunk);
            chunk.structureSpawned = true;
        }
    }

    void DespawnObjectsInChunk(VoxelChunk chunk)
    {
        foreach (GameObject obj in chunk.objects)
        {
            Destroy(obj);
        }
        chunk.objects.Clear();
    }

    void RespawnObjectsInChunk(VoxelChunk chunk)
    {
        for (int i = 0; i < chunk.savedObjects.Count; i++)
        {
            SpawnedObjectData data = chunk.savedObjects[i];
            GameObject prefab = PrefabRegistry.GetPrefabByName(data.prefabName);

            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {data.prefabName}");
                continue;
            }

            GameObject obj = Instantiate(prefab, data.position, Quaternion.identity);

            if (!string.IsNullOrEmpty(data.savedStateJson) && obj.TryGetComponent(out ISaveableObject saveable))
            {
                saveable.LoadState(data.savedStateJson);
            }

            obj.transform.parent = chunk.chunkObject.transform;
            chunk.objects.Add(obj);

            if (obj.TryGetComponent(out BreakableObject breakable))
            {
                breakable.owningChunk = chunk;
                breakable.savedObjectIndex = i;
            }
        }
    }

    void SpawnClusterAround(Vector3 centerPosition, VoxelChunk chunk, GameObject parentObject)
    {
        int clusterCount = Random.Range(2, 6);
        float clusterRadius = 1.5f;

        for (int i = 0; i < clusterCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * clusterRadius;
            Vector3 candidatePosition = centerPosition + new Vector3(randomOffset.x, 5f, randomOffset.y);

            if (Physics.Raycast(candidatePosition, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            {
                Vector3 finalPosition = hit.point;

                bool tooClose = false;
                foreach (Vector3 existing in chunk.savedObjectPositions)
                {
                    if (Vector3.Distance(finalPosition, existing) < 0.3f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                GameObject smallPrefab = SelectSmallObjectPrefab(parentObject, chunk);
                if (smallPrefab != null)
                {
                    GameObject smallObj = Instantiate(smallPrefab, finalPosition, Quaternion.identity);
                    smallObj.transform.parent = chunk.chunkObject.transform;
                    chunk.objects.Add(smallObj);
                    chunk.savedObjectPositions.Add(finalPosition);


                    chunk.savedObjects.Add(new SpawnedObjectData(finalPosition, smallPrefab));

                    if (smallObj.TryGetComponent(out InteractableItem interactable))
                    {
                        interactable.itemCount = 1;
                    }
                }
            }
        }
    }

    GameObject SelectSmallObjectPrefab(GameObject parentObject, VoxelChunk chunk)
    {
        if (!parentObject.TryGetComponent(out ObjectCategory category)) return null;

        switch (category.objectType)
        {
            case ObjectType.Tree:
                if (chunk.biome.treeClusterPrefabs != null && chunk.biome.treeClusterPrefabs.Count > 0)
                    return chunk.biome.treeClusterPrefabs[Random.Range(0, chunk.biome.treeClusterPrefabs.Count)];
                break;

            case ObjectType.Rock:
                if (chunk.biome.rockClusterPrefabs != null && chunk.biome.rockClusterPrefabs.Count > 0)
                    return chunk.biome.rockClusterPrefabs[Random.Range(0, chunk.biome.rockClusterPrefabs.Count)];
                break;
        }

        return null;
    }

    Vector2[] GetUVsForHeight(BiomeData biome, float height)
    {
        // Find the voxel type within the biome that matches the height
        VoxelType voxelType = biome.voxelTypes.FirstOrDefault(v => height >= v.minHeight && height <= v.maxHeight);

        if (voxelType == null)
        {
            return new Vector2[]
            {
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1)
            };
        }

        // Use index within the biome's voxel list for atlas lookup
        int index = voxelType.atlasIndex;
        int atlasSize = Mathf.CeilToInt(Mathf.Sqrt(biome.voxelTypes.Count));
        float tileSize = 1f / atlasSize;

        int x = index % atlasSize;
        int y = index / atlasSize;

        float u = x * tileSize;
        float v = y * tileSize;

        return new Vector2[]
        {
        new(u, v),
        new(u + tileSize, v),
        new(u + tileSize, v + tileSize),
        new(u, v + tileSize)
        };
    }

    void ManageChunks()
    {
        if (player == null)
        {
            Debug.LogWarning("Player is not assigned!");
            return;
        }

        playerPosition = player.transform.position;

        foreach (VoxelChunk chunk in chunks)
        {
            Vector3 chunkPosition = chunk.chunkObject.transform.position;
            float chunkDistance = Vector3.Distance(playerPosition, chunkPosition);

            // --- RENDERING & VISIBILITY ---
            if (chunkDistance > viewDistance * chunkSize * voxelSize)
            {
                if (chunk.objectsSpawned)
                {
                    SaveSystem.SaveChunk(worldName, chunk);
                    DespawnObjectsInChunk(chunk);

                    chunk.objectsSpawned = false;
                }

                SetChunkVisualsActive(chunk, false);
            }
            else
            {
                if (!chunk.objectsSpawned)
                {
                    if (!chunk.wasLoadedFromSave)
                    {
                        ChunkSaveData savedData = SaveSystem.LoadChunk(worldName, chunk.chunkPosition);
                        if (savedData != null)
                        {
                            LoadChunkObjects(chunk, savedData);
                            chunk.wasLoadedFromSave = true;
                        }
                        else
                        {
                            SpawnObjectsInChunk(chunk);
                        }
                    }

                    if (chunk.objects.Count == 0 && chunk.savedObjects.Count > 0)
                    {
                        RespawnObjectsInChunk(chunk);
                    }

                    chunk.objectsSpawned = true;
                }

                SetChunkVisualsActive(chunk, true);
            }

            // --- SIMULATION MANAGEMENT ---
            if (chunkDistance <= simulationDistance * chunkSize * voxelSize)
            {
                EnableSimulationInChunk(chunk);
            }
            else
            {
                DisableSimulationInChunk(chunk);
            }
        }
    }

    void SetChunkVisualsActive(VoxelChunk chunk, bool active)
    {
        MeshRenderer[] renderers = chunk.chunkObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
            renderer.enabled = active;
    }

    void EnableSimulationInChunk(VoxelChunk chunk)
    {
        foreach (ISimulatable sim in chunk.simulatedEntities)
            sim?.OnSimulateStart();

        Collider[] colliders = chunk.chunkObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = true;
    }

    void DisableSimulationInChunk(VoxelChunk chunk)
    {
        foreach (ISimulatable sim in chunk.simulatedEntities)
            sim?.OnSimulateStop();

        Collider[] colliders = chunk.chunkObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = false;
    }

    bool ShouldSpawnObject(int x, int z, int seed, Vector3 chunkPosition)
    {
        Vector3 worldPos = chunkPosition + new Vector3(x * voxelSize, 0, z * voxelSize);
        float frequency = 0.05f;
        float densityThreshold = 0.5f;

        float seedOffsetX = Mathf.Sin(seed * 0.1f) * 100f;
        float seedOffsetZ = Mathf.Cos(seed * 0.1f) * 100f;

        float noiseValue = Mathf.PerlinNoise(
            (worldPos.x + seedOffsetX) * frequency,
            (worldPos.z + seedOffsetZ) * frequency
        );

        return noiseValue > densityThreshold;
    }

    GameObject SelectDeterministicObjectPrefab(int x, int z, int seed, VoxelChunk chunk)
    {
        List<GameObject> allPrefabs = new();
        allPrefabs.AddRange(chunk.biome.treePrefabs);
        allPrefabs.AddRange(chunk.biome.rockPrefabs);

        if (allPrefabs.Count == 0) return null;

        int hash = x * 73856093 ^ z * 19349663 ^ (seed * 83492791);
        int randomIndex = Mathf.Abs(hash) % allPrefabs.Count;

        return allPrefabs[randomIndex];
    }

    public Vector3 CalculatePositionOffset(int x, int z, int seed)
    {
        float frequency = 0.1f;
        float amplitude = voxelSize * 0.2f;

        float noiseX = Mathf.PerlinNoise((x + seed) * frequency, (z + seed) * frequency);
        float noiseZ = Mathf.PerlinNoise((z + seed) * frequency, (x + seed) * frequency);

        return new Vector3(noiseX * amplitude, 0f, noiseZ * amplitude);
    }

    void CreateWorldBorders()
    {
        float terrainSize = gridSize * chunkSize * voxelSize;
        float borderOffset = chunkSize * voxelSize;

        float innerSize = terrainSize - 2 * borderOffset;
        float wallHeight = 50f;

        Vector3 center = new(terrainSize / 2, 0, terrainSize / 2);

        // Positive Z wall
        CreateBorderWall(new Vector3(center.x, wallHeight / 2, terrainSize - borderOffset), new Vector3(innerSize, wallHeight, 1));

        // Negative Z wall
        CreateBorderWall(new Vector3(center.x, wallHeight / 2, borderOffset), new Vector3(innerSize, wallHeight, 1));

        // Positive X wall
        CreateBorderWall(new Vector3(terrainSize - borderOffset, wallHeight / 2, center.z), new Vector3(1, wallHeight, innerSize));

        // Negative X wall
        CreateBorderWall(new Vector3(borderOffset, wallHeight / 2, center.z), new Vector3(1, wallHeight, innerSize));
    }

    void CreateBorderWall(Vector3 position, Vector3 scale)
    {
        if (borderWallPrefab == null)
        {
            Debug.LogWarning("Border wall prefab not assigned!");
            return;
        }

        GameObject wall = Instantiate(borderWallPrefab, position, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.name = "WorldBorder";
    }

    public bool IsOccupied(Vector3Int position)
    {
        return voxelOccupancy.Contains(position);
    }

    public void SetOccupied(Vector3Int position, bool occupy = true)
    {
        if (occupy)
            voxelOccupancy.Add(position);
        else
            voxelOccupancy.Remove(position);
    }

    public void MarkAreaOccupied(GameObject prefab, bool occupy = true)
    {
        Bounds bounds = prefab.GetComponentInChildren<Renderer>().bounds;

        Vector3Int min = WorldToVoxelCoord(bounds.min);
        Vector3Int max = WorldToVoxelCoord(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                Vector3Int voxelPos = new(x, 0, z);
                SetOccupied(voxelPos, occupy);
            }
        }
    }

    Color LightToColor(byte lightLevel, float dayMultiplier)
    {
        float intensity = (lightLevel / 15f) * dayMultiplier;
        return new Color(intensity, intensity, intensity, 1f);
    }

    BiomeData SelectBiome(Vector3 chunkPosition)
    {
        float biomeNoise = GenerateBiomeNoise(chunkPosition.x, chunkPosition.z);
        float normalizedNoise = Mathf.InverseLerp(-1f, 1f, biomeNoise);

        for (int i = 0; i < biomes.Count; i++)
        {
            float t = (i + 1) / (float)biomes.Count;
            if (normalizedNoise < t)
            {
                return biomes[i];
            }
        }

        return biomes[^1];
    }

    float GenerateBiomeHeight(float x, float z, NoiseSettings settings)
    {
        float total = 0f;
        float frequency = settings.baseScale;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        float seedOffsetX = Mathf.Sin(seed * 0.1f) * 100f;
        float seedOffsetZ = Mathf.Cos(seed * 0.1f) * 100f;

        for (int i = 0; i < settings.octaves; i++)
        {
            float noiseValue = Mathf.PerlinNoise((x + seedOffsetX) * frequency, (z + seedOffsetZ) * frequency);
            total += noiseValue * amplitude;
            maxAmplitude += amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        float normalizedHeight = total / maxAmplitude;

        if (settings.useHeightCurve)
        {
            normalizedHeight = settings.heightCurve.Evaluate(normalizedHeight);
        }
        else
        {
            normalizedHeight = Mathf.Pow(normalizedHeight, settings.heightExponent);
        }

        return normalizedHeight * settings.heightScale;
    }

    float GenerateBiomeNoise(float x, float z)
    {
        float scale = biomeNoiseSettings.baseScale;
        return Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale) * 2f - 1f;
    }

    void LoadChunkObjects(VoxelChunk chunk, ChunkSaveData savedData)
    {
        chunk.savedObjects.Clear();
        chunk.objects.Clear();
        chunk.savedObjectPositions.Clear();

        foreach (SpawnedObjectData objData in savedData.spawnedObjects)
        {
            GameObject prefab = PrefabRegistry.GetPrefabByName(objData.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {objData.prefabName}");
                continue;
            }

            Vector3 spawnPosition = objData.position;
            GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            obj.transform.parent = chunk.chunkObject.transform;

            if (!string.IsNullOrEmpty(objData.savedStateJson) && obj.TryGetComponent(out ISaveableObject saveable))
            {
                saveable.LoadState(objData.savedStateJson);
            }

            chunk.objects.Add(obj);
            chunk.savedObjectPositions.Add(spawnPosition);
            chunk.savedObjects.Add(objData);

            if (obj.TryGetComponent(out BreakableObject breakable))
            {
                breakable.owningChunk = chunk;
                breakable.savedObjectIndex = chunk.savedObjects.Count - 1;
            }

            SpawnClusterAround(spawnPosition, chunk, obj);
        }
    }

    public void ResetWorld()
    {
        Debug.Log("Resetting world...");

        // Destroy all chunk GameObjects
        foreach (VoxelChunk chunk in chunks)
        {
            if (chunk.chunkObject != null)
                Destroy(chunk.chunkObject);
        }

        chunks.Clear();

        if (structureManager != null) 
            structureManager.ClearStructures();

        SaveSystem.ClearAllChunkSaves(worldName);

        // Regenerate
        GenerateTerrain();
        CreateWorldBorders();

        Debug.Log("World reset complete.");
    }

    public Vector3Int WorldToVoxelCoord(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / voxelSize),
            Mathf.FloorToInt(worldPos.y / voxelSize),
            Mathf.FloorToInt(worldPos.z / voxelSize)
        );
    }
}
