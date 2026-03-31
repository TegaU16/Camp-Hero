using System.Collections.Generic;
using System.Threading.Tasks;
using Game.AI.Animals;
using UnityEngine;

namespace Game.AI
{
    public class WanderManager : MonoBehaviour
    {
        public static WanderManager Instance { get; private set; }

        private readonly Queue<WanderRequest> wanderQueue = new();
        [SerializeField] private int maxRequestsPerFrame = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            Instance = this;
        }

        public void Enqueue(WanderRequest request) => wanderQueue.Enqueue(request);

        private void Update()
        {
            int requestsThisFrame = 0;

            while (wanderQueue.Count > 0 && requestsThisFrame < maxRequestsPerFrame)
            {
                WanderRequest request = wanderQueue.Dequeue();
                request.Execute();
                requestsThisFrame++;
            }
        }
    }

    public class WanderRequest
    {
        private readonly Animal animal;
        private readonly Vector3Int targetGrid;
        private readonly System.Action onComplete;

        public WanderRequest(Animal animal, Vector3Int targetGrid, System.Action onComplete)
        {
            this.animal = animal;
            this.targetGrid = targetGrid;
            this.onComplete = onComplete;
        }

        public async void Execute()
        {
            if (animal == null) return;

            Debug.Log($"[{animal.name}] Executing wander request...");

            List<Vector3Int> result = await Task.Run(() => animal.ComputeWanderPathAsync(targetGrid));

            if (animal == null) return; // <- check again, it might have despawned

            if (result != null && result.Count > 0)
            {
                Debug.Log($"[{animal.name}] Path found: {result.Count} points.");
                animal.ApplyWanderPath(result);
            }
            else
            {
                Debug.LogWarning($"[{animal.name}] No path found, staying still.");
                animal.ApplyWanderPath(null);
            }

            onComplete?.Invoke();
        }
    }
}
