using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Game.Saving;
using Game.Terrain.Structures;
using Game.Terrain.Structures.Trials;
using Game.Tutorial;
using UnityEngine;

namespace Game.Terrain
{
    public class VoxelGrid : MonoBehaviour
    {
        public static VoxelGrid Instance;

        [Flags]
        public enum VoxelState
        {
            None = 0,
            Occupied = 1 << 0,
            Walkable = 1 << 1,
            Buildable = 1 << 2,
        }

        public Material voxelMaterial;
        [SerializeField] private Transform voxelGridRoot;

        [HideInInspector] public int seed = 12345;
        private string worldName;

        [SerializeField] private GameObject borderWallPrefab;
        [SerializeField] private int chunksFromBorder;

        [HideInInspector] public bool worldGenerated = false;

        public LayerMask interactableLayer;
        public LayerMask groundLayer;
        
        [HideInInspector] public Vector3 worldCenter;
        private float terrainWidth;

        public int chunkViewDistance = 10;
        [HideInInspector] public Transform playerTransform;

        public readonly List<VoxelChunk> chunks = new();
        public Dictionary<Vector2Int, VoxelChunk> chunkMap = new();

        public delegate void WorldGenerationProgress(float progress);
        public WorldGenerationProgress OnProgress;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            //const long budgetMs = 5;
            int chunksThisFrame = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

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

                    chunk.meshFilter = chunkObject.AddComponent<MeshFilter>();
                    chunk.meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                    chunk.meshCollider = chunkObject.AddComponent<MeshCollider>();
                    chunk.surfaceType = chunkObject.AddComponent<SurfaceType>();

                    chunk.surfaceType.surfaceType = SurfaceType.Type.Grass;

                    Vector2Int chunkKey = new(x, z);
                    chunkMap[chunkKey] = chunk;

                    TerrainGenerator.Instance.GenerateChunkTerrain(chunk, chunkPosition);
                    chunksThisFrame++;

                    currentStep++;
                    OnProgress?.Invoke((float)currentStep / totalSteps);

                    yield return null;

