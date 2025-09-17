using System.Collections;
using UnityEngine;

public class AltarSpawner : MonoBehaviour
{
    public GameObject[] altarPrefabs; // Array of 4 altar prefabs
    public Vector3 worldCenter = Vector3.zero;
    public float offsetFromEdge = 20f;
    public VoxelGrid voxelGrid;
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

            int chunkX = Mathf.FloorToInt(spawnPos.x / voxelGrid.chunkSize);
            int chunkZ = Mathf.FloorToInt(spawnPos.z / voxelGrid.chunkSize);

            Vector2Int chunkKey = new(chunkX, chunkZ);

            if (voxelGrid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk))
            {
                chunk.objects.Add(gemAltar);
                chunk.savedObjectPositions.Add(spawnPos);
                chunk.savedObjects.Add(new SpawnedObjectData(spawnPos, altarPrefabs[i]));
            }

            voxelGrid.MarkAreaOccupied(gemAltar);

            // Report progress (0 to 1)
            onProgress?.Invoke((float)(i + 1) / altarPositions.Length);

            // Yield so loading bar can update
            yield return null;
        }
    }

    Vector3 AdjustHeightToTerrain(Vector3 position)
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
