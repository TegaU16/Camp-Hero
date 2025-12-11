using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Terrain.Structures.Trials
{
    public class TrialMenu : MonoBehaviour
    {
        public static TrialMenu Instance;

        [Header("UI References")]
        public GameObject waveEntryPrefab;
        public Transform waveListContainer;
        public Button startWaveButton;
        public GameObject enemyIconPrefab;

        private TextMeshProUGUI startButtonText;
        private int selectedWave;

        private TrialAltar currentAltar;
        private readonly List<WaveEntry> waveEntries = new();

        private void Awake()
        {
            Instance = this;

            startButtonText = startWaveButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Open(TrialAltar altar)
        {
            currentAltar = altar;
            gameObject.SetActive(true);

            CameraControlToggle.Instance.SetCameraControl(false);

            TryGiveTrialQuest();

            BuildWaveList();
            UpdateButtons();
        }

        private void BuildWaveList()
        {
            foreach (Transform child in waveListContainer)
                Destroy(child.gameObject);

            int totalWaves = currentAltar.waves.Count;

            waveEntries.Clear();

            for (int i = 0; i < totalWaves; i++)
            {
                GameObject entry = Instantiate(waveEntryPrefab, waveListContainer);
                WaveEntry waveEntry = entry.GetComponent<WaveEntry>();
                waveEntries.Add(waveEntry);

                TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
                Transform enemyListContainer = entry.GetComponent<WaveEntry>().enemyListContainer;

                text.text = $"WAVE {i + 1}";

                List<WaveEnemy> waveEnemies = currentAltar.GetEnemiesForWave(i);

                foreach (WaveEnemy waveEnemy in waveEnemies)
                {
                    GameObject iconGO = Instantiate(enemyIconPrefab, enemyListContainer);

                    Image img = iconGO.transform.Find("Icon").GetComponent<Image>();
                    TextMeshProUGUI enemyCountText = iconGO.transform.Find("Enemy Count Text").GetComponent<TextMeshProUGUI>();

                    if (waveEnemy.enemyPrefab != null && waveEnemy.enemyPrefab.TryGetComponent(out Enemy enemyData))
                    {
                        if (enemyData.enemyIcon != null)
                            img.sprite = enemyData.enemyIcon;

                        if (enemyCountText != null)
                            enemyCountText.text = waveEnemy.count.ToString();
                    }
                }
            }

            SelectWave(currentAltar.GetCurrentWave());
        }

        public void SelectWave(int index)
        {
            for (int i = 0; i < waveEntries.Count; i++)
            {
                if (i == index)
                {
                    waveEntries[i].Select();
                    selectedWave = i + 1;
                }
                else
                {
                    waveEntries[i].Deselect();
                }
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
            if (!QuestManager.Instance.hasOpenedTrialMenu)
            {
                QuestManager.Instance.AddQuest(QuestManager.Instance.trialQuest);
                QuestManager.Instance.hasOpenedTrialMenu = true;
                QuestManager.Instance.SaveQuestData();
            }
        }

        public void OnStartWaveButtonPressed()
        {
            currentAltar.StartTrialWave();
            OnClose();
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
            CameraControlToggle.Instance.SetCameraControl(true);
        }
    }
}
