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
            ClearGhostWalls();
            previousItem = null;
            return;
        }

        selectedItem = InventoryManager.Instance.GetSelectedItem(false);

        if (selectedItem == null || (selectedItem.itemTypes & ItemType.Building) == 0)
        {
            ClearGhostIfNeeded();
            ClearGhostWalls();
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

    private void ClearGhostIfNeeded()
    {
        if (currentGhost != null)
        {
            ClearVisualIndicators();
            Destroy(currentGhost);
        }
    }

    private void UpdateGhostPosition()
    {
        Vector2Int buildingSize = selectedItem.buildingSize;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Vector3 snappedPosition = GetSnappedPosition(hit.point);

            // Offset the position by half the size to center the building
            Vector3 offset = new((buildingSize.x - 1) * gridSize / 2f, 0, (buildingSize.y - 1) * gridSize / 2f);
            currentGhost.transform.position = snappedPosition + offset;

            bool areaFree = IsAreaFree(currentGhost, snappedPosition);

            if (!areaFree)
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

                    visualIndicator.GetComponent<Renderer>().material.color = areaFree ? Color.green : Color.red;
                }
            }
        }
    }

    private void UpdateWallGhostAnchorPosition()
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

        Vector3Int min = VoxelGrid.Instance.WorldToVoxelCoord(bounds.min);
        Vector3Int max = VoxelGrid.Instance.WorldToVoxelCoord(bounds.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                Vector3Int voxelPos = new(x, 0, z);
                if (!VoxelGrid.Instance.IsBuildable(voxelPos))
                    return false;
            }
        }

        return true;
    }

    private Vector3 GetSnappedPosition(Vector3 hitPoint)
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

    private void PlaceObject()
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
                int chunkX = Mathf.FloorToInt(finalPosition.x / VoxelGrid.Instance.chunkSize);
                int chunkZ = Mathf.FloorToInt(finalPosition.z / VoxelGrid.Instance.chunkSize);

                Vector2Int chunkKey = new(chunkX, chunkZ);
                VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

                GameObject placedObject = Instantiate(selectedItem.buildingGhost, finalPosition, currentGhost.transform.rotation);
                SetAllScriptsEnabled(placedObject, true);

                placedObject.transform.parent = chunk.chunkObject.transform;
                chunk.objects.Add(placedObject);

                SpawnedObjectData data = new(finalPosition, placedObject, selectedItem.buildingGhost);

                chunk.savedObjects.Add(data);
                chunk.savedObjectPositions.Add(finalPosition);

                int savedIndex = chunk.savedObjects.Count - 1;
                chunk.isDirty = true;

                if (placedObject.TryGetComponent(out BreakableObject breakableObject))
                {
                    breakableObject.owningChunk = chunk;
                    breakableObject.savedObjectIndex = savedIndex;
                    breakableObject.PlacedByPlayer = true;
                    breakableObject.DestroyedByEnemy = false;
                }

                if (placedObject.TryGetComponent(out InteractableItem interactable))
                {
                    interactable.owningChunk = chunk;
                    interactable.savedObjectIndex = savedIndex;
                }

                StartCoroutine(BouncePlacedObject(placedObject.transform));

                VoxelGrid.Instance.MarkAreaOccupied(placedObject);

                InventoryManager.Instance.UseSelectedItem();

                Destroy(currentGhost);
                ClearVisualIndicators();
            }
        }
    }

    private IEnumerator BouncePlacedObject(Transform objTransform)
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

    private void SetGhostAlpha(float alpha)
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

    private void DisableGhostColliders(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void ClearVisualIndicators()
    {
        foreach (GameObject indicator in visualIndicators)
        {
            Destroy(indicator);
        }
        visualIndicators.Clear();
    }

    private void SetAllScriptsEnabled(GameObject obj, bool enabled)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script is WallSegment) continue;
            script.enabled = enabled;
        }
    }

    private void UpdateWallVisuals(Vector3Int position)
    {
        WallSegment current = GetWallAtPosition(position);
        if (current == null) return;

        // Left and right neighbors
        Vector3Int leftPos = position + Vector3Int.left;
        Vector3Int rightPos = position + Vector3Int.right;

        WallSegment leftWall = GetWallAtPosition(leftPos);
        WallSegment rightWall = GetWallAtPosition(rightPos);

        // Current wall poles
        bool showLeftPole = (leftWall == null);
        bool showRightPole = (rightWall == null);
        current.SetPoleVisibility(showLeftPole, showRightPole);

        // Current wall connectors
        current.SetConnectorToRight(rightWall != null);

        // Update left neighbor
        if (leftWall != null)
        {
            leftWall.SetPoleVisibility(!IsWallAt(leftPos + Vector3Int.left), rightWall == null);
            leftWall.SetConnectorToRight(true);
        }

        // Update right neighbor
        if (rightWall != null)
        {
            rightWall.SetPoleVisibility(leftWall == null, !IsWallAt(rightPos + Vector3Int.right));
            rightWall.SetConnectorToRight(IsWallAt(rightPos + Vector3Int.right));
        }
    }

    private void ShowGhostWall(Vector3 anchor)
    {
        ClearVisualIndicators();
        ClearGhostWalls(); // Clear previously instantiated ghost walls

        anchor = GetSnappedPosition(anchor);
        Vector3 dir = new(1, 0, 0); // +X direction for now

        InventoryItem selectedInvItem = InventoryManager.Instance.GetSelectedInventoryItem();
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

            Vector3Int gridPos = WorldToGrid(pos);
            UpdateWallVisuals(gridPos);
        }
    }

    private void PlaceWall(Vector3 anchor)
    {
        anchor = GetSnappedPosition(anchor);
        Vector3 dir = new(1, 0, 0); // +X direction for now

        InventoryItem selectedInvItem = InventoryManager.Instance.GetSelectedInventoryItem();
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

            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, placementMask))
            {
                pos = hit.point;
            }

            int chunkX = Mathf.FloorToInt(pos.x / VoxelGrid.Instance.chunkSize);
            int chunkZ = Mathf.FloorToInt(pos.z / VoxelGrid.Instance.chunkSize);

            Vector2Int chunkKey = new(chunkX, chunkZ);
            VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

            Vector3Int gridPos = WorldToGrid(pos);

            GameObject wall = Instantiate(wallPrefab, pos, rotations[currentRotationIndex]);
            SetAllScriptsEnabled(wall, true);

            VoxelGrid.Instance.MarkAreaOccupied(wall);
            StartCoroutine(BouncePlacedObject(wall.transform));

            InventoryManager.Instance.UseSelectedItem();

            WallSegment wallSegment = wall.GetComponent<WallSegment>();
            placedWalls[gridPos] = wallSegment;

            UpdateWallVisuals(gridPos);

            wall.transform.parent = chunk.chunkObject.transform;
            chunk.objects.Add(wall);

            SpawnedObjectData data = new(pos, wall, selectedItem.buildingGhost);

            chunk.savedObjects.Add(data);
            chunk.savedObjectPositions.Add(pos);

            int savedIndex = chunk.savedObjects.Count - 1;
            chunk.isDirty = true;

            if (wall.TryGetComponent(out BreakableObject breakableObject))
            {
                breakableObject.owningChunk = chunk;
                breakableObject.savedObjectIndex = savedIndex;
                breakableObject.PlacedByPlayer = true;
                breakableObject.DestroyedByEnemy = false;
            }
        }

        Destroy(currentGhost);
        currentGhost = null;
        ClearVisualIndicators();
    }

    public void DestroyWall(Vector3Int position)
    {
        WallSegment wall = GetWallAtPosition(position);
        if (wall == null) return;

        placedWalls.Remove(position);

        UpdateWallVisuals(position + Vector3Int.left);
        UpdateWallVisuals(position + Vector3Int.right);
    }

    private void ClearGhostWalls()
    {
        foreach (GameObject obj in ghostWallObjects)
            Destroy(obj);

        ghostWallObjects.Clear();
    }

    private bool IsWallAt(Vector3Int pos)
    {
        return placedWalls.ContainsKey(pos);
    }

    private WallSegment GetWallAtPosition(Vector3Int pos)
    {
        placedWalls.TryGetValue(pos, out WallSegment wall);
        return wall;
    }

    public Vector3Int WorldToGrid(Vector3 worldPos)
    {
        return Vector3Int.RoundToInt(worldPos / gridSize);
    }
}
