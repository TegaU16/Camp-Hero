using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance;

    [Header("UI References")]
    public GameObject loadingScreenRoot;
    public Image progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI tipText;

    [Header("Tip Settings")]
    [TextArea] public List<string> tips = new();
    public float tipDisplayDuration = 4f; // How long each tip stays visible
    public float fadeDuration = 0.5f;     // How long fade in/out lasts
    public int cooldownCycles = 3;        // Number of tips shown before a tip can reappear

    private float lastProgress = 0f;
    private int currentTipIndex = -1;
    private readonly Dictionary<int, int> tipCooldowns = new();
    private Coroutine tipRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(this);
        Hide();
    }

    public void Show()
    {
        loadingScreenRoot.SetActive(true);

        progressBar.fillAmount = 0f;
        progressText.text = "0%";

        if (tipRoutine != null)
            StopCoroutine(tipRoutine);

        tipRoutine = StartCoroutine(TipCycleRoutine());
    }

    public void Hide()
    {
        loadingScreenRoot.SetActive(false);

        if (tipRoutine != null)
        {
            StopCoroutine(tipRoutine);
            tipRoutine = null;
        }
    }

    public void SetProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (clamped < lastProgress) return;

        lastProgress = clamped;

        progressBar.fillAmount = clamped;
        progressText.text = Mathf.RoundToInt(clamped * 100f) + "%";
    }

    public void SetProgressRange(float phaseProgress, float min, float max)
    {
        float clamped = Mathf.Clamp01(phaseProgress);
        float scaled = Mathf.Lerp(min, max, clamped);
        SetProgress(scaled);
    }

    private IEnumerator TipCycleRoutine()
    {
        yield return new WaitForSeconds(tipDisplayDuration / 2f);
        if (tips.Count == 0) yield break;

        if (!tipText.TryGetComponent(out CanvasGroup tipCanvasGroup))
            tipCanvasGroup = tipText.gameObject.AddComponent<CanvasGroup>();

        // Make sure alpha starts at 0 (invisible)
        tipCanvasGroup.alpha = 0f;

        while (true)
        {
            int nextTipIndex = GetNextTipIndex();
            string nextTip = tips[nextTipIndex];
            currentTipIndex = nextTipIndex;

            tipText.text = $"TIP: {nextTip}";

            // fade in
            yield return StartCoroutine(FadeTip(tipCanvasGroup, 0f, 1f, fadeDuration));

            // stay visible
            yield return new WaitForSeconds(tipDisplayDuration);

            // fade out
            yield return StartCoroutine(FadeTip(tipCanvasGroup, 1f, 0f, fadeDuration));

            // update cooldowns
            UpdateCooldowns();
            tipCooldowns[nextTipIndex] = cooldownCycles;
        }
    }

    private IEnumerator FadeTip(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private int GetNextTipIndex()
    {
        List<int> availableIndices = new();

        for (int i = 0; i < tips.Count; i++)
        {
            if (i != currentTipIndex && (!tipCooldowns.ContainsKey(i) || tipCooldowns[i] <= 0))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
        {
            // If everything is on cooldown, reset cooldowns (except last tip)
            tipCooldowns.Clear();
            for (int i = 0; i < tips.Count; i++)
            {
                if (i != currentTipIndex)
                    availableIndices.Add(i);
            }
        }

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    private void UpdateCooldowns()
    {
        List<int> keys = new(tipCooldowns.Keys);
        foreach (int key in keys)
            tipCooldowns[key] = Mathf.Max(0, tipCooldowns[key] - 1);
    }
}
