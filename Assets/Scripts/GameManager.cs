using System.Collections;
using System.Linq;
using Game.AI.Animals;
using Game.AI.Enemies;
using Game.Crafting;
using Game.Food;
using Game.Inventory;
using Game.Level;
using Game.Players;
using Game.Quests;
using Game.Saving;
using Game.Smelting;
using Game.Terrain;
using Game.Terrain.Structures;
using Game.Terrain.Structures.Trials;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using Worlds;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);
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
        public GameObject darkBackground;
        public GameObject savingWorldMenuUI;
        public GameObject settingsMenuUI;

        [Header("References")]
        public CompassBar compassBar;

        public bool IsLoading { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }

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
            if (IsGameOver) return;

            if (Input.GetKeyDown(togglePauseKey))
            {
                if (InventoryManager.Instance.JustClosedExtension)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    InventoryManager.Instance.JustClosedExtension = false;
                    return;
                }

                if (IsPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        private void OnApplicationQuit() => SaveGame(true);
        private void OnApplicationPause(bool pause) { if (pause) SaveGame(); }

        // ----------------- PLAYER -----------------
        public void SpawnPlayer(Vector3 position, PlayerSaveData data = null)
        {
            Vector3 rayStart = position + Vector3.up * 100f;
            Vector3 spawnPos = position;

            LayerMask groundMask = LayerMask.GetMask("Ground");
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, groundMask))
                spawnPos = hit.point + Vector3.up * 0.2f;
            else
                spawnPos.y = 3f;

            playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            if (!playerInstance.TryGetComponent(out Player _)) return;

            HookSystems(playerInstance);

            if (data != null)
                pendingPlayerData = data;
        }

        public void OnUIBound()
        {
            if (pendingPlayerData != null && playerInstance.TryGetComponent(out Player player))
            {
                ApplyPlayerLoadedData(player, pendingPlayerData);
                pendingPlayerData = null;
            }
        }

        private void ApplyPlayerLoadedData(Player player, PlayerSaveData data)
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
            player.staminaBar.SetNewStamina((int)data.currentStamina);

            player.transform.position = data.position;

            InventoryManager.Instance.AddSavedPlayerItems(data.inventory.savedItems);
            InventoryManager.Instance.LoadDiscoveredItems(data.inventory.discoveredItems);
            InventoryManager.Instance.EquipSelectedItem();

            PlayerStatsManager.Instance.stats.availablePoints = data.attributes.availablePoints;
            PlayerStatsManager.Instance.stats.goldenPoints = data.attributes.goldenPoints;
            PlayerStatsManager.Instance.UpdateAvailablePoints();
            PlayerStatsManager.Instance.LoadPlayerUpgrades(data.attributes.unlockedUpgrades);

            foreach (PlayerStats.Stat stat in PlayerStatsManager.Instance.AllStats)
                PlayerStatsManager.Instance.UpdateStatOuterUI(stat);

            LevelManager.Instance.SetLevelData(data.levelData);

            player.UpdateVitals();
        }

        public void BindPlayerUI(Player player)
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

        public IEnumerator RespawnPlayer(Player player)
        {
            if (player == null)
            {
                Debug.LogWarning("[Respawn] Player instance is null!");
                yield break;
            }

            // Wait until the terrain is generated
            while (!VoxelGrid.Instance.worldGenerated)
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
            InventoryManager.Instance.SetPlayer(playerInstance);
            DayNightCycle.Instance.SetPlayer(playerInstance);
            EnemySpawner.Instance.SetPlayer(playerInstance);
            PlayerStatsManager.Instance.SetPlayer(playerInstance);
            FoodManager.Instance.SetPlayer(playerInstance);
            InteractableItemManager.Instance.SetPlayer(playerInstance);
            VoxelGrid.Instance.SetPlayer(playerInstance);

            CinemachineCamera cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera != null && playerInstance.TryGetComponent(out Player player))
            {
                if (player.cameraTarget == null)
                {
                    Debug.LogWarning("Camera target not yet initialized");
                    return;
                }

                cinemachineCamera.Follow = player.cameraTarget;
                cinemachineCamera.LookAt = player.cameraTarget;
            }
        }

        // ----------------- CAMPFIRE -----------------
        public void SetCampfire(GameObject campfireObj, CampfireSaveData campfireData = null)
        {
            campFireInstance = campfireObj;

            if (campFireInstance.TryGetComponent(out Campfire campfire))
            {
                campfire.health.healthBar = UIManager.Instance.GetHealthBar("Campfire");

                int campfireHealth = campfireData != null ? campfireData.currentHealth : campfire.health.maxHealth;

                if (campfire.health.healthBar != null)
                    campfire.health.healthBar.Initialize(campfire.health.maxHealth, campfireHealth);
            }
        }

        // ----------------- SAVE / LOAD -----------------
        public void SaveGame(bool exit = false)
        {
            if (string.IsNullOrEmpty(WorldSession.CurrentWorldName)) return;
            if (playerInstance == null) return;

            if (playerInstance.TryGetComponent(out Player player))
                player.SavePlayer();

            VoxelGrid.Instance.SaveAllChunks(WorldSession.CurrentWorldName);

            QuestManager.Instance.SaveQuestData();

            CraftingManager.Instance.SaveCraftingProgress();
            FurnaceManager.Instance.SaveSmeltingProgress();

            DayNightCycle.Instance.SaveDayNight();

            if (exit)
            {
                foreach (TrialAltar trialAltar in KeyStructureSpawner.Instance.activeTrialAltars.ToList())
                {
                    if (trialAltar != null && trialAltar.IsWaveInProgress())
                        trialAltar.FailTrial();
                }
            }

            EnemySpawner.Instance.SaveAllEnemies();
            AnimalSpawner.Instance.SaveAllAnimals();

            WorldMetaData metadata = SaveSystem.LoadWorldMeta(WorldSession.CurrentWorldName);
            if (metadata != null)
            {
                metadata.lastPlayedDate = System.DateTime.Now.ToString();
                SaveSystem.SaveWorldMeta(metadata);
            }
        }

        public void LoadGame()
        {
            if (IsLoading) return;
            IsLoading = true;

            if (string.IsNullOrEmpty(WorldSession.CurrentWorldName))
            {
                WorldSession.CurrentWorldName = "New World";
                WorldSession.CurrentSeed = "12345";
            }

            WorldSession.CurrentRunStats ??= new RunStats();

            // Start full load routine
            StartCoroutine(LoadGameRoutine());
        }

        private IEnumerator LoadGameRoutine()
        {
            // 1. Wait until UIManager and bars are ready
            yield return StartCoroutine(WaitForUI());

            LoadingScreenUI.Instance.SetProgress(0f);

            // 2. Terrain generation
            VoxelGrid.Instance.OnProgress = p =>
            {
                LoadingScreenUI.Instance.SetProgressRange(p, 0f, 0.7f);
            };
            yield return StartCoroutine(GenerateTerrainPhase());
            VoxelGrid.Instance.OnProgress = null;

            // 3. Spawning
            yield return StartCoroutine(SpawnEntitiesPhase(p =>
            {
                LoadingScreenUI.Instance.SetProgressRange(p, 0.7f, 0.8f);
            }));

            // 4. Bind UI
            yield return StartCoroutine(BindUIAndApplySaves());

            // 5. NPC loading
            yield return StartCoroutine(LoadNPCsPhase(p =>
            {
                LoadingScreenUI.Instance.SetProgressRange(p, 0.85f, 0.95f);
            }));

            // 6. Finalization
            LoadingScreenUI.Instance.SetProgress(1f);

            yield return null;
            yield return _waitForSeconds0_1;

            IsLoading = false;
            LoadingScreenUI.Instance.Hide();
        }

        private IEnumerator GenerateTerrainPhase()
        {
            string worldName = WorldSession.CurrentWorldName;
            string worldSeed = WorldSession.CurrentSeed;

            WorldMetaData meta = SaveSystem.LoadWorldMeta(worldName);

            if (meta != null)
            {
                worldName = meta.worldName;
                worldSeed = meta.seed;
                meta.lastPlayedDate = System.DateTime.Now.ToString();
                meta.worldStats ??= new RunStats();
                WorldSession.CurrentRunStats = meta.worldStats;
                SaveSystem.SaveWorldMeta(meta);
            }
            else
            {
                Debug.LogWarning("[GenerateTerrainPhase] Metadata not found. Creating new.");

                if (string.IsNullOrEmpty(worldSeed))
                    worldSeed = System.Guid.NewGuid().ToString();

                meta = new WorldMetaData
                {
                    worldName = worldName,
                    seed = worldSeed,
                    createdDate = System.DateTime.Now.ToString(),
                    lastPlayedDate = System.DateTime.Now.ToString(),
                    difficulty = DifficultyManager.Instance.GetDifficulty(),
                    worldStats = new RunStats()
                };

                SaveSystem.SaveWorldMeta(meta);

                WorldSession.CurrentRunStats = new RunStats();
                WorldSession.CurrentRunStats.ResetStats();
            }

            int seed = Utility.ConsistentHash(worldSeed);
            Random.InitState(seed);

            VoxelGrid.Instance.SetWorld(seed, worldName);

            while (!VoxelGrid.Instance.worldGenerated)
                yield return null;
        }

        private IEnumerator SpawnEntitiesPhase(System.Action<float> onProgress)
        {
            PlayerSaveData data = SaveSystem.LoadPlayer(WorldSession.CurrentWorldName);

            if (data != null)
                SpawnPlayer(data.position, data);
            else
                SpawnPlayer(VoxelGrid.Instance.GetDefaultSpawnPosition());

            onProgress(0.5f);

            compassBar.SetCampfireTransform(campFireInstance);
            animalSystemGameObject.SetActive(true);

            onProgress(1f);
            yield return null;
        }

        private IEnumerator BindUIAndApplySaves()
        {
            float timer = 0f;
            while (playerInstance == null || campFireInstance == null)
            {
                if (timer > 5f)
                {
                    Debug.LogWarning("[BindUIAndApplySaves] Timeout waiting for objects.");
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            BindPlayerUI(playerInstance.GetComponent<Player>());

            QuestManager.Instance.LoadQuestData();
            CraftingManager.Instance.LoadCraftingProgress();
            FurnaceManager.Instance.LoadSmeltingProgress();
            DayNightCycle.Instance.LoadDayNight();

            OnUIBound();
        }

        private IEnumerator LoadNPCsPhase(System.Action<float> onProgress)
        {
            yield return new WaitUntil(() => VoxelGrid.Instance.worldGenerated);

            EnemySpawner.Instance.LoadAllEnemies();
            AnimalSpawner.Instance.LoadAllAnimals();

            onProgress(1f);
            yield return null;
        }

        private IEnumerator WaitForUI()
        {
            while (!UIManager.Instance.IsInitialized)
                yield return null;
        }

        // ----------------- UI -----------------
        public void GameOver(bool win)
        {
            Time.timeScale = 0f;

            WorldSession.CurrentRunStats.daysSurvived = DayNightCycle.Instance.GetElapsedTime();
            WorldSession.CurrentRunStats.totalExpGained = LevelManager.Instance.GetTotalExp();

            GameOverUI.Instance.DisplayStats(WorldSession.CurrentRunStats);

            gameOverMenuUI.SetActive(true);
            darkBackground.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Button[] buttonsInScene = FindObjectsByType<Button>(FindObjectsSortMode.None);
            Utility.DisableButtonsOutside(gameOverMenuUI.transform, buttonsInScene, true);

            IsGameOver = true;
            if (win)
                UpdateWorldState(WorldState.Won);
            else
                UpdateWorldState(WorldState.Failed);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SaveGame(true);

            StartCoroutine(ShowSavingWorldMenu(3f));
        }

        private IEnumerator ShowSavingWorldMenu(float duration)
        {
            if (savingWorldMenuUI != null)
                savingWorldMenuUI.SetActive(true);

            yield return new WaitForSeconds(duration);

            SceneLoader.Instance.LoadScene("MainMenuScene");
        }

        public void PauseGame()
        {
            pauseMenuUI.SetActive(true);
            darkBackground.SetActive(true);
            Time.timeScale = 0f;
            IsPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            pauseMenuUI.SetActive(false);
            darkBackground.SetActive(false);
            Time.timeScale = 1f;
            IsPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            SceneLoader.Instance.LoadScene("MainMenuScene");
        }

        private void UpdateWorldState(WorldState newState)
        {
            if (string.IsNullOrEmpty(WorldSession.CurrentWorldName)) return;

            WorldMetaData meta = SaveSystem.LoadWorldMeta(WorldSession.CurrentWorldName);
            if (meta != null)
            {
                meta.worldState = newState;
                meta.lastPlayedDate = System.DateTime.Now.ToString();
                SaveSystem.SaveWorldMeta(meta);
            }
        }

        public bool IsGameManagerReady()
        {
            return !IsGameOver && !IsPaused && !IsLoading;
        }

        public void OpenSettingsMenu()
        {
            settingsMenuUI.SetActive(true);
        }
    }
}
