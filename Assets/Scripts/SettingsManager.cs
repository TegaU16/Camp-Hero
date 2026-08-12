using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioMixer audioMixer; // For controlling volume levels

    public GameSettingsData currentSettings = new();

    private const string SettingsKey = "GameSettings";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    public void ApplySettings()
    {
        float master = Mathf.Log10(Mathf.Max(currentSettings.masterVolume, 0.0001f)) * 20f;
        float music = Mathf.Log10(Mathf.Max(currentSettings.musicVolume, 0.0001f)) * 20f;
        float sfx = Mathf.Log10(Mathf.Max(currentSettings.sfxVolume, 0.0001f)) * 20f;

        audioMixer.SetFloat("MasterVolume", master);
        audioMixer.SetFloat("MusicVolume", music);
        audioMixer.SetFloat("SFXVolume", sfx);

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
