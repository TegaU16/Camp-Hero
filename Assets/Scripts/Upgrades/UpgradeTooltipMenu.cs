using System.Collections.Generic;
using Game.Level;
using Game.Players;
using TMPro;
using Unity.VisualScripting;
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
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI levelRequiredText;
        [SerializeField] private TextMeshProUGUI purchaseText;
        [SerializeField] private Image holdProgressImage;
        [SerializeField] private HoldToPurchase holdToPurchase;

        [Header("Text Colors")]
        [SerializeField] private Color costTextColor;
        [SerializeField] private Color levelTextColor;

        private bool isHovered;
        private UpgradeBase currentUpgrade;
        private Vector2 currentPosition;
        private bool currentShowLevelRequirement;

        private GameObject currentButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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
            currentUpgrade = upgradeBase;
            currentPosition = position;
            currentShowLevelRequirement = showLevelRequirement;

            // Set text fields
            nameText.text = upgradeBase.UpgradeName;
            descriptionText.text = upgradeBase.effect.description;
            costText.text = $"<color=#{costTextColor.ToHexString()}>{upgradeBase.Cost}</color>";

            // Campfire upgrades have LevelRequirement
            if (showLevelRequirement && upgradeBase is CampfireUpgrade campfire)
                levelRequiredText.text = $"<color=#{levelTextColor.ToHexString()}>LV</color> {campfire.LevelRequirement}";
            else
                levelRequiredText.text = "";

            purchaseText.text = upgradeBase.purchased ? "Purchased" : "Purchase";

            // Reset hold progress
            holdProgressImage.fillAmount = upgradeBase.purchased ? 1f : 0f;
            isHovered = true;

            holdToPurchase.SetCurrentUpgrade(currentUpgrade);

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

            holdProgressImage.fillAmount = 0f;
            holdToPurchase.SetCurrentUpgrade(currentUpgrade);
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
                if (hit.gameObject == gameObject || hit.gameObject == upgradeButton) return true;
            }

            return false;
        }

        public bool CanPurchase()
        {
            if (currentUpgrade == null) return false;

            return currentUpgrade switch
            {
                StatUpgrade statUpgrade => PlayerStatsManager.Instance.stats.goldenPoints >= statUpgrade.Cost
                                           && !statUpgrade.purchased,

                CampfireUpgrade campfireUpgrade => PlayerStatsManager.Instance.stats.goldenPoints >= campfireUpgrade.Cost
                                           && !campfireUpgrade.purchased
                                           && LevelManager.Instance.GetLevel() >= campfireUpgrade.LevelRequirement,
                _ => false,
            };
        }

        public void RefreshCurrent()
        {
            if (currentUpgrade == null) return;

            switch (currentUpgrade)
            {
                case StatUpgrade playerUpgrade:
                    ShowUpgrade(playerUpgrade, currentPosition, currentShowLevelRequirement);
                    break;

                case CampfireUpgrade campfireUpgrade:
                    ShowUpgrade(campfireUpgrade, currentPosition, currentShowLevelRequirement);
                    break;
            }
        }

        public bool IsShowing(UpgradeBase upgrade) => currentUpgrade == upgrade;
    }
}
