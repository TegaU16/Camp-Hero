using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PathfinderManager : MonoBehaviour
{
    public static PathfinderManager Instance { get; private set; }

    [SerializeField] private int maxRequestsPerFrame = 5;

    private readonly Queue<PathRequest> requestQueue = new();
    private readonly Queue<PathResult> resultQueue = new();
    private readonly Dictionary<VoxelAgent, PathRequest> latestRequests = new();

    private readonly object queueLock = new();

    private int nextRequestId = 1;

    private CancellationTokenSource cts;

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
        cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                await ProcessQueueAsync(cts.Token);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[PathfinderManager] Worker fatal crash: " + ex);
            }
        }, cts.Token);
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
                        if (latestRequests.TryGetValue(request.Agent, out PathRequest latest) && latest.RequestId != request.RequestId) continue;

                    if (!IsWalkable(request.Target))
                        request.Target = FindNearestUnblocked(request.Target);

                    GridAStar astar = BuildLocalPathfinder();

                    if (astar == null) continue;

                    List<Vector3Int> path = null;
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        path = astar.FindPath(request.Start, request.Target);
                        sw.Stop();
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

    private float lastResultTime = 0f;

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
        cts?.Cancel();
        cts?.Dispose();
    }

    // --- Helpers and structs ---
    public GridAStar BuildLocalPathfinder()
    {
        GridAStar astar = new(
            (x, _, z) => VoxelGrid.Instance.GetHeightAt(x, z),
            pos => !VoxelGrid.Instance.IsWalkable(pos),
            maxStepHeight: 1
        );

        return astar;
    }

    public bool IsWalkable(Vector3Int pos) => VoxelGrid.Instance.IsWalkable(pos);

    public Vector3Int FindNearestUnblocked(Vector3Int center)
    {
        Vector3Int[] offsets = {
            new(1,0,0), new(-1,0,0),
            new(0,0,1), new(0,0,-1),
            new(1,0,1), new(-1,0,1),
            new(1,0,-1), new(-1,0,-1)
        };

        foreach (Vector3Int offset in offsets)
        {
            Vector3Int candidate = center + offset;
            if (IsWalkable(candidate)) return candidate;
        }

        return center;
    }

    private Vector3Int WorldToGrid(Vector3 pos) =>
        new(Mathf.RoundToInt(pos.x), 0, Mathf.RoundToInt(pos.z));

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
