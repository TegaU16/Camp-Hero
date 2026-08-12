using System.Collections;
using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Registries;
using Game.Saving;
using Game.Storage;
using Game.Terrain.Structures;
using Game.Terrain.Structures.Trials;
using UnityEngine;

namespace Game.Terrain
{
    public class WorldObjectPlacer : MonoBehaviour
    {
        public static WorldObjectPlacer Instance;
        private static readonly WaitForSeconds _waitForSeconds0_2 = new(0.2f);

        [SerializeField] private Mesh grassMesh;
        [SerializeField] private Material grassMaterial;

        [SerializeField] private GameObject campFirePrefab;

        [SerializeField] private string naturalObjectsTag = "Natural";
        [SerializeField] private float minSpacing = 2.0f;

        private Coroutine visibilityCoroutine;

        private Vector3 worldCenter;

        private int seed;
        private int chunkSize;
        private float voxelSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            seed = VoxelGrid.Instance.seed;
            chunkSize = VoxelGrid.Instance.chunkSize;
            voxelSize = VoxelGrid.Instance.voxelSize;

            worldCenter = VoxelGrid.Instance.worldCenter;
        }

        private void LateUpdate()
        {
            if (VoxelGrid.Instance.playerTransform == null) return;

            List<VoxelChunk> chunks = VoxelGrid.Instance.chunks;
            Vector3 playerPos = VoxelGrid.Instance.playerTransform.position;

            for (int i = 0; i < chunks.Count; i++)
            {
                VoxelChunk chunk = chunks[i];

                float dist = (chunk.chunkObject.transform.position - playerPos).sqrMagnitude;
                float viewDist = VoxelGrid.Instance.chunkViewDistance * chunkSize;
                viewDist *= viewDist;

                if (dist > viewDist) continue;
                if (chunk.grassMatrices.Count == 0) continue;

                RenderGrass(chunk);
            }
        }

        #region Spawn Objects

