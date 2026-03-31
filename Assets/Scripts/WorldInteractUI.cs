using System.Collections;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldInteractUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private Image objectImage;
    [SerializeField] private Image objectImageBackground;
    [SerializeField] private RarityBackgroundConfig rarityBackgroundConfig;

    [SerializeField] private float uiHeightOffset = 1.5f;

    private Transform target;
    private Camera cam;

    private IInteractable currentInteractable;

    private void Start()
    {
        InteractableItemManager.Instance.OnInteractTextChanged += UpdateInteractText;
    }

    private void LateUpdate()
    {
        if (!GameManager.Instance.IsGameActive) return;
        if (target == null || cam == null) return;

        FaceCamera();
    }

    private void UpdateInteractText(string newText)
    {
        if (currentInteractable == null) return;
        interactText.text = newText;
    }

    public void Setup(IInteractable currentInteractable)
    {
        if (currentInteractable == null) return;

        this.currentInteractable = currentInteractable;
        target = currentInteractable.GetTransform();
        if (target == null) return;

        objectImage.sprite = currentInteractable.ObjectIcon;

        InteractableItem interactableItem = target.GetComponent<InteractableItem>();
        bool hasItem = interactableItem != null && interactableItem.item != null;

        objectImageBackground.sprite = hasItem
            ? rarityBackgroundConfig.GetBackground(interactableItem.item.rarity)
            : rarityBackgroundConfig.defaultBackground;

        interactText.text = currentInteractable.GetInteractText();
        cam = Camera.main;

        StartCoroutine(SetInitialPosition(moveTime: 0.2f));
    }

    private IEnumerator SetInitialPosition(float moveTime)
    {
        Bounds bounds = Utility.GetObjectBounds(target);

        Vector3 startPos = bounds.center + Vector3.up * bounds.extents.y;
        Vector3 finalPos = bounds.center + Vector3.up * uiHeightOffset;

        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveTime);

            transform.position = Vector3.Lerp(startPos, finalPos, t);

            yield return null;
        }

        transform.position = finalPos;
    }

    private void FaceCamera()
    {
        Vector3 camPos = cam.transform.position;
        camPos.y = transform.position.y; // lock vertical tilt

        transform.LookAt(camPos);
        transform.Rotate(0f, 180f, 0f);
    }
}
