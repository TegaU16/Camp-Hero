using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum HealthBarID { Player, Campfire }

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public float tweenDuration = 0.25f;
    public TextMeshProUGUI healthText;

    [SerializeField] private HealthBarID id;

    private int currentHealth;

    private void Awake()
    {
        UIManager.Instance.RegisterHealthBar(id.ToString(), this);
    }

    public void Initialize(int maxHealth, int currentHealth)
    {
        this.currentHealth = currentHealth;

        slider.maxValue = maxHealth;
        slider.value = currentHealth;
        UpdateHealthText(currentHealth);
    }

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
    }

    public void SetHealth(int health)
    {
        currentHealth = health;
        slider.DOValue(currentHealth, tweenDuration).SetEase(Ease.OutQuad);
        UpdateHealthText(currentHealth);
    }

    private void UpdateHealthText(int health) => healthText.text = health.ToString();
}
