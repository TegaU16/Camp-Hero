using System.Collections.Generic;
using UnityEngine;

namespace Game.AI.Animals
{
    public class WanderManager : MonoBehaviour
    {
        public static WanderManager Instance { get; private set; }

        private readonly Queue<Animal> wanderQueue = new();

        [SerializeField] private int maxAnimalsPerFrame = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Enqueue(Animal animal)
        {
            if (animal == null) return;
            wanderQueue.Enqueue(animal);
        }

        private void Update()
        {
            int processed = 0;

            while (wanderQueue.Count > 0 && processed < maxAnimalsPerFrame)
            {
                Animal animal = wanderQueue.Dequeue();

                if (animal != null && animal.IsActiveAI)
                    animal.StartWanderStep();

                processed++;
            }
        }
    }
}