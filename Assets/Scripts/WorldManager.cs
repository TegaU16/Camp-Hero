using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldManager : MonoBehaviour
{
    private string selectedWorldName;
    private string selectedSeed;

    [Header("World Selection Menu")]
    public Button playButton;
    public Button deleteButton;
    public Button editButton;
    public Transform worldListParent;
    public GameObject worldButtonPrefab;

    [Header("World Create Menu")]
    public TMP_InputField worldNameInput;
    public TMP_InputField seedInput;
    public Button createWorldButton;
    public GameObject[] difficultyLabels;
    private Difficulty pendingDifficulty = Difficulty.Easy;

    private string WorldsPath => Path.Combine(Application.persistentDataPath, "Worlds");

    // Called by the play button in main menu
    public void Setup()
    {
        LoadWorldList();
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;

        playButton.GetComponent<SelectableImage>().Deselect();
        deleteButton.GetComponent<SelectableImage>().Deselect();
        editButton.GetComponent<SelectableImage>().Deselect();

        pendingDifficulty = DifficultyManager.Instance.GetDifficulty();
        UpdateDifficultyLabels(pendingDifficulty);

        worldNameInput.onValueChanged.AddListener(OnWorldNameChanged);
        OnWorldNameChanged(worldNameInput.text);
    }

    public void CreateWorld()
    {
        string worldName = worldNameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(worldName)) return;

        string seedString = string.IsNullOrWhiteSpace(seedInput.text)
            ? Random.Range(0, int.MaxValue).ToString()
            : seedInput.text.Trim();

        string worldDir = Path.Combine(WorldsPath, worldName);
        if (!Directory.Exists(worldDir))
            Directory.CreateDirectory(Path.Combine(worldDir, "chunks"));

        WorldMetaData metadata = new()
        {
            worldName = worldName,
            seed = seedString,
            createdDate = System.DateTime.Now.ToString(),
            lastPlayedDate = System.DateTime.Now.ToString(),
            difficulty = pendingDifficulty
        };

        string metaPath = Path.Combine(worldDir, "meta.json");
        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));

        selectedWorldName = worldName;
        selectedSeed = seedString;

        WorldSession.CurrentWorldName = selectedWorldName;
        WorldSession.CurrentSeed = selectedSeed;

        UpdateDifficultyLabels(metadata.difficulty);

        SceneManager.LoadScene("GameScene");
    }


    void LoadWorldList()
    {
        foreach (Transform child in worldListParent)
            Destroy(child.gameObject);

        if (!Directory.Exists(WorldsPath)) return;

        List<WorldMetaData> worldList = new();

        foreach (string dir in Directory.GetDirectories(WorldsPath))
        {
            string metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath)) continue;

            string json = File.ReadAllText(metaPath);
            WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

            worldList.Add(metadata);
        }

        // Sort worlds by lastPlayedDate (newest first)
        worldList.Sort((a, b) =>
        {
            System.DateTime dateA = System.DateTime.Parse(a.lastPlayedDate);
            System.DateTime dateB = System.DateTime.Parse(b.lastPlayedDate);
            return dateB.CompareTo(dateA); // newest first
        });

        foreach (WorldMetaData metadata in worldList)
        {
            GameObject buttonObj = Instantiate(worldButtonPrefab, worldListParent);

            Transform worldName = buttonObj.transform.Find("World Name");
            worldName.GetComponent<TextMeshProUGUI>().text = metadata.worldName;

            Transform lastPlayed = buttonObj.transform.Find("Last Played Text");
            lastPlayed.GetComponent<TextMeshProUGUI>().text = metadata.lastPlayedDate;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectWorld(metadata, buttonObj);
            });
        }
    }

    public void SelectWorld(WorldMetaData metadata, GameObject currentButton)
    {
        selectedWorldName = metadata.worldName;
        selectedSeed = metadata.seed;

        DifficultyManager.Instance.SetDifficulty(metadata.difficulty);
        UpdateDifficultyLabels(metadata.difficulty);

        playButton.interactable = true;
        deleteButton.interactable = true;
        editButton.interactable = true;

        playButton.GetComponent<SelectableImage>().Select();
        deleteButton.GetComponent<SelectableImage>().Select();
        editButton.GetComponent<SelectableImage>().Select();

        currentButton.GetComponent<SelectableImage>().Select();

        foreach (Transform child in worldListParent.transform)
        {
            if (child != null && child != currentButton.transform && child.TryGetComponent(out SelectableImage selectable))
            {
                selectable.Deselect();
            }
        }
    }

    public void PlaySelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);
        metadata.lastPlayedDate = System.DateTime.Now.ToString();
        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));

        WorldSession.CurrentWorldName = selectedWorldName;
        WorldSession.CurrentSeed = selectedSeed;

        SceneManager.LoadScene("GameScene");
    }

    public void DeleteSelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        string worldDir = Path.Combine(WorldsPath, selectedWorldName);
        if (Directory.Exists(worldDir))
            Directory.Delete(worldDir, true);

        selectedWorldName = null;
        selectedSeed = null;
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;

        LoadWorldList();
    }

    public void SetDifficulty()
    {
        if (string.IsNullOrEmpty(selectedWorldName))
        {
            // We're in world creation mode
            pendingDifficulty = (Difficulty)(((int)pendingDifficulty + 1) % System.Enum.GetValues(typeof(Difficulty)).Length);
            DifficultyManager.Instance.SetDifficulty(pendingDifficulty);
            UpdateDifficultyLabels(pendingDifficulty);
            return;
        }

        // We're editing an existing world
        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        Difficulty next = (Difficulty)(((int)metadata.difficulty + 1) % System.Enum.GetValues(typeof(Difficulty)).Length);

        metadata.difficulty = next;
        DifficultyManager.Instance.SetDifficulty(next);
        UpdateDifficultyLabels(next);

        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));
    }

    private void UpdateDifficultyLabels(Difficulty difficulty)
    {
        for (int i = 0; i < difficultyLabels.Length; i++)
        {
            difficultyLabels[i].SetActive(i == (int)difficulty);
        }
    }

    private void OnWorldNameChanged(string text)
    {
        bool valid = !string.IsNullOrWhiteSpace(text);
        createWorldButton.interactable = valid;

        if (createWorldButton.TryGetComponent(out SelectableImage selectable))
        {
            if (valid)
                selectable.Select();
            else
                selectable.Deselect();
        }
    }
}
