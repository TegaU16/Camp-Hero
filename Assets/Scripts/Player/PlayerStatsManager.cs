using System.Collections.Generic;
using Game.Level;
using Game.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.Players.PlayerStats;

namespace Game.Players
{
    public class PlayerStatsManager : MonoBehaviour
    {
        public IEnumerable<Stat> AllStats
        {
            get
            {
                yield return stats.strength;
                yield return stats.vitality;
                yield return stats.endurance;
                yield return stats.stamina;
                yield return stats.luck;
                yield return stats.speed;
            }
        }

        public static PlayerStatsManager Instance;

        [Header("Menu References")]
        [SerializeField] private GameObject campfireStatsMenu;
        [SerializeField] private GameObject playerSkillsSelectorMenu;
        [SerializeField] private GameObject playerSkillMenu;

        [Header("Skill Menu UI")]
        [SerializeField] private GameObject skillLabel;
        [SerializeField] private TextMeshProUGUI availablePointsText;
        [SerializeField] private TextMeshProUGUI playerAvailableGoldenPointsText;
        [SerializeField] private TextMeshProUGUI campfireAvailableGoldenPointsText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Player Stats")]
        public PlayerStats stats;
        private Player player;

        [Header("Add Stat Button Settings")]
        [SerializeField] private GameObject addStatButtonObj;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite inactiveSprite;
        private Image addStatButtonImg;
        private Button addStatButton;

        [Header("Skills List Menu")]
        [SerializeField] private GameObject skillsListMenu;

        private Stat selectedStat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            addStatButtonImg = addStatButtonObj.GetComponent<Image>();
            addStatButton = addStatButtonObj.GetComponent<Button>();

            foreach (Stat stat in AllStats)
                UpdateStatOuterUI(stat);

            CloseStatsMenu();
        }

        // Called by button
        public void AllocatePoint()
        {
            if (stats.availablePoints <= 0) return;
            if (selectedStat.Value >= selectedStat.MaxValue) return;

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
        }

        public void UpdateAvailablePoints()
        {
            availablePointsText.text = $"<color=#22DC61>{stats.availablePoints}</color>";
            playerAvailableGoldenPointsText.text = $"<color=#EEEE2C>{stats.goldenPoints}</color>";
            campfireAvailableGoldenPointsText.text = $"<color=#EEEE2C>{stats.goldenPoints}</color>";

            addStatButtonImg.sprite = stats.availablePoints > 0 ? activeSprite : inactiveSprite;
            addStatButton.interactable = stats.availablePoints > 0;

            if (selectedStat != null)
            {
                bool isMaxed = selectedStat.Value == selectedStat.MaxValue;
                levelText.text = isMaxed ? "Lv. MAX" : $"Lv. {selectedStat.Value}";
            }
        }

        public void SetPlayer(GameObject playerObj)
        {
            if (playerObj.TryGetComponent(out Player playerScript))
                player = playerScript;
        }

        public void UpgradeStat(Stat stat, int pointsToAdd = 1)
        {
            if ((stat.Value + pointsToAdd) > stat.MaxValue)
                pointsToAdd = stat.MaxValue - stat.Value;

            stat.Value += pointsToAdd;

            UpdateStatOuterUI(stat);
        }

        public void UpdateStatOuterUI(Stat stat) => stat.OuterLevelText.text = $"{stat.Value}/{stat.MaxValue}";

        public void ToggleCampfireStatsMenu(bool open)
        {
            if (open)
                RefreshUpgradeUI(stats.campfireUpgrades, showLevelRequirement: true);

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
            UpgradeTooltipMenu.Instance.HideInstant();
            ToggleCampfireStatsMenu(false);
            TogglePlayerSkillsSelectorMenu(false);
            TogglePlayerSkillMenu(null);
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

            ToggleCampfireStatsMenu(false);
            TogglePlayerSkillsSelectorMenu(false);
            playerSkillMenu.SetActive(true);
        }

        private void UpdateSkillMenu(Stat stat)
        {
            selectedStat = stat;
            bool isMaxed = stat.Value == stat.MaxValue;

            if (skillLabel != null)
            {
                Image skillIcon = skillLabel.transform.Find("Skill Icon").GetComponent<Image>();
                skillIcon.sprite = selectedStat.Icon;

                TextMeshProUGUI skillName = skillLabel.transform.Find("Skill Name").GetComponent<TextMeshProUGUI>();
                skillName.text = selectedStat.Name;
            }

            levelText.text = isMaxed ? "Lv. MAX" : $"Lv. {stat.Value}";
            RefreshUpgradeUI(selectedStat.upgrades);
            HideNonSelectedUpgrades(selectedStat);
        }

