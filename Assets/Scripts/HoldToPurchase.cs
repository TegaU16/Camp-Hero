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

    private UpgradeBase currentUpgrade;

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
                if (currentUpgrade != null)
                {
                    if (currentUpgrade is StatUpgrade playerUpgrade)
                        PlayerStatsManager.Instance.PurchaseUpgrade(playerUpgrade);
                    else if (currentUpgrade is CampfireUpgrade campfireUpgrade)
                        PlayerStatsManager.Instance.PurchaseUpgrade(campfireUpgrade);
                }

                holdProgressImage.fillAmount = 0f;
                yield break;
            }

            yield return null;
        }

        // Released early
        holdProgressImage.fillAmount = 0f;
    }

    public void SetCurrentUpgrade(UpgradeBase upgrade) => currentUpgrade = upgrade;
}
