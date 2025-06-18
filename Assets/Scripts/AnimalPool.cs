using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Collections;

public class AnimalPool : MonoBehaviour
{
    public static AnimalPool Instance;

    public AnimalSpawner animalSpawner;
    public Animal animalPrefab;
    public int initialPoolSize = 50;

    public Vector3 poolGraveyardPosition = new(0, -1000, 0);

    readonly Queue<Animal> pool = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateInitialPool();
    }

    public void CreateInitialPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAnimal();
        }
    }

    private void CreateAnimal()
    {
        Animal newAnimal = Instantiate(animalPrefab);
        pool.Enqueue(newAnimal);
        newAnimal.gameObject.SetActive(false);
    }

    public Animal GetAnimal(Vector3 spawnPos, VoxelChunk chunk)
    {
        if (pool.Count == 0) CreateAnimal();

        Animal a = pool.Dequeue();

        // Do NOT warp again — you already did it!
        // Just enable agent:
        if (a.TryGetComponent(out NavMeshAgent agent))
        {
            agent.enabled = true;
            agent.Warp(spawnPos);
        }

        a.transform.position = spawnPos;

        a.gameObject.SetActive(true);
        a.Init(spawnPos, chunk);

        return a;
    }

    public void ReturnAnimal(Animal a)
    {
        if (a.TryGetComponent(out NavMeshAgent agent))
        {
            // Fully stop & disable agent first
            agent.ResetPath();
            agent.isStopped = true;
            agent.enabled = false; // 👈 KEY STEP!

            // Now it's safe to move transform
            a.transform.position = poolGraveyardPosition;
        }

        a.gameObject.SetActive(false);
        pool.Enqueue(a);
    }
}
