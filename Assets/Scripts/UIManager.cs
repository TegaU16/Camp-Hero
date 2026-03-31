using System.Collections;
using System.Collections.Generic;
using Game.Players;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly Dictionary<string, HealthBar> healthBars = new();
    private StaminaBar staminaBar;
    private ShieldBar shieldBar;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void RegisterHealthBar(string id, HealthBar healthBar)
    {
        healthBars[id] = healthBar;
        CheckReady();
    }

    public void RegisterStaminaBar(StaminaBar staminaBar)
    {
        this.staminaBar = staminaBar;
        CheckReady();
    }

    public void RegisterShieldBar(ShieldBar shieldBar)
    {
        this.shieldBar = shieldBar;
        CheckReady();
    }

    private void CheckReady()
    {
        if (healthBars.ContainsKey("Player") && staminaBar != null && shieldBar != null)
            IsInitialized = true;
    }

    public HealthBar GetHealthBar(string id)
    {
        healthBars.TryGetValue(id, out HealthBar healthBar);
        return healthBar;
    }

    public StaminaBar GetStaminaBar() => staminaBar;

    public ShieldBar GetShieldBar() => shieldBar;

    public IEnumerator DisplayTextRoutine(TextMeshProUGUI text, float displayTime, float fadeInTime, float fadeOutTime)
    {
        if (!text.TryGetComponent(out CanvasGroup canvasGroup)) yield break;

        text.gameObject.SetActive(true);

        yield return StartCoroutine(Utility.Fade(canvasGroup, 0f, 1f, fadeInTime));

        yield return new WaitForSeconds(displayTime);

        yield return StartCoroutine(Utility.Fade(canvasGroup, 1f, 0f, fadeOutTime));

        text.gameObject.SetActive(false);
    }
}