        public void PurchaseUpgrade<T>(UpgradeBase<T> upgrade) where T : UpgradeEffect
        {
            if (upgrade.purchased) return;
            if (stats.goldenPoints < upgrade.Cost) return;
            if (upgrade is CampfireUpgrade campfireUpgrade &&
                LevelManager.Instance.GetLevel() < campfireUpgrade.LevelRequirement) return;

            stats.goldenPoints -= upgrade.Cost;
            upgrade.purchased = true;

            if (upgrade.effect != null && player != null)
                upgrade.effect.OnUnlocked(player);

            // Refresh UI
            if (playerSkillMenu.activeSelf)
            {
                RefreshUpgradeUI(selectedStat.upgrades);
                HideNonSelectedUpgrades(selectedStat);
            }
            else if (campfireStatsMenu.activeSelf)
            {
                RefreshUpgradeUI(stats.campfireUpgrades, showLevelRequirement: true);
            }

            if (UpgradeTooltipMenu.Instance.IsShowing(upgrade))
                UpgradeTooltipMenu.Instance.RefreshCurrent();
        }

        private void RefreshUpgradeUI<T>(IEnumerable<UpgradeBase<T>> upgrades, bool showLevelRequirement = false)
        where T : UpgradeEffect
        {
            foreach (UpgradeBase<T> upgrade in upgrades)
            {
                if (upgrade.upgradeButtonObj == null) continue;

                GameObject buttonObj = upgrade.upgradeButtonObj;
                buttonObj.SetActive(true);

                if (!buttonObj.TryGetComponent(out EventTrigger eventTrigger))
                    eventTrigger = buttonObj.AddComponent<EventTrigger>();

                eventTrigger.triggers.Clear();

                // Show tooltip on hover
                AddEvent(eventTrigger, EventTriggerType.PointerEnter, (e) =>
                {
                    RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                    Canvas canvas = UpgradeTooltipMenu.Instance.GetComponentInParent<Canvas>();
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();

                    // Convert button position to canvas local space
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        buttonRect.position,
                        canvas.worldCamera,
                        out Vector2 localPos
                    );

                    // Send localPos directly
                    UpgradeTooltipMenu.Instance.ShowUpgrade(upgrade, localPos, showLevelRequirement);
                });

                // Hide tooltip when not hovering over the button nor tooltip
                AddEvent(eventTrigger, EventTriggerType.PointerExit, (e) =>
                {
                    UpgradeTooltipMenu.Instance.TryHide();
                });
            }

            UpdateAvailablePoints();
        }

        private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(callback.Invoke);
            trigger.triggers.Add(entry);
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

        public void LoadPlayerUpgrades(HashSet<string> unlockedUpgrades)
        {
            foreach (Stat stat in AllStats)
                SetUnlockedUpgradesForStat(stat, unlockedUpgrades);
        }

        private void SetUnlockedUpgradesForStat(Stat stat, HashSet<string> unlockedUpgrades)
        {
            foreach (StatUpgrade upgrade in stat.upgrades)
            {
                if (!unlockedUpgrades.Contains(upgrade.UpgradeName)) continue;
                if (upgrade.effect == null || player == null) continue;

                upgrade.purchased = true;
                upgrade.effect.OnUnlocked(player);
            }
        }

        public HashSet<string> GetPlayerUpgrades()
        {
            HashSet<string> upgrades = new();

            foreach (Stat stat in AllStats)
                upgrades.UnionWith(GetUpgradesForStat(stat));

            return upgrades;
        }

        private HashSet<string> GetUpgradesForStat(Stat stat)
        {
            HashSet<string> upgrades = new();

            foreach (StatUpgrade upgrade in stat.upgrades)
            {
                if (upgrade.purchased)
                    upgrades.Add(upgrade.UpgradeName);
            }

            return upgrades;
        }

        public void AddSkill(GameObject skillPrefab)
        {
            if (skillPrefab == null) return;

            if (!skillsListMenu.activeSelf)
                skillsListMenu.SetActive(true);

            Instantiate(skillPrefab, skillsListMenu.transform);
        }
    }

}
