using Game.AI;
using Game.Terrain;
using UnityEngine;

namespace Game.Debuggers
{
    public class VoxelAgentDebugger : MonoBehaviour
    {
        private VoxelAgent agent;

        [Header("Settings")]
        [SerializeField] private bool debugEveryFrame = false;
        [SerializeField] private float debugInterval = 1f; // seconds

        private float nextDebugTime = 0f;

        private void Start()
        {
            if (!TryGetComponent(out agent))
            {
                Debug.LogError("[Debugger] No VoxelAgent found on this GameObject!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!debugEveryFrame && Time.time < nextDebugTime) return;
            nextDebugTime = Time.time + debugInterval;

            Vector3 pos = agent.transform.position;
            Vector3Int gridPos = WorldToGrid(pos);

            // 1️⃣ Walkability at current position
            bool walkable = VoxelGrid.Instance.IsWalkable(gridPos);
            Debug.Log($"[{agent.name}] GridPos: {gridPos}, Walkable: {walkable}");

            // 2️⃣ Height info
            float terrainHeight = Utility.GetHeightAt(gridPos.x, gridPos.z);
            float agentY = pos.y;
            float targetY = agent.GetTargetY();
            Debug.Log($"[{agent.name}] AgentY: {agentY:F2}, TerrainY: {terrainHeight:F2}, TargetY: {targetY:F2}");

            // 3️⃣ Path info
            bool hasPath = agent.HasPath;
            Debug.Log($"[{agent.name}] HasPath: {hasPath}");

            // 4️⃣ Direct target info
            Vector3Int directTarget = agent.DebugDirectTarget;
            bool isDirect = agent.DebugIsWalkingDirect;
            bool canWalkDirect = agent.CanWalkDirectly(gridPos, directTarget);
            Debug.Log($"[{agent.name}] DirectTarget: {directTarget}, IsWalkingDirect: {isDirect}, CanWalkDirect: {canWalkDirect}");

            // 5️⃣ Velocity info
            Vector3 movement = agent.GetMovementThisFrame();
            Debug.Log($"[{agent.name}] Velocity: {movement}, WantsToMove: {agent.WantsToMove()}");
        }

        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x),
                0,
                Mathf.RoundToInt(worldPos.z)
            );
        }
    }
}
