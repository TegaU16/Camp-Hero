using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using static Game.Players.PlayerStats;
using Game.Players;
using Game.Upgrades;

public class HoldToPurchase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image holdProgressImage;
    public float holdDuration = 1f;

    private bool isHolding = false;
    private Coroutine holdRoutine;

    private StatUpgrade currentPlayerUpgrade;
    private CampfireUpgrade currentCampfireUpgrade;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!UpgradeTooltipMenu.Instance.CanPurchase()) return;

        isHolding = true;

        if (holdRoutine != null)
            StopCoroutine(holdRoutine);

        holdRoutine = StartCoroutine(HoldCoroutine());
    }

    public void OnPointerUp(PointerEventData eventData) => isHolding = false;

    private IEnumerator HoldCoroutine()
    {
        float timeHeld = 0f;

        while (isHolding)
        {
            timeHeld += Time.deltaTime;
            holdProgressImage.fillAmount = Mathf.Clamp01(timeHeld / holdDuration);

            if (timeHeld >= holdDuration)
            {
                if (currentCampfireUpgrade != null)
                    PlayerStatsManager.Instance.PurchaseUpgrade(currentCampfireUpgrade);
                else if (currentPlayerUpgrade != null)
                    PlayerStatsManager.Instance.PurchaseUpgrade(currentPlayerUpgrade);

                holdProgressImage.fillAmount = 0f;
                yield break;
            }

            yield return null;
        }

        // Released early
        holdProgressImage.fillAmount = 0f;
    }

    public void SetCurrentUpgrades(StatUpgrade playerUpgrade, CampfireUpgrade campfireUpgrade)
    {
        currentPlayerUpgrade = playerUpgrade; 
        currentCampfireUpgrade = campfireUpgrade;
    }
}
