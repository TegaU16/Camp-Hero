using UnityEngine;
using UnityEngine.UI;

public static class Utility
{
    public static Vector3Int WorldToVoxelCoord(Vector3 worldPos)
    {
        VoxelGrid voxelGrid = VoxelGrid.Instance;

        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / voxelGrid.voxelSize),
            Mathf.FloorToInt(worldPos.y / voxelGrid.voxelSize),
            Mathf.FloorToInt(worldPos.z / voxelGrid.voxelSize)
        );
    }

    public static Vector3 VoxelCoordToWorld(Vector3Int voxelCoord)
    {
        return new Vector3(
            voxelCoord.x + 0.5f,
            voxelCoord.y + 0.5f,
            voxelCoord.z + 0.5f);
    }

    public static float GetHeightAt(int x, int z)
    {
        VoxelGrid voxelGrid = VoxelGrid.Instance;

        int chunkX = Mathf.FloorToInt((float)x / voxelGrid.chunkSize);
        int chunkZ = Mathf.FloorToInt((float)z / voxelGrid.chunkSize);

        // Handle negative modulus properly
        int localX = x - chunkX * voxelGrid.chunkSize;
        int localZ = z - chunkZ * voxelGrid.chunkSize;

        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (!voxelGrid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) return 0f; // or some default height

        if (localX < 0 || localX >= voxelGrid.chunkSize || localZ < 0 || localZ >= voxelGrid.chunkSize) return 0f; // or default height

        return chunk.heightMap[localX, localZ];
    }

    public static void DisableButtonsOutside(Transform menuTransform, Button[] buttonsInScene, bool open)
    {
        foreach (Button button in buttonsInScene)
        {
            if (button.transform.parent != menuTransform)
            {
                button.interactable = !open;
                if (button.TryGetComponent(out InteractiveButton interactiveButton))
                    interactiveButton.isActive = !open;
            }
        }
    }
}
