using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using Game.Inventory;

public class PickupNotification : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemCountText;
    public RectTransform rectTransform;

    private CanvasGroup canvasGroup;
    private int totalCount;
    private Action onCompleteCallback;

    public Item Item { get; private set; }
    public bool IsFading { get; private set; }

    public Vector2 BaseAnchoredPos { get; private set; }

    public void Initialize(Item item, int count, Action onComplete)
    {
        Item = item;
        totalCount = count;
        onCompleteCallback = onComplete;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        UpdateUI();
        PlayAnimation();
    }

    public void AddCount(int count)
    {
        totalCount += count;
        UpdateUI();

        // Restart fade-out animation so updated count is visible longer
        rectTransform.DOKill();
        canvasGroup.DOKill();
        PlayAnimation();
    }

    public void MoveTo(Vector2 targetPos) => rectTransform.DOAnchorPos(targetPos, 0.3f).SetEase(Ease.OutCubic);

    private void UpdateUI()
    {
        itemIcon.sprite = Item.icon;
        itemCountText.text = totalCount + "x " + Item.name;
        gameObject.SetActive(true);
    }

    private void PlayAnimation()
    {
        IsFading = false;

        float slideDistance = 80f;

        rectTransform.anchoredPosition = new Vector2(
            BaseAnchoredPos.x + slideDistance,
            BaseAnchoredPos.y
        );

        canvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        // Slide in + fade in
        seq.Append(rectTransform.DOAnchorPosX(BaseAnchoredPos.x, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(canvasGroup.DOFade(1f, 0.4f));

        // Stay visible
        seq.AppendInterval(1.5f);

        // Fade out
        seq.Append(
            canvasGroup.DOFade(0f, 1f)
                .OnStart(() => IsFading = true)
        );

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onCompleteCallback?.Invoke();
        });
    }

    public void CaptureBasePosition() => BaseAnchoredPos = rectTransform.anchoredPosition;
}
