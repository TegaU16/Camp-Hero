using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI), typeof(RectTransform))]
public class TextNotification : MonoBehaviour
{
    private TextMeshProUGUI notificationText;
    private RectTransform rectTransform;
    [SerializeField] private Color textColor;
    
    [SerializeField] private float notificationOffset;
    [SerializeField] private float textDuration;
    [SerializeField] private float fadeInDuration;
    [SerializeField] private float fadeOutDuration;

    private void Awake()
    {
        notificationText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup()
    {
        notificationText.color = textColor;

        StartCoroutine(UIManager.Instance.DisplayTextRoutine(
            notificationText,
            textDuration,
            fadeInDuration,
            fadeOutDuration)
        );

        float textCompletionTime = textDuration + fadeInDuration + fadeOutDuration;
        StartCoroutine(Rise(textDuration, textCompletionTime));
    }

    private IEnumerator Rise(float floatDuration, float completionTime)
    {
        float endPositionY = rectTransform.localPosition.y + notificationOffset;
        rectTransform.DOMoveY(endPositionY, floatDuration);

        yield return new WaitForSeconds(completionTime);
        TextNotificationPool.Instance.ReturnTextNotification(this);
    }
}
