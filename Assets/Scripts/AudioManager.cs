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
    private Coroutine musicLoopCoroutine;
    private string currentMusicCategory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
    public void PlayMusicCategory(string categoryName)
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

        fadeCoroutine = StartCoroutine(FadeMusicToClip(clip));
    }

    private IEnumerator FadeMusicToClip(AudioClip newClip)
    {
        // Stop any existing music loop logic
        if (musicLoopCoroutine != null)
            StopCoroutine(musicLoopCoroutine);

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
        musicSource.loop = false;
        musicSource.Play();

        // Fade in
        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, startVolume, t / musicFadeDuration);
            yield return null;
        }

        musicLoopCoroutine = StartCoroutine(PlayNextMusicLoop());
    }

    private IEnumerator PlayNextMusicLoop()
    {
        while (true)
        {
            yield return new WaitWhile(() => musicSource.isPlaying);

            string currentCategory = GetCurrentCategoryFromScene();

            if (!musicCategoryDict.TryGetValue(currentCategory, out List<AudioClip> clips) || clips.Count == 0)
                yield break;

            AudioClip nextClip = clips[Random.Range(0, clips.Count)];

            musicSource.clip = nextClip;
            musicSource.Play();
        }
    }

    private string GetCurrentCategoryFromScene() => currentMusicCategory;

    // === SFX FUNCTIONS ===

    public void PlaySFX(string clipName, bool loop = false, float pitch = 1f, Vector3? position = null)
    {
        if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
            return;
        }

        PlaySFX(clip, loop, pitch, position);
    }

    public void PlaySFX(AudioClip clip, bool loop = false, float pitch = 1f, Vector3? position = null)
    {
        if (clip == null) return;

        AudioSource src = GetPooledSource();

        bool is3D = position.HasValue;

        src.transform.position = position ?? Vector3.zero;

        src.pitch = pitch;
        src.clip = clip;
        src.loop = loop;

        // 2D vs 3D setup
        src.spatialBlend = is3D ? 1f : 0f;      // 1 = fully 3D, 0 = UI/flat sound
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;                   // full volume within 2 units
        src.maxDistance = 20f;                  // silent at 20 units

        src.Play();

        if (!src.loop)
            StartCoroutine(ReturnAfter(src, clip.length / Mathf.Abs(Mathf.Max(0.0001f, pitch))));
    }

    // === SCENE HANDLING ===

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "MainMenuScene":
                currentMusicCategory = "MainMenu";
                PlayMusicCategory("MainMenu");
                break;
            case "GameScene":
                currentMusicCategory = "Game";
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
