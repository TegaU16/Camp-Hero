using System.Collections.Generic;
using Game.Level;
using Game.Players;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.Players.PlayerStats;

namespace Game.Upgrades
{
    [RequireComponent(typeof(RectTransform))]
    public class UpgradeTooltipMenu : MonoBehaviour
    {
        public static UpgradeTooltipMenu Instance;

        [Header("UI References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI levelRequiredText;
        public TextMeshProUGUI purchaseText;
        public Image holdProgressImage;
        public HoldToPurchase holdToPurchase;

        private bool isHovered;
        private StatUpgrade currentPlayerUpgrade;
        private CampfireUpgrade currentCampfireUpgrade;

        private GameObject currentButton;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            EventTrigger trigger = gameObject.AddComponent<EventTrigger>();

            AddEvent(trigger, EventTriggerType.PointerEnter, (e) => isHovered = true);
            AddEvent(trigger, EventTriggerType.PointerExit, (e) => TryHide());
        }

        private void AddEvent(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(callback.Invoke);
            trigger.triggers.Add(entry);
        }

        public void ShowUpgrade<T>(UpgradeBase<T> upgradeBase, Vector3 position, bool showLevelRequirement = false)
        where T : UpgradeEffect
        {
            currentButton = upgradeBase.upgradeButtonObj;

            // Store current upgrade based on type
            if (upgradeBase is StatUpgrade statUpgrade)
                currentPlayerUpgrade = statUpgrade;
            else if (upgradeBase is CampfireUpgrade campfireUpgrade)
                currentCampfireUpgrade = campfireUpgrade;

            // Set text fields
            nameText.text = upgradeBase.UpgradeName;
            descriptionText.text = upgradeBase.effect.description;
            costText.text = $"<color=#EEEE2C>{upgradeBase.Cost}</color>";

            // Campfire upgrades have LevelRequirement
            if (showLevelRequirement && upgradeBase is CampfireUpgrade campfire)
                levelRequiredText.text = $"<color=#20DF40>LV</color> {campfire.LevelRequirement}";
            else
                levelRequiredText.text = "";

            purchaseText.text = upgradeBase.purchased ? "Purchased" : "Purchase";

            // Reset hold progress
            holdProgressImage.fillAmount = upgradeBase.purchased ? 1f : 0f;
            isHovered = true;

            holdToPurchase.SetCurrentUpgrades(currentPlayerUpgrade, currentCampfireUpgrade);

            // Activate tooltip
            gameObject.SetActive(true);

            // Move tooltip correctly
            RectTransform rect = GetComponent<RectTransform>();
            Vector2 offset = new(96, -50);
            rect.anchoredPosition = (Vector2)position + offset;
        }

        public void TryHide()
        {
            isHovered = false;
            // Delay hide slightly to allow mouse movement between button and tooltip
            CancelInvoke(nameof(HideInstant));
            Invoke(nameof(HideInstant), 0.15f);
        }

        public void HideInstant()
        {
            if (isHovered) return;

            if (currentButton != null && IsPointerOverTooltipOrButton(currentButton)) return;

            gameObject.SetActive(false);

            currentButton = null;
            currentPlayerUpgrade = null;
            currentCampfireUpgrade = null;

            holdProgressImage.fillAmount = 0f;

            holdToPurchase.SetCurrentUpgrades(currentPlayerUpgrade, currentCampfireUpgrade);
        }

        private bool IsPointerOverTooltipOrButton(GameObject upgradeButton)
        {
            PointerEventData data = new(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> hits = new();
            EventSystem.current.RaycastAll(data, hits);

            foreach (RaycastResult hit in hits)
            {
                // Tooltip itself
                if (hit.gameObject == gameObject) return true;

                // Upgrade button that opened the tooltip
                if (hit.gameObject == upgradeButton) return true;
            }

            return false;
        }

        public bool CanPurchase()
        {
            if (currentPlayerUpgrade != null)
            {
                bool canBuyPlayerUpgrade = PlayerStatsManager.Instance.stats.goldenPoints >= currentPlayerUpgrade.Cost
                    && !currentPlayerUpgrade.purchased;

                return canBuyPlayerUpgrade;
            }

            if (currentCampfireUpgrade != null)
            {
                bool canBuyCampfireUpgrade = PlayerStatsManager.Instance.stats.goldenPoints >= currentCampfireUpgrade.Cost
                    && !currentCampfireUpgrade.purchased
                    && LevelManager.Instance.GetLevel() >= currentCampfireUpgrade.LevelRequirement;

                return canBuyCampfireUpgrade;
            }

            return false;
        }
    }
}
