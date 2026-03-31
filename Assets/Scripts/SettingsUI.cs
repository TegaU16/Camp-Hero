using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider chunkViewDistanceSlider;
    [SerializeField] private Slider qualitySlider;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private TextMeshProUGUI chunkViewText;
    [SerializeField] private TextMeshProUGUI qualityText;

    [Header("Toggles")]
    [SerializeField] private Toggle fullscreenToggle;

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
            masterVolumeText.text = $"{(int)(v * 100f)}%";

            SettingsManager.Instance.ApplySettings();
        });

        musicVolumeSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.musicVolume = v;
            musicVolumeText.text = $"{(int)(v * 100f)}%";

            SettingsManager.Instance.ApplySettings();
        });

        sfxVolumeSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.sfxVolume = v;
            sfxVolumeText.text = $"{(int)(v * 100f)}%";

            SettingsManager.Instance.ApplySettings();
        }); ;

        chunkViewDistanceSlider.onValueChanged.AddListener(v =>
        {
            currentSettings.chunkViewDistance = (int)v;
            chunkViewText.text = $"{v} Chunks";

            SettingsManager.Instance.ApplySettings();
        });

        qualitySlider.onValueChanged.AddListener(v => 
        {
            currentSettings.qualityLevel = (int)v;
            qualityText.text = $"Lv. {v + 1}";

            SettingsManager.Instance.ApplySettings();
        });

        fullscreenToggle.onValueChanged.AddListener(v => currentSettings.fullscreen = v);
    }

    // Called by button
    public void OnApplyPressed()
    {
        SettingsManager.Instance.ApplySettings();
        SettingsManager.Instance.SaveSettings();

        // Update the "original" copy
        originalSettings = SettingsManager.Instance.currentSettings.Clone();
    }

    // Called by button
    public void ToggleSettings(bool open) => gameObject.SetActive(open);
}
