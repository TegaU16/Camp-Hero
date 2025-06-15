using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [HideInInspector] public List<VoxelChunk> chunks = new();
    public VoxelGrid voxelGrid;

    public int clusterCount = 3;
    public int animalsPerCluster = 5;
    public float clusterRadius = 5f;
    public LayerMask groundLayer;

    private float spawnDistance;
    private float despawnDistance;

    private Transform player;
    private readonly Dictionary<VoxelChunk, List<Animal>> chunkAnimals = new();

    [Header("Mob Limits")]
    public int globalAnimalCap = 100;

    private int currentAnimalCount = 0;

    public int MaxAnimalsPerChunk => clusterCount * animalsPerCluster;

    private void Start()
    {
        spawnDistance = voxelGrid.viewDistance * voxelGrid.chunkSize;
        despawnDistance = spawnDistance + 30f;
        StartCoroutine(ManageAnimals());
    }

    IEnumerator ManageAnimals()
    {
        while (true)
        {
            if (player != null)
            {
                Vector3 playerPos = player.position;

                List<VoxelChunk> sortedChunks = new(chunks);
                sortedChunks.Sort((a, b) =>
                {
                    float distA = Vector3.Distance(playerPos, a.chunkObject.transform.position);
                    float distB = Vector3.Distance(playerPos, b.chunkObject.transform.position);
                    return distA.CompareTo(distB);
                });

                foreach (var chunk in sortedChunks)
                {
                    float dist = Vector3.Distance(playerPos, chunk.chunkObject.transform.position);

                    bool hasAnimals = chunkAnimals.ContainsKey(chunk) && chunkAnimals[chunk].Count > 0;

                    if (dist < spawnDistance && !hasAnimals)
                    {
                        yield return StartCoroutine(SpawnAnimalsForChunk(chunk));
                    }
                    else if (dist > despawnDistance && hasAnimals)
                    {
                        DespawnAnimalsForChunk(chunk);
                    }
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator SpawnAnimalsForChunk(VoxelChunk chunk)
    {
        if (!chunkAnimals.TryGetValue(chunk, out var animalsList))
        {
            animalsList = new List<Animal>();
            chunkAnimals[chunk] = animalsList;
        }

        if (animalsList.Count >= MaxAnimalsPerChunk)
            yield break;

        Vector3 chunkOrigin = chunk.chunkObject.transform.position;
        float chunkSize = voxelGrid.chunkSize;

        for (int i = 0; i < clusterCount; i++)
        {
            if (currentAnimalCount >= globalAnimalCap)
                yield break;

            Vector3 clusterCenter = new(
                Random.Range(chunkOrigin.x, chunkOrigin.x + chunkSize),
                0,
                Random.Range(chunkOrigin.z, chunkOrigin.z + chunkSize)
            );


            if (Physics.Raycast(clusterCenter + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                clusterCenter.y = hit.point.y;
            }
            else
            {
                continue; // skip this cluster
            }

            for (int j = 0; j < animalsPerCluster; j++)
            {
                if (currentAnimalCount >= globalAnimalCap)
                    yield break;

                Vector2 offset = Random.insideUnitCircle * clusterRadius;
                Vector3 candidatePos = clusterCenter + new Vector3(offset.x, 0, offset.y);

                if (Physics.Raycast(candidatePos + Vector3.up * 100f, Vector3.down, out RaycastHit animalHit, 200f, groundLayer))
                {
                    Vector3 spawnPos = animalHit.point;

                    Animal animal = AnimalPool.Instance.GetAnimal(spawnPos, chunk);
                    animal.transform.parent = null;

                    animalsList.Add(animal);
                    currentAnimalCount++;
                }

                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    void DespawnAnimalsForChunk(VoxelChunk chunk)
    {
        if (!chunkAnimals.ContainsKey(chunk)) return;

        foreach (Animal animal in chunkAnimals[chunk])
        {
            AnimalPool.Instance.ReturnAnimal(animal);
            currentAnimalCount--;
        }

        chunkAnimals.Remove(chunk);
    }

    public void SetPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        player = playerObj.transform;
    }
}
