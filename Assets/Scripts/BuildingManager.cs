using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private Item selectedItem;
    private Item previousItem;

    public LayerMask placementMask;
    private GameObject currentGhost;

    public GameObject gridSquarePrefab;
    private readonly List<GameObject> visualIndicators = new();

    public VoxelGrid voxelGrid;
    public float gridSize = 1f;

    private bool isPlacingWall = false;
    private readonly Dictionary<Vector3Int, WallSegment> placedWalls = new();
    private readonly List<GameObject> ghostWallObjects = new();

    public KeyCode rotateBuildingKey = KeyCode.R;

    private int currentRotationIndex = 0;
    private static readonly Quaternion[] rotations = {
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 90, 0),
        Quaternion.Euler(0, 180, 0),
        Quaternion.Euler(0, 270, 0)
    };

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (InventoryManager.Instance.IsExtensionOpen())
        {
            ClearGhostIfNeeded();
            previousItem = null;
            return;
        }

        selectedItem = InventoryManager.Instance.GetSelectedItem(false);

        if (selectedItem == null || selectedItem.itemType != ItemType.Building)
        {
            ClearGhostIfNeeded();
            previousItem = null;
            return;
        }

        if (selectedItem != previousItem)
        {
            ClearGhostIfNeeded();
            ClearGhostWalls();
            currentGhost = null;
            previousItem = selectedItem;
            currentRotationIndex = 0;
        }

        if (selectedItem.buildingGhost == null) return;

        isPlacingWall = selectedItem.buildingGhost.GetComponent<WallSegment>() != null;

        if (isPlacingWall)
        {
            UpdateWallGhostAnchorPosition();

            if (currentGhost != null)
            {
                ShowGhostWall(currentGhost.transform.position);
            }

            if (Input.GetMouseButtonDown(1) && currentGhost != null)
            {
                PlaceWall(currentGhost.transform.position);
            }
        }
        else
        {
            if (currentGhost == null)
            {
                currentGhost = Instantiate(selectedItem.buildingGhost);
                currentGhost.transform.rotation = rotations[currentRotationIndex];
                DisableGhostColliders(currentGhost);
                SetAllScriptsEnabled(currentGhost, false);
            }

            UpdateGhostPosition();

            if (Input.GetMouseButtonDown(1))
            {
                PlaceObject();
            }
        }

        if (Input.GetKeyDown(rotateBuildingKey))
        {
            if (currentGhost != null)
            {
                currentRotationIndex = (currentRotationIndex + 1) % rotations.Length;
                currentGhost.transform.rotation = rotations[currentRotationIndex];
            }
        }
    }

    void ClearGhostIfNeeded()
    {
        if (currentGhost != null)
        {
            ClearVisualIndicators();
            Destroy(currentGhost);
        }
    }


    void UpdateGhostPosition()
    {
        Vector2Int buildingSize = selectedItem.buildingSize;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);

            // Offset the position by half the size to center the building
            Vector3 offset = new((buildingSize.x - 1) * gridSize / 2f, 0, (buildingSize.y - 1) * gridSize / 2f);
            currentGhost.transform.position = snappedPosition + offset;

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

    void UpdateWallGhostAnchorPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);

            if (currentGhost == null)
            {
                currentGhost = new GameObject("WallGhostAnchor");
            }

            currentGhost.transform.position = snappedPosition;

            ClearVisualIndicators();
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

            Vector2Int buildingSize = selectedItem.buildingSize;
            Vector3 offset = new((buildingSize.x - 1) * gridSize / 2f, 0, (buildingSize.y - 1) * gridSize / 2f);
            Vector3 finalPosition = snappedPosition + offset;

            if (IsAreaFree(currentGhost, snappedPosition))
            {
                GameObject placedObject = Instantiate(selectedItem.buildingGhost, finalPosition, currentGhost.transform.rotation);
                SetAllScriptsEnabled(placedObject, true);

                StartCoroutine(BouncePlacedObject(placedObject.transform));

                voxelGrid.MarkAreaOccupied(placedObject);

                InventoryManager.Instance.UseSelectedItem();

                Destroy(currentGhost);
                ClearVisualIndicators();
            }
        }
    }

    IEnumerator BouncePlacedObject(Transform objTransform)
    {
        Vector3 originalScale = objTransform.localScale;
        Vector3 shrunkenScale = originalScale * 0.8f; // 80% size

        float duration = 0.1f; // shrink duration
        float elapsed = 0f;

        // Shrink
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(originalScale, shrunkenScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = shrunkenScale;

        // Expand back
        elapsed = 0f;
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(shrunkenScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = originalScale;
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

    void UpdateWallVisuals(Vector3Int position)
    {
        WallSegment current = GetWallAtPosition(position);
        if (current == null) return;

        bool hasLeft = IsWallAt(position + Vector3Int.left);
        bool hasRight = IsWallAt(position + Vector3Int.right);

        current.SetPoleVisibility(!hasLeft, !hasRight);
        current.SetConnectorToRight(hasRight);

        // Also update neighboring walls
        WallSegment leftWall = GetWallAtPosition(position + Vector3Int.left);
        if (leftWall != null)
        {
            leftWall.SetPoleVisibility(!IsWallAt(position + Vector3Int.left * 2), false);
            leftWall.SetConnectorToRight(true);
        }

        WallSegment rightWall = GetWallAtPosition(position + Vector3Int.right);
        if (rightWall != null)
        {
            rightWall.SetPoleVisibility(false, !IsWallAt(position + Vector3Int.right * 2));
        }
    }

    void ShowGhostWall(Vector3 anchor)
    {
        ClearVisualIndicators();
        ClearGhostWalls(); // Clear previously instantiated ghost walls

        anchor = GetSnappedPosition(anchor);
        Vector3 dir = new(1, 0, 0); // +X direction for now

        InventoryItem selectedInvItem = InventoryManager.Instance.GetInventoryItem();
        GameObject wallPrefab = selectedItem.buildingGhost;
        int maxWalls = selectedInvItem.count;

        if (wallPrefab == null)
        {
            Debug.LogWarning("No wall prefab found.");
            return;
        }

        for (int i = 0; i < maxWalls; i++)
        {
            Vector3 pos = anchor + gridSize * i * dir;

            GameObject wallGhost = Instantiate(wallPrefab, pos, rotations[currentRotationIndex]);
            SetAllScriptsEnabled(wallGhost, false);
            ghostWallObjects.Add(wallGhost); // Track it

            GameObject visual = Instantiate(gridSquarePrefab, pos, gridSquarePrefab.transform.rotation);
            visualIndicators.Add(visual);

            bool isFree = IsAreaFree(wallPrefab, pos);
            visual.GetComponent<Renderer>().material.color = isFree ? Color.green : Color.red;
        }
    }

    void PlaceWall(Vector3 anchor)
    {
        anchor = GetSnappedPosition(anchor);
        Vector3 dir = new(1, 0, 0); // +X direction for now

        InventoryItem selectedInvItem = InventoryManager.Instance.GetInventoryItem();
        GameObject wallPrefab = selectedItem.buildingGhost;
        int maxWalls = selectedInvItem.count;

        if (wallPrefab == null)
        {
            Debug.LogWarning("No wall prefab found.");
            return;
        }

        // First, check if all positions are free
        for (int i = 0; i < maxWalls; i++)
        {
            Vector3 pos = anchor + gridSize * i * dir;
            pos = GetSnappedPosition(pos);

            if (!IsAreaFree(wallPrefab, pos))
            {
                Debug.Log("Cannot place full wall: area occupied at " + pos);
                return; // Abort placement if any part blocked
            }
        }

        // If we get here, all spots are free - place all wall segments
        for (int i = 0; i < maxWalls; i++)
        {
            Vector3 pos = anchor + gridSize * i * dir;
            Vector3Int gridPos = WorldToGrid(pos);

            GameObject wall = Instantiate(wallPrefab, pos, rotations[currentRotationIndex]);
            SetAllScriptsEnabled(wall, true);
            voxelGrid.MarkAreaOccupied(wall);
            InventoryManager.Instance.UseSelectedItem();

            WallSegment wallSegment = wall.GetComponent<WallSegment>();
            placedWalls[gridPos] = wallSegment;

            UpdateWallVisuals(gridPos);
        }

        Destroy(currentGhost);
        currentGhost = null;
        ClearVisualIndicators();
    }

    void ClearGhostWalls()
    {
        foreach (var obj in ghostWallObjects)
        {
            Destroy(obj);
        }
        ghostWallObjects.Clear();
    }

    bool IsWallAt(Vector3Int pos)
    {
        return placedWalls.ContainsKey(pos);
    }

    WallSegment GetWallAtPosition(Vector3Int pos)
    {
        placedWalls.TryGetValue(pos, out var wall);
        return wall;
    }

    Vector3Int WorldToGrid(Vector3 worldPos)
    {
        return Vector3Int.RoundToInt(worldPos / gridSize);
    }
}
