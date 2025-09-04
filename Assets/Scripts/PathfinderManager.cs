using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PathfinderManager : MonoBehaviour
{
    public static PathfinderManager Instance { get; private set; }

    [Header("Voxel grid reference")]
    public VoxelGrid voxelGrid;

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
            Debug.LogWarning("[PathfinderManager] Duplicate instance found, destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        StartWorker();
    }

    private void StartWorker()
    {
        Debug.Log("[PathfinderManager] Starting worker thread.");
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
        if (agent == null)
        {
            Debug.LogError("[PathfinderManager] RequestPath called with null agent.");
            return;
        }

        if (voxelGrid == null)
        {
            Debug.LogError("[PathfinderManager] voxelGrid is null in RequestPath.");
            return;
        }

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
        Debug.Log("[PathfinderManager] Worker thread is alive.");

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
                            continue;
                        }
                    }

                    if (!IsWalkable(request.Target))
                        request.Target = FindNearestUnblocked(request.Target);

                    GridAStar astar = BuildLocalPathfinder();

                    if (astar == null)
                    {
                        Debug.LogError("[PathfinderManager] GridAStar could not be created.");
                        continue;
                    }

                    List<Vector3Int> path = null;
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        path = astar.FindPath(request.Start, request.Target);
                        sw.Stop();

                        if (sw.ElapsedMilliseconds > 100)
                        {
                            Debug.LogWarning($"[PathfinderManager] Path took {sw.ElapsedMilliseconds}ms for {request.AgentName}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[PathfinderManager] Exception in pathfinding: {ex}");
                        continue;
                    }


                    if (path == null || path.Count == 0)
                    {
                        Debug.LogWarning($"[PathfinderManager] No valid path found from {request.Start} to {request.Target} for {request.AgentName}");
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

                if (!latestRequests.ContainsKey(result.Agent))
                {
                    Debug.LogWarning($"[PathfinderManager] No matching latestRequest for agent {result.Agent.name}. Possible mismatch or stale result.");
                }

                if (latestRequests.TryGetValue(result.Agent, out PathRequest latest) &&
                    latest.RequestId != result.RequestId)
                {
                    Debug.LogWarning($"[PathfinderManager] Ignored outdated path result for {result.Agent.name} (RequestId {result.RequestId} ≠ Latest {latest.RequestId})");
                }

                if (latestRequests.TryGetValue(result.Agent, out PathRequest latest2) &&
                    latest2.RequestId == result.RequestId)
                {
                    latestRequests.Remove(result.Agent);
                    result.Agent.OnPathResult(result.Path);
                }

                processed++;
                lastResultTime = Time.time;
            }

            if (latestRequests.Count > 0 && Time.time - lastResultTime > 5f)
            {
                Debug.LogWarning($"[PathfinderManager] {latestRequests.Count} requests pending for over 5 seconds.");
                lastResultTime = Time.time;
            }
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
        if (voxelGrid == null)
        {
            Debug.LogError("[PathfinderManager] voxelGrid is null when building pathfinder!");
            return null; // Prevent thread crash
        }

        GridAStar astar = new(
            (x, _, z) => voxelGrid.GetHeightAt(x, z),
            pos => !voxelGrid.IsWalkable(pos),
            maxStepHeight: 1
        );

        return astar;
    }

    public bool IsWalkable(Vector3Int pos) => voxelGrid.IsWalkable(pos);

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
