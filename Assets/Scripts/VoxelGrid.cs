using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VoxelGrid : MonoBehaviour
{
    [System.Flags]
    public enum VoxelState
    {
        None = 0,
        Occupied = 1 << 0,
        Walkable = 1 << 1,
        Buildable = 1 << 2,
    }

    public Material voxelMaterial;
    [SerializeField] private Transform voxelGridRoot;
    public GameObject campFirePrefab;

    public float simulationDistance = 10f;
    public float viewDistance = 2f; // For object spawning/visibility

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
    private Vector3 worldCenter;
    private float terrainWidth;

    public readonly List<VoxelChunk> chunks = new();
    public Dictionary<Vector2Int, VoxelChunk> chunkMap = new();

    public List<BiomeData> biomes;
    public NoiseSettings biomeNoiseSettings;

    private readonly Dictionary<Vector3Int, VoxelState> voxelStates = new();

    public delegate void WorldGenerationProgress(float progress);
    public WorldGenerationProgress OnProgress;

    [Header("World Dimensions")]
    public int chunkSize = 16;
    public int gridSize = 10;
    public float voxelSize = 1f;
    public float maxHeight = 5f;
    public float heightScale = 0.5f;

    [Header("Managers")]
    [SerializeField] private ColliderPool colliderPool;
    public StructureManager structureManager;
    public AnimalSpawner animalSpawner;
    public AltarSpawner altarSpawner;
    public DayNightCycle dayNightCycle;
    public KeyStructureSpawner keyStructureSpawner;

    private void Start()
    {
        terrainWidth = chunkSize * gridSize;
        worldCenter = new(terrainWidth / 2f, 0f, terrainWidth / 2f);
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            this.player = player;
            playerPosition = player.transform.position;
        }
    }

    public void SetWorld(int worldSeed, string name)
    {
        Debug.Log($"[VoxelGrid] SetWorld seed={worldSeed}, name={name}");

        if (worldGenerated) return;

        animalSpawner.groundLayer = groundLayer;
        seed = worldSeed;
        worldName = name;

        string worldDir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");

        if (Directory.Exists(worldDir) && Directory.GetFiles(worldDir, "*.chunk").Length > 0)
        {
            // Load saved chunks instead of regenerating
            StartCoroutine(LoadChunksRoutine(worldName));
        }
        else
        {
            // First time: generate and save
            StartCoroutine(GenerateTerrain());
        }
    }

    public IEnumerator GenerateTerrain()
    {
        int totalSteps = gridSize * gridSize // chunk generation
                     + 6; // key structures, natural objects, altars, campfire, borders, saving
        int currentStep = 0;

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 chunkPosition = new(x * chunkSize * voxelSize, 0, z * chunkSize * voxelSize);
                GameObject chunkObject = new("VoxelChunk " + x + "," + z);
                chunkObject.transform.SetParent(voxelGridRoot, false);
                chunkObject.transform.position = chunkPosition;

                VoxelChunk chunk = new(chunkObject, chunkSize);
                chunks.Add(chunk);

                Vector2Int chunkKey = new(x, z);
                chunkMap[chunkKey] = chunk;

                animalSpawner.chunks.Add(chunk);

                GenerateChunkTerrain(chunk, chunkPosition);

                currentStep++;
                OnProgress?.Invoke((float)currentStep / totalSteps);

                yield return null;
            }
        }

        // --- Chunk management routine ---
        StartCoroutine(ManageChunksRoutine());

        // --- Key structures ---
        yield return StartCoroutine(keyStructureSpawner.SpawnKeyStructures(terrainWidth, worldCenter, (progress) =>
        {
            OnProgress?.Invoke((currentStep + progress) / totalSteps);
        }));
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- Altars ---
        altarSpawner.worldCenter = worldCenter;
        yield return StartCoroutine(altarSpawner.SpawnAltarsRoutine(terrainWidth, (progress) =>
        {
            OnProgress?.Invoke((currentStep + progress) / totalSteps);
        }));
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- Campfire ---
        yield return null;
        SpawnCampfireAtCenter();
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- World borders ---
        yield return null;
        CreateWorldBorders();
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- Natural objects ---
        yield return StartCoroutine(SpawnNaturalObjects());
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- Save chunks ---
        SaveAllChunks(worldName);
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        worldGenerated = true;
        OnProgress?.Invoke(1f);
    }

    private void SpawnCampfireAtCenter()
    {
        if (campFirePrefab == null) return;

        Vector3Int gridPos = WorldToVoxelCoord(worldCenter);
        Vector3 snappedPos = VoxelCoordToWorld(gridPos);

        Vector3 rayStart = snappedPos + Vector3.up * maxHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            float heightOffset = 0f;
            if (campFirePrefab.TryGetComponent(out CapsuleCollider col))
                heightOffset = col.height / 2f;

            Vector3 spawnPos = hit.point + new Vector3(0, heightOffset, 0);
            GameObject spawnedCampfire = Instantiate(campFirePrefab, spawnPos, Quaternion.identity);
            MarkAreaOccupied(spawnedCampfire);

            int x = (int)(spawnPos.x / chunkSize);
            int z = (int)(spawnPos.z / chunkSize);
            Vector2Int chunkKey = new(x, z);

            VoxelChunk chunk = chunkMap[chunkKey];
            chunk.objects.Add(spawnedCampfire);

            GameManager.Instance.SetCampfire(spawnedCampfire);
        }
    }

    private IEnumerator SpawnNaturalObjects()
    {
        int totalChunks = chunks.Count;
        int chunkIndex = 0;

        foreach (VoxelChunk chunk in chunks)
        {
            SpawnObjectsInChunk(chunk); // your existing logic
            chunk.hasNaturalObjects = true;

            chunkIndex++;
            OnProgress?.Invoke((float)chunkIndex / totalChunks);

            yield return null;
        }
    }

    public void SaveAllChunks(string worldName)
    {
        foreach (VoxelChunk chunk in chunks)
        {
            SaveSystem.SaveChunk(worldName, chunk);
        }
    }

    void GenerateChunkTerrain(VoxelChunk chunk, Vector3 chunkPosition)
    {
        BiomeData biome = SelectBiome(chunkPosition);
        chunk.biome = biome;

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();
        List<Color> vertexColors = new();
        List<Vector3> normals = new();

        int faceCount = 0;

        chunk.heightMap = new float[chunkSize, chunkSize];

        // Generate height map
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

        // Generate mesh
        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                float height = chunk.heightMap[cx, cz];
                Vector3 voxelPosition = new(cx * voxelSize, height, cz * voxelSize);

                Voxel voxel = new(voxelPosition);
                
                chunk.voxels[cx, cz] = voxel;

                Color lightColor = Color.white;

                AddFace(vertices, triangles, uvs, vertexColors, normals, voxelPosition, Vector3.up, ref faceCount, lightColor);

                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, chunkPosition, voxelPosition, Vector3.left, -1, 0, ref faceCount, vertices, triangles, uvs, vertexColors, normals, lightColor, biome.noiseSettings);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, chunkPosition, voxelPosition, Vector3.right, 1, 0, ref faceCount, vertices, triangles, uvs, vertexColors, normals, lightColor, biome.noiseSettings);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, chunkPosition, voxelPosition, Vector3.back, 0, -1, ref faceCount, vertices, triangles, uvs, vertexColors, normals, lightColor, biome.noiseSettings);
                TryAddSideIfNeighborLower(chunk.heightMap, cx, cz, chunkSize, chunkPosition, voxelPosition, Vector3.forward, 0, 1, ref faceCount, vertices, triangles, uvs, vertexColors, normals, lightColor, biome.noiseSettings);
            }
        }

        // Finalize mesh
        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            colors = vertexColors.ToArray(),
            normals = normals.ToArray()
        };

        MeshFilter filter = chunk.chunkObject.GetComponent<MeshFilter>();
        if (!filter) filter = chunk.chunkObject.AddComponent<MeshFilter>();

        MeshRenderer renderer = chunk.chunkObject.GetComponent<MeshRenderer>();
        if (!renderer) renderer = chunk.chunkObject.AddComponent<MeshRenderer>();

        filter.mesh = mesh;
        renderer.material = voxelMaterial;

        MeshCollider meshCollider = chunk.chunkObject.GetComponent<MeshCollider>();
        if (!meshCollider) meshCollider = chunk.chunkObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;

        chunk.chunkObject.layer = LayerMask.NameToLayer("Ground");
        chunk.generatedMesh = mesh;
    }

    void TryAddSideIfNeighborLower(float[,] heightMap, int x, int z, int size, Vector3 chunkPos, Vector3 pos, Vector3 dir, int dx, int dz, ref int faceCount, 
        List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> vertexColors, List<Vector3> normals, Color vertexColor, NoiseSettings noiseSettings)
    {
        int nx = x + dx;
        int nz = z + dz;

        float currentHeight = heightMap[x, z];
        float neighborHeight;

        if (nx < 0 || nz < 0 || nx >= size || nz >= size)
        {
            // Sample from world position
            float worldX = chunkPos.x + nx;
            float worldZ = chunkPos.z + nz;

            neighborHeight = GenerateBiomeHeight(worldX, worldZ, noiseSettings);
            neighborHeight = Mathf.Floor(neighborHeight / heightScale) * heightScale;
            neighborHeight = Mathf.Clamp(neighborHeight, 0, maxHeight);
        }
        else
        {
            neighborHeight = heightMap[nx, nz];
        }

        if (neighborHeight < currentHeight)
        {
            AddFace(vertices, triangles, uvs, vertexColors, normals, pos, dir, ref faceCount, vertexColor);
        }
    }

    void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Color> colors, List<Vector3> normals, Vector3 position, Vector3 normal, ref int faceCount, Color vertexColor)
    {
        Vector3[] faceVerts = new Vector3[4];
        Vector2[] faceUVs = GetStandardUVs();

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
            normals.Add(normal);
        }

        faceCount++;
    }

    void SpawnObjectsInChunk(VoxelChunk chunk)
    {
        Vector3 chunkPosition = chunk.chunkObject.transform.position;
        float minDistanceFromCenter = 10f;

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                Vector3 position = chunkPosition + new Vector3((cx + 0.5f) * voxelSize, maxHeight * voxelSize, (cz + 0.5f) * voxelSize);

                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    if (Vector3.Distance(hit.point, worldCenter) < minDistanceFromCenter)
                        continue;

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

    Vector2[] GetStandardUVs()
    {
        return new Vector2[]
        {
        new(0, 0),
        new(0, 1),
        new(1, 1),
        new(1, 0)
        };
    }

    IEnumerator ManageChunksRoutine()
    {
        while (true)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            playerPosition = player.transform.position;

            int chunksProcessed = 0;
            const int chunksPerFrame = 5; // Tweak as needed

            foreach (VoxelChunk chunk in chunks)
            {
                ProcessChunk(chunk);

                chunksProcessed++;
                if (chunksProcessed >= chunksPerFrame)
                {
                    chunksProcessed = 0;
                    yield return null; // Spread work over frames
                }
            }

            yield return null; // Wait for next frame before starting again
        }
    }

    void ProcessChunk(VoxelChunk chunk)
    {
        float chunkDistance = Vector3.Distance(playerPosition, chunk.chunkPosition);

        // --- RENDERING & VISIBILITY ---
        if (chunkDistance > viewDistance * chunkSize * voxelSize)
        {
            if (chunk.objectsSpawned)
            {
                SaveSystem.SaveChunk(worldName, chunk);
                DespawnObjectsInChunk(chunk);
                animalSpawner.DespawnAnimalsForChunk(chunk);
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

                StartCoroutine(animalSpawner.SpawnAnimalsForChunk(chunk));

                if (chunk.objects.Count == 0 && chunk.savedObjects.Count > 0)
                {
                    RespawnObjectsInChunk(chunk);
                }

                chunk.objectsSpawned = true;
            }

            SetChunkVisualsActive(chunk, true);
        }

        // --- SIMULATION ---
        if (chunkDistance <= simulationDistance * chunkSize * voxelSize)
        {
            EnableSimulationInChunk(chunk);
        }
        else
        {
            DisableSimulationInChunk(chunk);
        }
    }

    void SetChunkVisualsActive(VoxelChunk chunk, bool active)
    {
        if (chunk.visualsEnabled == active)
            return;

        foreach (MeshRenderer renderer in chunk.cachedRenderers)
            renderer.enabled = active;

        chunk.visualsEnabled = active;
    }

    void EnableSimulationInChunk(VoxelChunk chunk)
    {
        if (chunk.simulationEnabled)
            return;

        foreach (ISimulatable sim in chunk.simulatedEntities)
            sim?.OnSimulateStart();

        foreach (Collider col in chunk.cachedColliders)
            col.enabled = true;

        chunk.simulationEnabled = true;
    }

    void DisableSimulationInChunk(VoxelChunk chunk)
    {
        if (!chunk.simulationEnabled)
            return;

        foreach (ISimulatable sim in chunk.simulatedEntities)
            sim?.OnSimulateStop();

        foreach (Collider col in chunk.cachedColliders)
            col.enabled = false;

        chunk.simulationEnabled = false;
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
            return;
        }

        GameObject wall = Instantiate(borderWallPrefab, position, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.name = "WorldBorder";
    }

    public bool IsOccupied(Vector3Int pos) =>
        voxelStates.TryGetValue(pos, out VoxelState state) && state.HasFlag(VoxelState.Occupied);

    public bool IsWalkable(Vector3Int pos)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState state))
        {
            // Voxel not in dictionary → treat as walkable by default
            return true;
        }
        return state.HasFlag(VoxelState.Walkable);
    }

    public bool IsBuildable(Vector3Int pos)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState state))
        {
            // Voxel not in dictionary → treat as buildable by default
            return true;
        }
        return state.HasFlag(VoxelState.Buildable);
    }

    public void SetVoxelState(Vector3Int pos, VoxelState flag, bool enable)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState current))
            current = VoxelState.None;

        if (enable)
            voxelStates[pos] = current | flag;
        else
        {
            current &= ~flag;
            if (current == VoxelState.None)
                voxelStates.Remove(pos); // cleanup
            else
                voxelStates[pos] = current;
        }
    }

    public void MarkAreaOccupied(GameObject prefab, bool occupy = true, bool walkable = false)
    {
        Bounds bounds = prefab.GetComponentInChildren<Renderer>().bounds;

        Vector3Int min = WorldToVoxelCoord(bounds.min);
        Vector3Int max = WorldToVoxelCoord(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                Vector3Int voxelPos = new(x, 0, z);

                SetVoxelState(voxelPos, VoxelState.Occupied, occupy);
                SetVoxelState(voxelPos, VoxelState.Walkable, walkable);
                SetVoxelState(voxelPos, VoxelState.Buildable, false); // prevent building
            }
        }
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

    public IEnumerator LoadChunksRoutine(string worldName)
    {
        string chunksDir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");
        if (!Directory.Exists(chunksDir))
        {
            Debug.LogWarning($"No chunk folder found for world '{worldName}'. Generating new world.");
            yield return StartCoroutine(GenerateTerrain());
            yield break;
        }

        string[] files = Directory.GetFiles(chunksDir, "*.json");
        if (files.Length == 0)
        {
            Debug.LogWarning($"No saved chunks found for world '{worldName}'. Generating new world.");
            yield return StartCoroutine(GenerateTerrain());
            yield break;
        }

        int totalSteps = files.Length
                       + 6; // same extra steps as GenerateTerrain
        int currentStep = 0;

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file); // "chunk_x_z"
            string[] parts = fileName.Split('_');
            if (parts.Length != 3) continue;

            if (!float.TryParse(parts[1], out float x)) continue;
            if (!float.TryParse(parts[2], out float z)) continue;

            Vector3 chunkPos = new(x, 0, z);

            ChunkSaveData data = SaveSystem.LoadChunk(worldName, chunkPos);
            if (data == null)
            {
                Debug.LogWarning($"Failed to load chunk at {chunkPos}. Skipping.");
                continue;
            }

            // Rebuild chunk
            GameObject chunkObj = new($"VoxelChunk {chunkPos.x},{chunkPos.z}");
            chunkObj.transform.SetParent(voxelGridRoot, false);
            chunkObj.transform.position = chunkPos;

            VoxelChunk chunk = new(chunkObj, chunkSize)
            {
                chunkPosition = data.chunkPosition,
                savedObjects = new List<SpawnedObjectData>(data.spawnedObjects),
                hasNaturalObjects = data.hasNaturalObjects,
                hasKeyStructure = data.hasKeyStructure
            };

            GenerateChunkTerrain(chunk, chunk.chunkPosition);

            chunks.Add(chunk);
            chunkMap[new Vector2Int(
                (int)data.chunkPosition.x / chunkSize,
                (int)data.chunkPosition.z / chunkSize)] = chunk;
            animalSpawner.chunks.Add(chunk);

            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null; // let UI update
        }

        // Start managing chunks
        StartCoroutine(ManageChunksRoutine());

        // Key structures / natural objects / altars / campfire / borders / save
        yield return StartCoroutine(keyStructureSpawner.SpawnKeyStructures(terrainWidth, worldCenter,
            progress => OnProgress?.Invoke((currentStep + progress) / totalSteps)));
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        yield return StartCoroutine(SpawnNaturalObjects());
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        altarSpawner.worldCenter = worldCenter;
        yield return StartCoroutine(altarSpawner.SpawnAltarsRoutine(terrainWidth,
            progress => OnProgress?.Invoke((currentStep + progress) / totalSteps)));
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        SpawnCampfireAtCenter();
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        CreateWorldBorders();
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        SaveAllChunks(worldName);
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        worldGenerated = true;
        OnProgress?.Invoke(1f);
    }

    void LoadChunkObjects(VoxelChunk chunk, ChunkSaveData savedData)
    {
        chunk.savedObjects.Clear();
        chunk.objects.Clear();
        chunk.savedObjectPositions.Clear();

        if (savedData.spawnedObjects == null || savedData.spawnedObjects.Count == 0)
        {
            return;
        }

        foreach (SpawnedObjectData objData in savedData.spawnedObjects)
        {
            GameObject prefab = PrefabRegistry.GetPrefabByName(objData.prefabName);
            if (prefab == null)
            {
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
        StartCoroutine(GenerateTerrain());
        CreateWorldBorders();
    }

    public Vector3Int WorldToVoxelCoord(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / voxelSize),
            Mathf.FloorToInt(worldPos.y / voxelSize),
            Mathf.FloorToInt(worldPos.z / voxelSize)
        );
    }

    public Vector3 VoxelCoordToWorld(Vector3Int voxelCoord)
    {
        return new Vector3(
            voxelCoord.x + 0.5f,
            voxelCoord.y + 0.5f,
            voxelCoord.z + 0.5f);
    }

    public float GetHeightAt(int x, int z)
    {
        int chunkX = Mathf.FloorToInt((float)x / chunkSize);
        int chunkZ = Mathf.FloorToInt((float)z / chunkSize);

        // Handle negative modulus properly
        int localX = x - chunkX * chunkSize;
        int localZ = z - chunkZ * chunkSize;

        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (!chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
        {
            return 0f; // or some default height
        }

        if (localX < 0 || localX >= chunkSize || localZ < 0 || localZ >= chunkSize)
        {
            return 0f; // or default height
        }

        return chunk.heightMap[localX, localZ];
    }

    public Vector3 GetDefaultSpawnPosition()
    {
        Vector3 defaultSpawn = GameManager.Instance.campFireInstance.transform.position + new Vector3(2, 0, 2);
        float height = GetHeightAt((int)defaultSpawn.x, (int)defaultSpawn.z);
        defaultSpawn.y = height;

        return defaultSpawn;
    }
}
