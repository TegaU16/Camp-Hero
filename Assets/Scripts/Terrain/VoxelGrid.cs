using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.AI.Animals;
using Game.Registries;
using Game.Saving;
using Game.Storage;
using Game.Terrain.Structures;
using Game.Terrain.Structures.Trials;
using Game.Tutorial;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace Game.Terrain
{
    public class VoxelGrid : MonoBehaviour
    {
        private static readonly WaitForSeconds _waitForSeconds0_2 = new(0.2f);
        public static VoxelGrid Instance;

        [Flags]
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

        [HideInInspector] public bool worldGenerated = false;

        public LayerMask interactableLayer;
        public LayerMask groundLayer;
        [SerializeField] private string naturalObjectsTag = "Natural";
        private Vector3 worldCenter;
        private float terrainWidth;

        public int chunkViewDistance = 10;
        private Transform playerTransform;

        public readonly List<VoxelChunk> chunks = new();
        public Dictionary<Vector2Int, VoxelChunk> chunkMap = new();

        public List<BiomeData> biomes;
        public float biomeNoiseScale;

        private readonly Dictionary<Vector3Int, VoxelState> voxelStates = new();

        public delegate void WorldGenerationProgress(float progress);
        public WorldGenerationProgress OnProgress;

        private Coroutine visibilityCoroutine;
        private static readonly Collider[] hitBuffer = new Collider[64];

        [Header("World Dimensions")]
        public int chunkSize = 16;
        public int gridSize = 10;
        public float voxelSize = 1f;
        public float maxHeight = 5f;
        public float heightOffset = 0.5f;
        [Range(0f, 0.5f)] public float outerRadius;

        [Header("Tutorials")]
        public TutorialData pickStoneTutorial;
        public GameObject stoneTutorialPrefab;

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

        #region World Generation

        public void SetWorld(int worldSeed, string name)
        {
            if (worldGenerated) return;

            seed = worldSeed;
            worldName = name;

            StartCoroutine(GenerateOrLoadPipeline());
        }

        private IEnumerator GenerateOrLoadPipeline()
        {
            if (Utility.WorldFirstGenerated(worldName))
                yield return StartCoroutine(GenerateTerrain());
            else
                yield return StartCoroutine(LoadChunksRoutine(worldName));
        }

        private IEnumerator GenerateTerrain()
        {
            int totalChunks = gridSize * gridSize;
            int extraSteps = 5; // Extra steps is every function after the loop (Not OnProgress?.Invoke())
            int totalSteps = totalChunks + extraSteps;
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

                    GenerateChunkTerrain(chunk, chunkPosition);

                    currentStep++;
                    OnProgress?.Invoke((float)currentStep / totalSteps);

                    yield return null;
                }
            }

            // --- Key structures ---
            yield return StartCoroutine(KeyStructureSpawner.Instance.SpawnKeyStructures(terrainWidth, worldCenter, (progress) =>
            {
                OnProgress?.Invoke((currentStep + progress) / totalSteps);
            }));
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null;

            // --- Altars ---
            GemAltarSpawner.Instance.worldCenter = worldCenter;
            yield return StartCoroutine(GemAltarSpawner.Instance.SpawnAltarsRoutine(terrainWidth, (progress) =>
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

            worldGenerated = true;
            OnProgress?.Invoke(1f);

            StartCoroutine(StartPickStoneTutorial(waitTime: 5f));
        }

        private IEnumerator LoadChunksRoutine(string worldName)
        {
            string chunksDir = Path.Combine(Application.persistentDataPath, "Worlds", worldName, "chunks");
            if (!Directory.Exists(chunksDir))
            {
                UnityEngine.Debug.LogWarning($"No chunk folder found for world '{worldName}'. Generating new world.");
                yield return StartCoroutine(GenerateTerrain());
                yield break;
            }

            string[] files = Directory.GetFiles(chunksDir, "*.json");
            if (files.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"No saved chunks found for world '{worldName}'. Generating new world.");
                yield return StartCoroutine(GenerateTerrain());
                yield break;
            }

            int extraSteps = 2; // Extra steps is every function after the loop (Not OnProgress?.Invoke())
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
                    UnityEngine.Debug.LogWarning($"Failed to load chunk at {chunkPos}. Skipping.");
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

                Vector2Int chunkKey = new((int)data.chunkPosition.x / chunkSize, (int)data.chunkPosition.z / chunkSize);
                chunkMap[chunkKey] = chunk;

                currentStep++;
                OnProgress?.Invoke((float)currentStep / totalSteps);
                yield return null; // let UI update
            }

            CreateWorldBorders();
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);

            yield return StartCoroutine(SpawnNaturalObjects());
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null;

            worldGenerated = true;
            OnProgress?.Invoke(1f);
        }

        #endregion

        #region Tutorial

        private IEnumerator StartPickStoneTutorial(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);

            if (pickStoneTutorial == null) yield break;
            if (!stoneTutorialPrefab.TryGetComponent(out PrefabID prefabID)) yield break;

            GameObject closestStone = FindClosestObject(prefabID.prefabKey, interactableLayer);
            if (closestStone == null) yield break;

            Collider col = closestStone.GetComponent<Collider>();
            float offset = col ? col.bounds.extents.y : 0.5f;

            ObjectHighlightTutorialData stoneHighlightData = new()
            {
                target = closestStone,
                yOffset = offset
            };

            pickStoneTutorial.objectHighlightData = stoneHighlightData;

            TutorialManager.Instance.RegisterTutorial(pickStoneTutorial);
            TutorialManager.Instance.ActivateTutorial(pickStoneTutorial);
        }

        private GameObject FindClosestObject(string prefabKey, LayerMask layer)
        {
            if (prefabKey == null) return null;

            float radius = chunkSize * 2f;
            int hitCount = Physics.OverlapSphereNonAlloc(playerTransform.position, radius, hitBuffer, layer);

            Transform closest = null;
            float closestSqrDist = Mathf.Infinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hitBuffer[i];
                if (!hit.TryGetComponent(out PrefabID hitObjectID)) continue;
                if (hitObjectID.prefabKey != prefabKey) continue;

                float sqrDist = (hit.transform.position - playerTransform.position).sqrMagnitude;
                if (sqrDist >= closestSqrDist) continue;

                closestSqrDist = sqrDist;
                closest = hit.transform;
            }

            return closest ? closest.gameObject : null;
        }

        #endregion

        #region Spawn Objects

        private void SpawnCampfireAtCenter()
        {
            if (campFirePrefab == null)
            {
                UnityEngine.Debug.LogError("Campfire prefab is null!");
                return;
            }

            Vector3Int gridPos = Utility.WorldToVoxelCoord(worldCenter);
            Vector3 snappedPos = Utility.VoxelCoordToWorld(gridPos);

            int snappedX = Mathf.FloorToInt(snappedPos.x);
            int snappedZ = Mathf.FloorToInt(snappedPos.z);

            float height = Utility.GetHeightAt(snappedX, snappedZ);
            Vector3 spawnPos = new(snappedPos.x, height, snappedPos.z);

            GameObject spawnedCampfire = Instantiate(campFirePrefab, spawnPos, Quaternion.identity);

            if (GameManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[Campfire] GameManager.Instance is null!");
                return;
            }

            GameManager.Instance.SetCampfire(spawnedCampfire);

            if (spawnedCampfire.TryGetComponent(out Health health))
                health.SetHealth(health.maxHealth);

            MarkVoxelArea(spawnedCampfire, buildable: true);

            int x = (int)(spawnPos.x / chunkSize);
            int z = (int)(spawnPos.z / chunkSize);
            Vector2Int chunkKey = new(x, z);

            if (!chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
            {
                UnityEngine.Debug.LogError($"[Campfire] No chunk found at key {chunkKey}");
                return;
            }

            spawnedCampfire.transform.parent = chunk.chunkObject.transform;
            chunk.objects.Add(spawnedCampfire);

            SpawnedObjectData data = new(spawnPos, spawnedCampfire, campFirePrefab);
            Utility.AddObjectDataToChunk(data, spawnPos, chunk);
        }

        private IEnumerator SpawnNaturalObjects()
        {
            int totalChunks = chunks.Count;
            int chunkIndex = 0;

            foreach (VoxelChunk chunk in chunks)
            {
                float distance = Vector3.Distance(worldCenter, chunk.chunkObject.transform.position);
                bool withinView = distance < (chunkViewDistance * chunkSize);

                if (!chunk.objectsGenerated)
                    GenerateObjectDataForChunk(chunk);

                if (withinView && !chunk.objectsInstantiated)
                {
                    InstantiateChunkObjects(chunk);
                    SetChunkObjectVisibility(chunk, true);
                }

                // Mark chunk as processed
                chunk.hasNaturalObjects = true;
                chunkIndex++;

                // Progress callback (0–1)
                OnProgress?.Invoke((float)chunkIndex / totalChunks);

                // Small delay to prevent stutter
                if (chunkIndex % 2 == 0) yield return null; // every 2 chunks
			}

            visibilityCoroutine ??= StartCoroutine(UpdateChunkVisibility());
        }

        #endregion

        #region Chunk Save/Load

        public void SaveAllChunks(string worldName)
        {
            foreach (VoxelChunk chunk in chunks)
                SaveSystem.SaveChunk(worldName, chunk);
        }

        private void LoadChunkObjects(VoxelChunk chunk, ChunkSaveData savedData)
        {
            chunk.objects.Clear();
            chunk.savedObjectPositions.Clear();

            if (savedData.spawnedObjects == null || savedData.spawnedObjects.Count == 0)
            {
                chunk.savedObjects = new List<SpawnedObjectData>();
                chunk.objectsGenerated = true;
                chunk.objectsInstantiated = false;
                return;
            }

            chunk.savedObjects = new List<SpawnedObjectData>(savedData.spawnedObjects);

            foreach (SpawnedObjectData objData in chunk.savedObjects)
            {
                chunk.savedObjectPositions.Add(objData.position);
                Vector3Int spawnKey = Utility.WorldToVoxelCoord(objData.position);
                chunk.savedObjectPositionsInt.Add(spawnKey);
            }

            chunk.structureSpawned = true;
            chunk.objectsGenerated = true;
            chunk.objectsInstantiated = false;
            chunk.hasNaturalObjects = true;
        }

        #endregion

        #region Terrain Generation

        public void GenerateChunkTerrain(VoxelChunk chunk, Vector3 chunkPosition)
        {
            // --- Select biome and get its noise settings ---
            BiomeData biome = SelectBiome(chunkPosition);
            if (biome == null)
            {
                UnityEngine.Debug.LogWarning("Biome not found at " + chunkPosition + " - using default settings.");
                // fallback default
                biome = ScriptableObject.CreateInstance<BiomeData>();
                biome.noiseSettings = new NoiseSettings();
            }

            chunk.biome = biome;

            // Map biome.noiseSettings to BurstNoise / NoiseLayer
            NoiseSettings biomeNoiseSettings = biome.noiseSettings;

            // Build a single NoiseLayer for this chunk (B1)
            NoiseLayer[] managedLayer = new NoiseLayer[1];

            BurstNoise burstNoise = BurstNoise.Default(seed);

            burstNoise.SetFrequency(biomeNoiseSettings.frequency);
            burstNoise.SetFractalOctaves(biomeNoiseSettings.octaves);
            burstNoise.SetLacunarity(biomeNoiseSettings.lacunarity);
            burstNoise.SetFractalGain(biomeNoiseSettings.persistence);

            NoiseLayer nl = new()
            {
                noise = burstNoise,
                offset = new float3(0, 0, 0),
                scale = biomeNoiseSettings.baseScale,
                weight = 1f
            };

            managedLayer[0] = nl;

            // Bake into NativeArray<NoiseLayer> (TempJob lifetime)
            NativeArray<NoiseLayer> bakedLayers = new(1, Allocator.TempJob);
            bakedLayers[0] = managedLayer[0];

            // --- Allocate native arrays for noise & height ---
            int total = chunkSize * chunkSize;
            NativeArray<float> noiseNative = new(total, Allocator.TempJob);

            // Prepare height curve table if needed
            NativeArray<float> curveTable = new(256, Allocator.TempJob);
            if (biomeNoiseSettings.useHeightCurve && biomeNoiseSettings.heightCurve != null)
            {
                // Bake animation curve into 256 samples
                for (int i = 0; i < 256; i++)
                {
                    float t = i / 255f;
                    curveTable[i] = biomeNoiseSettings.heightCurve.Evaluate(t);
                }
            }
            else
            {
                // keep as zeros (will not be used)
                for (int i = 0; i < 256; i++)
                    curveTable[i] = 0f;
            }

            // --- Schedule noise job ---
            NoiseMapJob2D noiseJob = new()
            {
                size = new int2(chunkSize, chunkSize),
                worldOffset = new float2(chunkPosition.x, chunkPosition.z),
                layers = bakedLayers,
                noiseOut = noiseNative
            };

            JobHandle noiseHandle = noiseJob.Schedule(total, 64);

            // New voxel count array
            NativeArray<float> heights = new(total, Allocator.TempJob);
            int maxStackHeight = Mathf.CeilToInt(biomeNoiseSettings.heightScale / voxelSize);

            // --- Schedule height generation job ---
            GenerateHeightMapJob heightJob = new()
            {
                noiseIn = noiseNative,
                useHeightCurve = biomeNoiseSettings.useHeightCurve,
                heightCurve = curveTable,
                heightExponent = biomeNoiseSettings.heightExponent,
                heightMultiplier = biomeNoiseSettings.heightScale,
                heightOffset = this.heightOffset,

                heightOut = heights
            };

            JobHandle heightHandle = heightJob.Schedule(total, 64, noiseHandle);

            // Mesh arrays (same as before)
            int maxFaces = total * maxStackHeight * 5; // conservative estimate
            NativeArray<float3> vertsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> trisNative = new(maxFaces * 6, Allocator.TempJob);
            NativeArray<float2> uvsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<uint> colsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<float3> normsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> outVertCount = new(1, Allocator.TempJob);
            NativeArray<int> outTriCount = new(1, Allocator.TempJob);
            NativeArray<float> outHeightMap = new(chunkSize * chunkSize, Allocator.TempJob);

            MeshBuildJob meshJob = new()
            {
                chunkSizeX = chunkSize,
                chunkSizeZ = chunkSize,
                voxelSize = voxelSize,
                voxelHeights = heights, // use the new integer array

                outVertices = vertsNative,
                outTriangles = trisNative,
                outUVs = uvsNative,
                outColors = colsNative,
                outNormals = normsNative,
                outVertCount = outVertCount,
                outTriCount = outTriCount,
                outHeightMap = outHeightMap
            };

            JobHandle meshHandle = meshJob.Schedule(heightHandle);
            meshHandle.Complete();

            chunk.heightMap = new float[chunkSize, chunkSize];

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int i = x * chunkSize + z;
                    chunk.heightMap[x, z] = outHeightMap[i];
                }
            }

            // --- Read back and create managed mesh arrays ---
            int vertCount = outVertCount[0];
            int triCount = outTriCount[0];

            vertCount = math.min(vertCount, vertsNative.Length);
            triCount = math.min(triCount, trisNative.Length);

            Vector3[] meshVerts = new Vector3[vertCount];
            Vector3[] meshNormals = new Vector3[vertCount];
            Vector2[] meshUVs = new Vector2[vertCount];
            Color32[] meshColors = new Color32[vertCount];

            for (int i = 0; i < vertCount; i++)
            {
                float3 v = vertsNative[i];
                meshVerts[i] = new Vector3(v.x, v.y, v.z);

                float3 n = normsNative[i];
                meshNormals[i] = new Vector3(n.x, n.y, n.z);

                float2 uv = uvsNative[i];
                meshUVs[i] = new Vector2(uv.x, uv.y);

                uint c = colsNative[i];
                byte r = (byte)(c & 0xFF);
                byte g = (byte)((c >> 8) & 0xFF);
                byte b = (byte)((c >> 16) & 0xFF);
                byte a = (byte)((c >> 24) & 0xFF);
                meshColors[i] = new Color32(r, g, b, a);
            }

            int[] meshTris = new int[triCount];
            for (int i = 0; i < triCount; i++)
                meshTris[i] = trisNative[i];

            Mesh mesh = new()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = meshVerts,
                triangles = meshTris,
                uv = meshUVs,
                colors32 = meshColors,
                normals = meshNormals
            };
            mesh.RecalculateBounds();

            // Attach to chunk GameObject (ensure components exist)
            if (!chunk.chunkObject.TryGetComponent(out MeshFilter mf))
                mf = chunk.chunkObject.AddComponent<MeshFilter>();

            if (!chunk.chunkObject.TryGetComponent(out MeshRenderer mr))
                mr = chunk.chunkObject.AddComponent<MeshRenderer>();

            if (!chunk.chunkObject.TryGetComponent(out MeshCollider mc))
                mc = chunk.chunkObject.AddComponent<MeshCollider>();

            mf.mesh = mesh;
            if (voxelMaterial != null)
                mr.material = voxelMaterial;

            mc.sharedMesh = null;
            mc.sharedMesh = mesh;

            SurfaceType chunkSurface = chunk.chunkObject.AddComponent<SurfaceType>();
            chunkSurface.surfaceType = SurfaceType.Type.Grass;

            chunk.chunkObject.layer = LayerMask.NameToLayer("Ground");
            chunk.chunkObject.SetActive(true);
            chunk.generatedMesh = mesh;

            // --- Dispose native arrays ---
            noiseNative.Dispose();
            bakedLayers.Dispose();
            curveTable.Dispose();
            heights.Dispose();

            vertsNative.Dispose();
            trisNative.Dispose();
            uvsNative.Dispose();
            colsNative.Dispose();
            normsNative.Dispose();
            outVertCount.Dispose();
            outTriCount.Dispose();
            outHeightMap.Dispose();
        }

        #endregion

        #region Terrain Preview

        public void GeneratePreviewChunk(
            GameObject targetObject,
            BiomeData biome,
            Vector3 chunkPosition,
            int customChunkSize,
            int customSeed)
        {
            seed = customSeed;

            VoxelChunk previewChunk = new(targetObject, customChunkSize);

            GenerateChunkTerrainPreview(previewChunk, chunkPosition, biome, customChunkSize);
        }

        public void GenerateChunkTerrainPreview(VoxelChunk chunk, Vector3 chunkPosition, BiomeData biome, int customChunkSize)
        {
            chunk.biome = biome;

            // Map biome.noiseSettings to BurstNoise / NoiseLayer
            NoiseSettings biomeNoiseSettings = biome.noiseSettings;

            // Build a single NoiseLayer for this chunk (B1)
            NoiseLayer[] managedLayer = new NoiseLayer[1];

            BurstNoise burstNoise = BurstNoise.Default(seed);

            burstNoise.SetFrequency(biomeNoiseSettings.frequency);
            burstNoise.SetFractalOctaves(biomeNoiseSettings.octaves);
            burstNoise.SetLacunarity(biomeNoiseSettings.lacunarity);
            burstNoise.SetFractalGain(biomeNoiseSettings.persistence);

            NoiseLayer nl = new()
            {
                noise = burstNoise,
                offset = new float3(0, 0, 0),
                scale = biomeNoiseSettings.baseScale,
                weight = 1f
            };

            managedLayer[0] = nl;

            // Bake into NativeArray<NoiseLayer> (TempJob lifetime)
            NativeArray<NoiseLayer> bakedLayers = new(1, Allocator.TempJob);
            bakedLayers[0] = managedLayer[0];

            // --- Allocate native arrays for noise & height ---
            int total = customChunkSize * customChunkSize;
            NativeArray<float> noiseNative = new(total, Allocator.TempJob);

            // Prepare height curve table if needed
            NativeArray<float> curveTable = new(256, Allocator.TempJob);
            if (biomeNoiseSettings.useHeightCurve && biomeNoiseSettings.heightCurve != null)
            {
                // Bake animation curve into 256 samples
                for (int i = 0; i < 256; i++)
                {
                    float t = i / 255f;
                    curveTable[i] = biomeNoiseSettings.heightCurve.Evaluate(t);
                }
            }
            else
            {
                // keep as zeros (will not be used)
                for (int i = 0; i < 256; i++)
                    curveTable[i] = 0f;
            }

            // --- Schedule noise job ---
            NoiseMapJob2D noiseJob = new()
            {
                size = new int2(customChunkSize, customChunkSize),
                worldOffset = new float2(chunkPosition.x, chunkPosition.z),
                layers = bakedLayers,
                noiseOut = noiseNative
            };

            JobHandle noiseHandle = noiseJob.Schedule(total, 64);

            // New voxel count array
            NativeArray<float> heights = new(total, Allocator.TempJob);
            int maxStackHeight = Mathf.CeilToInt(biomeNoiseSettings.heightScale / voxelSize);

            // --- Schedule height generation job ---
            GenerateHeightMapJob heightJob = new()
            {
                noiseIn = noiseNative,
                useHeightCurve = biomeNoiseSettings.useHeightCurve,
                heightCurve = curveTable,
                heightExponent = biomeNoiseSettings.heightExponent,
                heightMultiplier = biomeNoiseSettings.heightScale,
                heightOffset = this.heightOffset,

                heightOut = heights
            };

            JobHandle heightHandle = heightJob.Schedule(total, 64, noiseHandle);

            // Mesh arrays (same as before)
            int maxFaces = total * maxStackHeight * 5; // conservative estimate
            NativeArray<float3> vertsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> trisNative = new(maxFaces * 6, Allocator.TempJob);
            NativeArray<float2> uvsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<uint> colsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<float3> normsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> outVertCount = new(1, Allocator.TempJob);
            NativeArray<int> outTriCount = new(1, Allocator.TempJob);
            NativeArray<float> outHeightMap = new(customChunkSize * customChunkSize, Allocator.TempJob);

            MeshBuildJob meshJob = new()
            {
                chunkSizeX = customChunkSize,
                chunkSizeZ = customChunkSize,
                voxelSize = voxelSize,
                voxelHeights = heights, // use the new integer array

                outVertices = vertsNative,
                outTriangles = trisNative,
                outUVs = uvsNative,
                outColors = colsNative,
                outNormals = normsNative,
                outVertCount = outVertCount,
                outTriCount = outTriCount,
                outHeightMap = outHeightMap
            };

            JobHandle meshHandle = meshJob.Schedule(heightHandle);
            meshHandle.Complete();

            chunk.heightMap = new float[customChunkSize, customChunkSize];

            for (int z = 0; z < customChunkSize; z++)
            {
                for (int x = 0; x < customChunkSize; x++)
                {
                    int i = x * customChunkSize + z;
                    chunk.heightMap[x, z] = outHeightMap[i];
                }
            }

            // --- Read back and create managed mesh arrays ---
            int vertCount = outVertCount[0];
            int triCount = outTriCount[0];

            vertCount = math.min(vertCount, vertsNative.Length);
            triCount = math.min(triCount, trisNative.Length);

            Vector3[] meshVerts = new Vector3[vertCount];
            Vector3[] meshNormals = new Vector3[vertCount];
            Vector2[] meshUVs = new Vector2[vertCount];
            Color32[] meshColors = new Color32[vertCount];

            for (int i = 0; i < vertCount; i++)
            {
                float3 v = vertsNative[i];
                meshVerts[i] = new Vector3(v.x, v.y, v.z);

                float3 n = normsNative[i];
                meshNormals[i] = new Vector3(n.x, n.y, n.z);

                float2 uv = uvsNative[i];
                meshUVs[i] = new Vector2(uv.x, uv.y);

                uint c = colsNative[i];
                byte r = (byte)(c & 0xFF);
                byte g = (byte)((c >> 8) & 0xFF);
                byte b = (byte)((c >> 16) & 0xFF);
                byte a = (byte)((c >> 24) & 0xFF);
                meshColors[i] = new Color32(r, g, b, a);
            }

            int[] meshTris = new int[triCount];
            for (int i = 0; i < triCount; i++)
                meshTris[i] = trisNative[i];

            Mesh mesh = new()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = meshVerts,
                triangles = meshTris,
                uv = meshUVs,
                colors32 = meshColors,
                normals = meshNormals
            };
            mesh.RecalculateBounds();

            if (!chunk.chunkObject.TryGetComponent(out MeshFilter mf))
                mf = chunk.chunkObject.AddComponent<MeshFilter>();

            if (mf.sharedMesh != null)
                DestroyImmediate(mf.sharedMesh);

            mf.sharedMesh = mesh;

            if (!chunk.chunkObject.TryGetComponent(out MeshRenderer mr))
                mr = chunk.chunkObject.AddComponent<MeshRenderer>();

            mr.sharedMaterial = voxelMaterial;

            chunk.chunkObject.SetActive(true);
            chunk.generatedMesh = mesh;

            // --- Dispose native arrays ---
            noiseNative.Dispose();
            bakedLayers.Dispose();
            curveTable.Dispose();
            heights.Dispose();

            vertsNative.Dispose();
            trisNative.Dispose();
            uvsNative.Dispose();
            colsNative.Dispose();
            normsNative.Dispose();
            outVertCount.Dispose();
            outTriCount.Dispose();
            outHeightMap.Dispose();
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

            float minDistanceFromCenter = 10f;
            float maxDistanceFromCenter = terrainWidth * outerRadius;

            // Stopwatch sw = Stopwatch.StartNew();

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

                    if (IsOccupied(spawnPosSnapped)) continue;

                    int testX = Mathf.FloorToInt(testPosition.x);
                    int testZ = Mathf.FloorToInt(testPosition.z);

                    float height = Utility.GetHeightAt(testX, testZ);
                    Vector3 spawnPosition = new(testPosition.x, height, testPosition.z);

                    float dist = Vector2.Distance(
                        new Vector2(spawnPosition.x, spawnPosition.z),
                        new Vector2(worldCenter.x, worldCenter.z)
                    );

                    Vector3Int spawnKey = Utility.WorldToVoxelCoord(spawnPosition);
                    if (chunk.savedObjectPositionsInt.Contains(spawnKey)) continue;
                    if (dist < minDistanceFromCenter || dist > maxDistanceFromCenter) continue;
                    if (!IsWithinBorders(spawnPosition)) continue;

                    bool tooClose = false;
                    foreach (Vector3 existing in chunk.savedObjectPositions)
                    {
                        if (Vector3.Distance(spawnPosition, existing) >= minSpacing) continue;

                        tooClose = true;
                        break;
                    }

                    if (tooClose) continue;

                    GameObject prefab = SelectDeterministicObjectPrefab(cx, cz, seed, chunk);
                    if (prefab == null) continue;

					SpawnedObjectData data = new(spawnPosition, prefab, prefab);
					Utility.AddObjectDataToChunk(data, spawnPosition, chunk);
				}
            }

            /*sw.Stop();
            UnityEngine.Debug.Log($"Chunk {chunk.chunkObject.name} object data took {sw.ElapsedMilliseconds} ms");*/

            chunk.objectsGenerated = true;
        }

        private void ValidateSavedObjects(VoxelChunk chunk)
        {
            Stopwatch total = Stopwatch.StartNew();

            long prefabLookupTime = 0;
            long areaCheckTime = 0;

            List<SpawnedObjectData> valid = new();

            foreach (SpawnedObjectData data in chunk.savedObjects)
            {
                Stopwatch sw = Stopwatch.StartNew();
                GameObject prefab = PrefabRegistry.GetPrefabByKey(data.prefabID);
                sw.Stop();
                prefabLookupTime += sw.ElapsedTicks;

                if (!prefab) continue;

                sw.Restart();
                bool blocked = Utility.AreaCheck(prefab, data.position, IsOccupied);
                sw.Stop();
                areaCheckTime += sw.ElapsedTicks;

                if (blocked) continue;

                valid.Add(data);
            }

            chunk.savedObjects = valid;

            total.Stop();

            UnityEngine.Debug.Log(
                $"ValidateSavedObjects | Total: {total.ElapsedMilliseconds} ms | " +
                $"PrefabLookup: {prefabLookupTime / (double)Stopwatch.Frequency * 1000:F2} ms | " +
                $"AreaCheck: {areaCheckTime / (double)Stopwatch.Frequency * 1000:F2} ms"
            );
        }

        private void InstantiateChunkObjects(VoxelChunk chunk)
        {
            if (chunk.objectsInstantiated) return;

            ValidateSavedObjects(chunk);

            foreach (SpawnedObjectData data in chunk.savedObjects)
            {
                GameObject prefab = PrefabRegistry.GetPrefabByKey(data.prefabID);
                if (!prefab) continue;

                GameObject obj = Instantiate(prefab, data.position, Quaternion.identity);
                obj.transform.parent = chunk.chunkObject.transform;
                data.instance = obj;

                if (obj.CompareTag(naturalObjectsTag))
                {
                    int hash = data.position.GetHashCode() ^ seed;
                    int rotationIndex = Mathf.Abs(hash) % 4;
                    Quaternion rotation = Quaternion.Euler(0, rotationIndex * 90f, 0);
                    obj.transform.rotation = rotation;
                }

                if (!string.IsNullOrEmpty(data.savedStateJson))
                {
                    MultiSaveData multiData = JsonUtility.FromJson<MultiSaveData>(data.savedStateJson);
                    ISaveableObject[] saveables = obj.GetComponents<ISaveableObject>();

                    for (int i = 0; i < saveables.Length && i < multiData.states.Count; i++)
                        saveables[i].LoadState(multiData.states[i]);
                }
               
                chunk.objects.Add(obj);

                if (data.structureRef != null)
                {
                    StorageUnit storage = obj.GetComponentInChildren<StorageUnit>();

                    if (storage != null)
                    {
                        if (!string.IsNullOrEmpty(data.savedStateJson))
                            storage.LoadState(data.savedStateJson);
                        else if (storage.TryGetComponent(out LootTableReference lootRef) && lootRef.lootTable != null)
                            storage.items = lootRef.lootTable.GetRandomLoot().ToArray();
                    }

                    MarkVoxelArea(obj, buildable: false);

                    continue;
                }

                if (obj.TryGetComponent(out GemAltar _))
                {
                    MarkVoxelArea(obj, buildable: false);
                    continue;
                }

                if (obj.GetComponentInChildren<TrialAltar>() != null)
                {
                    MarkVoxelArea(obj, walkable: true, buildable: false);
                    continue;
                }

                if (obj.TryGetComponent(out Campfire _))
                {
                    GameManager.Instance.SetCampfire(obj);
                    MarkVoxelArea(obj, buildable: false);
                    continue;
                }

                if (obj.TryGetComponent(out BreakableObject breakable))
                    breakable.owningChunk = chunk;

                if (obj.TryGetComponent(out InteractableItem interactable))
                    interactable.owningChunk = chunk;

                MarkVoxelArea(obj);
            }

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

        private GameObject SelectDeterministicObjectPrefab(int x, int z, int seed, VoxelChunk chunk)
        {
            List<GameObject> allPrefabs = new();
            allPrefabs.AddRange(chunk.biome.treePrefabs);
            allPrefabs.AddRange(chunk.biome.rockPrefabs);

            if (allPrefabs.Count == 0) return null;

            int hash = x * 73856093 ^ z * 19349663 ^ (seed * 83492791);
            int randomIndex = Mathf.Abs(hash) % allPrefabs.Count;

            return allPrefabs[randomIndex];
        }

        #endregion

        #region Chunk/Object Handling

        private void SetChunkObjectVisibility(VoxelChunk chunk, bool visible)
        {
            if (!chunk.objectsInstantiated) return;

            foreach (SpawnedObjectData data in chunk.savedObjects)
            {
                if (data.instance == null) continue;

                foreach (Renderer renderer in data.instance.GetComponentsInChildren<Renderer>())
                    renderer.enabled = visible;

                foreach (Collider collider in data.instance.GetComponentsInChildren<Collider>())
                    collider.enabled = visible;
            }
        }

        private IEnumerator UpdateChunkVisibility()
        {
            while (playerTransform == null)
                yield return null;

            while (true)
            {
                Vector3 playerPos = playerTransform.position;

                foreach (VoxelChunk chunk in chunks)
				{
                    float dist = Vector3.Distance(playerPos, chunk.chunkObject.transform.position);
                    bool shouldBeVisible = dist < (chunkViewDistance * chunkSize);

                    if (!shouldBeVisible)
                    {
                        SetChunkObjectVisibility(chunk, visible: false);
                        continue;
                    }

                    InstantiateChunkObjects(chunk);
                    SetChunkObjectVisibility(chunk, visible: true);
                }

				yield return _waitForSeconds0_2;
            }
        }

        public void RemoveObjectFromChunk(VoxelChunk chunk, GameObject instance)
        {
            if (chunk == null) return;
            if (instance == null) return;
            if (!chunk.objects.Contains(instance)) return;

            // Remove from set
            SpawnedObjectData spawnedObjectData = chunk.savedObjects.FirstOrDefault(x => x.instance == instance);
            chunk.savedObjects.Remove(spawnedObjectData);

            Destroy(instance);
            chunk.objects.Remove(instance);
        }

        #endregion

        #region Borders

        private void CreateWorldBorders()
        {
            float terrainSize = gridSize * chunkSize * voxelSize;
            float borderOffset = chunkSize * voxelSize * chunksFromBorder;

            float innerSize = terrainSize - 2 * borderOffset;
            float wallHeight = 50f;

            // Positive Z wall
            CreateBorderWall(
                new Vector3(worldCenter.x, wallHeight / 2, terrainSize - borderOffset),
                new Vector3(innerSize, wallHeight, 1)
            );

            // Negative Z wall
            CreateBorderWall(
                new Vector3(worldCenter.x, wallHeight / 2, borderOffset),
                new Vector3(innerSize, wallHeight, 1)
            );

            // Positive X wall
            CreateBorderWall(
                new Vector3(terrainSize - borderOffset, wallHeight / 2, worldCenter.z),
                new Vector3(1, wallHeight, innerSize)
            );

            // Negative X wall
            CreateBorderWall(
                new Vector3(borderOffset, wallHeight / 2, worldCenter.z),
                new Vector3(1, wallHeight, innerSize)
            );
        }

        private void CreateBorderWall(Vector3 position, Vector3 scale)
        {
            if (borderWallPrefab == null) return;

            GameObject wall = Instantiate(borderWallPrefab, position, Quaternion.identity, transform);
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

        #endregion

        #region Voxel States

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
                return;
            }

            current &= ~flag;
            if (current == VoxelState.None)
                voxelStates.Remove(pos); // cleanup
            else
                voxelStates[pos] = current;
        }

        public void MarkVoxelArea(GameObject instance, bool occupy = true, bool walkable = false, bool buildable = true)
        {
            Bounds bounds = instance.GetComponentInChildren<Renderer>().bounds;

            Vector3Int min = Utility.WorldToVoxelCoord(bounds.min);
            Vector3Int max = Utility.WorldToVoxelCoord(bounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int voxelPos = new(x, 0, z);

                    SetVoxelState(voxelPos, VoxelState.Occupied, occupy);
                    SetVoxelState(voxelPos, VoxelState.Walkable, walkable);
                    SetVoxelState(voxelPos, VoxelState.Buildable, buildable);
                }
            }
        }

        #endregion

        #region Biome

        private BiomeData SelectBiome(Vector3 chunkPosition)
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

        private float GenerateBiomeNoise(float x, float z)
        {
            float scale = biomeNoiseScale;
            return Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale) * 2f - 1f;
        }

        #endregion

        #region Position Calculations

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

        public Vector3 GetDefaultSpawnPosition()
        {
            Vector3 defaultSpawn = GameManager.Instance.campFireInstance.transform.position + new Vector3(2, 0, 2);
            int spawnX = Mathf.FloorToInt(defaultSpawn.x);
            int spawnZ = Mathf.FloorToInt(defaultSpawn.z);

            float height = Utility.GetHeightAt(spawnX, spawnZ);
            defaultSpawn.y = height;

            return defaultSpawn;
        }

        #endregion

        #region Set Player

        public void SetPlayer(GameObject player)
        {
            UnityEngine.Debug.Log("Set Player called");
            if (player == null) return;
            playerTransform = player.transform;
        }

        #endregion
    }

    #region Terrain Burst

    [BurstCompile]
    public struct NoiseMapJob2D : IJobParallelFor
    {
        public int2 size; // X,Z
        public float2 worldOffset; // world X,Z to add

        [ReadOnly] public NativeArray<NoiseLayer> layers; // length >= 1 for B1
        [WriteOnly] public NativeArray<float> noiseOut; // length = size.x*size.y

        public void Execute(int index)
        {
            int cx = index / size.y; // row (x)
            int cz = index % size.y; // col (z)
            float2 point = new(worldOffset.x + cx, worldOffset.y + cz);

            float sum = 0f;
            float sumWeight = 0f;
            for (int i = 0; i < layers.Length; i++)
            {
                NoiseLayer layer = layers[i];
                float2 sample = (point + layer.offset.xz) * layer.scale;
                float s = layer.noise.Sample(sample);
                sum += s * layer.weight;
                sumWeight += layer.weight;
            }

            float final = (sumWeight > 0f) ? (sum / sumWeight) : 0f;
            noiseOut[index] = final;
        }
    }

    [BurstCompile]
    public struct GenerateHeightMapJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> noiseIn;
        [ReadOnly] public bool useHeightCurve;
        [ReadOnly] public NativeArray<float> heightCurve;
        [ReadOnly] public float heightExponent;
        [ReadOnly] public float heightMultiplier; // world height scale
        [ReadOnly] public float heightOffset;     // voxel height in world units

        [WriteOnly] public NativeArray<float> heightOut;

        public void Execute(int index)
        {
            float n = noiseIn[index];
            n = (n + 1f) * 0.5f;
            n = math.clamp(n, 0f, 1f);

            if (useHeightCurve && heightCurve.Length == 256)
            {
                int ci = (int)(n * 255f);
                n = heightCurve[math.clamp(ci, 0, 255)];
            }
            else
            {
                n = math.pow(n, heightExponent);
            }

            float worldHeight = n * heightMultiplier;
            float voxelH = worldHeight / heightOffset;
            voxelH = math.round(voxelH);
            voxelH *= heightOffset;          // convert back to world height
            heightOut[index] = voxelH;
        }
    }

    [BurstCompile]
    public struct MeshBuildJob : IJob
    {
        [ReadOnly] public int chunkSizeX;
        [ReadOnly] public int chunkSizeZ;
        [ReadOnly] public float voxelSize;
        [ReadOnly] public NativeArray<float> voxelHeights;

        [WriteOnly] public NativeArray<float3> outVertices;
        [WriteOnly] public NativeArray<int> outTriangles;
        [WriteOnly] public NativeArray<float2> outUVs;
        [WriteOnly] public NativeArray<uint> outColors;
        [WriteOnly] public NativeArray<float3> outNormals;
        [WriteOnly] public NativeArray<float> outHeightMap;

        public NativeArray<int> outVertCount;
        public NativeArray<int> outTriCount;

        public void Execute()
        {
            int vertCursor = 0;
            int triCursor = 0;

            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int index = x * chunkSizeZ + z;
                    float topY = voxelHeights[index];
                    if (topY <= 0f) continue;

                    float blockTop = topY;
                    outHeightMap[index] = blockTop;

                    float blockBottom = topY - voxelSize; // always one full block

                    float3 posBase = new(x * voxelSize, 0f, z * voxelSize);

                    // corners
                    float3 b00 = posBase + new float3(0f, blockBottom, 0f);
                    float3 b10 = posBase + new float3(voxelSize, blockBottom, 0f);
                    float3 b01 = posBase + new float3(0f, blockBottom, voxelSize);
                    float3 b11 = posBase + new float3(voxelSize, blockBottom, voxelSize);

                    float3 t00 = posBase + new float3(0f, blockTop, 0f);
                    float3 t10 = posBase + new float3(voxelSize, blockTop, 0f);
                    float3 t01 = posBase + new float3(0f, blockTop, voxelSize);
                    float3 t11 = posBase + new float3(voxelSize, blockTop, voxelSize);

                    // --- Top face
                    AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                        ref vertCursor, ref triCursor,
                        t00, t10, t01, t11,
                        new float3(0, 1, 0));

                    // --- Side faces (emit only if neighbor is LOWER)

                    // +X
                    if (x == chunkSizeX - 1 || voxelHeights[(x + 1) * chunkSizeZ + z] < blockTop)
                        AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                            ref vertCursor, ref triCursor,
                            b10, t10, b11, t11,
                            new float3(1, 0, 0));

                    // -X
                    if (x == 0 || voxelHeights[(x - 1) * chunkSizeZ + z] < blockTop)
                        AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                            ref vertCursor, ref triCursor,
                            b01, t01, b00, t00,
                            new float3(-1, 0, 0));

                    // +Z
                    if (z == chunkSizeZ - 1 || voxelHeights[x * chunkSizeZ + (z + 1)] < blockTop)
                        AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                            ref vertCursor, ref triCursor,
                            b01, b11, t01, t11,
                            new float3(0, 0, 1));

                    // -Z
                    if (z == 0 || voxelHeights[x * chunkSizeZ + (z - 1)] < blockTop)
                        AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                            ref vertCursor, ref triCursor,
                            b00, t00, b10, t10,
                            new float3(0, 0, -1));
                }
            }

            outVertCount[0] = vertCursor;
            outTriCount[0] = triCursor;
        }

        static void AddQuad(
            NativeArray<float3> verts, NativeArray<int> tris,
            NativeArray<float3> norms, NativeArray<float2> uvs,
            NativeArray<uint> cols,
            ref int vert, ref int tri,
            float3 v0, float3 v1, float3 v2, float3 v3,
            float3 normal)
        {
            if (vert + 4 > verts.Length || tri + 6 > tris.Length) return;

            verts[vert + 0] = v0;
            verts[vert + 1] = v1;
            verts[vert + 2] = v2;
            verts[vert + 3] = v3;

            norms[vert + 0] = normal;
            norms[vert + 1] = normal;
            norms[vert + 2] = normal;
            norms[vert + 3] = normal;

            if (normal.x > 0.5f)         // +X face
            {
                uvs[vert + 0] = new float2(0, 0);
                uvs[vert + 1] = new float2(0, 1);
                uvs[vert + 2] = new float2(1, 0);
                uvs[vert + 3] = new float2(1, 1);
            }
            else if (normal.x < -0.5f)   // -X face
            {
                uvs[vert + 0] = new float2(1, 0);
                uvs[vert + 1] = new float2(1, 1);
                uvs[vert + 2] = new float2(0, 0);
                uvs[vert + 3] = new float2(0, 1);
            }
            else if (normal.z > 0.5f)    // +Z face
            {
                uvs[vert + 0] = new float2(1, 0);
                uvs[vert + 1] = new float2(0, 0);
                uvs[vert + 2] = new float2(1, 1);
                uvs[vert + 3] = new float2(0, 1);
            }
            else if (normal.z < -0.5f)   // -Z face
            {
                uvs[vert + 0] = new float2(1, 0);
                uvs[vert + 1] = new float2(1, 1);
                uvs[vert + 2] = new float2(0, 0);
                uvs[vert + 3] = new float2(0, 1);
            }
            else                         // Top/bottom (you can tweak to taste)
            {
                uvs[vert + 0] = new float2(0, 0);
                uvs[vert + 1] = new float2(1, 0);
                uvs[vert + 2] = new float2(0, 1);
                uvs[vert + 3] = new float2(1, 1);
            }

            uint col = 0xFFFFFFFFu;
            cols[vert + 0] = col;
            cols[vert + 1] = col;
            cols[vert + 2] = col;
            cols[vert + 3] = col;

            if (math.abs(normal.y) < 0.1f) // horizontal side
            {
                // Reversed triangle order for correct CCW winding
                tris[tri + 0] = vert + 0;
                tris[tri + 1] = vert + 1;
                tris[tri + 2] = vert + 2;

                tris[tri + 3] = vert + 1;
                tris[tri + 4] = vert + 3;
                tris[tri + 5] = vert + 2;
            }
            else
            {
                // Top face: keep original
                tris[tri + 0] = vert + 0;
                tris[tri + 1] = vert + 2;
                tris[tri + 2] = vert + 1;
                tris[tri + 3] = vert + 1;
                tris[tri + 4] = vert + 2;
                tris[tri + 5] = vert + 3;
            }

            vert += 4;
            tri += 6;
        }
    }

    #endregion
}
