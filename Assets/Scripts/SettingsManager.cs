using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("References")]
    public AudioMixer audioMixer; // For controlling volume levels

    public GameSettingsData currentSettings = new();

    private const string SettingsKey = "GameSettings";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplySettings()
    {
        // Apply volume
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(currentSettings.masterVolume) * 20);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(currentSettings.musicVolume) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(currentSettings.sfxVolume) * 20);

        // Apply graphics
        QualitySettings.SetQualityLevel(currentSettings.qualityLevel);
        Screen.fullScreen = currentSettings.fullscreen;
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(currentSettings);
        PlayerPrefs.SetString(SettingsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(SettingsKey))
        {
            string json = PlayerPrefs.GetString(SettingsKey);
            currentSettings = JsonUtility.FromJson<GameSettingsData>(json);
        }

        ApplySettings();
    }
}
