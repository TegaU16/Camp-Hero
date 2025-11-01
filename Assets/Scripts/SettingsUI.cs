using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown qualityDropdown;

    private void Start()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        GameSettingsData s = SettingsManager.Instance.currentSettings;
        masterVolumeSlider.value = s.masterVolume;
        musicVolumeSlider.value = s.musicVolume;
        sfxVolumeSlider.value = s.sfxVolume;
        fullscreenToggle.isOn = s.fullscreen;
        qualityDropdown.value = s.qualityLevel;

        // Add listeners
        masterVolumeSlider.onValueChanged.AddListener(v => s.masterVolume = v);
        musicVolumeSlider.onValueChanged.AddListener(v => s.musicVolume = v);
        sfxVolumeSlider.onValueChanged.AddListener(v => s.sfxVolume = v);
        fullscreenToggle.onValueChanged.AddListener(v => s.fullscreen = v);
        qualityDropdown.onValueChanged.AddListener(v => s.qualityLevel = v);
    }

    public void OnApplyPressed()
    {
        SettingsManager.Instance.ApplySettings();
        SettingsManager.Instance.SaveSettings();
    }
}
