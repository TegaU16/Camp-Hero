using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[System.Serializable]
public class MusicCategory
{
    public string name;            // e.g., "MainMenu", "Game"
    public List<AudioClip> clips;  // Music clips for this category
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("References")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;       // Persistent background music
    public Transform sfxContainer;        // Parent for dynamically spawned SFX sources

    [Header("Audio Clips")]
    public List<MusicCategory> musicCategories;
    public List<AudioClip> sfxClips;

    private Dictionary<string, List<AudioClip>> musicCategoryDict;
    private Dictionary<string, AudioClip> sfxDict;

    private readonly Queue<AudioSource> sfxPool = new();
    private readonly int poolSize = 10;

    [Header("Music Settings")]
    public float musicFadeDuration = 1f;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Build dictionaries
        musicCategoryDict = new Dictionary<string, List<AudioClip>>();
        foreach (MusicCategory category in musicCategories)
        {
            if (category != null && !string.IsNullOrEmpty(category.name))
                musicCategoryDict[category.name] = category.clips ?? new List<AudioClip>();
        }

        sfxDict = new Dictionary<string, AudioClip>();
        foreach (AudioClip clip in sfxClips)
        {
            if (clip != null && !sfxDict.ContainsKey(clip.name))
                sfxDict[clip.name] = clip;
        }

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new("PooledSFX");
            go.transform.SetParent(sfxContainer);

            AudioSource src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];

            go.SetActive(false);
            sfxPool.Enqueue(src);
        }

        musicSource.volume = 1f;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // === MUSIC FUNCTIONS ===
    public void PlayMusicCategory(string categoryName, bool loop = true)
    {
        if (!musicCategoryDict.TryGetValue(categoryName, out List<AudioClip> clips) || clips.Count == 0)
        {
            Debug.LogWarning($"Music category '{categoryName}' not found or empty!");
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Count)];

        if (musicSource.clip == clip && musicSource.isPlaying) return; // Already playing this clip

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeMusicToClip(clip, loop));
    }

    private IEnumerator FadeMusicToClip(AudioClip newClip, bool loop)
    {
        // Fade out
        float startVolume = musicSource.volume;
        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeDuration);
            yield return null;
        }

        float waitTime = Random.Range(0f, 5f);
        yield return new WaitForSeconds(waitTime);

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // Fade in
        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, startVolume, t / musicFadeDuration);
            yield return null;
        }
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        musicSource.Stop();
    }

    // === SFX FUNCTIONS ===

    public void PlaySFX(string clipName, float pitch = 1f, Vector3? position = null)
    {
        if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
            return;
        }

        PlaySFX(clip, pitch, position); // reuse the clip overload
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f, Vector3? position = null)
    {
        if (clip == null) return;

        AudioSource src = GetPooledSource();
        src.transform.position = position ?? Vector3.zero;

        src.pitch = pitch;
        src.clip = clip;
        src.Play();

        StartCoroutine(ReturnAfter(src, clip.length / Mathf.Abs(Mathf.Max(0.0001f, pitch))));
    }

    // === SCENE HANDLING ===

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "MainMenuScene":
                PlayMusicCategory("MainMenu");
                break;
            case "GameScene":
                PlayMusicCategory("Game");
                break;
        }
    }

    private AudioSource GetPooledSource()
    {
        AudioSource src = sfxPool.Count > 0 ? sfxPool.Dequeue() : new GameObject("ExtraSFX").AddComponent<AudioSource>();
        src.gameObject.SetActive(true);
        return src;
    }

    private void ReturnToPool(AudioSource src)
    {
        src.Stop();
        src.gameObject.SetActive(false);
        sfxPool.Enqueue(src);
    }

    private IEnumerator ReturnAfter(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(src);
    }
}