                    /*// Yield only when we've exceeded our time budget
                    if (stopwatch.ElapsedMilliseconds >= budgetMs)
                    {
                        UnityEngine.Debug.Log($"Generated {chunksThisFrame} chunks this frame");

                        chunksThisFrame = 0;
                        stopwatch.Restart();
                        yield return null;
                    }*/
                }
            }

            // --- Gem Altars ---
            GemAltarSpawner.Instance.worldCenter = worldCenter;
            yield return StartCoroutine(GemAltarSpawner.Instance.SpawnGemAltars(terrainWidth, (progress) =>
            {
                OnProgress?.Invoke((currentStep + progress) / totalSteps);
            }));
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null;

            // --- Trial Altars ---
            yield return StartCoroutine(TrialAltarSpawner.Instance.SpawnTrialAltars(terrainWidth, worldCenter, (progress) =>
            {
                OnProgress?.Invoke((currentStep + progress) / totalSteps);
            }));
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);
            yield return null;

            // --- Campfire ---
            WorldObjectPlacer.Instance.SpawnCampfireAtCenter();
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);

            // --- World borders ---
            CreateWorldBorders();
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);

            // --- Natural objects ---
            yield return StartCoroutine(WorldObjectPlacer.Instance.SpawnNaturalObjects());
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
                GameObject chunkObject = new("VoxelChunk " + x + "," + z);
                chunkObject.transform.SetParent(voxelGridRoot, false);
                chunkObject.transform.position = chunkPos;

                VoxelChunk chunk = new(chunkObject, chunkSize);
                chunks.Add(chunk);

                chunk.meshFilter = chunkObject.AddComponent<MeshFilter>();
                chunk.meshRenderer = chunkObject.AddComponent<MeshRenderer>();
                chunk.meshCollider = chunkObject.AddComponent<MeshCollider>();
                chunk.surfaceType = chunkObject.AddComponent<SurfaceType>();

                chunk.surfaceType.surfaceType = SurfaceType.Type.Grass;

                TerrainGenerator.Instance.GenerateChunkTerrain(chunk, chunk.chunkPosition);

                LoadChunkObjects(chunk, data);

                chunks.Add(chunk);

                Vector2Int chunkKey = new((int)data.chunkPosition.x / chunkSize, (int)data.chunkPosition.z / chunkSize);
                chunkMap[chunkKey] = chunk;

                currentStep++;
                OnProgress?.Invoke((float)currentStep / totalSteps);
                yield return null;
            }

            CreateWorldBorders();
            currentStep++;
            OnProgress?.Invoke((float)currentStep / totalSteps);

            yield return StartCoroutine(WorldObjectPlacer.Instance.SpawnNaturalObjects());
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

        #region Chunk Save/Load

        public void SaveAllChunks(string worldName)
        {
            foreach (VoxelChunk chunk in chunks)
                SaveSystem.SaveChunk(worldName, chunk);
        }

        private void LoadChunkObjects(VoxelChunk chunk, ChunkSaveData savedData)
        {
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
        }

        #endregion

        #region Chunk/Object Handling

        public void SetChunkObjectVisibility(VoxelChunk chunk, bool visible)
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

        public void RemoveObjectFromChunk(VoxelChunk chunk, GameObject instance)
        {
            if (chunk == null || instance == null) return;

            // Remove from set
            SpawnedObjectData spawnedObjectData = chunk.savedObjects.FirstOrDefault(x => x.instance == instance);
            chunk.savedObjects.Remove(spawnedObjectData);

            Destroy(instance);
        }

        #endregion

        #region Borders

        private void CreateWorldBorders()
        {
            float terrainSize = gridSize * chunkSize * voxelSize;
            float borderOffset = chunkSize * voxelSize * chunksFromBorder;

            float zFightOffset = 0.001f;

            float innerSize = terrainSize - 2 * borderOffset;
            float wallHeight = 100f;

            List<GameObject> walls = new()
            {
                CreateBorderWall(
                    new Vector3(worldCenter.x, wallHeight / 2,
                    terrainSize - borderOffset + zFightOffset),
                    Quaternion.identity,
                    new Vector3(innerSize, wallHeight, 1)
                ),
                CreateBorderWall(
                    new Vector3(worldCenter.x, wallHeight / 2,
                    borderOffset - zFightOffset),
                    Quaternion.identity,
                    new Vector3(innerSize, wallHeight, 1)
                ),
                CreateBorderWall(
                    new Vector3(terrainSize - borderOffset + zFightOffset,
                    wallHeight / 2,
                    worldCenter.z),
                    Quaternion.Euler(0, 90, 0),
                    new Vector3(innerSize, wallHeight, 1)
                ),
                CreateBorderWall(
                    new Vector3(borderOffset - zFightOffset,
                    wallHeight / 2,
                    worldCenter.z),
                    Quaternion.Euler(0, 90, 0),
                    new Vector3(innerSize, wallHeight, 1)
                )
            };

            CombineWalls(walls);
        }

        private GameObject CreateBorderWall(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject wall = Instantiate(borderWallPrefab, position, rotation, transform);

            wall.transform.localScale = scale;

            if (wall.TryGetComponent(out Renderer renderer))
                renderer.material.mainTextureScale = (Vector2)scale;

            return wall;
        }

        private void CombineWalls(List<GameObject> walls)
        {
            List<CombineInstance> combines = new();

            foreach (GameObject wall in walls)
            {
                MeshFilter filter = wall.GetComponent<MeshFilter>();

                if (filter == null || filter.sharedMesh == null) continue;

                CombineInstance combine = new()
                {
                    mesh = filter.sharedMesh,
                    transform = filter.transform.localToWorldMatrix
                };

                combines.Add(combine);
            }

            Mesh combinedMesh = new();
            combinedMesh.CombineMeshes(combines.ToArray(), true, true);

            GameObject combinedObject = new("WorldBorders");

            combinedObject.transform.SetParent(transform);

            MeshFilter combinedFilter = combinedObject.AddComponent<MeshFilter>();
            MeshRenderer combinedRenderer = combinedObject.AddComponent<MeshRenderer>();

            combinedFilter.sharedMesh = combinedMesh;
            combinedRenderer.sharedMaterial = borderWallPrefab.GetComponent<MeshRenderer>().sharedMaterial;

            foreach (GameObject wall in walls)
            {
                if (wall.TryGetComponent(out Renderer renderer))
                    renderer.enabled = false;
            }
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

        #region Player Spawning

        public Vector3 GetDefaultSpawnPosition()
        {
            Vector3 defaultSpawn = GameManager.Instance.campFireInstance.transform.position + new Vector3(2, 0, 2);
            int spawnX = Mathf.FloorToInt(defaultSpawn.x);
            int spawnZ = Mathf.FloorToInt(defaultSpawn.z);

            float height = Utility.GetHeightAt(spawnX, spawnZ);
            defaultSpawn.y = height;

            return defaultSpawn;
        }

        public void SetPlayer(GameObject player)
        {
            UnityEngine.Debug.Log("Set Player called");
            if (player == null) return;

            playerTransform = player.transform;

            BorderWallShaderController borderWallShaderController = GetComponentInChildren<BorderWallShaderController>();
            if (borderWallShaderController == null) return;

            borderWallShaderController.SetPlayer(playerTransform);
        }

        #endregion
    }
}
