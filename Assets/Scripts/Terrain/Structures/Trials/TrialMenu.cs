using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Terrain.Structures.Trials
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class TrialMenu : MonoBehaviour
    {
        public static TrialMenu Instance;

        [Header("UI References")]
        [SerializeField] private GameObject waveEntryPrefab;
        [SerializeField] private Transform waveListContainer;
        [SerializeField] private Button startWaveButton;
        [SerializeField] private GameObject enemyIconPrefab;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private TextMeshProUGUI startButtonText;
        private int selectedWave;

        private TrialAltar currentAltar;
        private readonly List<WaveEntry> waveEntries = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            startButtonText = startWaveButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Open(TrialAltar altar)
        {
            currentAltar = altar;

            CameraControlToggle.Instance.SetCameraControl(false);

            TryGiveTrialQuest();
            BuildWaveList();
            UpdateButtons();

            UITween.DefaultOpenMenu(canvasGroup, rectTransform);
        }

        private void BuildWaveList()
        {
            foreach (Transform child in waveListContainer)
                Destroy(child.gameObject);

            int totalWaves = currentAltar.GetNumberOfWaves();

            waveEntries.Clear();

            for (int i = 0; i < totalWaves; i++)
            {
                GameObject entry = Instantiate(waveEntryPrefab, waveListContainer);
                WaveEntry waveEntry = entry.GetComponent<WaveEntry>();
                waveEntries.Add(waveEntry);

                TMP_Text waveText = entry.GetComponentInChildren<TMP_Text>();
                Transform enemyListContainer = entry.GetComponent<WaveEntry>().enemyListContainer;

                waveText.text = $"WAVE {i + 1}";

                List<WaveEnemy> waveEnemies = currentAltar.GetEnemiesForWave(i);

                foreach (WaveEnemy waveEnemy in waveEnemies)
                {
                    GameObject iconGO = Instantiate(enemyIconPrefab, enemyListContainer);

                    Transform iconTransform = iconGO.transform.Find("Icon");
                    if (iconTransform == null)
                    {
                        Debug.LogWarning("Missing Icon child");
                        continue;
                    }

                    Image icon = iconTransform.GetComponent<Image>();
                    TextMeshProUGUI enemyCountText = iconGO.transform.Find("Enemy Count Text").GetComponent<TextMeshProUGUI>();

                    if (waveEnemy.enemyPrefab == null || !waveEnemy.enemyPrefab.TryGetComponent(out EnemyCombat enemyCombat)) continue;

                    if (enemyCombat.enemyIcon == null)
                        Debug.LogWarning($"Missing icon for enemy: {enemyCombat.name}", enemyCombat);

                    icon.sprite = enemyCombat.enemyIcon;

                    if (enemyCountText != null)
                        enemyCountText.text = waveEnemy.count.ToString();
                }
            }

            SelectWave(currentAltar.GetCurrentWave());
        }

        public void SelectWave(int index)
        {
            for (int i = 0; i < waveEntries.Count; i++)
            {
                if (i != index)
                {
                    waveEntries[i].Deselect();
                    continue;
                }

                waveEntries[i].Select();
                selectedWave = i + 1;
            }

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            startWaveButton.interactable = !currentAltar.IsWaveInProgress() && !currentAltar.IsCompleted();

            if (startWaveButton.interactable)
                startWaveButton.GetComponent<InteractiveButton>().Select();
            else
                startWaveButton.GetComponent<InteractiveButton>().Deselect();

            startButtonText.text = $"Start Wave {selectedWave}";
        }

        private void TryGiveTrialQuest()
        {
            if (QuestManager.Instance.hasOpenedTrialMenu) return;

            QuestManager.Instance.AddQuest(QuestManager.Instance.trialQuest);
            QuestManager.Instance.hasOpenedTrialMenu = true;
            QuestManager.Instance.SaveQuestData();
        }

        // Called by start wave button
        public void OnStartWaveButtonPressed()
        {
            currentAltar.StartTrialWave();
            OnClose();
        }

        // Called by close button
        public void OnClose()
        {
            gameObject.SetActive(false);
            CameraControlToggle.Instance.SetCameraControl(true);
        }
    }
}
