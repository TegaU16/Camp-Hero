using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    private int level = 1;
    private int maxExp = 100;
    private int currentExp = 0;
    public GameObject expBar;
    public TextMeshProUGUI levelText;
    public PlayerStatsManager playerStatsManager;

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
    }

    void UpdateUI()
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

        int pointsToAdd = 1;

        if (level % 10 == 0)
            pointsToAdd = 5;
        else if (level % 5 == 0)
            pointsToAdd = 3;
        
        playerStatsManager.AddPoints(pointsToAdd);
    }

    private void UpdateMaxExp()
    {
        maxExp = (int)Mathf.Round(100 * level * Mathf.Pow(1.3f, level - 1));

        if (expBar != null)
        {
            if (expBar.TryGetComponent(out Slider expSlider))
            {
                expSlider.maxValue = maxExp;
            }
        }
    }

    private void UpdateExpBar()
    {
        if (expBar != null)
        {
            if (expBar.TryGetComponent(out Slider expSlider))
            {
                expSlider.DOValue(currentExp, 0.5f).SetEase(Ease.OutQuad);
            }
        }
    }

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
}
