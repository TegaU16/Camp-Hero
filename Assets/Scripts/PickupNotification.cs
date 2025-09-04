using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PickupNotification : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemCountText;
    public RectTransform rectTransform;

    private CanvasGroup canvasGroup;
    private Vector2 baseAnchoredPos;

    private void Awake()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseAnchoredPos = rectTransform.anchoredPosition;
    }

    public void ShowPickup(Item item, int count)
    {
        // Reset visuals
        itemIcon.sprite = item.icon;
        itemCountText.text = count + "x " + item.name;
        gameObject.SetActive(true);

        // Reset state
        rectTransform.anchoredPosition = new Vector2(600f, baseAnchoredPos.y);
        canvasGroup.alpha = 0f;

        // Kill previous tweens if any
        DOTween.Kill(rectTransform);
        DOTween.Kill(canvasGroup);

        Sequence seq = DOTween.Sequence();

        // Slide in + fade in
        seq.Append(rectTransform.DOAnchorPosX(baseAnchoredPos.x, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(canvasGroup.DOFade(1f, 0.4f));

        // Stay visible
        seq.AppendInterval(1.5f);

        // Float up + fade out
        seq.Append(rectTransform.DOAnchorPosY(baseAnchoredPos.y + 50f, 1f).SetEase(Ease.OutCubic));
        seq.Join(canvasGroup.DOFade(0f, 1f));

        // Disable object after done
        seq.OnComplete(() => gameObject.SetActive(false));
    }
}
