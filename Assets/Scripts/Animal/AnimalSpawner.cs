using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Saving;
using Game.Terrain;
using UnityEngine;
using Worlds;

namespace Game.AI.Animals
{
    public class AnimalSpawner : MonoBehaviour
    {
        private static readonly WaitForSeconds _waitForSeconds0_05 = new(0.05f);
        public static AnimalSpawner Instance;

        [SerializeField] private int clusterCount = 3;
        [SerializeField] private int animalsPerCluster = 5;
        [SerializeField] private float clusterRadius = 5f;

        private readonly Dictionary<VoxelChunk, List<Animal>> chunkAnimals = new();

        [Header("Mob Limits")]
        [SerializeField] private int globalAnimalCap = 100;
        private int currentAnimalCount = 0;

        public int MaxAnimalsPerChunk => clusterCount * animalsPerCluster;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public IEnumerator SpawnAnimalsForChunk(VoxelChunk chunk)
        {
            if (!chunkAnimals.TryGetValue(chunk, out List<Animal> animalsList))
            {
                animalsList = new List<Animal>();
                chunkAnimals[chunk] = animalsList;
            }

            if (animalsList.Count >= MaxAnimalsPerChunk) yield break;

            Vector3 chunkOrigin = chunk.chunkObject.transform.position;
            float chunkSize = VoxelGrid.Instance.chunkSize;

            // Get chunk coordinates (integer, grid-based)
            int chunkX = Mathf.RoundToInt(chunkOrigin.x / chunkSize);
            int chunkZ = Mathf.RoundToInt(chunkOrigin.z / chunkSize);

            // Determine if this chunk *should* have animals
            float noise = Mathf.PerlinNoise(
                (chunkX + VoxelGrid.Instance.seed * 0.001f),
                (chunkZ + VoxelGrid.Instance.seed * 0.001f)
            );

            // Hash-based filter to randomize inclusion
            int hash = (chunkX * 73856093) ^ (chunkZ * 19349663) ^ VoxelGrid.Instance.seed;
            System.Random prng = new(hash);
            float hashValue = (float)prng.NextDouble();

            // Combine both
            if (noise * hashValue < 0.5f) yield break;

            for (int i = 0; i < clusterCount; i++)
            {
                if (currentAnimalCount >= globalAnimalCap) yield break;

                // Deterministic random position within chunk
                float localX = (float)prng.NextDouble() * chunkSize;
                float localZ = (float)prng.NextDouble() * chunkSize;

                Vector3 clusterCenter = new(chunkOrigin.x + localX, 0, chunkOrigin.z + localZ);

                float clusterNoise = Mathf.PerlinNoise(
                    (clusterCenter.x + 10000f) * 0.005f,
                    (clusterCenter.z + 10000f) * 0.005f
                );

                int animalsInCluster = Mathf.RoundToInt(animalsPerCluster * clusterNoise);
                if (animalsInCluster <= 0) 
                    animalsInCluster = 1;

                for (int j = 0; j < animalsInCluster; j++)
                {
                    if (currentAnimalCount >= globalAnimalCap) yield break;

                    // Deterministic offset inside cluster
                    float angle = (float)prng.NextDouble() * Mathf.PI * 2f;
                    float distance = (float)prng.NextDouble() * clusterRadius;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;

                    Vector3 candidatePos = clusterCenter + offset;

                    int spawnPosX = Mathf.FloorToInt(candidatePos.x);
                    int spawnPosZ = Mathf.FloorToInt(candidatePos.z);

                    float height = Utility.GetHeightAt(spawnPosX, spawnPosZ);

                    Vector3 spawnPos = new(spawnPosX, height, spawnPosZ);

                    if (!VoxelGrid.Instance.IsWithinBorders(spawnPos)) continue;

                    Vector3Int spawnPosInt = Utility.WorldToVoxelCoord(spawnPos);
                    if (!TerrainGenerator.Instance.IsWalkable(spawnPosInt)) continue;

                    Animal animal = PrepareAnimal(spawnPos);

                    animalsList.Add(animal);
                    chunk.simulatedEntities.Add(animal);

                    currentAnimalCount++;

                    yield return _waitForSeconds0_05;
                }
            }
        }

