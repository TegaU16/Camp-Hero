using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public float tweenDuration = 0.25f;

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
    }

    public void SetHealth(int health)
    {
        slider.DOValue(health, tweenDuration).SetEase(Ease.OutQuad);
    }
}