        public void SpawnCampfireAtCenter()
        {
            if (campFirePrefab == null)
            {
                Debug.LogError("Campfire prefab is null!");
                return;
            }

            Vector3Int gridPos = Utility.WorldToVoxelCoord(worldCenter);
            Vector3 snappedPos = Utility.VoxelCoordToWorld(gridPos);

            int snappedX = Mathf.FloorToInt(snappedPos.x);
            int snappedZ = Mathf.FloorToInt(snappedPos.z);

            float height = Utility.GetHeightAt(snappedX, snappedZ);
            Vector3 spawnPos = new(snappedPos.x, height, snappedPos.z);

            if (GameManager.Instance == null)
            {
                Debug.LogError("[Campfire] GameManager.Instance is null!");
                return;
            }

            int x = (int)(spawnPos.x / chunkSize);
            int z = (int)(spawnPos.z / chunkSize);
            Vector2Int chunkKey = new(x, z);

            if (!VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
            {
                Debug.LogError($"[Campfire] No chunk found at key {chunkKey}");
                return;
            }

            SpawnedObjectData data = new(spawnPos, campFirePrefab, campFirePrefab);
            Utility.AddObjectDataToChunk(data, spawnPos, chunk);
        }

        public IEnumerator SpawnNaturalObjects()
        {
            int totalChunks = VoxelGrid.Instance.chunks.Count;
            int chunkIndex = 0;

            OreGenerator.Instance.Initialize();

            foreach (VoxelChunk chunk in VoxelGrid.Instance.chunks)
            {
                float distance = Vector3.Distance(worldCenter, chunk.chunkObject.transform.position);
                bool withinView = distance < (VoxelGrid.Instance.chunkViewDistance * chunkSize);

                if (!chunk.objectsGenerated)
                    GenerateObjectDataForChunk(chunk);

                if (withinView && !chunk.objectsInstantiated)
                {
                    InstantiateChunkObjects(chunk);
                    VoxelGrid.Instance.SetChunkObjectVisibility(chunk, visible: true);
                }

                chunkIndex++;

                VoxelGrid.Instance.OnProgress?.Invoke((float)chunkIndex / totalChunks);

                if (chunkIndex % 2 == 0) yield return null;
            }

            visibilityCoroutine ??= StartCoroutine(UpdateChunkVisibility());
        }

        private IEnumerator UpdateChunkVisibility()
        {
            while (VoxelGrid.Instance.playerTransform == null)
                yield return null;

            while (true)
            {
                Vector3 playerPos = VoxelGrid.Instance.playerTransform.position;

                foreach (VoxelChunk chunk in VoxelGrid.Instance.chunks)
                {
                    float dist = Vector3.Distance(playerPos, chunk.chunkObject.transform.position);
                    bool shouldBeVisible = dist < (VoxelGrid.Instance.chunkViewDistance * chunkSize);

                    if (!shouldBeVisible)
                    {
                        VoxelGrid.Instance.SetChunkObjectVisibility(chunk, visible: false);
                        continue;
                    }

                    InstantiateChunkObjects(chunk);
                    VoxelGrid.Instance.SetChunkObjectVisibility(chunk, visible: true);
                }

                yield return _waitForSeconds0_2;
            }
        }

        #endregion

        #region Object Generation

        private void GenerateObjectDataForChunk(VoxelChunk chunk)
        {
            if (!chunk.structureSpawned)
            {
                StructureManager.Instance.SpawnStructuresInChunk(chunk);
                chunk.structureSpawned = true;
            }

            chunk.savedObjects ??= new List<SpawnedObjectData>();
            chunk.savedObjectPositions ??= new List<Vector3>();

            Vector3 chunkPosition = chunk.chunkObject.transform.position;

            float terrainWidth = chunkSize * VoxelGrid.Instance.gridSize;

            float minDistanceFromCenter = 10f;
            float maxDistanceFromCenter = terrainWidth * VoxelGrid.Instance.outerRadius;

            GenerateOreNodes(chunk);

            for (int cx = 0; cx < chunkSize; cx++)
            {
                for (int cz = 0; cz < chunkSize; cz++)
                {
                    if (!ShouldSpawnObject(cx, cz, seed, chunkPosition)) continue;

                    Vector3 basePosition = chunkPosition + new Vector3(
                        (cx + 0.5f) * voxelSize,
                        0f,
                        (cz + 0.5f) * voxelSize
                    );

                    Vector3 offset = CalculatePositionOffset(cx, cz, seed);
                    Vector3 testPosition = basePosition + offset;
                    Vector3Int spawnPosSnapped = Utility.WorldToVoxelCoord(testPosition);
                    testPosition = spawnPosSnapped + new Vector3(voxelSize / 2f, 0, voxelSize / 2f);

                    if (TerrainGenerator.Instance.IsOccupied(spawnPosSnapped)) continue;

                    int voxelX = Mathf.FloorToInt(testPosition.x / voxelSize);
                    int voxelZ = Mathf.FloorToInt(testPosition.z / voxelSize);

                    float height = Utility.GetHeightAt(voxelX, voxelZ);

                    Vector3 spawnPosition = new(testPosition.x, height, testPosition.z);

                    float dist = Vector2.Distance(
                        new Vector2(spawnPosition.x, spawnPosition.z),
                        new Vector2(worldCenter.x, worldCenter.z)
                    );

                    Vector3Int spawnKey = Utility.WorldToVoxelCoord(spawnPosition);
                    if (chunk.savedObjectPositionsInt.Contains(spawnKey)) continue;
                    if (dist < minDistanceFromCenter || dist > maxDistanceFromCenter) continue;
                    if (!VoxelGrid.Instance.IsWithinBorders(spawnPosition)) continue;

                    bool tooClose = false;
                    foreach (Vector3 existing in chunk.savedObjectPositions)
                    {
                        if (Vector3.Distance(spawnPosition, existing) >= minSpacing) continue;

                        tooClose = true;
                        break;
                    }

                    if (tooClose) continue;

                    GameObject prefab = SelectDeterministicTreePrefab(cx, cz, seed, chunk);
                    if (prefab == null) continue;

                    SpawnedObjectData data = new(spawnPosition, prefab, prefab);
                    Utility.AddObjectDataToChunk(data, spawnPosition, chunk);
                }
            }

            chunk.objectsGenerated = true;
        }

        private void ValidateSavedObjects(VoxelChunk chunk)
        {
            List<SpawnedObjectData> valid = new();

            foreach (SpawnedObjectData data in chunk.savedObjects)
            {
                if (data.prefabID == null)
                {
                    Debug.LogError("[ValidateSavedObjects] prefabID is NULL. Skipping object.");
                    continue;
                }

                GameObject prefab = PrefabRegistry.Instance.GetByKey(data.prefabID);

                if (!prefab)
                {
                    Debug.LogError($"[ValidateSavedObjects] No prefab found for ID: {data.prefabID}");
                    continue;
                }

                bool blocked = Utility.AreaCheck(prefab, data.position, TerrainGenerator.Instance.IsOccupied);

                if (blocked)
                {
                    Debug.LogWarning($"[ValidateSavedObjects] Area blocked for prefab: {prefab.name} at position: {data.position}");
                    continue;
                }

                valid.Add(data);
            }

            chunk.savedObjects = valid;
        }

        private void InstantiateChunkObjects(VoxelChunk chunk)
        {
            if (chunk.objectsInstantiated) return;

            ValidateSavedObjects(chunk);

            foreach (SpawnedObjectData data in chunk.savedObjects)
            {
                GameObject prefab = PrefabRegistry.Instance.GetByKey(data.prefabID);
                if (!prefab) continue;

                GameObject obj = Instantiate(prefab);
                obj.transform.position = data.position;
                obj.transform.parent = chunk.chunkObject.transform;
                data.instance = obj;

                if (obj.CompareTag(naturalObjectsTag))
                {
                    int hash = data.position.GetHashCode() ^ seed;
                    int rotationIndex = Mathf.Abs(hash) % 4;

                    Vector3 baseEuler = prefab.transform.eulerAngles;
                    Quaternion rotation = Quaternion.Euler(
                        baseEuler.x,
                        baseEuler.y + rotationIndex * 90f,
                        baseEuler.z
                    );

                    obj.transform.rotation = rotation;
                }

                if (!string.IsNullOrEmpty(data.savedStateJson))
                {
                    MultiSaveData multiData = JsonUtility.FromJson<MultiSaveData>(data.savedStateJson);
                    ISaveableObject[] saveables = obj.GetComponents<ISaveableObject>();

                    for (int i = 0; i < saveables.Length && i < multiData.states.Count; i++)
                        saveables[i].LoadState(multiData.states[i]);
                }

                if (data.structureRef != null)
                {
                    if (obj.TryGetComponent(out TrialAltar trialAltar))
                    {
                        TrialAltarSpawner.Instance.RegisterAltar(trialAltar);
                        TerrainGenerator.Instance.MarkVoxelArea(radius: 5f, center: obj.transform.position, buildable: false);
                        continue;
                    }

                    StorageUnit storage = obj.GetComponentInChildren<StorageUnit>();

                    if (storage == null)
                    {
                        TerrainGenerator.Instance.MarkVoxelArea(obj, buildable: false);
                        continue;
                    }

                    if (storage.TryGetComponent(out Targetable targetable))
                        targetable.enabled = false;

                    if (!string.IsNullOrEmpty(data.savedStateJson))
                    {
                        storage.LoadState(data.savedStateJson);
                    }
                    else
                    {
                        LootTable lootTable = LootTableRegistry.Instance.GetByKey(data.structureRef.lootTableName);

                        if (lootTable != null)
                            storage.items = lootTable.GetRandomLoot(data.position, storage.maxSlots);
                    }

                    TerrainGenerator.Instance.MarkVoxelArea(obj, buildable: false);
                    continue;
                }

                if (obj.TryGetComponent(out GemAltar _))
                {
                    TerrainGenerator.Instance.MarkVoxelArea(obj, buildable: false);
                    continue;
                }

                if (obj.TryGetComponent(out Campfire _))
                {
                    GameManager.Instance.SetCampfire(obj);
                    TerrainGenerator.Instance.MarkVoxelArea(obj, buildable: false);
                    continue;
                }

                if (obj.TryGetComponent(out BreakableObject breakable))
                    breakable.owningChunk = chunk;

                if (obj.TryGetComponent(out InteractableItem interactable))
                    interactable.owningChunk = chunk;

                TerrainGenerator.Instance.MarkVoxelArea(obj);
            }

            GenerateGrassForChunk(chunk);
            chunk.objectsInstantiated = true;
        }

        private bool ShouldSpawnObject(int x, int z, int seed, Vector3 chunkPosition)
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

        private GameObject SelectDeterministicTreePrefab(int x, int z, int seed, VoxelChunk chunk)
        {
            List<GameObject> allPrefabs = chunk.biome.treePrefabs;
            if (allPrefabs.Count == 0) return null;

            int hash = x * 73856093 ^ z * 19349663 ^ (seed * 83492791);
            int randomIndex = Mathf.Abs(hash) % allPrefabs.Count;

            return allPrefabs[randomIndex];
        }

        #endregion

        #region Grass Generation

        private void GenerateGrassForChunk(VoxelChunk chunk)
        {
            if (chunk.grassGenerated) return;

            Vector3 chunkPosition = chunk.chunkObject.transform.position;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float worldX = chunkPosition.x + (x + 0.5f) * voxelSize;
                    float worldZ = chunkPosition.z + (z + 0.5f) * voxelSize;

                    Vector3 worldPos = new(worldX, 0, worldZ);

                    Vector3Int voxelCoord = Utility.WorldToVoxelCoord(worldPos);

                    if (TerrainGenerator.Instance.IsOccupied(voxelCoord)) continue;
                    if (!ShouldSpawnGrass(worldX, worldZ, seed)) continue;

                    int vx = Mathf.FloorToInt(worldX / voxelSize);
                    int vz = Mathf.FloorToInt(worldZ / voxelSize);

                    float height = Utility.GetHeightAt(vx, vz);

                    Vector3 pos = new(
                        worldX,
                        height * voxelSize,
                        worldZ
                    );

                    int hash = x * 73856093 ^ z * 19349663 ^ seed;
                    float rotation = hash % 360;
                    float scale = 0.8f + ((hash % 100) / 100f) * 0.4f;

                    Matrix4x4 matrix = Matrix4x4.TRS(
                        pos,
                        Quaternion.Euler(0, rotation, 0),
                        Vector3.one * scale
                    );

                    chunk.grassMatrices.Add(matrix);
                }
            }

