using UnityEngine;
using TMPro;
using DG.Tweening;

public class DayTextUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI dayText;
    public float fadeDuration = 1f;
    public float displayDuration = 2f;
    public Vector3 moveOffset = new(0f, 50f, 0f); // Optional: move text up slightly

    private Vector3 originalPosition;

    void Awake()
    {
        originalPosition = dayText.rectTransform.anchoredPosition;
        canvasGroup.alpha = 0f;
    }

    public void ShowDay(int day)
    {
        dayText.text = $"Day {day}";
        canvasGroup.alpha = 0f;
        dayText.rectTransform.anchoredPosition = originalPosition;

        Sequence seq = DOTween.Sequence();

        // Move up and fade in
        seq.Append(canvasGroup.DOFade(1f, fadeDuration));
        seq.Join(dayText.rectTransform.DOAnchorPos(originalPosition + moveOffset, fadeDuration));

        // Wait while visible
        seq.AppendInterval(displayDuration);

        // Move down and fade out
        seq.Append(canvasGroup.DOFade(0f, fadeDuration));
        seq.Join(dayText.rectTransform.DOAnchorPos(originalPosition, fadeDuration));
    }
}
