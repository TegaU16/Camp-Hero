using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Setup()
    {
        LoadWorldList();
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;
    }

    public void CreateWorld()
    {
        string worldName = worldNameInput.text;
        string seed = seedInput.text;

        if (string.IsNullOrEmpty(seed))
            seed = Random.Range(0, int.MaxValue).ToString();

        // Save to PlayerPrefs for the current session
        PlayerPrefs.SetString("WorldName", worldName);
        PlayerPrefs.SetString("Seed", seed);

        // Save metadata to disk
        WorldMetadata metadata = new() { worldName = worldName, seed = seed };
        string path = Application.persistentDataPath + $"/{worldName}.json";
        File.WriteAllText(path, JsonUtility.ToJson(metadata));

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    void LoadWorldList()
    {
        foreach (Transform child in worldListParent)
        {
            Destroy(child.gameObject);
        }

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            WorldMetadata metadata = JsonUtility.FromJson<WorldMetadata>(json);

            GameObject buttonObj = Instantiate(worldButtonPrefab, worldListParent);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = metadata.worldName;

            // Capture local copy for lambda
            string worldName = metadata.worldName;
            string seed = metadata.seed;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedWorldName = metadata.worldName;
                selectedSeed = metadata.seed;

                playButton.interactable = true;
                deleteButton.interactable = true;
                editButton.interactable = true; // optional
            });
        }
    }

    public void PlaySelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        PlayerPrefs.SetString("WorldName", selectedWorldName);
        PlayerPrefs.SetString("Seed", selectedSeed);
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void DeleteSelectedWorld()
    {
        if (string.IsNullOrEmpty(selectedWorldName)) return;

        string path = Application.persistentDataPath + $"/{selectedWorldName}.json";
        if (File.Exists(path))
            File.Delete(path);

        // Clear selection and reload list
        selectedWorldName = null;
        selectedSeed = null;
        playButton.interactable = false;
        deleteButton.interactable = false;
        editButton.interactable = false;

        // Destroy all existing buttons and reload
        foreach (Transform child in worldListParent)
            Destroy(child.gameObject);

        LoadWorldList();
    }
}

[System.Serializable]
public class WorldMetadata
{
    public string worldName;
    public string seed;
}
