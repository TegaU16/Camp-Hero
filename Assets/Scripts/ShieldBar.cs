using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShieldBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private float tweenDuration = 0.25f;

    private int currentShield;

    private void Awake()
    {
        UIManager.Instance.RegisterShieldBar(this);
    }

    public void SetMaxShield(int maxShield)
    {
        currentShield = maxShield;
        slider.maxValue = maxShield;
        slider.value = currentShield;

        shieldText.text = currentShield.ToString();
    }

    public void SetShield(int shield)
    {
        currentShield = shield;
        slider.DOValue(currentShield, tweenDuration).SetEase(Ease.OutQuad);

        shieldText.text = currentShield.ToString();
    }
}
