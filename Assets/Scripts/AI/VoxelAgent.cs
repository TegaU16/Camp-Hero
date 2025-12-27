using System.Collections.Generic;
using Game.Terrain;
using UnityEngine;

namespace Game.AI
{
    [RequireComponent(typeof(Animator))]
    public class VoxelAgent : MonoBehaviour
    {
        private List<Vector3Int> path;
        private int pathIndex;

        public Vector3Int DebugDirectTarget => directTarget;
        public bool DebugIsWalkingDirect => isWalkingDirect;

        public Vector3 DesiredPosition { get; private set; }
        public bool HasPath => path != null && pathIndex < path.Count;

        // Movement
        private Vector3 velocity;
        [Header("Movement Settings")]
        private float stoppingDistance = 0.1f;
        [SerializeField] private float rotationSpeed = 720f; // degrees per second

        [Header("Animator Settings")]
        private Animator animator;
        [SerializeField] private string speedParam = "Speed";

        // Y smoothing
        [SerializeField] private float yLerpSpeed = 5f;
        private float currentY;

        private Vector3Int directTarget;
        private bool isWalkingDirect = false;

        private Transform trackingTarget;
        private readonly float directTrackingCheckInterval = 0.5f;
        private float nextTrackingCheckTime = 0f;

        private void Start()
        {
            animator = GetComponent<Animator>();
            if (TryGetComponent(out CharacterController cc))
                stoppingDistance = cc.radius + 0.25f;
        }

        public void Init(Vector3 startWorldPos)
        {
            path = null;
            pathIndex = 0;
            DesiredPosition = startWorldPos;
            velocity = Vector3.zero;

            currentY = startWorldPos.y;
        }

        public void RequestPath(Vector3Int targetGrid)
        {
            PathfinderManager.Instance.RequestPath(this, targetGrid);
        }

        public void OnPathResult(List<Vector3Int> newPath)
        {
            isWalkingDirect = false;
            trackingTarget = null;
            path = (newPath != null && newPath.Count > 0) ? newPath : null;
            pathIndex = 0;
            UpdateDesired();
        }

        public void CancelPath()
        {
            path = null;
            pathIndex = 0;
            velocity = Vector3.zero;
        }

        public bool CanWalkDirectly(Vector3Int from, Vector3Int to)
        {
            Vector3 dir = (to - from);
            int steps = (int)Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.z));
            Vector3 step = dir / steps;

            Vector3 pos = from;

            for (int i = 0; i <= steps; i++)
            {
                int x = Mathf.RoundToInt(pos.x);
                int z = Mathf.RoundToInt(pos.z);
                float height = Utility.GetHeightAt(x, z);
                Vector3Int checkPos = new(x, Mathf.RoundToInt(height), z);

                if (!VoxelGrid.Instance.IsWalkable(checkPos))
                {
                    Debug.Log($"[VoxelAgent] [{name}] {checkPos} is not walkable");
                    return false;
                }

                if (i > 0)
                {
                    float heightDiff = Mathf.Abs(height - from.y);
                    float heightThreshold = 10f;
                    if (heightDiff > heightThreshold)
                    {
                        Debug.Log($"[VoxelAgent] [{name}] heightDiff is greater than 4. Height: {height}, From.y: {from.y}");
                        return false;
                    }
                }

                pos += step;
            }

            return true;
        }

        public void UpdateAgent()
        {
            Vector3 currentPos = transform.position;

            if (trackingTarget != null && Time.time >= nextTrackingCheckTime)
            {
                Vector3Int from = WorldToGrid(transform.position);
                Vector3Int to = WorldToGrid(trackingTarget.position);

                if (CanWalkDirectly(from, to))
                {
                    SetDirectTarget(to);
                }
                else
                {
                    RequestPath(to); // fallback to A*
                    StopTracking();  // stop direct tracking temporarily
                }

                nextTrackingCheckTime = Time.time + directTrackingCheckInterval;
            }

            if (HasPath)
            {
                Vector3 next = GridToWorld(path[pathIndex]);
                DesiredPosition = Vector3.Lerp(DesiredPosition, next, 0.2f);

                Vector2 flatCur = new(currentPos.x, currentPos.z);
                Vector2 flatNext = new(next.x, next.z);

                if (Vector2.Distance(flatCur, flatNext) < stoppingDistance)
                {
                    pathIndex++;
                    if (pathIndex >= path.Count)
                    {
                        path = null;
                        velocity = Vector3.zero;
                        isWalkingDirect = false;
                    }
                }
            }
            else
            {
                DesiredPosition = transform.position;
                velocity = Vector3.zero;
            }

            Vector3 directWorldTarget = GridToWorld(directTarget);
            Vector3 toTarget;

            toTarget = isWalkingDirect ? directWorldTarget - currentPos : DesiredPosition - currentPos;
            toTarget.y = 0f;

            velocity = toTarget.magnitude > stoppingDistance ? toTarget.normalized : Vector3.zero;

            float targetY = Utility.GetHeightAt(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z)) + 1f;
            currentY = Mathf.Lerp(currentY, targetY, yLerpSpeed * Time.deltaTime);

            Vector3 faceDir = isWalkingDirect ? (directWorldTarget - transform.position) : velocity;
            faceDir.y = 0;

            if (faceDir.magnitude > 0.01f)
            {
                Vector3 smoothedDir = Vector3.Lerp(transform.forward, toTarget.normalized, 0.2f);
                Quaternion targetRot = Quaternion.LookRotation(smoothedDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            if (animator != null)
                animator.SetFloat(speedParam, flatSpeed > 0.05f ? 1f : 0f, 0.2f, Time.deltaTime);
        }

        public bool WantsToMove()
        {
            float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            return flatSpeed > 0.05f || isWalkingDirect;
        }

        public void StopPath()
        {
            CancelPath();
        }

        private void UpdateDesired()
        {
            if (path != null && pathIndex < path.Count)
                DesiredPosition = GridToWorld(path[pathIndex]);
        }

        public float GetCurrentSpeedFraction()
        {
            return velocity.magnitude;
        }

        private Vector3 GridToWorld(Vector3Int gridPos)
        {
            float y = Utility.GetHeightAt(gridPos.x, gridPos.z);
            return new Vector3(gridPos.x, y + 1f, gridPos.z);
        }

        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x),
                0,
                Mathf.RoundToInt(worldPos.z)
            );
        }

        public void SetDirectTarget(Vector3Int target)
        {
            directTarget = target;
            isWalkingDirect = true;
        }

        public void StartTracking(Transform target)
        {
            trackingTarget = target;
            isWalkingDirect = true;
            nextTrackingCheckTime = Time.time;
        }

        public void StopTracking()
        {
            trackingTarget = null;
            isWalkingDirect = false;
        }

        public Vector3 GetMovementThisFrame()
        {
            if (!isWalkingDirect) return velocity;

            Vector3 targetWorld = GridToWorld(directTarget);
            Vector3 direction = targetWorld - transform.position;
            direction.y = 0;

            if (trackingTarget == null && direction.magnitude < 0.1f)
            {
                isWalkingDirect = false;
                return Vector3.zero;
            }

            return direction.normalized;
        }

        public float GetTargetY() => currentY;
    }
}
