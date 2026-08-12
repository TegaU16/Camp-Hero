using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game.Inventory
{
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance;

        [SerializeField] private GameObject attributeContentRoot;
        private RectTransform attributeRect;
        private CanvasGroup attributeGroup;

        [SerializeField] private GameObject foodContentRoot;
        private RectTransform foodRect;
        private CanvasGroup foodGroup;

        [SerializeField] private GameObject tooltipExpansionSuggestion;

        [SerializeField] private RectTransform backgroundRect;

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI attributesText;
        [SerializeField] private TextMeshProUGUI healthGainText;
        private RectTransform currentTarget;

        [SerializeField] private float padding = 10f;
        [SerializeField] private float fixedHeight = 10f;

        public Item HoveredItem {  get; private set; }
        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            Instance = this;
            HideTooltip();
        }

        private void Start()
        {
            attributeRect = attributeContentRoot.GetComponent<RectTransform>();
            attributeGroup = attributeContentRoot.GetComponent<CanvasGroup>();

            foodRect = foodContentRoot.GetComponent<RectTransform>();
            foodGroup = foodContentRoot.GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                HideTooltip();
        }

        private void LateUpdate()
        {
            if (!gameObject.activeSelf || currentTarget == null) return;

            Vector3[] corners = new Vector3[4];
            currentTarget.GetWorldCorners(corners);

            transform.position = corners[2]; // top-right
        }

        public void ShowTooltip(Item item, RectTransform itemTransform)
        {
            if (item == null) return;

            tooltipExpansionSuggestion.SetActive(false);
            attributeContentRoot.SetActive(false);
            foodContentRoot.SetActive(false);

            HoveredItem = item;

            currentTarget = itemTransform;
            titleText.text = item.itemName;

            if (item.toolAttribute != null)
            {
                attributesText.text = item.toolAttribute.GetAttributesText();
                tooltipExpansionSuggestion.SetActive(true);
            }

            if (item.foodValue > 0)
            {
                healthGainText.text = $"+{item.foodValue}";
                tooltipExpansionSuggestion.SetActive(true);
            }

            // Fix height
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight + padding);

            // Fit width
            float width = titleText.preferredWidth + padding;
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + padding);

            Vector3[] corners = new Vector3[4];
            itemTransform.GetWorldCorners(corners); // [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent,
                screenPoint,
                null,
                out Vector2 localPoint
            );

            ((RectTransform)transform).anchoredPosition = localPoint;
            gameObject.SetActive(true);
        }

        public void ExpandTooltip()
        {
            tooltipExpansionSuggestion.SetActive(false);

            if (HoveredItem.toolAttribute != null)
                UITween.PopIn(attributeRect, attributeGroup);
            else if (HoveredItem.foodValue > 0)
                UITween.PopIn(foodRect, foodGroup);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            RectTransform tooltipRect = (RectTransform)transform;
            RectTransform canvasRect = tooltipRect.root as RectTransform;

            Vector2 anchoredPos = tooltipRect.anchoredPosition;

            Vector2 min = canvasRect.rect.min;
            Vector2 max = canvasRect.rect.max;

            Vector2 size = tooltipRect.rect.size;

            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, min.x + halfWidth, max.x - halfWidth);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, min.y + halfHeight, max.y - halfHeight);

            tooltipRect.anchoredPosition = anchoredPos;
        }

        public void HideTooltip()
        {
            HoveredItem = null;
            attributeContentRoot.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