            chunk.grassGenerated = true;
        }

        private bool ShouldSpawnGrass(float worldX, float worldZ, int seed)
        {
            float region = Mathf.PerlinNoise(
                (worldX + seed) * 0.02f,
                (worldZ + seed) * 0.02f
            );

            float variation = Mathf.PerlinNoise(
                (worldX + seed * 2) * 0.08f,
                (worldZ + seed * 2) * 0.08f
            );

            float random = Mathf.PerlinNoise(
                (worldX + seed * 3) * 0.3f,
                (worldZ + seed * 3) * 0.3f
            );

            // Combine layers
            float final = region * 0.5f + variation * 0.3f + random * 0.2f;

            return final > 0.55f;
        }

        private void RenderGrass(VoxelChunk chunk)
        {
            if (chunk.grassMatrices.Count == 0) return;

            const int batchSize = 1023;

            for (int i = 0; i < chunk.grassMatrices.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, chunk.grassMatrices.Count - i);

                Matrix4x4[] batch = new Matrix4x4[count];

                for (int j = 0; j < count; j++)
                    batch[j] = chunk.grassMatrices[i + j];

                MaterialPropertyBlock mpb = new();

                Vector4[] colors = new Vector4[count];

                for (int j = 0; j < count; j++)
                {
                    Vector3 pos = batch[j].GetColumn(3); // world position

                    float noise = Mathf.PerlinNoise(
                        pos.x * 0.2f,
                        pos.z * 0.2f
                    );

                    float variation = (noise - 0.5f) * 0.8f;

                    Color baseColor = new(0.4f, 0.9f, 0.4f);
                    colors[j] = baseColor * (0.9f + variation);
                }

                mpb.SetVectorArray("_Color", colors);

                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    batch,
                    count,
                    mpb
                );
            }
        }

        #endregion

        #region Ore Generation

        private void GenerateOreNodes(VoxelChunk chunk)
        {
            if (!OreGenerator.Instance.TryGetDepositAt(chunk, out OreDeposit deposit)) return;

            foreach (Vector3 orePosition in deposit.orePositions)
            {
                OreSize size = OreGenerator.Instance.GetOreSize(orePosition, deposit);
                GameObject prefab = OreGenerator.Instance.GetOrePrefab(deposit.oreType, size);
                if (!prefab) continue;

                if (Utility.AreaCheck(prefab, orePosition, TerrainGenerator.Instance.IsOccupied)) continue;

                Vector3Int oreVoxelPosition = Utility.WorldToVoxelCoord(orePosition);
                float height = Utility.GetHeightAt(oreVoxelPosition.x, oreVoxelPosition.z);
                Vector3 spawnPos = new(oreVoxelPosition.x + voxelSize / 2f, height, oreVoxelPosition.z + voxelSize / 2f);

                SpawnedObjectData data = new(spawnPos, prefab, prefab);
                Utility.AddObjectDataToChunk(data, spawnPos, chunk);
            }
        }

        #endregion

        private Vector3 CalculatePositionOffset(int x, int z, int seed)
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
    }
}
