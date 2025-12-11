using System.Collections;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public class GemAltarSpawner : MonoBehaviour
    {
        public GameObject[] altarPrefabs; // Array of 4 altar prefabs
        public Vector3 worldCenter = Vector3.zero;
        public float offsetFromEdge = 20f;
        [SerializeField] private LayerMask terrainMask;

        public IEnumerator SpawnAltarsRoutine(float worldSize, System.Action<float> onProgress = null)
        {
            if (altarPrefabs.Length < 4)
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
                Vector3 spawnPos = AdjustHeightToTerrain(altarPositions[i]);
                GameObject gemAltar = Instantiate(altarPrefabs[i], spawnPos, Quaternion.identity);

                int chunkX = Mathf.FloorToInt(spawnPos.x / VoxelGrid.Instance.chunkSize);
                int chunkZ = Mathf.FloorToInt(spawnPos.z / VoxelGrid.Instance.chunkSize);

                Vector2Int chunkKey = new(chunkX, chunkZ);
                VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

                gemAltar.transform.parent = chunk.chunkObject.transform;
                chunk.objects.Add(gemAltar);

                SpawnedObjectData data = new(spawnPos, gemAltar, altarPrefabs[i]);
                Utility.AddObjectDataToChunk(data, spawnPos, chunk);

                VoxelGrid.Instance.MarkVoxelArea(gemAltar, buildable: false);

                // Report progress (0 to 1)
                onProgress?.Invoke((float)(i + 1) / altarPositions.Length);

                // Yield so loading bar can update
                yield return null;
            }
        }

        private Vector3 AdjustHeightToTerrain(Vector3 position)
        {
            Vector3 rayStart = position + Vector3.up * 200f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 500f, terrainMask))
            {
                return hit.point;
            }
            else
            {
                Debug.LogWarning($"No terrain found below altar position: {position}");
                return position;
            }
        }
    }
}
