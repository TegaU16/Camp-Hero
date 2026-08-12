using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarManager : MonoBehaviour
{
    public static BossHealthBarManager Instance;

    private readonly List<BossHealthBar> bossHealthBars = new();

    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private GameObject healthBarsParent;

    [SerializeField] private float baseWidth = 600f; // width of a single boss bar

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnBossHealthBar(GameObject enemy)
    {
        if (enemy == null) return;

        GameObject bossHealthBarInstance = Instantiate(healthBarPrefab, healthBarsParent.transform);
        if (bossHealthBarInstance != null && bossHealthBarInstance.TryGetComponent(out BossHealthBar bossHealthBar))
        {
            RegisterHealthBar(bossHealthBar, enemy);
            Canvas.ForceUpdateCanvases();
        }
    }

    private void RegisterHealthBar(BossHealthBar healthBar, GameObject enemy)
    {
        if (healthBar == null || enemy == null) return;
        if (bossHealthBars.Contains(healthBar)) return;

        healthBar.Setup(enemy);
        bossHealthBars.Add(healthBar);
        UpdateHealthBarSizes();
    }

    public void UnRegisterHealthBar(BossHealthBar healthBar)
    {
        if (healthBar == null) return;
        if (!bossHealthBars.Contains(healthBar)) return;

        bossHealthBars.Remove(healthBar);
        Destroy(healthBar.gameObject);
        UpdateHealthBarSizes();
    }

    private void UpdateHealthBarSizes()
    {
        if (bossHealthBars.Count == 0) return;

        float resizedWidth = baseWidth / bossHealthBars.Count;

        foreach (BossHealthBar bar in bossHealthBars)
        {
            if (bar == null) continue;
            if (!bar.TryGetComponent(out LayoutElement layout)) continue;

            layout.minWidth = resizedWidth;
            layout.preferredWidth = resizedWidth;
            layout.flexibleWidth = 0f;

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resizedWidth);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(healthBarsParent.GetComponent<RectTransform>());
    }

    public void ClearHealthBars()
    {
        foreach (BossHealthBar healthBar in bossHealthBars)
            Destroy(healthBar.gameObject);

        bossHealthBars.Clear();
    }
}
