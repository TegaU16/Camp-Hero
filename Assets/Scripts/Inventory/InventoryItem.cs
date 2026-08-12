using System.Collections;
using DG.Tweening;
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

        [HideInInspector] public int count = 1;
        [SerializeField] private TextMeshProUGUI countText;

        [SerializeField] private float padding;

        [HideInInspector] public Image image;

        [HideInInspector] public bool isBeingDragged;
        private InventorySlot originalParentSlot;

        private Canvas dragCanvas;

        private void Awake() => image = GetComponent<Image>();

        private void Start()
        {
            countText.raycastTarget = false;

            StartCoroutine(RefreshCount());
            originalParentSlot = transform.parent.GetComponent<InventorySlot>();
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

            if (isActiveAndEnabled)
                StartCoroutine(RefreshCount());
            StretchToFit(padding);
        }

        public void PlaceInSlot(InventorySlot slot)
        {
            if (slot == null) return;

            transform.SetParent(slot.transform);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;

            StretchToFit(padding);

            originalParentSlot = slot;
            isBeingDragged = false;

            // Re-enable raycast targeting once placed in the slot
            image.raycastTarget = true;

            ItemTooltipUI.Instance.HideTooltip();

            if (originalParentSlot.TryGetComponent(out InventorySlot parentSlot))
                InventoryManager.Instance.OnInventoryItemChanged?.Invoke(parentSlot);

            if (!InventoryManager.Instance.InventorySlots.Contains(slot)) return;

            InventoryManager.Instance.SetTutorialForItem(item);
        }

        public void RevertToOriginalSlot() => PlaceInSlot(originalParentSlot);

        public IEnumerator RefreshCount()
        {
            if (count <= 1)
            {
                countText.gameObject.SetActive(false);
                yield break;
            }

            countText.text = count.ToString();
            countText.gameObject.SetActive(true);

            RectTransform rect = (RectTransform)countText.transform;
            yield return UITween.ScaleOut(rect, to: 0.8f, disableOnComplete: false).WaitForCompletion();
            yield return UITween.ScaleIn(rect, from: 0.8f).WaitForCompletion();
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

        public void SwapWith(InventoryItem other)
        {
            if (other == null) return;

            InventorySlot mySlot = originalParentSlot;
            InventorySlot otherSlot = other.transform.parent.GetComponent<InventorySlot>();

            if (mySlot == null || otherSlot == null) return;

            other.PlaceInSlot(mySlot);
            PlaceInSlot(otherSlot);

            ItemTooltipUI.Instance.HideTooltip();
        }
    }
}
