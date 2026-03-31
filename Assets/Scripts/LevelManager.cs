using System.Collections.Generic;
using DG.Tweening;
using Game.Players;
using Game.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Level
{
    public class LevelManager : MonoBehaviour
    {
        private int level = 1;
        private int maxExp = 100;
        private int currentExp = 0;
        private int maxLevelWithPointsGiven;

        [SerializeField] private GameObject expBar;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject levelUpPopupPrefab;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private Canvas worldCanvas;

        public delegate void LevelUpDelegate(int newLevel);
        public event LevelUpDelegate OnLevelUp;

        public static LevelManager Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            UpdateUI();

            Dictionary<int, int> bonuses = new()
            {
                { 5, 3 },
                { 10, 5 }
            };

            maxLevelWithPointsGiven = FindLevelForPoints(120, bonuses);
        }

        private void UpdateUI()
        {
            UpdateExpBar();
            UpdateMaxExp();
            levelText.text = level.ToString();
        }

        public void AddExp(int exp)
        {
            currentExp += exp;

            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                IncreaseLevel();
            }

            UpdateExpBar();
        }

        public void IncreaseLevel()
        {
            level += 1;
            UpdateUI();

            int regularPointsToAdd = 1;
            int goldenPointsToAdd = 0;

            if (level % 10 == 0)
            {
                regularPointsToAdd = 5;
                goldenPointsToAdd = 2;
            }
            else if (level % 5 == 0)
            {
                regularPointsToAdd = 3;
                goldenPointsToAdd = 1;
            }

            if (level > maxLevelWithPointsGiven)
            {
                regularPointsToAdd = 0;
                goldenPointsToAdd = 0;
            }

            PlayerStatsManager.Instance.AddPoints(regularPointsToAdd, goldenPointsToAdd);

            if (GameManager.Instance.playerInstance.TryGetComponent(out Player player))
            {
                int playerHealthIncrease = (int)(player.health.maxHealth / 2f);
                player.health.AddHealth(playerHealthIncrease);

                float playerStaminaIncrease = player.staminaBar.maxStamina / 2f;
                player.staminaBar.AddStamina(playerStaminaIncrease);
            }

            OnLevelUp?.Invoke(level);

            GameObject popupInstance = Instantiate(levelUpPopupPrefab, worldCanvas.transform);
            if (popupInstance.TryGetComponent(out LevelUpPopup popup))
                popup.Setup();

            AudioManager.Instance.PlaySFX(levelUpSound);
        }

        private int FindLevelForPoints(int maxPoints, Dictionary<int, int> bonuses, int basePoints = 1)
        {
            int total = 0;
            int level = 2; // Start from level 2 (first transition from level 1)

            while (total < maxPoints)
            {
                int pointsToAdd = basePoints;
                foreach (KeyValuePair<int, int> bonus in bonuses)
                {
                    if (level % bonus.Key == 0)
                        pointsToAdd = bonus.Value;
                }

                total += pointsToAdd;
                if (total >= maxPoints) return level;

                level++;
            }

            return level;
        }

        private void UpdateMaxExp()
        {
            maxExp = MaxExpForLevel(level);

            if (expBar != null && expBar.TryGetComponent(out Slider expSlider))
                expSlider.maxValue = maxExp;
        }

        private void UpdateExpBar()
        {
            if (expBar != null && expBar.TryGetComponent(out Slider expSlider))
                expSlider.DOValue(currentExp, 0.5f).SetEase(Ease.OutQuad);
        }

        public int GetLevel() => level;

        public LevelData GetLevelData()
        {
            LevelData levelData = new()
            {
                level = level,
                maxExp = maxExp,
                currentExp = currentExp
            };

            return levelData;
        }

        public void SetLevelData(LevelData levelData)
        {
            level = levelData.level;
            maxExp = levelData.maxExp;
            currentExp = levelData.currentExp;

            UpdateUI();
        }

        public int GetTotalExp()
        {
            int sumMax = 0;
            for (int i = 0; i < level; i++)
                sumMax += MaxExpForLevel(i);

            int totalExp = currentExp + sumMax;
            return totalExp;
        }

        private int MaxExpForLevel(int level) => (int)Mathf.Round(100 * level * Mathf.Pow(1.05f, level - 1));
    }
}
