using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider chunkViewDistanceSlider;
    public Slider qualitySlider;

    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI sfxVolumeText;
    public TextMeshProUGUI chunkViewText;
    public TextMeshProUGUI qualityText;

    public Toggle fullscreenToggle;

    private GameSettingsData originalSettings;

    private void Start()
    {
        RefreshUI();
    }

    private void OnEnable()
    {
        // Make a clone so UI changes don't touch real settings
        if (SettingsManager.Instance == null) return;

        originalSettings = SettingsManager.Instance.currentSettings.Clone();
        RefreshUI();
    }

    private void OnDisable()
    {
        // Restore original settings ONLY if Apply was NOT pressed
        if (SettingsManager.Instance == null || originalSettings == null) return;

        SettingsManager.Instance.currentSettings = originalSettings.Clone();
    }

    private void RefreshUI()
    {
        GameSettingsData currentSettings = SettingsManager.Instance.currentSettings;
        masterVolumeSlider.value = currentSettings.masterVolume;
        musicVolumeSlider.value = currentSettings.musicVolume;
        sfxVolumeSlider.value = currentSettings.sfxVolume;
        chunkViewDistanceSlider.value = currentSettings.chunkViewDistance;
        qualitySlider.value = currentSettings.qualityLevel;
        fullscreenToggle.isOn = currentSettings.fullscreen;

        masterVolumeText.text = $"{(int)(currentSettings.masterVolume * 100f)}%";
        musicVolumeText.text = $"{(int)(currentSettings.musicVolume * 100f)}%";
        sfxVolumeText.text = $"{(int)(currentSettings.sfxVolume * 100f)}%";
        chunkViewText.text = $"{currentSettings.chunkViewDistance} Chunks";
        qualityText.text = $"Lv. {currentSettings.qualityLevel + 1}";

        // Clear listeners
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        chunkViewDistanceSlider.onValueChanged.RemoveAllListeners();
        qualitySlider.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();

        // Add listeners
        masterVolumeSlider.onValueChanged.AddListener(v => 
        {
            currentSettings.masterVolume = v;
            masterVolumeText.text = $"{(int)(currentSettings.masterVolume * 100f)}%";
        });

        musicVolumeSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.musicVolume = v;
            musicVolumeText.text = $"{(int)(currentSettings.musicVolume * 100f)}%";
        });

        sfxVolumeSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.sfxVolume = v;
            sfxVolumeText.text = $"{(int)(currentSettings.sfxVolume * 100f)}%";
        }); ;

        chunkViewDistanceSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.chunkViewDistance = (int)v;
            chunkViewText.text = $"{currentSettings.chunkViewDistance} Chunks";
        });

        qualitySlider.onValueChanged.AddListener(v => 
        {
            currentSettings.qualityLevel = (int)v;
            qualityText.text = $"Lv. {currentSettings.qualityLevel + 1}";
        });

        fullscreenToggle.onValueChanged.AddListener(v => currentSettings.fullscreen = v);
    }

    public void OnApplyPressed()
    {
        SettingsManager.Instance.ApplySettings();
        SettingsManager.Instance.SaveSettings();

        // Update the "original" copy
        originalSettings = SettingsManager.Instance.currentSettings.Clone();
    }

    public void ToggleSettings(bool open)
    {
        gameObject.SetActive(open);
    }
}
