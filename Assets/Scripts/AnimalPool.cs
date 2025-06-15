using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class AnimalPool : MonoBehaviour
{
    public static AnimalPool Instance;

    public Animal animalPrefab;
    public int initialPoolSize = 50;

    readonly Queue<Animal> pool = new();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAnimal();
        }
    }

    void CreateAnimal()
    {
        Animal a = Instantiate(animalPrefab, transform);
        a.gameObject.SetActive(false);
        pool.Enqueue(a);
    }

    public Animal GetAnimal(Vector3 spawnPos, VoxelChunk chunk)
    {
        if (pool.Count == 0) CreateAnimal();

        Animal a = pool.Dequeue();

        var agent = a.GetComponent<NavMeshAgent>();
        agent.Warp(spawnPos);
        agent.ResetPath();

        a.gameObject.SetActive(true);

        a.Init(spawnPos, chunk);

        Debug.Log($"[Pool] Spawned Animal at: {spawnPos}");

        return a;
    }

    public void ReturnAnimal(Animal a)
    {
        a.GetComponent<NavMeshAgent>().ResetPath();
        a.gameObject.SetActive(false);
        pool.Enqueue(a);
    }
}
