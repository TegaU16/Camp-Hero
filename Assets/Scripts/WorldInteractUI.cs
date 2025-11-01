using TMPro;
using UnityEngine;

public class WorldInteractUI : MonoBehaviour
{
    public TextMeshProUGUI interactText;
    private Transform target;
    private Camera cam;
    private float heightOffset;

    private void Update()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (target == null) return;

        // still update each frame in case player/camera moves
        RefreshImmediately();
    }
    public void Setup(string text, Transform targetTransform)
    {
        interactText.text = text;
        target = targetTransform;
        cam = Camera.main;

        Renderer rend = target.GetComponentInChildren<Renderer>();
        if (rend != null)
            heightOffset = rend.bounds.extents.y + 1f;
        else
            heightOffset = 1.34f; // fallback

        // Immediately refresh the position this frame
        RefreshImmediately();
    }

    public void RefreshImmediately()
    {
        if (target == null || cam == null) return;

        transform.position = target.position + Vector3.up * heightOffset;
        transform.forward = cam.transform.forward;
    }
}
