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
    public float gridSize = 1f;

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

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
        voxelGrid.DrawOccupiedVoxels();

        Vector2Int buildingSize = selectedItem.buildingSize;
        float voxelSize = voxelGrid.voxelSize;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);
            currentGhost.transform.position = snappedPosition;

            if (!IsAreaFree(currentGhost, snappedPosition))
            {
                SetGhostAlpha(0.2f);
            }
            else
            {
                SetGhostAlpha(0.5f);
            }

            ClearVisualIndicators();

            for (int x = 0; x < buildingSize.x; x++)
            {
                for (int z = 0; z < buildingSize.y; z++)
                {
                    Vector3 worldPos = snappedPosition + new Vector3(x * gridSize, 0, z * gridSize);
                    GameObject visualIndicator = Instantiate(gridSquarePrefab, worldPos, gridSquarePrefab.transform.rotation);
                    visualIndicators.Add(visualIndicator);

                    if (!IsAreaFree(visualIndicator, visualIndicator.transform.position))
                        visualIndicator.GetComponent<Renderer>().material.color = Color.red;
                    else
                        visualIndicator.GetComponent<Renderer>().material.color = Color.green;
                }
            }
        }
    }

    public bool IsAreaFree(GameObject prefab, Vector3 intendedPosition)
    {
        Bounds bounds = prefab.GetComponentInChildren<Renderer>().bounds;

        // Move bounds to where it would be instantiated
        bounds.center = intendedPosition + (bounds.center - prefab.transform.position);

        Vector3Int min = voxelGrid.WorldToVoxelCoord(bounds.min);
        Vector3Int max = voxelGrid.WorldToVoxelCoord(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                Vector3Int voxelPos = new(x, 0, z);
                if (voxelGrid.IsOccupied(voxelPos))
                    return false;
            }
        }

        return true;
    }

    Vector3 GetSnappedPosition(Vector3 hitPoint)
    {
        float snappedX = Mathf.Round(hitPoint.x / gridSize) * gridSize + gridSize / 2f;
        float snappedZ = Mathf.Round(hitPoint.z / gridSize) * gridSize + gridSize / 2f;

        Vector3 origin = new(snappedX, hitPoint.y + 10f, snappedZ);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 20f, placementMask))
        {
            return new Vector3(snappedX, groundHit.point.y, snappedZ);
        }

        return new Vector3(snappedX, hitPoint.y, snappedZ);
    }

    void PlaceObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);

            if (IsAreaFree(currentGhost, snappedPosition))
            {
                GameObject placedObject = Instantiate(selectedItem.buildingGhost, snappedPosition, currentGhost.transform.rotation);
                SetAllScriptsEnabled(placedObject, true);

                voxelGrid.MarkAreaOccupied(placedObject);

                InventoryManager.Instance.UseSelectedItem();

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

    void ClearVisualIndicators()
    {
        foreach (GameObject indicator in visualIndicators)
        {
            Destroy(indicator);
        }
        visualIndicators.Clear();
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
