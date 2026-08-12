using DG.Tweening;
using Game.Registries;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI), typeof(RectTransform), typeof(PrefabID))]
public class TextNotification : MonoBehaviour
{
    private TextMeshProUGUI notificationText;
    private RectTransform rectTransform;
    private PrefabID prefabID;

    [SerializeField] private Color textColor;
    
    [SerializeField] private float notificationOffset;
    [SerializeField] private float textDuration;
    [SerializeField] private float fadeInDuration;
    [SerializeField] private float fadeOutDuration;

    private void Awake()
    {
        notificationText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        prefabID = GetComponent<PrefabID>();
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
        GameObject prefab = PrefabRegistry.Instance.GetByKey(prefabID.prefabKey);

        Vector2 from = rectTransform.anchoredPosition;
        Vector2 to = new(from.x, from.y + notificationOffset);
        UITween.SlideIn(rectTransform, from, to, textCompletionTime)
            .OnComplete(() => TextNotificationPool.Instance.Return(this, prefab));
    }
}
