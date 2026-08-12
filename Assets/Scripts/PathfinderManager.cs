using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Terrain;
using UnityEngine;

namespace Game.AI
{
    public class PathfinderManager : MonoBehaviour
    {
        public static PathfinderManager Instance { get; private set; }

        [SerializeField] private int maxRequestsPerFrame = 5;

        private float lastResultTime = 0f;

        private readonly Queue<PathRequest> requestQueue = new();
        private readonly Queue<PathResult> resultQueue = new();
        private readonly Dictionary<VoxelAgent, PathRequest> latestRequests = new();

        private readonly object queueLock = new();

        private int nextRequestId = 1;

        private CancellationTokenSource cancellationTokenSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            StartWorker();
        }

        private void StartWorker()
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    await ProcessQueueAsync(cancellationTokenSource.Token);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[PathfinderManager] Worker fatal crash: " + ex);
                }
            }, cancellationTokenSource.Token);
        }

        public void RequestPath(VoxelAgent agent, Vector3Int targetGrid)
        {
            if (agent == null) return;

            PathRequest request = new()
            {
                Agent = agent,
                AgentName = agent.name,
                Start = WorldToGrid(agent.transform.position),
                Target = targetGrid,
                RequestId = Interlocked.Increment(ref nextRequestId)
            };

            lock (queueLock)
            {
                latestRequests[agent] = request;
                requestQueue.Enqueue(request);
            }
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    PathRequest request = null;

                    lock (queueLock)
                    {
                        if (requestQueue.Count > 0)
                            request = requestQueue.Dequeue();
                    }

                    if (request != null)
                    {
                        lock (queueLock)
                        {
                            if (latestRequests.TryGetValue(request.Agent, out PathRequest latest) &&
                                latest.RequestId != request.RequestId)
                            {
                                Debug.LogWarning($"Dropped outdated request for {request.Agent.name}");
                                continue;
                            }
                        }

                        if (!TerrainGenerator.Instance.IsWalkable(request.Start))
                            request.Start = FindNearestUnblocked(request.Start);

                        if (!TerrainGenerator.Instance.IsWalkable(request.Target))
                            request.Target = FindNearestUnblocked(request.Target);

                        GridAStar astar = BuildLocalPathfinder();
                        if (astar == null) continue;

                        List<Vector3Int> path = null;
                        try
                        {
                            path = astar.FindPath(request.Start, request.Target);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[PathfinderManager] Exception in pathfinding: {ex}");
                            continue;
                        }

                        lock (queueLock)
                        {
                            resultQueue.Enqueue(new PathResult
                            {
                                Agent = request.Agent,
                                RequestId = request.RequestId,
                                Path = path
                            });
                        }
                    }
                    else
                    {
                        await Task.Delay(1, token);
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (ex is TaskCanceledException)
                    Debug.Log("[PathfinderManager] Worker shut down cleanly.");
                else
                    Debug.LogError("[PathfinderManager] Worker crashed: " + ex);
            }
        }

        private void Update()
        {
            int processed = 0;

            lock (queueLock)
            {
                while (resultQueue.Count > 0 && processed < maxRequestsPerFrame)
                {
                    PathResult result = resultQueue.Dequeue();

                    if (latestRequests.TryGetValue(result.Agent, out PathRequest latest) &&
                        latest.RequestId == result.RequestId)
                    {
                        latestRequests.Remove(result.Agent);
                        result.Agent.OnPathResult(result.Path);
                    }

                    processed++;
                    lastResultTime = Time.time;
                }

                if (latestRequests.Count > 0 && Time.time - lastResultTime > 5f)
                    lastResultTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        private GridAStar BuildLocalPathfinder()
        {
            GridAStar astar = new(
                (x, _, z) => Utility.GetHeightAt(x, z),
                pos => !TerrainGenerator.Instance.IsWalkable(pos),
                maxStepHeight: 1
            );

            return astar;
        }

        public Vector3Int FindNearestUnblocked(Vector3Int center, int maxRadius = 5)
        {
            if (TerrainGenerator.Instance.IsWalkable(center)) return center;

            Queue<Vector3Int> queue = new();
            HashSet<Vector3Int> visited = new();

            queue.Enqueue(center);
            visited.Add(center);

            int radius = 0;

            // 4-connected neighbors (N, S, E, W) + diagonals for 8-connected
            Vector3Int[] directions = {
                new(1,0,0), new(-1,0,0),
                new(0,0,1), new(0,0,-1),
                new(1,0,1), new(-1,0,1),
                new(1,0,-1), new(-1,0,-1)
            };

            while (queue.Count > 0 && radius <= maxRadius)
            {
                int count = queue.Count;

                for (int i = 0; i < count; i++)
                {
                    Vector3Int current = queue.Dequeue();

                    foreach (Vector3Int dir in directions)
                    {
                        Vector3Int neighbor = current + dir;
                        if (visited.Contains(neighbor)) continue;

                        visited.Add(neighbor);

                        if (TerrainGenerator.Instance.IsWalkable(neighbor)) return neighbor;

                        queue.Enqueue(neighbor);
                    }
                }

                radius++;
            }

            return center; // fallback if nothing found
        }

        private Vector3Int WorldToGrid(Vector3 pos) => new(Mathf.RoundToInt(pos.x), 0, Mathf.RoundToInt(pos.z));

        private class PathRequest
        {
            public VoxelAgent Agent;
            public string AgentName;
            public Vector3Int Start;
            public Vector3Int Target;
            public int RequestId;
        }

        private class PathResult
        {
            public VoxelAgent Agent;
            public int RequestId;
            public List<Vector3Int> Path;
        }
    }
}
