using Game;
using TMPro;
using UnityEngine;

public class WorldInteractUI : MonoBehaviour
{
    public TextMeshProUGUI interactText;
    private Transform target;
    private Camera cam;
    public float uiHeightOffset = 1.5f;
    public float surfaceOffset = 0.15f;

    private void Update()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (target == null) return;

        if (cam != null)
            transform.forward = cam.transform.forward;
    }

    public void Setup(IInteractable currentInteractable)
    {
        target = currentInteractable.GetTransform();
        if (target == null) return;

        interactText.text = currentInteractable.GetInteractText();
        cam = Camera.main;

        SetInitialPosition();
    }

    private void SetInitialPosition()
    {
        if (target == null) return;

        Bounds bounds = Utility.GetObjectBounds(target);

        Vector3 finalPos = bounds.center + Vector3.up * uiHeightOffset;

        transform.position = finalPos;
        transform.forward = cam.transform.forward;
    }
}
