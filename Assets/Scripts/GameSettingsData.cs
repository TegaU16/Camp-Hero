using UnityEngine;

[System.Serializable]
public class GameSettingsData
{
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    public int resolutionIndex = 0;
    public bool fullscreen = true;
    public int qualityLevel = 2;
}
