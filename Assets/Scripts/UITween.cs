using UnityEngine;
using DG.Tweening;

public static class UITween
{
    public static Tween ScaleIn(RectTransform target, float duration = 0.25f, float from = 0f)
    {
        target.gameObject.SetActive(true);
        target.localScale = Vector3.one * from;

        return target.DOScale(1f, duration).SetEase(Ease.OutBack);
    }

    public static Tween ScaleOut(RectTransform target, float duration = 0.2f, float to = 0f, bool disableOnComplete = true)
    {
        Tween t = target.DOScale(to, duration).SetEase(Ease.InBack);

        if (disableOnComplete)
            t.OnComplete(() => target.gameObject.SetActive(false));

        return t;
    }

    public static Tween FadeIn(CanvasGroup canvas, float duration = 0.25f)
    {
        canvas.gameObject.SetActive(true);
        canvas.alpha = 0f;

        return canvas.DOFade(1f, duration).SetEase(Ease.OutCubic);
    }

    public static Tween FadeOut(CanvasGroup canvas, float duration = 0.25f, bool disableOnComplete = true)
    {
        Tween t = canvas.DOFade(0f, duration).SetEase(Ease.InCubic);

        if (disableOnComplete)
            t.OnComplete(() => canvas.gameObject.SetActive(false));

        return t;
    }

    public static Tween PopIn(RectTransform target, CanvasGroup canvas, float duration = 0.25f)
    {
        target.gameObject.SetActive(true);

        target.localScale = Vector3.one * 0.85f;
        canvas.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Join(target.DOScale(1f, duration).SetEase(Ease.OutBack));
        seq.Join(canvas.DOFade(1f, duration * 0.8f));

        return seq;
    }

    public static Sequence PopOut(RectTransform target, CanvasGroup canvas, float duration = 0.2f)
    {
        Sequence seq = DOTween.Sequence();

        seq.Join(target.DOScale(0.9f, duration).SetEase(Ease.InBack));
        seq.Join(canvas.DOFade(0f, duration));

        return seq;
    }

    public static Tween FloatY(RectTransform target, float amplitude = 6f, float duration = 0.8f)
    {
        Vector2 start = target.anchoredPosition;

        return target
            .DOAnchorPosY(start.y + amplitude, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public static Tween SlideIn(RectTransform target, Vector2 from, Vector2 to, float duration = 0.3f)
    {
        target.anchoredPosition = from;

        return target.DOAnchorPos(to, duration).SetEase(Ease.OutCubic);
    }

    public static Tween SlideOut(RectTransform target, Vector2 to, float duration = 0.25f)
    {
        return target.DOAnchorPos(to, duration).SetEase(Ease.InCubic);
    }

    public static Tween DefaultOpenMenu(CanvasGroup canvasGroup, RectTransform rectTransform, float distance = 30f)
    {
        canvasGroup.DOKill();
        rectTransform.DOKill();

        canvasGroup.gameObject.SetActive(true);

        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 startPosition = originalPosition + Vector2.down * distance;

        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(
            canvasGroup.DOFade(1f, 0.25f)
                .SetEase(Ease.OutCubic)
        );
        sequence.Join(
            rectTransform.DOAnchorPos(originalPosition, 0.3f)
                .SetEase(Ease.OutCubic)
        );

        return sequence;
    }

    public static Tween DefaultCloseMenu(CanvasGroup canvasGroup, RectTransform rectTransform, float distance = 30f)
    {
        canvasGroup.DOKill();
        rectTransform.DOKill();

        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = originalPosition + Vector2.down * distance;

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            canvasGroup.DOFade(0f, 0.2f)
                .SetEase(Ease.InCubic)
        );

        sequence.Join(
            rectTransform.DOAnchorPos(endPosition, 0.2f)
                .SetEase(Ease.InCubic)
        );

        sequence.OnComplete(() =>
        {
            rectTransform.anchoredPosition = originalPosition;
            canvasGroup.gameObject.SetActive(false);
        });

        return sequence;
    }
}