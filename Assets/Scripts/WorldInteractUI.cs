using UnityEngine;
using UnityEngine.UI;

public class WorldInteractUI : MonoBehaviour
{
    public Text interactText;
    private Transform target;
    private Camera cam;

    public void Setup(string text, Transform targetTransform)
    {
        interactText.text = text;
        target = targetTransform;
        cam = Camera.main;
    }

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (target == null) return;

        // Hover slightly above the object
        transform.position = target.position + Vector3.up * 2f;

        // Always face the camera
        transform.forward = cam.transform.forward;
    }
}
