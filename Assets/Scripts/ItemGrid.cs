using System.Collections.Generic;
using UnityEngine;

public static class ItemGrid
{
    public static float cellSize = 2f; // adjust as needed
    private static readonly Dictionary<Vector2Int, List<InteractableItem>> grid = new();

    // Temporary reusable buffer to avoid allocations
    private static readonly List<InteractableItem> tempBuffer = new(50);

    private static Vector2Int GetCell(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    public static void Register(InteractableItem item)
    {
        Vector2Int cell = GetCell(item.transform.position);

        if (!grid.TryGetValue(cell, out List<InteractableItem> list))
        {
            list = new List<InteractableItem>();
            grid[cell] = list;
        }

        if (!list.Contains(item))
            list.Add(item);

        item.CurrentCell = cell;
    }

    public static void Unregister(InteractableItem item)
    {
        if (grid.TryGetValue(item.CurrentCell, out List<InteractableItem> list))
        {
            list.Remove(item);
            if (list.Count == 0)
                grid.Remove(item.CurrentCell);
        }
    }

    public static void UpdateItemCell(InteractableItem item)
    {
        Vector2Int newCell = GetCell(item.transform.position);

        if (newCell != item.CurrentCell)
        {
            Unregister(item);
            Register(item);
        }
    }

    public static IEnumerable<InteractableItem> GetNearby(Vector3 pos)
    {
        tempBuffer.Clear(); // reuse buffer
        Vector2Int cell = GetCell(pos);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                Vector2Int neighbor = new(cell.x + dx, cell.y + dz);
                if (grid.TryGetValue(neighbor, out List<InteractableItem> list))
                    tempBuffer.AddRange(list);
            }
        }

        return tempBuffer;
    }
}
