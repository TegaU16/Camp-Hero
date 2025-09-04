using TMPro;
using UnityEngine;

public class WorldInteractUI : MonoBehaviour
{
    public TextMeshProUGUI interactText;
    private Transform target;
    private Camera cam;
    private float heightOffset;

    public void Setup(string text, Transform targetTransform)
    {
        interactText.text = text;
        target = targetTransform;
        cam = Camera.main;

        Renderer rend = target.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            heightOffset = rend.bounds.extents.y + 1f;
        }
        else
        {
            heightOffset = 1.34f; // fallback
        }
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;
        if (target == null) return;

        transform.position = target.position + Vector3.up * heightOffset;

        // Always face the camera
        transform.forward = cam.transform.forward;
    }
}
