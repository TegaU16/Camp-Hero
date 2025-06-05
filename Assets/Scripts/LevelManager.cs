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

    void Start()
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
        UpdateMaxExp();
        UpdateExpBar();
        levelText.text = level.ToString();
    }

    private void UpdateMaxExp()
    {
        maxExp = (int)Mathf.Round(100 * level * Mathf.Pow(1.3f, level - 1));

        if (expBar != null)
        {
            if (expBar.TryGetComponent<Slider>(out var expSlider))
            {
                expSlider.maxValue = maxExp;
            }
        }
    }

    private void UpdateExpBar()
    {
        if (expBar != null)
        {
            if (expBar.TryGetComponent<Slider>(out var expSlider))
            {
                expSlider.DOValue(currentExp, 0.5f).SetEase(Ease.OutQuad);
            }
        }
    }
}
