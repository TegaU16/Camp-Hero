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
    public GameObject retryConfirmMenu;

    [Header("World Create Menu")]
    public TMP_InputField worldNameInput;
    public TMP_InputField seedInput;
    public Button createWorldButton;
    public GameObject[] difficultyLabels;
    private Difficulty pendingDifficulty = Difficulty.Easy;

    [Header("World Edit Menu")]
    public TMP_InputField worldNameEditInput;
    public GameObject[] difficultyLabelsEdit;
    private Difficulty editingDifficulty;

    private string WorldsPath => Path.Combine(Application.persistentDataPath, "Worlds");

    // Called by the play button in main menu
    public void Setup()
    {
        LoadWorldList();
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;

        playButton.GetComponent<InteractiveButton>().Deselect();
        deleteButton.GetComponent<InteractiveButton>().Deselect();
        editButton.GetComponent<InteractiveButton>().Deselect();

        pendingDifficulty = DifficultyManager.Instance.GetDifficulty();
        UpdateDifficultyLabels(pendingDifficulty);

        worldNameInput.onValueChanged.AddListener(OnWorldNameChanged);
        OnWorldNameChanged(worldNameInput.text);
    }

    public void CreateWorld()
    {
        string baseName = worldNameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(baseName)) return;

        // Ensure unique world name
        string worldName = GetUniqueWorldName(baseName);

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

        LoadingScreenUI.Instance.Show();

        SceneLoader.Instance.LoadScene("GameScene");
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

            GameObject activeIcon = buttonObj.transform.Find("Active World Icon").gameObject;
            GameObject failedIcon = buttonObj.transform.Find("Failed World Icon").gameObject;
            GameObject wonIcon = buttonObj.transform.Find("Won World Icon").gameObject;

            activeIcon.SetActive(metadata.worldState == WorldState.Active);
            failedIcon.SetActive(metadata.worldState == WorldState.Failed);
            wonIcon.SetActive(metadata.worldState == WorldState.Won);

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

        UpdateDifficultyLabels(metadata.difficulty);

        playButton.interactable = true;
        deleteButton.interactable = true;
        editButton.interactable = true;

        playButton.GetComponent<InteractiveButton>().Select();
        deleteButton.GetComponent<InteractiveButton>().Select();
        editButton.GetComponent<InteractiveButton>().Select();

        currentButton.GetComponent<InteractiveButton>().Select();

        foreach (Transform child in worldListParent.transform)
        {
            if (child != null && child != currentButton.transform && child.TryGetComponent(out InteractiveButton interactiveButton))
                interactiveButton.Deselect();
        }
    }

    public void PlaySelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        if (metadata.worldState == WorldState.Failed)
        {
            ToggleRetryConfirm(true);
            return;
        }

        metadata.lastPlayedDate = System.DateTime.Now.ToString();
        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));

        WorldSession.CurrentWorldName = selectedWorldName;
        WorldSession.CurrentSeed = selectedSeed;

        LoadingScreenUI.Instance.Show();

        SceneLoader.Instance.LoadScene("GameScene");
    }

    public void DeleteSelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        SaveSystem.DeleteWorldMeta(selectedWorldName);

        selectedWorldName = null;
        selectedSeed = null;

        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;

        playButton.GetComponent<InteractiveButton>().Deselect();
        deleteButton.GetComponent<InteractiveButton>().Deselect();
        editButton.GetComponent<InteractiveButton>().Deselect();

        LoadWorldList();
    }

    public void ToggleRetryConfirm(bool open)
    {
        retryConfirmMenu.SetActive(open);
        Button[] buttonsInScene = FindObjectsByType<Button>(FindObjectsSortMode.None);

        Utility.DisableButtonsOutside(retryConfirmMenu.transform, buttonsInScene, open);
    }

    public void RetryFailedWorld()
    {
        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        string currentWorldName = metadata.worldName;
        string currentSeed = metadata.seed;

        SaveSystem.DeleteWorldMeta(currentWorldName); // delete old folder

        // Recreate metadata after deletion
        WorldMetaData newMeta = new()
        {
            worldName = currentWorldName,
            seed = currentSeed,
            createdDate = System.DateTime.Now.ToString(),
            lastPlayedDate = System.DateTime.Now.ToString(),
            difficulty = DifficultyManager.Instance.GetDifficulty(),
            worldState = WorldState.Active
        };
        SaveSystem.SaveWorldMeta(newMeta);

        newMeta.lastPlayedDate = System.DateTime.Now.ToString();
        File.WriteAllText(metaPath, JsonUtility.ToJson(newMeta, true));

        WorldSession.CurrentWorldName = selectedWorldName;
        WorldSession.CurrentSeed = selectedSeed;

        LoadingScreenUI.Instance.Show();

        SceneLoader.Instance.LoadScene("GameScene");
    }

    public void SetDifficulty(bool right)
    {
        int cycleStep = right ? 1 : -1;
        int length = System.Enum.GetValues(typeof(Difficulty)).Length;
        int nextIndex = ((int)pendingDifficulty + cycleStep + length) % length;

        if (string.IsNullOrEmpty(selectedWorldName))
        {
            // We're in world creation mode
            pendingDifficulty = (Difficulty)nextIndex;
            UpdateDifficultyLabels(pendingDifficulty);
            return;
        }

        // We're editing an existing world
        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        Difficulty next = (Difficulty)nextIndex;
        metadata.difficulty = next;
        UpdateDifficultyLabels(next);

        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));
    }

    private void UpdateDifficultyLabels(Difficulty difficulty)
    {
        DifficultyManager.Instance.SetDifficulty(difficulty);

        for (int i = 0; i < difficultyLabels.Length; i++)
            difficultyLabels[i].SetActive(i == (int)difficulty);
    }

    private void OnWorldNameChanged(string text)
    {
        bool valid = !string.IsNullOrWhiteSpace(text);
        createWorldButton.interactable = valid;

        if (createWorldButton.TryGetComponent(out InteractiveButton interactiveButton))
        {
            if (valid)
                interactiveButton.Select();
            else
                interactiveButton.Deselect();
        }
    }

    public void OpenEditWorldMenu()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        string metaPath = Path.Combine(WorldsPath, selectedWorldName, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        worldNameEditInput.text = metadata.worldName;
        editingDifficulty = metadata.difficulty;
        UpdateEditDifficultyLabels(editingDifficulty);
    }

    public void CycleEditDifficulty(bool right)
    {
        int cycleStep = right ? 1 : -1;
        int length = System.Enum.GetValues(typeof(Difficulty)).Length;

        int nextIndex = ((int)editingDifficulty + cycleStep + length) % length;
        editingDifficulty = (Difficulty)nextIndex;

        UpdateEditDifficultyLabels(editingDifficulty);
    }

    private void UpdateEditDifficultyLabels(Difficulty difficulty)
    {
        for (int i = 0; i < difficultyLabelsEdit.Length; i++)
            difficultyLabelsEdit[i].SetActive(i == (int)difficulty);
    }

    public void SaveEditedWorld()
    {
        string oldName = selectedWorldName;
        string newName = worldNameEditInput.text.Trim();

        if (string.IsNullOrWhiteSpace(newName)) return;

        string oldWorldDir = Path.Combine(WorldsPath, oldName);
        if (!Directory.Exists(oldWorldDir)) return;

        // Ensure unique name if changed
        if (oldName != newName)
            newName = GetUniqueWorldName(newName);

        string newWorldDir = Path.Combine(WorldsPath, newName);

        // If name changed, rename folder
        if (oldName != newName)
            Directory.Move(oldWorldDir, newWorldDir);

        string metaPath = Path.Combine(newWorldDir, "meta.json");
        if (!File.Exists(metaPath)) return;

        string json = File.ReadAllText(metaPath);
        WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

        metadata.worldName = newName;
        metadata.difficulty = editingDifficulty;
        metadata.lastPlayedDate = System.DateTime.Now.ToString();

        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));

        // Refresh state
        selectedWorldName = newName;
        UpdateDifficultyLabels(editingDifficulty);

        LoadWorldList();
    }

    private string GetUniqueWorldName(string baseName)
    {
        if (!Directory.Exists(WorldsPath))
            Directory.CreateDirectory(WorldsPath);

        string worldDir = Path.Combine(WorldsPath, baseName);

        // If no conflict, we’re done
        if (!Directory.Exists(worldDir)) return baseName;

        // If conflict, append suffixes incrementally until unique
        int counter = 1;
        string newName;
        do
        {
            newName = $"{baseName}_{counter:D2}";
            worldDir = Path.Combine(WorldsPath, newName);
            counter++;
        }
        while (Directory.Exists(worldDir));

        return newName;
    }
}
