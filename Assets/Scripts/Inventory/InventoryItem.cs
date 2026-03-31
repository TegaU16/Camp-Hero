using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Inventory
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static InventoryItem selectedItem;

        [HideInInspector] public Item item;

        public int count = 1;
        public TextMeshProUGUI countText;

        public float padding;

        [HideInInspector] public Image image;

        [HideInInspector] public bool isBeingDragged;
        private Transform originalParentSlot;

        private Canvas dragCanvas;

        private void Awake() => image = GetComponent<Image>();

        private void Start()
        {
            RefreshCount();
            originalParentSlot = transform.parent; // Set the original parent

            // Disable raycast on countText to prevent it from interfering with slot detection
            if (countText != null)
                countText.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (ItemTooltipUI.Instance != null && ItemTooltipUI.Instance.HoveredItem == item)
                ItemTooltipUI.Instance.HideTooltip();
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
            image.rectTransform.localScale = Vector3.one;

            // Scale the image to fit within the slot
            RectTransform slotRect = transform.parent.GetComponent<RectTransform>();
            RectTransform imageRect = image.rectTransform;

            // Set the image to match the parent's size
            imageRect.sizeDelta = slotRect.sizeDelta;

            RefreshCount();
            StretchToFit(padding);
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

            ItemTooltipUI.Instance.HideTooltip();

            if (originalParentSlot.TryGetComponent(out InventorySlot parentSlot))
                InventoryManager.Instance.OnInventoryItemChanged?.Invoke(parentSlot);

            if (!slotTransform.TryGetComponent(out InventorySlot slot)) return;
            if (!InventoryManager.Instance.InventorySlots.Contains(slot)) return;

            InventoryManager.Instance.SetTutorialForItem(item);
        }

        public void RevertToOriginalSlot() => PlaceInSlot(originalParentSlot);

        public void RefreshCount()
        {
            if (count <= 1)
            {
                countText.gameObject.SetActive(false);
                return;
            }

            countText.text = count.ToString();
            countText.gameObject.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!GameManager.Instance.IsGameActive) return;

            if (selectedItem == null)
                ItemTooltipUI.Instance.ShowTooltip(item, GetComponent<RectTransform>());
        }

        public void OnPointerExit(PointerEventData eventData) => ItemTooltipUI.Instance.HideTooltip();

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
            if (dragCanvas == null) return;

            dragCanvas.overrideSorting = false;
            dragCanvas.sortingOrder = 0;
        }
    }
}
