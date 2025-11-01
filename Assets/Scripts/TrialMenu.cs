using System.Collections.Generic;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

public class TrialMenu : MonoBehaviour
{
    public static TrialMenu Instance;

    [Header("UI References")]
    public GameObject waveEntryPrefab;
    public Transform waveListContainer;
    public Button startWaveButton;
    public GameObject enemyIconPrefab;
    public Sprite waveSelectedImage, waveUnselectedImage;

    private SVGImage startButtonImage;
    private TextMeshProUGUI startButtonText;
    private int selectedWave;

    private TrialAltar currentAltar;
    private readonly List<WaveEntry> waveEntries = new();

    private void Awake()
    {
        Instance = this;

        startButtonImage = startWaveButton.GetComponent<SVGImage>();
        startButtonText = startWaveButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Open(TrialAltar altar)
    {
        currentAltar = altar;
        gameObject.SetActive(true);

        GameManager.Instance.ToggleCameraFollow(false);
        BuildWaveList();
        UpdateButtons();
    }

    private void BuildWaveList()
    {
        foreach (Transform child in waveListContainer)
            Destroy(child.gameObject);

        int[] enemyCounts = currentAltar.enemiesPerWave;
        int totalWaves = currentAltar.enemyWavePrefabs.Length;

        waveEntries.Clear();

        for (int i = 0; i < totalWaves; i++)
        {
            GameObject entry = Instantiate(waveEntryPrefab, waveListContainer);
            WaveEntry waveEntry = entry.GetComponent<WaveEntry>();
            waveEntries.Add(waveEntry);

            TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
            Transform enemyListContainer = entry.GetComponent<WaveEntry>().enemyListContainer;

            int count = (i < enemyCounts.Length) ? enemyCounts[i] : 3;
            text.text = $"WAVE {i + 1}";

            for (int j = 0; j < count; j++)
            {
                GameObject iconGO = Instantiate(enemyIconPrefab, enemyListContainer);

                SVGImage img = iconGO.GetComponent<SVGImage>();
                Enemy enemyData = currentAltar.GetEnemyForWave(i);

                if (enemyData != null && enemyData.enemyIcon != null)
                    img.sprite = enemyData.enemyIcon;
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

    public bool IsWaveSelected()
    {
        foreach (WaveEntry entry in waveEntries)
        {
            if (entry != null && entry.IsSelected()) return true;
        }

        return false;
    }

    private void UpdateButtons()
    {
        startWaveButton.interactable = !currentAltar.IsWaveInProgress() && !currentAltar.IsCompleted();

        if (IsWaveSelected())
        {
            startButtonImage.sprite = waveSelectedImage;
            startButtonText.text = $"Start Wave {selectedWave}";
            startButtonText.color = Color.white;
        }
        else
        {
            startButtonImage.sprite = waveUnselectedImage;
            startButtonText.text = "Select Wave";
            startButtonText.color = Color.black;
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
        GameManager.Instance.ToggleCameraFollow(true);
    }
}