        public List<AnimalSaveData> GetAllAnimalSaveData()
        {
            List<AnimalSaveData> dataList = new();

            foreach (KeyValuePair<VoxelChunk, List<Animal>> kvp in chunkAnimals)
            {
                foreach (Animal animal in kvp.Value)
                {
                    if (animal == null || !animal.TryGetComponent(out BreakableObject breakable)) continue;

                    PrefabID prefabID = animal.GetComponent<PrefabID>();
                    string id = prefabID.prefabKey;

                    dataList.Add(new AnimalSaveData
                    {
                        prefabID = id,
                        position = animal.transform.position,
                        currentHealth = breakable.GetHealth()
                    });
                }
            }

            return dataList;
        }

        public void SaveAllAnimals()
        {
            List<AnimalSaveData> data = GetAllAnimalSaveData();
            SaveSystem.SaveAnimals(WorldSession.CurrentWorldName, data);
        }

        public void LoadAnimals(List<AnimalSaveData> savedAnimals)
        {
            foreach (AnimalSaveData data in savedAnimals)
            {
                // Use pooling system
                Animal animal = PrepareAnimal(data.position);

                if (animal.TryGetComponent(out BreakableObject breakable))
                    breakable.SetHealth(data.currentHealth);

                Vector3Int voxelPos = Utility.WorldToVoxelCoord(data.position);
                int chunkX = Mathf.FloorToInt((float)voxelPos.x / VoxelGrid.Instance.chunkSize);
                int chunkZ = Mathf.FloorToInt((float)voxelPos.z / VoxelGrid.Instance.chunkSize);

                Vector2Int chunkKey = new(chunkX, chunkZ);

                // Find chunk it belongs to
                if (VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
                {
                    if (!chunkAnimals.ContainsKey(chunk))
                        chunkAnimals[chunk] = new List<Animal>();

                    chunkAnimals[chunk].Add(animal);
                    chunk.simulatedEntities.Add(animal);
                }

                currentAnimalCount++;
            }
        }

        private Animal PrepareAnimal(Vector3 spawnPos)
        {
            Animal animal = AnimalPool.Instance.Get();

            animal.animator.enabled = false;
            animal.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            animal.Init(spawnPos);
            animal.animator.enabled = true;

            animal.worldName = WorldSession.CurrentWorldName;
            animal.transform.parent = null;

            return animal;
        }

        public void LoadAllAnimals()
        {
            ClearAllAnimals();

            List<AnimalSaveData> savedAnimals = SaveSystem.LoadAnimals(WorldSession.CurrentWorldName);
            if (savedAnimals != null)
            {
                LoadAnimals(savedAnimals);
                return;
            }

            System.Random prng = new(VoxelGrid.Instance.seed);
            List<VoxelChunk> shuffledChunks = VoxelGrid.Instance.chunks.OrderBy(_ => prng.Next()).ToList();

            foreach (VoxelChunk chunk in shuffledChunks)
                StartCoroutine(SpawnAnimalsForChunk(chunk));
        }

        private void ClearAllAnimals()
        {
            string currentWorld = WorldSession.CurrentWorldName;

            foreach (KeyValuePair<VoxelChunk, List<Animal>> kvp in chunkAnimals)
            {
                VoxelChunk chunk = kvp.Key;
                List<Animal> animals = kvp.Value;

                foreach (Animal animal in animals)
                {
                    if (animal == null || animal.worldName != currentWorld) continue;

                    chunk.simulatedEntities.Remove(animal);
                    AnimalPool.Instance.Return(animal);
                    currentAnimalCount--;
                }
            }

            chunkAnimals.Clear();
        }
    }
}
