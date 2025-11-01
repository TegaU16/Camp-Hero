using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VoxelGrid : MonoBehaviour
{
    public static VoxelGrid Instance;

    [System.Flags]
    public enum VoxelState
    {
        None = 0,
        Occupied = 1 << 0,
        Walkable = 1 << 1,
        Buildable = 1 << 2,
    }

    [SerializeField] private Material voxelMaterial;
    [SerializeField] private Transform voxelGridRoot;
    [SerializeField] private GameObject campFirePrefab;

    [SerializeField] private float minSpacing = 2.0f;

    [HideInInspector] public int seed = 12345;
    private string worldName;

    [SerializeField] private GameObject borderWallPrefab;
    [SerializeField] private int chunksFromBorder;

    [HideInInspector]
    public bool worldGenerated = false;

    public LayerMask groundLayer;
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
    [SerializeField] private StructureManager structureManager;
    [SerializeField] private AnimalSpawner animalSpawner;
    [SerializeField] private GemAltarSpawner gemAltarSpawner;
    [SerializeField] private KeyStructureSpawner keyStructureSpawner;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        terrainWidth = chunkSize * gridSize;
        worldCenter = new(terrainWidth / 2f, 0f, terrainWidth / 2f);
    }

    public void SetWorld(int worldSeed, string name)
    {
        if (worldGenerated) return;

        animalSpawner.groundLayer = groundLayer;
        seed = worldSeed;
        worldName = name;

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

        if (hasChunks)
            StartCoroutine(LoadChunksRoutine(worldName));
        else
            StartCoroutine(GenerateTerrain());
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

        int extraSteps = 2;
        int totalSteps = files.Length + extraSteps;
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
                hasNaturalObjects = data.hasNaturalObjects,
                hasKeyStructure = data.hasKeyStructure
            };

            GenerateChunkTerrain(chunk, chunk.chunkPosition);

            LoadChunkObjects(chunk, data);

            chunks.Add(chunk);
            chunkMap[new Vector2Int(
                (int)data.chunkPosition.x / chunkSize,
                (int)data.chunkPosition.z / chunkSize)] = chunk;
            animalSpawner.chunks.Add(chunk);

            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null; // let UI update
        }

        CreateWorldBorders();
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        SaveAllChunks(worldName);
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);

        worldGenerated = true;
        OnProgress?.Invoke(1f);
    }

    public IEnumerator GenerateTerrain()
    {
        int extraSteps = 6;
        int totalSteps = gridSize * gridSize + extraSteps;
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

        // --- Key structures ---
        yield return StartCoroutine(keyStructureSpawner.SpawnKeyStructures(terrainWidth, worldCenter, (progress) =>
        {
            OnProgress?.Invoke((currentStep + progress) / totalSteps);
        }));
        currentStep++;
        OnProgress?.Invoke((float)currentStep / totalSteps);
        yield return null;

        // --- Altars ---
        gemAltarSpawner.worldCenter = worldCenter;
        yield return StartCoroutine(gemAltarSpawner.SpawnAltarsRoutine(terrainWidth, (progress) =>
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

        Vector3Int gridPos = Utility.WorldToVoxelCoord(worldCenter);
        Vector3 snappedPos = Utility.VoxelCoordToWorld(gridPos);

        Vector3 rayStart = snappedPos + Vector3.up * maxHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            float heightOffset = 0f;
            if (campFirePrefab.TryGetComponent(out CapsuleCollider col))
                heightOffset = col.height / 2f;

            Vector3 spawnPos = hit.point + new Vector3(0, heightOffset, 0);
            GameObject spawnedCampfire = Instantiate(campFirePrefab, spawnPos, Quaternion.identity);

            GameManager.Instance.SetCampfire(spawnedCampfire);
            if (spawnedCampfire.TryGetComponent(out Health health))
                health.SetHealth(health.maxHealth);

            MarkAreaOccupied(spawnedCampfire);

            int x = (int)(spawnPos.x / chunkSize);
            int z = (int)(spawnPos.z / chunkSize);
            Vector2Int chunkKey = new(x, z);

            VoxelChunk chunk = chunkMap[chunkKey];
            spawnedCampfire.transform.parent = chunk.chunkObject.transform;
            chunk.objects.Add(spawnedCampfire);

            SpawnedObjectData data = new(spawnPos, spawnedCampfire, campFirePrefab);

            chunk.savedObjects.Add(data);
            chunk.savedObjectPositions.Add(spawnPos);
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
            SaveSystem.SaveChunk(worldName, chunk);
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
        if (!filter) 
            filter = chunk.chunkObject.AddComponent<MeshFilter>();

        MeshRenderer renderer = chunk.chunkObject.GetComponent<MeshRenderer>();
        if (!renderer) 
            renderer = chunk.chunkObject.AddComponent<MeshRenderer>();

        MeshCollider meshCollider = chunk.chunkObject.GetComponent<MeshCollider>();
        if (!meshCollider)
            meshCollider = chunk.chunkObject.AddComponent<MeshCollider>();

        filter.mesh = mesh;
        renderer.material = voxelMaterial;
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
            AddFace(vertices, triangles, uvs, vertexColors, normals, pos, dir, ref faceCount, vertexColor);
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
        if (!chunk.structureSpawned)
        {
            structureManager.SpawnStructuresInChunk(chunk);
            chunk.structureSpawned = true;
        }

        // --- Safety init ---
        chunk.savedObjects ??= new List<SpawnedObjectData>();
        chunk.savedObjectPositions ??= new List<Vector3>();
        chunk.objects ??= new List<GameObject>();

        Vector3 chunkPosition = chunk.chunkObject.transform.position;
        float minDistanceFromCenter = 10f;

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cz = 0; cz < chunkSize; cz++)
            {
                if (ShouldSpawnObject(cx, cz, seed, chunkPosition))
                {
                    Vector3 basePosition = chunkPosition + new Vector3(
                        (cx + 0.5f) * voxelSize,
                        maxHeight * voxelSize,
                        (cz + 0.5f) * voxelSize
                    );

                    Vector3 offset = CalculatePositionOffset(cx, cz, seed);
                    Vector3 testPosition = basePosition + offset;
                    Vector3Int spawnPosSnapped = Utility.WorldToVoxelCoord(testPosition);
                    testPosition = spawnPosSnapped + new Vector3(voxelSize / 2f, 0, voxelSize / 2f);

                    if (IsOccupied(spawnPosSnapped)) continue;

                    // Single raycast for final ground snap
                    if (Physics.Raycast(testPosition, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
                    {
                        Vector3 spawnPosition = hit.point;

                        if (Vector3.Distance(spawnPosition, worldCenter) < minDistanceFromCenter)
                            continue;

                        if (!IsWithinBorders(spawnPosition)) continue;

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
                            GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
                            obj.transform.parent = chunk.chunkObject.transform;
                            chunk.objects.Add(obj);

                            SpawnedObjectData data = new(spawnPosition, obj, prefab);

                            chunk.savedObjects.Add(data);
                            chunk.savedObjectPositions.Add(spawnPosition);

                            int savedIndex = chunk.savedObjects.Count - 1;

                            MarkAreaOccupied(obj);

                            if (obj.TryGetComponent(out BreakableObject breakable))
                            {
                                breakable.owningChunk = chunk;
                                breakable.savedObjectIndex = savedIndex;
                            }

                            if (obj.TryGetComponent(out InteractableItem interactable))
                            {
                                interactable.owningChunk = chunk;
                                interactable.savedObjectIndex = savedIndex;
                            }

                            SpawnClusterAround(spawnPosition, chunk, obj);
                        }
                    }
                }
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
            Vector3Int candidatePosInt = Utility.WorldToVoxelCoord(candidatePosition);

            if (IsOccupied(candidatePosInt)) continue;

            if (Physics.Raycast(candidatePosition, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            {
                Vector3 finalPosition = hit.point;

                if (!IsWithinBorders(finalPosition)) continue;

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

                    SpawnedObjectData data = new(finalPosition, smallObj, smallPrefab);
                    chunk.savedObjects.Add(data);
                    chunk.savedObjectPositions.Add(finalPosition);

                    int savedIndex = chunk.savedObjects.Count - 1;
                    MarkAreaOccupied(smallObj);

                    if (smallObj.TryGetComponent(out InteractableItem interactable))
                    {
                        interactable.itemCount = 1;
                        interactable.owningChunk = chunk;
                        interactable.savedObjectIndex = savedIndex;
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

    public void RemoveObjectFromChunk(VoxelChunk chunk, int index)
    {
        if (chunk == null) return;
        if (index < 0 || index >= chunk.savedObjects.Count) return;

        if (index < chunk.objects.Count)
        {
            GameObject instance = chunk.objects[index];
            if (instance != null) Destroy(instance);
            chunk.objects.RemoveAt(index);
        }

        chunk.savedObjects.RemoveAt(index);
        if (index < chunk.savedObjectPositions.Count)
            chunk.savedObjectPositions.RemoveAt(index);

        chunk.isDirty = true;

        for (int i = 0; i < chunk.objects.Count; i++)
        {
            GameObject g = chunk.objects[i];
            if (g == null) continue;

            if (g.TryGetComponent(out BreakableObject br) && br.owningChunk == chunk)
                br.savedObjectIndex = i;

            if (g.TryGetComponent(out InteractableItem it) && it.owningChunk == chunk)
                it.savedObjectIndex = i;
        }
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

    void LoadChunkObjects(VoxelChunk chunk, ChunkSaveData savedData)
    {
        chunk.objects.Clear();
        chunk.savedObjectPositions.Clear();

        if (savedData.spawnedObjects == null || savedData.spawnedObjects.Count == 0) return;

        chunk.savedObjects = new List<SpawnedObjectData>(savedData.spawnedObjects);

        for (int i = 0; i < chunk.savedObjects.Count; i++)
        {
            SpawnedObjectData objData = chunk.savedObjects[i];

            GameObject prefab = PrefabRegistry.GetPrefabByKey(objData.prefabID);
            if (prefab == null) continue;

            Vector3 spawnPosition = objData.position;
            GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            obj.transform.parent = chunk.chunkObject.transform;

            if (!string.IsNullOrEmpty(objData.savedStateJson) && obj.TryGetComponent(out ISaveableObject saveable))
                saveable.LoadState(objData.savedStateJson);

            chunk.objects.Add(obj);
            chunk.savedObjectPositions.Add(spawnPosition);

            if (obj.TryGetComponent(out BreakableObject breakable))
            {
                breakable.owningChunk = chunk;
                breakable.savedObjectIndex = i;
            }

            if (obj.TryGetComponent(out InteractableItem interactable))
            {
                interactable.owningChunk = chunk;
                interactable.savedObjectIndex = i;
            }
        }

        chunk.hasNaturalObjects = true;
        chunk.structureSpawned = true;
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
        float frequency = 0.35f;
        float amplitude = voxelSize * 4f;

        // Smooth base offset (clusters)
        float noiseX = Mathf.PerlinNoise((x + seed) * frequency, (z + seed) * frequency);
        float noiseZ = Mathf.PerlinNoise((x + seed + 1337) * frequency, (z + seed + 9999) * frequency);
        float offsetX = (noiseX - 0.5f) * amplitude;
        float offsetZ = (noiseZ - 0.5f) * amplitude;

        // Add chaotic hash jitter (kills streaks)
        int hash = x * 73856093 ^ z * 19349663 ^ seed;
        System.Random rng = new(hash);
        float jitterX = (float)(rng.NextDouble() - 0.5) * voxelSize * chunkSize / 2f;
        float jitterZ = (float)(rng.NextDouble() - 0.5) * voxelSize * chunkSize / 2f;

        return new Vector3(offsetX + jitterX, 0f, offsetZ + jitterZ);
    }

    void CreateWorldBorders()
    {
        float terrainSize = gridSize * chunkSize * voxelSize;
        float borderOffset = chunkSize * voxelSize * chunksFromBorder;

        float innerSize = terrainSize - 2 * borderOffset;
        float wallHeight = 50f;

        // Positive Z wall
        CreateBorderWall(new Vector3(worldCenter.x, wallHeight / 2, terrainSize - borderOffset), new Vector3(innerSize, wallHeight, 1));

        // Negative Z wall
        CreateBorderWall(new Vector3(worldCenter.x, wallHeight / 2, borderOffset), new Vector3(innerSize, wallHeight, 1));

        // Positive X wall
        CreateBorderWall(new Vector3(terrainSize - borderOffset, wallHeight / 2, worldCenter.z), new Vector3(1, wallHeight, innerSize));

        // Negative X wall
        CreateBorderWall(new Vector3(borderOffset, wallHeight / 2, worldCenter.z), new Vector3(1, wallHeight, innerSize));
    }

    void CreateBorderWall(Vector3 position, Vector3 scale)
    {
        if (borderWallPrefab == null) return;

        GameObject wall = Instantiate(borderWallPrefab, position, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.name = "WorldBorder";
    }

    public bool IsWithinBorders(Vector3 position)
    {
        float terrainSize = gridSize * chunkSize * voxelSize;
        float borderOffset = chunkSize * voxelSize * chunksFromBorder;
        float innerSize = terrainSize - 2 * borderOffset;

        float halfInner = innerSize / 2f;

        return (Mathf.Abs(position.x - worldCenter.x) <= halfInner &&
                Mathf.Abs(position.z - worldCenter.z) <= halfInner);
    }

    public bool IsOccupied(Vector3Int pos) =>
        voxelStates.TryGetValue(pos, out VoxelState state) && state.HasFlag(VoxelState.Occupied);

    public bool IsWalkable(Vector3Int pos)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState state)) return true;
        return state.HasFlag(VoxelState.Walkable);
    }

    public bool IsBuildable(Vector3Int pos)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState state)) return true;
        return state.HasFlag(VoxelState.Buildable);
    }

    public void SetVoxelState(Vector3Int pos, VoxelState flag, bool enable)
    {
        if (!voxelStates.TryGetValue(pos, out VoxelState current))
            current = VoxelState.None;

        if (enable)
        {
            voxelStates[pos] = current | flag;
        }
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

        Vector3Int min = Utility.WorldToVoxelCoord(bounds.min);
        Vector3Int max = Utility.WorldToVoxelCoord(bounds.max);

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
            if (normalizedNoise < t) return biomes[i];
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
            normalizedHeight = settings.heightCurve.Evaluate(normalizedHeight);
        else
            normalizedHeight = Mathf.Pow(normalizedHeight, settings.heightExponent);

        return normalizedHeight * settings.heightScale;
    }

    float GenerateBiomeNoise(float x, float z)
    {
        float scale = biomeNoiseSettings.baseScale;
        return Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale) * 2f - 1f;
    }

    public Vector3 GetDefaultSpawnPosition()
    {
        Vector3 defaultSpawn = GameManager.Instance.campFireInstance.transform.position + new Vector3(2, 0, 2);
        float height = Utility.GetHeightAt((int)defaultSpawn.x, (int)defaultSpawn.z);
        defaultSpawn.y = height;

        return defaultSpawn;
    }
}