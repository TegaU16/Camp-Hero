using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public Slider slider;
    public float decrementRate;
    public float incrementRate;
    public TextMeshProUGUI staminaText;
    public float maxStamina;
    private float currentStamina;

    private void Awake()
    {
        UIManager.Instance.RegisterStaminaBar(this);
    }

    public void Initialize(float maxStamina, float currentStamina)
    {
        this.maxStamina = maxStamina;
        this.currentStamina = currentStamina;
        slider.maxValue = maxStamina;
        slider.value = currentStamina;
        UpdateStaminaText((int)currentStamina);
    }

    public void SetMaxStamina(float stamina)
    {
        maxStamina = stamina;
        currentStamina = stamina;

        slider.maxValue = maxStamina;
        slider.value = currentStamina;
        UpdateStaminaText((int)currentStamina);
    }

    public void DecreaseStamina()
    {
        currentStamina -= decrementRate * Time.deltaTime;
        slider.value = currentStamina;
        UpdateStaminaText((int)currentStamina);
    }

    public void IncreaseStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += incrementRate * Time.deltaTime;
            slider.value = currentStamina;
            UpdateStaminaText((int)currentStamina);
        }
    }

    public void SetNewStamina(int stamina)
    {
        slider.maxValue = maxStamina;
        SetCurrentStamina(stamina);
        slider.value = currentStamina;
    }

    public void SetCurrentStamina(int stamina)
    {
        currentStamina = stamina;
        UpdateStaminaText(stamina);
    }

    public float GetStamina()
    {
        return currentStamina;
    }

    private void UpdateStaminaText(int staminaValue)
    {
        staminaText.text = staminaValue.ToString();
    }
}
