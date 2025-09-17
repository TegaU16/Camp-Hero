using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject campFire;
    public GameObject animalSystemGameObject;

    [HideInInspector] public GameObject playerInstance;
    [HideInInspector] public GameObject campFireInstance;

    [Header("UI")]
    public KeyCode togglePauseKey = KeyCode.Escape;
    public GameObject pauseMenuUI;
    public GameObject gameOverMenuUI;
    public GameObject winMenuUI;
    public GameObject darkBackground;
    [HideInInspector] public bool isPaused = false;

    [Header("Managers")]
    public VoxelGrid voxelGrid;
    public InventoryManager inventoryManager;
    public DayNightCycle dayNightCycle;
    public EnemySpawner enemySpawner;
    public AnimalSpawner animalSpawner;
    public PlayerStatsManager playerStatsManager;
    public CompassBar compassBar;
    public FoodManager foodManager;
    public CameraControlToggle cameraControlToggle;

    [HideInInspector] public string currentWorldName;
    [HideInInspector] public string currentSeed;
    private bool isLoading = false;

    private PlayerSaveData pendingPlayerData;

    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);
    }

    private void Start()
    {
        LoadGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(togglePauseKey))
        {
            if (InventoryManager.Instance.JustClosedExtension)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                InventoryManager.Instance.JustClosedExtension = false;
                return;
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private void OnApplicationQuit() => SaveGame(true);
    private void OnApplicationPause(bool pause) { if (pause) SaveGame(); }

    // ----------------- WORLD -----------------
    private IEnumerator GenerateTerrainPhase()
    {
        WorldMetaData metadata = SaveSystem.LoadWorldMeta(currentWorldName);

        if (metadata != null)
        {
            currentWorldName = metadata.worldName;
            currentSeed = metadata.seed;

            // Update last played timestamp
            metadata.lastPlayedDate = System.DateTime.Now.ToString();
            SaveSystem.SaveWorldMeta(metadata);
        }
        else
        {
            Debug.LogWarning($"[GenerateTerrainPhase] No metadata found for world '{currentWorldName}'. Creating default metadata.");

            // Assign defaults
            if (string.IsNullOrEmpty(currentSeed))
                currentSeed = System.Guid.NewGuid().ToString(); // random but consistent string

            metadata = new WorldMetaData
            {
                worldName = currentWorldName,
                seed = currentSeed,
                createdDate = System.DateTime.Now.ToString(),
                lastPlayedDate = System.DateTime.Now.ToString(),
                difficulty = DifficultyManager.Instance.GetDifficulty()
            };

            SaveSystem.SaveWorldMeta(metadata);
        }

        int seed = ConsistentHash(currentSeed);
        Random.InitState(seed);
        voxelGrid.SetWorld(seed, currentWorldName);

        while (!voxelGrid.worldGenerated) yield return null;
    }

    private IEnumerator SpawnEntitiesPhase(System.Action<float> onProgress)
    {
        float progress;

        // Try load player save first
        PlayerSaveData playerData = SaveSystem.LoadPlayer(currentWorldName);

        if (playerData != null)
            SpawnPlayer(playerData.position, playerData);
        else
            SpawnPlayer(voxelGrid.GetDefaultSpawnPosition());

        progress = 0.5f;
        onProgress?.Invoke(progress);

        compassBar.SetCampfireTransform(campFireInstance);
        animalSystemGameObject.SetActive(true);

        progress = 1f;
        onProgress?.Invoke(progress);
        yield return null;
    }

    private IEnumerator LoadSystemsPhase(System.Action<float> onProgress)
    {
        int totalChunks = voxelGrid.chunks.Count;
        int processed = 0;

        foreach (VoxelChunk chunk in voxelGrid.chunks)
        {
            ChunkSaveData data = SaveSystem.LoadChunk(currentWorldName, chunk.chunkPosition);
            if (data != null)
            {
                chunk.LoadChunkFurnaces(data);
                chunk.LoadChunkStorages(data);
            }

            processed++;
            onProgress?.Invoke((float)processed / totalChunks);
            yield return null;
        }

        // Enemy & animal spawns (count as final part of systems)
        enemySpawner.LoadAllEnemies();
        animalSpawner.LoadAllAnimals();
        onProgress?.Invoke(1f);
    }

    // ----------------- PLAYER -----------------
    private void SpawnPlayer(Vector3 position, PlayerSaveData data = null)
    {
        Vector3 rayStart = position + Vector3.up * 10f;
        Vector3 spawnPos = position;

        LayerMask groundMask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, groundMask))
            spawnPos = hit.point + Vector3.up * 0.2f;
        else
        {
            Debug.LogWarning("[SpawnPlayer] Failed to find ground under spawn position. Using default Y=3.");
            spawnPos.y = 3f;
        }

        playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        if (!playerInstance.TryGetComponent(out Player _)) return;

        HookSystems(playerInstance);

        if (data != null)
            pendingPlayerData = data;
    }

    private void OnUIBound()
    {
        if (pendingPlayerData != null && playerInstance.TryGetComponent(out Player player))
        {
            ApplyPlayerLoadedData(player, pendingPlayerData);
            pendingPlayerData = null;
        }
    }

    private static void ApplyPlayerLoadedData(Player player, PlayerSaveData data)
    {
        PlayerStats stats = PlayerStatsManager.Instance.stats;
        stats.strength.Value = data.attributes.strength;
        stats.vitality.Value = data.attributes.vitality;
        stats.endurance.Value = data.attributes.endurance;
        stats.stamina.Value = data.attributes.stamina;
        stats.luck.Value = data.attributes.luck;

        player.health.maxHealth = data.maxHealth;
        player.health.SetHealth(data.currentHealth);

        player.staminaBar.maxStamina = data.maxStamina;
        player.staminaBar.SetCurrentStamina((int)data.currentStamina);

        player.transform.position = data.position;

        InventoryManager.Instance.AddSavedPlayerItems(data.inventory);
        InventoryManager.Instance.EquipSelectedItem();

        PlayerStatsManager.Instance.stats.availablePoints = data.availablePoints;
        PlayerStatsManager.Instance.UpdateAvailablePoints();

        LevelManager.Instance.SetLevelData(data.levelData);

        player.UpdateVitals();
    }

    private void BindPlayerUI(Player player)
    {
        if (player != null)
        {
            player.BindUI(
                UIManager.Instance.GetHealthBar("Player"),
                UIManager.Instance.GetStaminaBar()
            );

            if (player.healthBar != null)
                player.healthBar.Initialize(player.health.maxHealth, player.health.GetHealth());
            if (player.staminaBar != null)
                player.staminaBar.Initialize(player.playerAttributes.MaxStamina, (int)player.staminaBar.GetStamina());

            player.UpdateVitals();
        }
    }

    public void RespawnPlayer(Player player)
    {
        StartCoroutine(RespawnPlayerCoroutine(player));
    }

    private IEnumerator RespawnPlayerCoroutine(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("[Respawn] Player instance is null!");
            yield break;
        }

        // Wait until the terrain is generated
        while (!voxelGrid.worldGenerated)
            yield return null;

        if (campFireInstance == null)
        {
            Debug.LogWarning("[Respawn] No campfire found to respawn at.");
            yield break;
        }

        Vector3 campfirePosition = campFireInstance.transform.position;
        Vector3 respawnOffset = new(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        Vector3 respawnPosition = campfirePosition + respawnOffset;

        if (player.TryGetComponent(out CharacterController controller))
        {
            controller.enabled = false;
            player.transform.position = respawnPosition;
            controller.enabled = true;
        }
        else
        {
            player.transform.position = respawnPosition;
        }

        player.health.SetHealth(player.health.maxHealth);
        player.staminaBar.SetNewStamina((int)player.staminaBar.maxStamina);

        BindPlayerUI(player);
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

        CinemachineCamera cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera != null && playerInstance.TryGetComponent(out Player player))
        {
            cinemachineCamera.Follow = player.cameraTarget;
            cinemachineCamera.LookAt = player.cameraTarget;
        }
    }

    // ----------------- CAMPFIRE -----------------
    public void SetCampfire(GameObject campfire) => campFireInstance = campfire;

    private void BindCampfireUI(Campfire campfire)
    {
        if (campfire != null)
            campfire.health.healthBar = UIManager.Instance.GetHealthBar("Campfire");

        if (campfire != null)
        {
            CampfireSaveData campfireData = SaveSystem.LoadCampfire(currentWorldName);
            int campfireHealth = campfireData != null ? campfireData.currentHealth : campfire.health.maxHealth;

            if (campfire.health.healthBar != null)
                campfire.health.healthBar.Initialize(campfire.health.maxHealth, campfireHealth);

            if (campfireData != null)
                campfire.LoadFromSaveData(campfireData);
            else
                campfire.LoadDefault();
        }
    }

    // ----------------- SAVE / LOAD -----------------
    public void SaveGame(bool exit = false)
    {
        if (string.IsNullOrEmpty(currentWorldName)) return;
        if (playerInstance == null) return;

        if (playerInstance.TryGetComponent(out Player player))
            player.SavePlayer();

        voxelGrid.SaveAllChunks(currentWorldName);

        TrialAltar[] trialAltars = FindObjectsByType<TrialAltar>(FindObjectsSortMode.None);
        foreach (TrialAltar trialAltar in trialAltars)
            trialAltar.SaveAltarState();

        GemAltar[] gemAltars = FindObjectsByType<GemAltar>(FindObjectsSortMode.None);
        foreach (GemAltar gemAltar in gemAltars)
            gemAltar.SaveAltarState();

        WorldMetaData metadata = SaveSystem.LoadWorldMeta(currentWorldName);
        if (metadata != null)
        {
            metadata.lastPlayedDate = System.DateTime.Now.ToString();
            SaveSystem.SaveWorldMeta(metadata);
        }

        CraftingManager.Instance.SaveCraftingProgress();
        FurnaceManager.Instance.SaveSmeltingProgress();

        dayNightCycle.SaveDayNight();

        Campfire campfire = FindFirstObjectByType<Campfire>();
        if (campfire != null)
            SaveSystem.SaveCampfire(currentWorldName, campfire.GetSaveData());

        if (exit)
        {
            foreach (TrialAltar trialAltar in KeyStructureSpawner.Instance.activeTrialAltars.ToList())
            {
                if (trialAltar != null && trialAltar.IsWaveInProgress())
                    trialAltar.FailTrial();
            }
        }

        enemySpawner.SaveAllEnemies();
        animalSpawner.SaveAllAnimals();
    }

    public void LoadGame()
    {
        if (isLoading) return;
        isLoading = true;

        currentWorldName = WorldSession.CurrentWorldName;
        currentSeed = WorldSession.CurrentSeed;

        if (string.IsNullOrEmpty(currentWorldName))
        {
            currentWorldName = "New World";
            currentSeed = "12345";
        }

        // Start full load routine
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        LoadingScreenUI.Instance.Show();

        // 1. Wait until UIManager and bars are ready
        yield return StartCoroutine(WaitForUI());
        LoadingScreenUI.Instance.SetProgress(0f);

        // 2. Terrain generation (0%–70%)
        voxelGrid.OnProgress = p => LoadingScreenUI.Instance.SetProgressRange(p, 0f, 0.7f);
        yield return StartCoroutine(GenerateTerrainPhase());
        voxelGrid.OnProgress = null;

        // 3. Spawning (70%–80%)
        yield return StartCoroutine(SpawnEntitiesPhase(p => LoadingScreenUI.Instance.SetProgressRange(p, 0.7f, 0.8f)));

        // 4. Bind UI (80%–85%)
        yield return StartCoroutine(BindUIAndApplySaves());

        // 5. Systems (85%–95%)
        yield return StartCoroutine(LoadSystemsPhase(p => LoadingScreenUI.Instance.SetProgressRange(p, 0.85f, 0.95f)));

        // 6. Finalization (95%–100%)
        LoadingScreenUI.Instance.SetProgress(1f);

        LoadingScreenUI.Instance.Hide();
    }

    private IEnumerator BindUIAndApplySaves()
    {
        // Wait until player & campfire exist
        float waitTime = 0f;
        while (playerInstance == null || campFireInstance == null)
        {
            if (waitTime > 5f) // 5 seconds fallback
            {
                Debug.LogWarning("[BindUIAndApplySaves] Timeout waiting for player/campfire.");
                break;
            }
            waitTime += Time.deltaTime;
            yield return null;
        }

        Player player = playerInstance.GetComponent<Player>();
        Campfire campfire = campFireInstance.GetComponent<Campfire>();

        // --- Bind UI Bars ---
        BindPlayerUI(player);
        BindCampfireUI(campfire);

        // --- Load systems that depend on world/player ---
        CraftingManager.Instance.LoadCraftingProgress();
        FurnaceManager.Instance.LoadSmeltingProgress();
        dayNightCycle.LoadDayNight();

        OnUIBound();
    }


    private IEnumerator WaitForUI()
    {
        while (!UIManager.Instance.IsInitialized)
        {
            yield return null;
        }
    }

    // ----------------- UTILS -----------------
    public void ToggleCameraFollow(bool enabled) 
    { 
        if (cameraControlToggle != null) 
            cameraControlToggle.SetCameraControl(enabled); 
    }

    public static int ConsistentHash(string input)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in input) hash = hash * 31 + c;
            return hash;
        }
    }

    // ----------------- UI -----------------
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
        SaveGame(true);
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
