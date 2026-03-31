using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Inventory;
using Game.Saving;
using Game.Terrain;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    private Item selectedItem;
    private Item previousItem;

    [SerializeField] private int maxBuildDistance = 10;
    [SerializeField] private LayerMask placementMask;
    private GameObject currentGhost;
    private Vector3 snappedPosition;

    [SerializeField] private GameObject gridSquarePrefab;
    private readonly List<GameObject> visualIndicators = new();

    private readonly float buildingGridSize = 1f;

    private bool isPlacingWall = false;
    private readonly Dictionary<Vector3Int, WallSegment> placedWalls = new();
    private readonly List<GameObject> ghostWallObjects = new();

    [SerializeField] private KeyCode rotateBuildingKey = KeyCode.R;

    private int currentRotationIndex = 0;
    private static readonly Quaternion[] rotations = {
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 90, 0),
        Quaternion.Euler(0, 180, 0),
        Quaternion.Euler(0, 270, 0)
    };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameActive) return;

        if (InventoryManager.Instance.IsExtensionOpen())
        {
            ClearBuildings();
            return;
        }

        selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
        if (selectedItem == null || selectedItem.buildingGhost == null || (selectedItem.itemTypes & ItemType.Building) == 0)
        {
            ClearBuildings();
            return;
        }

        if (selectedItem != previousItem)
        {
            ClearBuildings(selectedItem);
            currentGhost = null;
            currentRotationIndex = 0;
        }

        if (currentGhost != null)
        {
            Vector2Int buildingSize = selectedItem.buildingSize;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
            {
                Vector3Int hitPoint = Utility.WorldToVoxelCoord(hit.point);
                Vector3Int cameraPos = Utility.WorldToVoxelCoord(Camera.main.transform.position);

                if (Utility.ManhattanDistance(hitPoint, cameraPos) <= maxBuildDistance)
                    snappedPosition = GetSnappedPosition(hit.point);

                // Offset the position by half the size to center the building
                Vector3 offset = new((buildingSize.x - 1) * buildingGridSize / 2f, 0, (buildingSize.y - 1) * buildingGridSize / 2f);
                currentGhost.transform.position = snappedPosition + offset;

                bool areaFree = Utility.AreaCheck(currentGhost, snappedPosition, VoxelGrid.Instance.IsBuildable);
                if (!areaFree) return;
            }
        }

        isPlacingWall = selectedItem.buildingGhost.GetComponent<WallSegment>() != null;
        if (isPlacingWall)
        {
            UpdateWallGhostAnchorPosition();
            ShowGhostWall(currentGhost.transform.position);

            if (Input.GetMouseButtonDown(1))
                PlaceWall(currentGhost.transform.position);
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
                PlaceObject();
        }

        if (Input.GetKeyDown(rotateBuildingKey) && currentGhost != null)
        {
            currentRotationIndex = (currentRotationIndex + 1) % rotations.Length;
            currentGhost.transform.rotation = rotations[currentRotationIndex];
        }
    }

    private void ClearGhostBuilding()
    {
        if (currentGhost == null) return;

        ClearVisualIndicators();
        Destroy(currentGhost);
    }

    private void ClearBuildings(Item selectedItem = null)
    {
        ClearGhostBuilding();
        ClearGhostWalls();
        previousItem = selectedItem;
    }

    private void UpdateGhostPosition()
    {
        Vector2Int buildingSize = selectedItem.buildingSize;

        // Offset the position by half the size to center the building
        Vector3 offset = new((buildingSize.x - 1) * buildingGridSize / 2f, 0, (buildingSize.y - 1) * buildingGridSize / 2f);
        currentGhost.transform.position = snappedPosition + offset;

        bool areaFree = !Utility.AreaCheck(currentGhost, snappedPosition, VoxelGrid.Instance.IsOccupied);

        if (!areaFree)
            SetGhostAlpha(0.2f);
        else
            SetGhostAlpha(0.5f);

        ClearVisualIndicators();

        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int z = 0; z < buildingSize.y; z++)
            {
                Vector3 worldPos = snappedPosition + new Vector3(x * buildingGridSize, 0f, z * buildingGridSize);
                GameObject visualIndicator = Instantiate(gridSquarePrefab, worldPos, gridSquarePrefab.transform.rotation);
                visualIndicators.Add(visualIndicator);

                visualIndicator.GetComponent<Renderer>().material.color = areaFree ? Color.green : Color.red;
            }
        }
    }

    private void UpdateWallGhostAnchorPosition()
    {
        if (currentGhost == null)
            currentGhost = new GameObject("WallGhostAnchor");

        currentGhost.transform.position = snappedPosition;

        ClearVisualIndicators();
    }

    private Vector3 GetSnappedPosition(Vector3 hitPoint)
    {
        float snappedX = Mathf.Round(hitPoint.x / buildingGridSize) * buildingGridSize + buildingGridSize / 2f;
        float snappedZ = Mathf.Round(hitPoint.z / buildingGridSize) * buildingGridSize + buildingGridSize / 2f;

        float snappedY = Utility.GetHeightAt((int)snappedX, (int)snappedZ);

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    private void PlaceObject()
    {
        Vector2Int buildingSize = selectedItem.buildingSize;
        Vector3 offset = new((buildingSize.x - 1) * buildingGridSize / 2f, 0, (buildingSize.y - 1) * buildingGridSize / 2f);
        Vector3 finalPosition = snappedPosition + offset;

        if (Utility.AreaCheck(currentGhost, snappedPosition, VoxelGrid.Instance.IsOccupied)) return;

        int chunkX = Mathf.FloorToInt(finalPosition.x / VoxelGrid.Instance.chunkSize);
        int chunkZ = Mathf.FloorToInt(finalPosition.z / VoxelGrid.Instance.chunkSize);

        Vector2Int chunkKey = new(chunkX, chunkZ);
        VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

        GameObject placedObject = Instantiate(selectedItem.buildingGhost, finalPosition, currentGhost.transform.rotation);
        SetAllScriptsEnabled(placedObject, true);

        placedObject.transform.parent = chunk.chunkObject.transform;
        chunk.objects.Add(placedObject);

        SpawnedObjectData data = new(finalPosition, placedObject, selectedItem.buildingGhost);
        Utility.AddObjectDataToChunk(data, finalPosition, chunk);

        if (placedObject.TryGetComponent(out BreakableObject breakableObject))
        {
            breakableObject.owningChunk = chunk;
            breakableObject.PlacedByPlayer = true;
            breakableObject.DestroyedByEnemy = false;
        }

        if (placedObject.TryGetComponent(out InteractableItem interactable))
            interactable.owningChunk = chunk;

        StartCoroutine(BouncePlacedObject(placedObject.transform));

        VoxelGrid.Instance.MarkVoxelArea(placedObject);
        InventoryManager.Instance.UseSelectedItem();

        Destroy(currentGhost);
        ClearVisualIndicators();
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
            col.enabled = false;
    }

    private void ClearVisualIndicators()
    {
        foreach (GameObject indicator in visualIndicators)
            Destroy(indicator);

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
            Vector3 pos = anchor + buildingGridSize * i * dir;

            GameObject wallGhost = Instantiate(wallPrefab, pos, rotations[currentRotationIndex]);
            SetAllScriptsEnabled(wallGhost, false);
            ghostWallObjects.Add(wallGhost); // Track it

            GameObject visual = Instantiate(gridSquarePrefab, pos, gridSquarePrefab.transform.rotation);
            visualIndicators.Add(visual);

            bool isFree = !Utility.AreaCheck(wallPrefab, pos, VoxelGrid.Instance.IsOccupied);
            visual.GetComponent<Renderer>().material.color = isFree ? Color.green : Color.red;

            Vector3Int gridPos = WorldToGrid(pos);
            UpdateWallVisuals(gridPos);
        }
    }

    private void PlaceWall(Vector3 anchor)
    {
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
            Vector3 pos = anchor + buildingGridSize * i * dir;

            if (Utility.AreaCheck(wallPrefab, pos, VoxelGrid.Instance.IsOccupied))
            {
                Debug.Log("Cannot place full wall: area occupied at " + pos);
                return; // Abort placement if any part blocked
            }
        }

        // If we get here, all spots are free - place all wall segments
        for (int i = 0; i < maxWalls; i++)
        {
            Vector3 pos = anchor + buildingGridSize * i * dir;

            int posX = Mathf.FloorToInt(pos.x);
            int posZ = Mathf.FloorToInt(pos.z);

            float posY = Utility.GetHeightAt(posX, posZ);

            pos.y = posY;

            int chunkX = Mathf.FloorToInt(pos.x / VoxelGrid.Instance.chunkSize);
            int chunkZ = Mathf.FloorToInt(pos.z / VoxelGrid.Instance.chunkSize);

            Vector2Int chunkKey = new(chunkX, chunkZ);
            VoxelChunk chunk = VoxelGrid.Instance.chunkMap[chunkKey];

            Vector3Int gridPos = WorldToGrid(pos);

            GameObject wall = Instantiate(wallPrefab, pos, rotations[currentRotationIndex]);
            SetAllScriptsEnabled(wall, true);

            VoxelGrid.Instance.MarkVoxelArea(wall);
            StartCoroutine(BouncePlacedObject(wall.transform));

            InventoryManager.Instance.UseSelectedItem();

            WallSegment wallSegment = wall.GetComponent<WallSegment>();
            placedWalls[gridPos] = wallSegment;

            UpdateWallVisuals(gridPos);

            wall.transform.parent = chunk.chunkObject.transform;
            chunk.objects.Add(wall);

            SpawnedObjectData data = new(pos, wall, selectedItem.buildingGhost);
            Utility.AddObjectDataToChunk(data, pos, chunk);

            if (!wall.TryGetComponent(out BreakableObject breakableObject)) continue;

            breakableObject.owningChunk = chunk;
            breakableObject.PlacedByPlayer = true;
            breakableObject.DestroyedByEnemy = false;
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

    private bool IsWallAt(Vector3Int pos) => placedWalls.ContainsKey(pos);

    private WallSegment GetWallAtPosition(Vector3Int pos)
    {
        placedWalls.TryGetValue(pos, out WallSegment wall);
        return wall;
    }

    public Vector3Int WorldToGrid(Vector3 worldPos) => Vector3Int.RoundToInt(worldPos / buildingGridSize);
}
