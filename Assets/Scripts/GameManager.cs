using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject campFire;

    [HideInInspector] public GameObject playerInstance;
    private GameObject spawnedCampFire;

    public NavMeshSurface navMeshSurface;

    public GameObject pauseMenuUI;
    [HideInInspector] public bool isPaused = false;
    public GameObject gameOverMenuUI;
    public GameObject winMenuUI;
    public GameObject darkBackground;

    public KeyCode togglePauseKey = KeyCode.Escape;

    [Header("Managers")]
    public VoxelGrid voxelGrid;
    public InventoryManager inventoryManager;
    public DayNightCycle dayNightCycle;
    public EnemySpawner enemySpawner;
    public PlayerStatsManager playerStatsManager;
    public CompassBar compassBar;
    public FoodManager foodManager;
    public AnimalSpawner animalSpawner;

    public static GameManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(GenerateWorldThenSpawnStuff());
    }

    void Update()
    {
        if (Input.GetKeyDown(togglePauseKey))
        {
            if (InventoryManager.Instance.JustClosedExtension)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private IEnumerator GenerateWorldThenSpawnStuff()
    {
        string worldName = PlayerPrefs.GetString("WorldName");
        string seedString = PlayerPrefs.GetString("Seed");

        int seed = ConsistentHash(seedString);
        UnityEngine.Random.InitState(seed);

        voxelGrid.SetWorld(seed, worldName);

        // Wait for terrain to generate
        while (!voxelGrid.worldGenerated)
            yield return null;

        // ✅ Bake NavMesh AFTER terrain is generated
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh baked.");
        }
        else
        {
            Debug.LogWarning("NavMeshSurface not assigned in GameManager.");
        }

        // Now safe to spawn objects that use NavMesh
        float terrainWidth = voxelGrid.chunkSize * voxelGrid.gridSize;
        Vector3 center = new(terrainWidth / 2, voxelGrid.maxHeight, terrainWidth / 2);

        if (playerPrefab != null)
        {
            Vector3 spawnPosition = center + new Vector3(2f, 0, 2f);
            Debug.Log("Player Spawn Position: " + spawnPosition);
            playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.transform.position = spawnPosition;

            HookSystems(playerInstance);
        }

        if (campFire != null)
            StartCoroutine(WaitAndSpawnObject(campFire, center, obj => spawnedCampFire = obj));

        compassBar.SetCampfireTransform(spawnedCampFire);
    }

    // Coroutine to wait for terrain generation to complete
    private IEnumerator WaitAndSpawnObject(GameObject spawnObject, Vector3 position, Action<GameObject> onSpawned)
    {
        // Wait until the terrain is fully generated
        while (!voxelGrid.worldGenerated)
        {
            yield return null; // Wait for the next frame
        }

        // Spawn the campfire after terrain is ready
        LayerMask layerMask = LayerMask.GetMask("Ground");

        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            float heightOffset = 0f;
            if (spawnObject.TryGetComponent(out CapsuleCollider col))
            {
                heightOffset = col.height / 2f;
            }

            Vector3 spawnPos = hit.point + new Vector3(0, heightOffset, 0);
            GameObject instance = Instantiate(spawnObject, spawnPos, Quaternion.identity);
            onSpawned?.Invoke(instance);
        }
    }

    public void RespawnPlayer(Player player)
    {
        StartCoroutine(RespawnPlayerCoroutine(player));
    }

    private IEnumerator RespawnPlayerCoroutine(Player player)
    {
        // Destroy old player
        if (player != null)
        {
            Destroy(player.gameObject);
        }

        playerInstance = null;

        // Wait a short time to ensure destruction is clean
        yield return new WaitForSeconds(0.5f);

        // Wait until the terrain is generated if it's not already
        while (!voxelGrid.worldGenerated)
        {
            yield return null;
        }

        if (spawnedCampFire == null)
        {
            Debug.LogError("No campfire found to respawn near!");
            yield break;
        }

        Vector3 campfirePosition = spawnedCampFire.transform.position;
        Vector3 respawnOffset = new(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
        Vector3 respawnPosition = campfirePosition + respawnOffset;

        // Raycast down to find the ground
        LayerMask groundMask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(respawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
        {
            float heightOffset = 0f;
            if (playerPrefab.TryGetComponent(out CapsuleCollider col))
            {
                heightOffset = col.height / 2f;
            }

            Vector3 finalPosition = hit.point + new Vector3(0, heightOffset, 0);
            playerInstance = Instantiate(playerPrefab, finalPosition, Quaternion.identity);

            // Re-hook systems
            HookSystems(playerInstance);
        }
        else
        {
            Debug.LogError("Failed to find ground to respawn player!");
        }
    }

    private void HookSystems(GameObject playerInstance)
    {
        voxelGrid.SetPlayer(playerInstance);
        inventoryManager.SetPlayer(playerInstance);
        dayNightCycle.SetPlayer(playerInstance);
        enemySpawner.SetPlayer(playerInstance);
        playerStatsManager.SetPlayer(playerInstance);
        compassBar.SetPlayer(playerInstance);
        foodManager.SetPlayer(playerInstance);
        animalSpawner.SetPlayer(playerInstance);

        CinemachineCamera cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera != null)
        {
            if (playerInstance.TryGetComponent(out Player player))
            {
                cinemachineCamera.Follow = player.cameraTarget;
                cinemachineCamera.LookAt = player.cameraTarget;
            }
        }
    }

    public static int ConsistentHash(string input)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        gameOverMenuUI.SetActive(true);
        darkBackground.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void WinGame()
    {
        Time.timeScale = 0f;
        winMenuUI.SetActive(true);
        darkBackground.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        gameOverMenuUI.SetActive(false);

        voxelGrid.ResetWorld();
        if (playerInstance.TryGetComponent(out Player player))
        {
            RespawnPlayer(player);
        }

        InventoryManager.Instance.ClearItems();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        darkBackground.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        darkBackground.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
