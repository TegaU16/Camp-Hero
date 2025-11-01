using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerStats;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance;

    public GameObject campfireStatsMenu;
    public GameObject playerSkillsSelectorMenu;
    public GameObject playerSkillMenu;

    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI availableGoldenPointsText;
    [SerializeField] private TextMeshProUGUI levelText;
    public PlayerStats stats = new();
    private Player player;

    [Header("Add Stat Button Settings")]
    public GameObject addStatButtonObj;
    public Sprite activeSprite;
    public Sprite inactiveSprite;
    private Image addStatButtonImg;
    private Button addStatButton;

    private Stat selectedStat;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        availablePointsText.text = stats.availablePoints.ToString();

        addStatButtonImg = addStatButtonObj.GetComponent<Image>();
        addStatButton = addStatButtonObj.GetComponent<Button>();
    }

    public void AllocatePoint()
    {
        if (stats.availablePoints <= 0) return;

        UpgradeStat(selectedStat);

        stats.availablePoints--;
        UpdateAvailablePoints();

        if (player != null)
            player.UpdateVitals();
    }

    public void AddPoints(int regularPoints, int goldenPoints)
    {
        stats.availablePoints += regularPoints;
        stats.goldenPoints += goldenPoints;

        UpdateAvailablePoints();
    }

    public void UpdateAvailablePoints()
    {
        availablePointsText.text = $"<color=#22DC61>{stats.availablePoints}</color>";
        availableGoldenPointsText.text = $"<color=#EEEE2C>{stats.goldenPoints}</color>";

        addStatButtonImg.sprite = stats.availablePoints > 0 ? activeSprite : inactiveSprite;
        addStatButton.interactable = stats.availablePoints > 0;
    }

    public void SetPlayer(GameObject playerObj)
    {
        if (playerObj.TryGetComponent(out Player playerScript))
            player = playerScript;
    }

    public void UpgradeStat(Stat stat, int pointsToAdd = 1)
    {
        stat.Value += pointsToAdd;
        stat.Value = Mathf.Clamp(stat.Value, 0, stat.MaxValue - 1);

        bool isMaxed = stat.Value == stat.MaxValue - 1;

        levelText.text = isMaxed ? "Lv. MAX" : $"Lv. {stat.Value + 1}";
        stat.OuterLevelText.text = $"{stat.Value + 1}/{stat.MaxValue}";
    }

    public void ToggleCampfireStatsMenu(bool open)
    {
        if (campfireStatsMenu != null)
            campfireStatsMenu.SetActive(open);
    }

    public void TogglePlayerSkillsSelectorMenu(bool open)
    {
        if (playerSkillsSelectorMenu != null)
            playerSkillsSelectorMenu.SetActive(open);
    }

    public void CloseStatsMenu()
    {
        ToggleCampfireStatsMenu(false);
        TogglePlayerSkillsSelectorMenu(false);
    }

    public void TogglePlayerSkillMenu(string statName)
    {
        if (playerSkillMenu == null) return;

        if (string.IsNullOrEmpty(statName))
        {
            playerSkillMenu.SetActive(false);
            return;
        }

        switch (statName.ToLower())
        {
            case "strength":
                UpdateSkillMenu(stats.strength);
                break;
            case "vitality":
                UpdateSkillMenu(stats.vitality);
                break;
            case "endurance":
                UpdateSkillMenu(stats.endurance);
                break;
            case "stamina":
                UpdateSkillMenu(stats.stamina);
                break;
            case "luck":
                UpdateSkillMenu(stats.luck);
                break;
            case "speed":
                UpdateSkillMenu(stats.speed);
                break;
            default:
                Debug.LogWarning("Invalid stat name");
                return;
        }

        playerSkillMenu.SetActive(true);
    }

    private void UpdateSkillMenu(Stat stat)
    {
        selectedStat = stat;
        bool isMaxed = stat.Value == stat.MaxValue - 1;

        levelText.text = isMaxed ? "Lv. MAX" : $"Lv. {stat.Value + 1}";
        RefreshUpgradeUI(selectedStat);
    }

    public void PurchaseUpgrade(StatUpgrade upgrade)
    {
        if (upgrade.purchased) return;

        if (stats.goldenPoints < upgrade.Cost) return;

        stats.goldenPoints -= upgrade.Cost;
        upgrade.purchased = true;
        UpdateAvailablePoints();

        if (upgrade.effect != null && player != null)
        {
            if (upgrade.effect is PassiveUpgradeEffect)
                upgrade.effect.OnUnlocked(player);
            else
                player.UnlockActiveAbility(upgrade.effect);
        }

        // Refresh UI
        RefreshUpgradeUI(selectedStat);
    }

    private void RefreshUpgradeUI(Stat stat)
    {
        foreach (StatUpgrade upgrade in stat.upgrades)
        {
            if (upgrade.upgradeButtonObj == null) continue;

            upgrade.upgradeButtonObj.SetActive(true);

            Button button = upgrade.upgradeButtonObj.GetComponent<Button>();
            TextMeshProUGUI costText = upgrade.upgradeButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            costText.text = upgrade.purchased ? "Purchased" : $"{upgrade.UpgradeName} - {upgrade.Cost} GP";

            button.interactable = !upgrade.purchased && stats.goldenPoints >= upgrade.Cost;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => PurchaseUpgrade(upgrade));
        }

        // Hide all other stat upgrades (not the selected stat)
        HideNonSelectedUpgrades(stat);
    }

    private void HideNonSelectedUpgrades(Stat selected)
    {
        Stat[] allStats = new[] { stats.strength, stats.vitality, stats.endurance, stats.stamina, stats.luck, stats.speed };

        foreach (Stat s in allStats)
        {
            if (s.Equals(selected)) continue;

            foreach (StatUpgrade upgrade in s.upgrades)
            {
                if (upgrade.upgradeButtonObj != null)
                    upgrade.upgradeButtonObj.SetActive(false);
            }
        }
    }

    public void LoadPlayerUpgrades(List<string> unlockedUpgrades)
    {
        SetUnlockedUpgradesForStat(stats.strength, unlockedUpgrades);
        SetUnlockedUpgradesForStat(stats.vitality, unlockedUpgrades);
        SetUnlockedUpgradesForStat(stats.endurance, unlockedUpgrades);
        SetUnlockedUpgradesForStat(stats.stamina, unlockedUpgrades);
        SetUnlockedUpgradesForStat(stats.luck, unlockedUpgrades);
        SetUnlockedUpgradesForStat(stats.speed, unlockedUpgrades);
    }

    private void SetUnlockedUpgradesForStat(Stat stat, List<string> unlockedUpgrades)
    {
        foreach (StatUpgrade upgrade in stat.upgrades)
        {
            if (!unlockedUpgrades.Contains(upgrade.UpgradeName)) continue;
            if (upgrade.effect == null || player == null) continue;

            upgrade.purchased = true;

            if (upgrade.effect is PassiveUpgradeEffect)
                upgrade.effect.OnUnlocked(player);
            else
                player.UnlockActiveAbility(upgrade.effect);
        }
    }

    public List<string> GetPlayerUpgrades()
    {
        List<string> upgrades = new();

        upgrades.AddRange(GetUpgradesForStat(stats.strength));
        upgrades.AddRange(GetUpgradesForStat(stats.vitality));
        upgrades.AddRange(GetUpgradesForStat(stats.endurance));
        upgrades.AddRange(GetUpgradesForStat(stats.stamina));
        upgrades.AddRange(GetUpgradesForStat(stats.luck));
        upgrades.AddRange(GetUpgradesForStat(stats.speed));

        return upgrades;
    }

    private List<string> GetUpgradesForStat(Stat stat)
    {
        List<string> upgrades = new();

        foreach (StatUpgrade upgrade in stat.upgrades)
        {
            if (upgrade.purchased)
                upgrades.Add(upgrade.UpgradeName);
        }

        return upgrades;
    }
}
