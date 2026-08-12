using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public enum HealthBarID { Player, Campfire }

    [SerializeField] private HealthBarID id;

    [Header("Bars")]
    [SerializeField] private Slider frontBar;
    [SerializeField] private Slider backBar;

    [Header("UI")]
    public TextMeshProUGUI healthText;

    [Header("Tuning")]
    [SerializeField] private float frontTween = 0.15f;
    [SerializeField] private float backDelay = 0.1f;
    [SerializeField] private float backTween = 0.35f;

    private int currentHealth;
    private Tween backTweenRef;
    private Tween punchTween;

    private Vector3 originalScale;

    private void Awake()
    {
        UIManager.Instance.RegisterHealthBar(id.ToString(), this);
    }

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void Initialize(int maxHealth, int currentHealth)
    {
        this.currentHealth = currentHealth;

        SetMaxHealth(maxHealth);

        frontBar.value = currentHealth;
        backBar.value = currentHealth;

        UpdateText(currentHealth);
    }

    public void SetMaxHealth(int health)
    {
        frontBar.maxValue = health;
        backBar.maxValue = health;
    }

    public void SetHealth(int health)
    {
        int oldHealth = currentHealth;
        currentHealth = health;

        float delta = oldHealth - health;

        frontBar.DOKill();
        frontBar.DOValue(currentHealth, frontTween).SetEase(Ease.OutQuad);

        backTweenRef?.Kill();
        backTweenRef = DOVirtual.DelayedCall(backDelay, () =>
        {
            backBar.DOValue(currentHealth, backTween).SetEase(Ease.OutCubic);
        });

        UpdateText(currentHealth);
        PlayImpact(delta);
    }

    private void PlayImpact(float damage)
    {
        RectTransform rt = (RectTransform)transform;

        punchTween?.Kill();

        // Reset scale before applying new punch
        rt.localScale = originalScale;

        float punchStrength = Mathf.Clamp(damage / 50f, 0.05f, 0.25f);

        punchTween = rt.DOPunchScale(
            Vector3.one * punchStrength,
            0.25f,
            vibrato: 8,
            elasticity: 0.6f
        ).OnKill(() =>
        {
            // Ensure exact reset
            rt.localScale = originalScale;
        });

        ScreenFlash();
    }

    private void UpdateText(int health) => healthText.text = health.ToString();

    private void ScreenFlash() => UIEffects.Instance.DamageFlash();
}