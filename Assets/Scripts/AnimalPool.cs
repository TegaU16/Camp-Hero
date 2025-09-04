using UnityEngine;
using System.Collections.Generic;

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

    private void CreateAnimal()
    {
        Animal newAnimal = Instantiate(animalPrefab);
        pool.Enqueue(newAnimal);
        newAnimal.gameObject.SetActive(false);
    }

    public Animal GetAnimal(Vector3 spawnPos)
    {
        if (pool.Count == 0) CreateAnimal();

        Animal animal = pool.Dequeue();

        animal.gameObject.SetActive(true);

        // Disable animator before positioning:
        animal.animator.enabled = false;

        // Set position and rotation first:
        animal.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);

        // Call Init, but do NOT enable animator inside Init anymore:
        animal.Init(spawnPos);

        // Reset animator pose BEFORE enabling:
        animal.ResetAnimatorPose();

        // Now enable Animator:
        animal.animator.enabled = true;

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
        {
            ragdollController.DisableRagdoll();
        }

        animal.IsActiveAI = false;

        animal.transform.position = poolGraveyardPosition;
        animal.gameObject.SetActive(false);

        pool.Enqueue(animal);
    }
}
