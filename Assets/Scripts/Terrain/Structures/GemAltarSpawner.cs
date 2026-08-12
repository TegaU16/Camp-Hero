using System.Collections;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public class GemAltarSpawner : MonoBehaviour
    {
        public static GemAltarSpawner Instance;

        [SerializeField] private GameObject[] altarPrefabs;
        [HideInInspector] public Vector3 worldCenter = Vector3.zero;
        [SerializeField] private float offsetFromEdge = 45f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public IEnumerator SpawnGemAltars(float worldSize, System.Action<float> onProgress = null)
        {
            if (altarPrefabs.Length != 4)
            {
                Debug.LogError("AltarSpawner requires exactly 4 altar prefabs.");
                yield break;
            }

            float halfSize = worldSize / 2f;

            Vector3[] altarPositions = new Vector3[]
            {
                worldCenter + new Vector3(-halfSize + offsetFromEdge, 0, -halfSize + offsetFromEdge),
                worldCenter + new Vector3(-halfSize + offsetFromEdge, 0,  halfSize - offsetFromEdge),
                worldCenter + new Vector3( halfSize - offsetFromEdge, 0, -halfSize + offsetFromEdge),
                worldCenter + new Vector3( halfSize - offsetFromEdge, 0,  halfSize - offsetFromEdge)
            };

            for (int i = 0; i < altarPositions.Length; i++)
            {
                Vector3 altarPos = altarPositions[i];
                Vector3Int voxelPos = Utility.WorldToVoxelCoord(altarPos);
                Vector3 spawnPos = voxelPos + new Vector3(VoxelGrid.Instance.voxelSize / 2f, 0, VoxelGrid.Instance.voxelSize / 2f);

                int posX = Mathf.FloorToInt(spawnPos.x);
                int posZ = Mathf.FloorToInt(spawnPos.z);
                float height = Utility.GetHeightAt(posX, posZ);

                spawnPos.y = height;

                int chunkX = Mathf.FloorToInt(spawnPos.x / VoxelGrid.Instance.chunkSize);
                int chunkZ = Mathf.FloorToInt(spawnPos.z / VoxelGrid.Instance.chunkSize);

                Vector2Int chunkKey = new(chunkX, chunkZ);
                VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

                GameObject gemAltar = altarPrefabs[i];
                SpawnedObjectData data = new(spawnPos, gemAltar, gemAltar);
                Utility.AddObjectDataToChunk(data, spawnPos, chunk);

                onProgress?.Invoke((float)(i + 1) / altarPositions.Length);

                yield return null;
            }
        }
    }
}
