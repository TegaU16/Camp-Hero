using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [HideInInspector] public List<VoxelChunk> chunks = new();

    public int clusterCount = 3;
    public int animalsPerCluster = 5;
    public float clusterRadius = 5f;
    public LayerMask groundLayer;

    private readonly Dictionary<VoxelChunk, List<Animal>> chunkAnimals = new();

    [Header("Mob Limits")]
    public int globalAnimalCap = 100;

    private int currentAnimalCount = 0;

    public int MaxAnimalsPerChunk => clusterCount * animalsPerCluster;

    public IEnumerator SpawnAnimalsForChunk(VoxelChunk chunk)
    {
        if (!chunkAnimals.TryGetValue(chunk, out List<Animal> animalsList))
        {
            animalsList = new List<Animal>();
            chunkAnimals[chunk] = animalsList;
        }

        if (animalsList.Count >= MaxAnimalsPerChunk)
            yield break;

        Vector3 chunkOrigin = chunk.chunkObject.transform.position;
        float chunkSize = VoxelGrid.Instance.chunkSize;

        for (int i = 0; i < clusterCount; i++)
        {
            if (currentAnimalCount >= globalAnimalCap)
                yield break;

            // Pick a cluster center in this chunk
            Vector3 clusterCenter = new(
                Random.Range(chunkOrigin.x, chunkOrigin.x + chunkSize),
                0,
                Random.Range(chunkOrigin.z, chunkOrigin.z + chunkSize)
            );

            // Apply noise
            float noise = Mathf.PerlinNoise(
                clusterCenter.x * 0.05f,
                clusterCenter.z * 0.05f
            );

            // Only spawn cluster if noise is above threshold
            if (noise < 0.5f) continue;

            // Project to ground
            if (Physics.Raycast(clusterCenter + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                clusterCenter.y = hit.point.y;
            }
            else
            {
                continue;
            }

            // Spawn animals in this cluster
            for (int j = 0; j < animalsPerCluster; j++)
            {
                if (currentAnimalCount >= globalAnimalCap)
                    yield break;

                Vector2 offset = Random.insideUnitCircle * clusterRadius;
                Vector3 candidatePos = clusterCenter + new Vector3(offset.x, 0, offset.y);

                if (Physics.Raycast(candidatePos + Vector3.up * 100f, Vector3.down, out RaycastHit animalHit, 200f, groundLayer))
                {
                    Vector3 candidateSpawn = animalHit.point;

                    Vector3 spawnPos = candidateSpawn;

                    Animal animal = AnimalPool.Instance.GetAnimal(spawnPos);
                    animal.transform.parent = null;

                    animalsList.Add(animal);
                    chunk.simulatedEntities.Add(animal);

                    currentAnimalCount++;
                }

                yield return new WaitForSeconds(0.05f);
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
                if (animal == null) continue;

                if (animal.TryGetComponent(out BreakableObject breakable))
                {
                    dataList.Add(new AnimalSaveData
                    {
                        prefabName = animal.name.Replace("(Clone)", ""), // or some ID
                        position = animal.transform.position,
                        currentHealth = breakable.GetHealth()
                    });
                }
            }
        }

        return dataList;
    }

    public void SaveAllAnimals()
    {
        List<AnimalSaveData> data = GetAllAnimalSaveData();
        SaveSystem.SaveAnimals(GameManager.Instance.currentWorldName, data);
    }

    public void LoadAnimals(List<AnimalSaveData> savedAnimals)
    {
        foreach (AnimalSaveData data in savedAnimals)
        {
            // Use pooling system
            Animal animal = AnimalPool.Instance.GetAnimal(data.position);
            animal.transform.parent = null;

            if (animal.TryGetComponent(out BreakableObject breakable))
            {
                breakable.SetHealth(data.currentHealth);
            }

            Vector3Int voxelPos = VoxelGrid.Instance.WorldToVoxelCoord(data.position);
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

    public void LoadAllAnimals()
    {
        List<AnimalSaveData> savedAnimals = SaveSystem.LoadAnimals(GameManager.Instance.currentWorldName);
        if (savedAnimals != null)
        {
            LoadAnimals(savedAnimals);
        }
        else
        {
            foreach (VoxelChunk chunk in chunks)
            {
                StartCoroutine(SpawnAnimalsForChunk(chunk));
            }
        }
    }
}
