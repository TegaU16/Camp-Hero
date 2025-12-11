using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArrowFillController : MonoBehaviour
{
    [Header("Mask Rect (the one that clips the arrow)")]
    public RectTransform maskRect;

    [Range(0f, 1f)]
    public float fillAmount = 1f;

    private float initialHeight;

    private void Start()
    {
        CacheInitialHeight();
        UpdateFill();
    }

    private void OnEnable()
    {
        CacheInitialHeight();
        UpdateFill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Delay update to avoid Unity editor internal errors
        EditorApplication.delayCall += () =>
        {
            if (this == null) return; // Object may have been deleted
            CacheInitialHeight();
            UpdateFill();
        };
    }
#endif

    private void Update()
    {
        if (Application.isPlaying)
            UpdateFill();
    }

    private void CacheInitialHeight()
    {
        if (maskRect != null && initialHeight <= 0f)
            initialHeight = maskRect.sizeDelta.y;
    }

    private void UpdateFill()
    {
        if (maskRect == null) return;

        Vector2 size = maskRect.sizeDelta;
        size.y = initialHeight * Mathf.Clamp01(fillAmount);
        maskRect.sizeDelta = size;
    }
}
