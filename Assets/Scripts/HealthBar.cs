using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public float tweenDuration = 0.25f;
    public TextMeshProUGUI healthText;

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
        UpdateHealthText(health);
    }

    public void SetHealth(int health)
    {
        slider.DOValue(health, tweenDuration).SetEase(Ease.OutQuad);
        UpdateHealthText(health);
    }

    private void UpdateHealthText(int health)
    {
        healthText.text = health.ToString();
    }
}
