using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static InventoryItem selectedItem;

    [HideInInspector] public Item item;

    public int count = 1;
    public TextMeshProUGUI countText;

    private Transform originalParent;
    public float padding;

    [HideInInspector] public Image image;
    public CanvasGroup canvasGroup;

    [HideInInspector] public bool isBeingDragged;
    [HideInInspector] public Transform originalParentSlot;

    private Canvas dragCanvas;

    private void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        RefreshCount();
        originalParent = transform.parent; // Set the original parent

        // Disable raycast on countText to prevent it from interfering with slot detection
        if (countText != null)
        {
            countText.raycastTarget = false;
        }
    }

    public void SetItem(Item newItem, int itemCount = 1)
    {
        item = newItem;
        count = itemCount;

        if (image == null)
        {
            image = GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("SetItem: Image is still null!");
                return;
            }
        }

        image.sprite = item.icon;
        image.rectTransform.localScale = Vector3.one; // Reset scale

        // Scale the image to fit within the slot
        RectTransform slotRect = transform.parent.GetComponent<RectTransform>(); // The parent slot
        RectTransform imageRect = image.rectTransform;

        // Set the image to match the parent's size
        imageRect.sizeDelta = slotRect.sizeDelta;

        RefreshCount();
        StretchToFit(padding);

        InventoryManager.Instance.TryDiscoverItem(item);
    }

    public void PlaceInSlot(Transform slotTransform)
    {
        transform.SetParent(slotTransform);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        StretchToFit(padding);

        originalParentSlot = slotTransform;
        isBeingDragged = false;

        // Re-enable raycast targeting once placed in the slot
        image.raycastTarget = true;

        // Update the original parent to the new slot
        originalParent = transform.parent;
    }

    public void RevertToOriginalSlot()
    {
        PlaceInSlot(originalParent);
    }

    public void RefreshCount()
    {
        if (count > 1)
        {
            countText.text = count.ToString();
            countText.gameObject.SetActive(true);
        }
        else
        {
            countText.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null)
        {
            ItemTooltipUI.Instance.ShowTooltip(item.itemName, GetComponent<RectTransform>());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance.HideTooltip();
    }

    public void StretchToFit(float padding)
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);

        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
    }

    public void EnableDragLayering()
    {
        if (dragCanvas == null)
            dragCanvas = gameObject.GetComponent<Canvas>();

        if (dragCanvas == null)
            dragCanvas = gameObject.AddComponent<Canvas>();

        dragCanvas.overrideSorting = true;
        dragCanvas.sortingOrder = 9999;

        // Optional: add GraphicRaycaster if needed
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    public void DisableDragLayering()
    {
        if (dragCanvas != null)
        {
            dragCanvas.overrideSorting = false;
            dragCanvas.sortingOrder = 0;
        }
    }
}
