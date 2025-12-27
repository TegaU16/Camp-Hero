using UnityEngine;

[System.Serializable]
public class GameSettingsData
{
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(4, 10)] public int chunkViewDistance = 8;

    public bool fullscreen = true;
    public int qualityLevel = 2;

    public GameSettingsData Clone() => (GameSettingsData)MemberwiseClone();
}
