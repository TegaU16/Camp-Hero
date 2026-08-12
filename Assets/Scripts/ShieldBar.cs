using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShieldBar : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Slider frontBar;
    [SerializeField] private Slider backBar;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI shieldText;

    [Header("Tuning")]
    public float frontTween = 0.15f;
    public float backDelay = 0.1f;
    public float backTween = 0.35f;

    private int currentShield;
    private Tween backTweenRef;
    private Tween punchTween;

    private void Awake()
    {
        UIManager.Instance.RegisterShieldBar(this);
    }

    public void SetMaxShield(int maxShield)
    {
        currentShield = maxShield;

        frontBar.maxValue = maxShield;
        frontBar.value = maxShield;
        
        backBar.maxValue = maxShield;
        backBar.value = maxShield;

        UpdateShieldText();
    }

    public void SetShield(int shield)
    {
        int oldShield = currentShield;
        currentShield = shield;

        float delta = oldShield - shield;

        frontBar.DOKill();
        frontBar.DOValue(currentShield, frontTween).SetEase(Ease.OutQuad);

        backTweenRef?.Kill();
        backTweenRef = DOVirtual.DelayedCall(backDelay, () =>
        {
            backBar.DOValue(currentShield, backTween).SetEase(Ease.OutCubic);
        });

        UpdateShieldText();

        PlayImpact(delta);
    }

    private void PlayImpact(float damage)
    {
        RectTransform rt = (RectTransform)transform;

        punchTween?.Kill();

        float punchStrength = Mathf.Clamp(damage / 50f, 0.05f, 0.25f);

        punchTween = rt.DOPunchScale(
            Vector3.one * punchStrength,
            0.25f,
            vibrato: 8,
            elasticity: 0.6f
        );
    }

    private void UpdateShieldText() => shieldText.text = currentShield.ToString();
}
