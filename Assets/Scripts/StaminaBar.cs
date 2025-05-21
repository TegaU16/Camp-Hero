using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public Slider slider;
    public float decrementRate;
    public float incrementRate;

    public void SetMaxStamina(float stamina)
    {
        slider.maxValue = stamina;
        slider.value = stamina;
    }

    public void DecreaseStamina()
    {
        slider.value -= decrementRate * Time.deltaTime;
    }

    public void IncreaseStamina()
    {
        slider.value += incrementRate * Time.deltaTime;
    }
}
