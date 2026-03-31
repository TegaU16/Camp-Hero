using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game.Inventory
{
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance;

        [SerializeField] private GameObject expandedContentRoot;
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI attributesText;
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

        private void Update()
        {
            if (currentTarget == null)
            {
                HideTooltip();
                return;
            }

            if (!currentTarget.gameObject.activeInHierarchy)
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

            HoveredItem = item;

            currentTarget = itemTransform;
            titleText.text = item.itemName;

            if (item.toolAttribute != null)
                attributesText.text = item.toolAttribute.GetAttributesText();

            expandedContentRoot.SetActive(false);

            // Fix height
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight + padding);

            // Fit width
            float width = titleText.preferredWidth + padding;
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + padding);

            Vector3[] corners = new Vector3[4];
            itemTransform.GetWorldCorners(corners); // [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right

            transform.position = corners[2];
            gameObject.SetActive(true);
        }

        public void ExpandTooltip(bool expand)
        {
            expandedContentRoot.SetActive(expand);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            RectTransform tooltipRect = transform as RectTransform;

            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);

            Vector3 offset = Vector3.zero;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Right edge
            if (corners[2].x > screenWidth)
                offset.x = screenWidth - corners[2].x;

            // Left edge
            if (corners[0].x < 0)
                offset.x = -corners[0].x;

            // Top edge
            if (corners[1].y > screenHeight)
                offset.y = screenHeight - corners[1].y;

            // Bottom edge
            if (corners[0].y < 0)
                offset.y = -corners[0].y;

            tooltipRect.position += offset;
        }

        public void HideTooltip()
        {
            HoveredItem = null;
            expandedContentRoot.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
