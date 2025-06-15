using UnityEngine;
using UnityEngine.UI;

public class CompassBar : MonoBehaviour
{
    public RectTransform bar;
    private Transform cameraTransform;

    public RectTransform northMarker;
    public RectTransform eastMarker;
    public RectTransform southMarker;
    public RectTransform westMarker;

    private Transform campfireTransform;
    public RectTransform campfireIcon;

    [Range(30f, 360f)]
    public float visibleFOV = 180f;

    private float barWidth;

    void Start()
    {
        barWidth = bar.rect.width;
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Camera.main == null || bar == null) return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0;

        float cameraAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        MoveCardinalMarkers(cameraAngle);
        MoveCampfireIcon(cameraAngle);
    }

    void MoveCardinalMarkers(float cameraAngle)
    {
        SetMarkerPosition(northMarker, cameraAngle, 0);
        SetMarkerPosition(eastMarker, cameraAngle, 90);
        SetMarkerPosition(southMarker, cameraAngle, 180);
        SetMarkerPosition(westMarker, cameraAngle, 270);
    }

    void MoveCampfireIcon(float cameraAngle)
    {
        if (campfireTransform == null || campfireIcon == null || cameraTransform == null) return;

        Vector3 toCampfire = campfireTransform.position - cameraTransform.position;
        toCampfire.y = 0;

        if (toCampfire.sqrMagnitude < 0.01f) return; // avoid NaN

        float campfireAngle = Mathf.Atan2(toCampfire.x, toCampfire.z) * Mathf.Rad2Deg;
        float relativeAngle = Mathf.DeltaAngle(cameraAngle, campfireAngle);
        float offset = (relativeAngle / visibleFOV) * barWidth;
        campfireIcon.anchoredPosition = new Vector2(offset, campfireIcon.anchoredPosition.y);

        float distanceFromCenter = Mathf.Abs(offset);
        if (distanceFromCenter > barWidth / 2f)
        {
            SetAlpha(campfireIcon, 0f);
            return;
        }

        float alpha = Mathf.Clamp01(1f - (distanceFromCenter / (barWidth / 2f)));
        SetAlpha(campfireIcon, alpha);
    }

    void SetMarkerPosition(RectTransform marker, float cameraAngle, float markerAngle)
    {
        float angleDiff = Mathf.DeltaAngle(cameraAngle, markerAngle);
        float markerPosition = (angleDiff / visibleFOV) * barWidth;

        float halfWidth = barWidth / 2f;
        float distanceFromCenter = Mathf.Abs(markerPosition);

        if (distanceFromCenter > halfWidth)
        {
            SetAlpha(marker, 0f);
            return;
        }

        marker.anchoredPosition = new Vector2(markerPosition, marker.anchoredPosition.y);
        float alpha = Mathf.Clamp01(1f - (distanceFromCenter / halfWidth));
        SetAlpha(marker, alpha);
    }

    void SetAlpha(RectTransform marker, float alpha)
    {
        if (marker.TryGetComponent(out CanvasGroup group))
        {
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.01f;
            group.interactable = alpha > 0.01f;
        }
        else
        {
            // fallback if CanvasGroup isn't found, for safety
            if (marker.TryGetComponent(out Image image))
            {
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
        }
    }

    public void SetPlayer(GameObject playerObj)
    {
        if (playerObj != null)
        {
            Camera cam = playerObj.GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    public void SetCampfireTransform(GameObject campfire)
    {
        if (campfire != null) campfireTransform = campfire.transform;
    }
}
