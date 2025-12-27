using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider slider;
    public TextMeshProUGUI staminaText;

    [Header("Settings")]
    public float decrementRate = 5f;
    public float incrementRate = 10f;
    public float maxStamina = 100f;

    private float currentStamina;

    private void Awake()
    {
        UIManager.Instance.RegisterStaminaBar(this);
    }

    public void Initialize(float maxStamina, float currentStamina)
    {
        this.maxStamina = maxStamina;
        this.currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        RefreshUI();
    }

    public void SetMaxStamina(float stamina)
    {
        maxStamina = stamina;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        RefreshUI();
    }

    public void AddStamina(float amount) => ChangeStamina(amount);

    public void DecreaseStamina() => ChangeStamina(-decrementRate * Time.deltaTime);

    public void IncreaseStamina()
    {
        if (currentStamina < maxStamina)
            ChangeStamina(incrementRate * Time.deltaTime);
    }

    public void SetNewStamina(float stamina)
    {
        currentStamina = Mathf.Clamp(stamina, 0, maxStamina);
        RefreshUI();
    }

    public float GetStamina() => currentStamina;

    private void ChangeStamina(float delta)
    {
        currentStamina = Mathf.Clamp(currentStamina + delta, 0, maxStamina);
        RefreshUI();
    }

    private void RefreshUI()
    {
        slider.maxValue = maxStamina;
        slider.value = currentStamina;
        staminaText.text = ((int)currentStamina).ToString();
    }
}
