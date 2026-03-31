using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Players
{
    public class StaminaBar : MonoBehaviour
    {
        [Header("UI References")]
        public Slider slider;
        public TextMeshProUGUI staminaText;

        [Header("Settings")]
        [SerializeField] private float decrementRate = 5f;
        [HideInInspector] public float incrementRate = 10f;
        [HideInInspector] public float maxStamina = 100f;

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

        public void IncreaseStamina(float regenMult)
        {
            float staminaRegenRate = incrementRate * regenMult;

            if (currentStamina < maxStamina)
                ChangeStamina(staminaRegenRate * Time.deltaTime);
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
}
