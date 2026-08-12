using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CompassBar : MonoBehaviour
{
    private RectTransform bar;
    private Camera mainCam;

    [SerializeField] private RectTransform northMarker;
    [SerializeField] private RectTransform eastMarker;
    [SerializeField] private RectTransform southMarker;
    [SerializeField] private RectTransform westMarker;

    private Vector3 campfirePosition;
    [SerializeField] private RectTransform campfireMarker;

    private Vector3 deathPosition;
    [SerializeField] private RectTransform deathMarker;

    [Range(30f, 360f)]
    [SerializeField] private float visibleFOV = 180f;

    private float barWidth;

    void Start()
    {
        bar = GetComponent<RectTransform>();
        barWidth = bar.rect.width;
        mainCam = Camera.main;
    }

    void Update()
    {
        if (mainCam == null || bar == null) return;

        Vector3 forward = mainCam.transform.forward;
        forward.y = 0;

        float cameraAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        MoveCardinalMarkers(cameraAngle);
        MoveIcon(campfireMarker, campfirePosition, cameraAngle);

        if (deathPosition != Vector3.zero)
            MoveIcon(deathMarker, deathPosition, cameraAngle);
    }

    private void MoveCardinalMarkers(float cameraAngle)
    {
        SetMarkerPosition(northMarker, cameraAngle, 0);
        SetMarkerPosition(eastMarker, cameraAngle, 90);
        SetMarkerPosition(southMarker, cameraAngle, 180);
        SetMarkerPosition(westMarker, cameraAngle, 270);
    }

    private void MoveIcon(RectTransform marker, Vector3 target, float cameraAngle)
    {
        if (target == null || marker == null) return;

        Vector3 toTarget = target - mainCam.transform.position;
        toTarget.y = 0;

        if (toTarget.sqrMagnitude < 0.01f) return; // avoid NaN

        float targetAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float relativeAngle = Mathf.DeltaAngle(cameraAngle, targetAngle);
        float offset = (relativeAngle / visibleFOV) * barWidth;
        marker.anchoredPosition = new Vector2(offset, marker.anchoredPosition.y);

        float distanceFromCenter = Mathf.Abs(offset);
        if (distanceFromCenter > barWidth / 2f)
        {
            SetAlpha(marker, 0f);
            return;
        }

        float alpha = Mathf.Clamp01(1f - (distanceFromCenter / (barWidth / 2f)));
        SetAlpha(marker, alpha);
    }

    private void SetMarkerPosition(RectTransform marker, float cameraAngle, float markerAngle)
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

    private void SetAlpha(RectTransform marker, float alpha)
    {
        if (marker.TryGetComponent(out CanvasGroup group))
        {
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.01f;
            group.interactable = alpha > 0.01f;
        }
        else if (marker.TryGetComponent(out Image image))
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    public void SetCampfireTransform(GameObject campfire)
    {
        if (campfire != null) 
            campfirePosition = campfire.transform.position;
    }

    public void SetDeathMarkerActive(bool active, Vector3 deathPos)
    {
        if (!active)
        {
            deathMarker.gameObject.SetActive(false);
            deathPosition = Vector3.zero;
            return;
        }

        deathPosition = deathPos;
        deathMarker.gameObject.SetActive(true);
    }
}
