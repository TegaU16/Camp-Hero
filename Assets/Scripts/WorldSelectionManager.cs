using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WorldSelectionManager : MonoBehaviour
{
    public TMP_InputField worldNameInput;
    public TMP_InputField seedInput;

    private string selectedWorldName;
    private string selectedSeed;

    public Button playButton;
    public Button deleteButton;
    public Button editButton;

    public Transform worldListParent;
    public GameObject worldButtonPrefab;

    private string WorldsPath => Path.Combine(Application.persistentDataPath, "Worlds");

    public void Setup()
    {
        LoadWorldList();
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;
    }

    public void CreateWorld()
    {
        string worldName = worldNameInput.text.Trim();
        if (string.IsNullOrEmpty(worldName)) return;

        string seedString = string.IsNullOrEmpty(seedInput.text)
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
            lastPlayedDate = System.DateTime.Now.ToString()
        };

        string metaPath = Path.Combine(worldDir, "meta.json");
        File.WriteAllText(metaPath, JsonUtility.ToJson(metadata, true));

        selectedWorldName = worldName;
        selectedSeed = seedString;

        WorldSession.CurrentWorldName = selectedWorldName;
        WorldSession.CurrentSeed = selectedSeed;

        SceneManager.LoadScene("GameScene");
    }


    void LoadWorldList()
    {
        foreach (Transform child in worldListParent)
            Destroy(child.gameObject);

        if (!Directory.Exists(WorldsPath)) return;

        foreach (string dir in Directory.GetDirectories(WorldsPath))
        {
            string metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath)) continue;

            string json = File.ReadAllText(metaPath);
            WorldMetaData metadata = JsonUtility.FromJson<WorldMetaData>(json);

            GameObject buttonObj = Instantiate(worldButtonPrefab, worldListParent);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = metadata.worldName;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedWorldName = metadata.worldName;
                selectedSeed = metadata.seed;

                playButton.interactable = true;
                deleteButton.interactable = true;
                editButton.interactable = true;
            });
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
}
