using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class HoverMove : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Movement Settings")]
    public Vector2 hoverOffset = new(0f, 20f);
    public float duration = 0.3f;
    public Ease ease = Ease.OutQuad;

    private RectTransform rect;
    private Vector2 originalPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOAnchorPos(originalPos + hoverOffset, duration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOKill();
        rect.DOAnchorPos(originalPos, duration).SetEase(ease);
    }
}
