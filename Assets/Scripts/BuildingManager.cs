using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private Item selectedItem;
    public LayerMask placementMask;
    private GameObject currentGhost;

    public GameObject gridSquarePrefab;
    private readonly List<GameObject> visualIndicators = new();

    public VoxelGrid voxelGrid;
    public float gridSize = 1f; // Grid size to match voxel size

    void Update()
    {
        if (!InventoryManager.Instance.IsExtensionOpen())
        {
            selectedItem = InventoryManager.Instance.GetSelectedItem(false);

            if (selectedItem != null && selectedItem.itemType == ItemType.Building)
            {
                if (selectedItem.buildingGhost != null)
                {
                    if (currentGhost == null)
                    {
                        currentGhost = Instantiate(selectedItem.buildingGhost);
                        DisableGhostColliders(currentGhost);
                        SetAllScriptsEnabled(currentGhost, false);
                    }

                    UpdateGhostPosition();

                    if (Input.GetMouseButtonDown(0))
                    {
                        PlaceObject();
                    }
                }
            }
            else if (currentGhost != null)
            {
                ClearVisualIndicators();
                Destroy(currentGhost);
            }
        }
        else if (currentGhost != null)
        {
            ClearVisualIndicators();
            Destroy(currentGhost);
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);
            currentGhost.transform.position = snappedPosition;

            // Check if the area is free for placement
            if (!IsAreaOccupied(snappedPosition, selectedItem.buildingSize))
            {
                SetGhostAlpha(0.2f); // Show ghost in a valid state
            }
            else
            {
                SetGhostAlpha(0.5f); // Show ghost in an invalid state (e.g., red)
            }

            ClearVisualIndicators();

            // Create new visual indicators based on the building's size
            for (int x = 0; x < selectedItem.buildingSize.x; x++)
            {
                for (int z = 0; z < selectedItem.buildingSize.y; z++)
                {
                    Vector3 position = new(snappedPosition.x + x * gridSize, snappedPosition.y, snappedPosition.z + z * gridSize);
                    GameObject visualIndicator = Instantiate(gridSquarePrefab, position, gridSquarePrefab.transform.rotation);
                    visualIndicators.Add(visualIndicator); // Add to the list for later cleanup

                    // Check if the position is already occupied
                    Vector3Int occupiedPosition = new(Mathf.RoundToInt(position.x / gridSize), Mathf.RoundToInt(position.y / gridSize), Mathf.RoundToInt(position.z / gridSize));
                    if (voxelGrid.IsOccupied(occupiedPosition))
                    {
                        // Change color to red if the area is occupied
                        visualIndicator.GetComponent<Renderer>().material.color = Color.red;
                    }
                    else
                    {
                        // Change color to green if the area is free
                        visualIndicator.GetComponent<Renderer>().material.color = Color.green;
                    }
                }
            }
        }
    }

    Vector3 GetSnappedPosition(Vector3 hitPoint)
    {
        float snappedX = Mathf.Round(hitPoint.x / gridSize) * gridSize + gridSize / 2f;
        float snappedZ = Mathf.Round(hitPoint.z / gridSize) * gridSize + gridSize / 2f;

        // Cast a ray downward from above the snapped position
        Vector3 origin = new(snappedX, hitPoint.y + 10f, snappedZ);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 20f, placementMask))
        {
            return new Vector3(snappedX, groundHit.point.y, snappedZ);
        }

        // Fallback in case no terrain was hit
        return new Vector3(snappedX, hitPoint.y, snappedZ);
    }

    void PlaceObject()
    {
        // Only place if area is free
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);

            if (!IsAreaOccupied(snappedPosition, selectedItem.buildingSize)) // Check if area is free
            {
                // Place the building
                GameObject placedObject = Instantiate(selectedItem.buildingGhost, snappedPosition, currentGhost.transform.rotation);
                SetAllScriptsEnabled(placedObject, true);

                // Mark the area as occupied
                MarkAreaAsOccupied(snappedPosition, selectedItem.buildingSize);

                // Use the item
                InventoryManager.Instance.UseSelectedItem();

                // Destroy the ghost
                Destroy(currentGhost);

                ClearVisualIndicators();
            }
        }
    }

    void SetGhostAlpha(float alpha)
    {
        Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                // Set white tint with desired alpha
                mat.color = new Color(1f, 1f, 1f, alpha);

                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
    }

    void DisableGhostColliders(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    bool IsAreaOccupied(Vector3 snappedPosition, Vector2Int buildingSize)
    {
        // Get the starting grid position (bottom-left corner of the building)
        Vector3Int startGridPosition = new(
            Mathf.RoundToInt(snappedPosition.x / gridSize),
            Mathf.RoundToInt(snappedPosition.y / gridSize),
            Mathf.RoundToInt(snappedPosition.z / gridSize)
        );

        // Loop through the area the building will occupy
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int z = 0; z < buildingSize.y; z++)
            {
                // Check if the voxel at this position is occupied
                Vector3Int occupiedPosition = new(startGridPosition.x + x, startGridPosition.y, startGridPosition.z + z);
                if (voxelGrid.IsOccupied(occupiedPosition))
                {
                    return true; // If any voxel is occupied, return true
                }
            }
        }

        return false; // If no voxels are occupied, return false
    }

    void MarkAreaAsOccupied(Vector3 snappedPosition, Vector2Int buildingSize)
    {
        Vector3Int startGridPosition = new(
            Mathf.RoundToInt(snappedPosition.x / gridSize),
            Mathf.RoundToInt(snappedPosition.y / gridSize),
            Mathf.RoundToInt(snappedPosition.z / gridSize)
        );

        // Loop through the building's area and mark voxels as occupied
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int z = 0; z < buildingSize.y; z++)
            {
                Vector3Int occupiedPosition = new Vector3Int(startGridPosition.x + x, startGridPosition.y, startGridPosition.z + z);
                voxelGrid.SetOccupied(occupiedPosition, true); // Mark this voxel as occupied
            }
        }
    }

    void ClearVisualIndicators()
    {
        // Destroy all existing visual indicators before creating new ones
        foreach (GameObject indicator in visualIndicators)
        {
            Destroy(indicator);
        }
        visualIndicators.Clear();  // Clear the list after destruction
    }

    void SetAllScriptsEnabled(GameObject obj, bool enabled)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = enabled;
        }
    }
}
