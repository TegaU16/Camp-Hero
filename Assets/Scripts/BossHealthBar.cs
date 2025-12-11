using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class BossHealthBar : MonoBehaviour
{
    private int currentHealth;

    public Slider slider;
    public float tweenDuration = 0.25f;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI enemyNameText;

    private BreakableObject breakableObject;

    public void Setup(GameObject bossEnemy)
    {
        if (bossEnemy == null) return;

        if (bossEnemy.TryGetComponent(out BreakableObject breakableObject))
        {
            this.breakableObject = breakableObject;

            currentHealth = breakableObject.GetHealth();
            healthText.text = currentHealth.ToString();

            string enemyName = bossEnemy.name.Replace("(Clone)", "").TrimEnd();
            enemyNameText.text = enemyName;

            slider.maxValue = breakableObject.GetMaxHealth();
            slider.value = currentHealth;

            breakableObject.OnBossHealthChange += OnBossHealthChanged;
        }
    }

    public void OnBossHealthChanged(int health)
    {
        currentHealth = health;
        slider.DOValue(currentHealth, tweenDuration).SetEase(Ease.OutQuad);
        healthText.text = currentHealth.ToString();

        if (currentHealth <= 0)
        {
            breakableObject.OnBossHealthChange -= OnBossHealthChanged;
            BossHealthBarManager.Instance.UnRegisterHealthBar(this);
        }
    }
}
