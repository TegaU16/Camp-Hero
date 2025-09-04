using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance;

    [Header("UI References")]
    public GameObject loadingScreenRoot;
    public Image progressBar;
    public TMPro.TextMeshProUGUI progressText;

    private float lastProgress = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Hide();
    }

    public void Show()
    {
        loadingScreenRoot.SetActive(true);
        progressBar.fillAmount = 0f;
        progressText.text = "0%";
    }

    public void Hide()
    {
        loadingScreenRoot.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress < lastProgress) return; // ignore backwards updates
        lastProgress = progress;
        progressBar.fillAmount = progress;
        progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    public void SetProgressRange(float phaseProgress, float min, float max)
    {
        float clamped = Mathf.Clamp01(phaseProgress);
        float scaled = Mathf.Lerp(min, max, clamped);
        SetProgress(scaled);
    }
}
