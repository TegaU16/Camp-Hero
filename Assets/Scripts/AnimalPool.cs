using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimalPool : MonoBehaviour
{
    public static AnimalPool Instance;

    public AnimalSpawner animalSpawner;
    public Animal animalPrefab;
    public int initialPoolSize = 50;

    public Vector3 poolGraveyardPosition = new(0, -1000, 0);

    private Queue<Animal> pool = new();
    private readonly List<Animal> activeAnimals = new();

    void Awake()
    {
        Instance = this;
    }

    private void CreateAnimal()
    {
        Animal newAnimal = Instantiate(animalPrefab);
        pool.Enqueue(newAnimal);
        newAnimal.gameObject.SetActive(false);
    }

    public Animal GetAnimal(Vector3 spawnPos)
    {
        if (pool.Count == 0) 
            CreateAnimal();

        Animal animal = pool.Dequeue();

        animal.gameObject.SetActive(true);
        animal.animator.enabled = false;
        animal.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        animal.Init(spawnPos);
        animal.animator.enabled = true;
        activeAnimals.Add(animal);

        return animal;
    }

    public void ReturnAnimal(Animal animal)
    {
        animal.CancelInvoke();
        animal.StopAllCoroutines();

        if (animal.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.SetFloat("Speed", 0f);
        }

        if (animal.TryGetComponent(out SimpleRagdollController ragdollController))
            ragdollController.DisableRagdoll();

        animal.IsActiveAI = false;

        animal.transform.position = poolGraveyardPosition;
        animal.gameObject.SetActive(false);

        activeAnimals.Remove(animal);
        pool.Enqueue(animal);
    }

    public void ClearPoolForWorld(string worldName)
    {
        // Remove any active or pooled animals that belong to this world
        foreach (Animal animal in activeAnimals.ToArray())
        {
            if (animal == null) continue;
            if (animal.worldName == worldName)
            {
                Destroy(animal.gameObject);
                activeAnimals.Remove(animal);
            }
        }

        foreach (Animal animal in pool.ToArray())
        {
            if (animal == null) continue;
            if (animal.worldName == worldName)
            {
                Destroy(animal.gameObject);
                pool = new Queue<Animal>(pool.Where(a => a != animal));
            }
        }

        Debug.Log($"Animal pool cleared for world: {worldName}");
    }
}
