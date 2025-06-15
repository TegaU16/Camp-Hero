using UnityEngine;
using TMPro;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    public RectTransform backgroundRect;
    public TextMeshProUGUI tooltipText;

    public float padding = 10f;
    public float fixedHeight = 10f;

    private void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(string itemName, RectTransform itemTransform)
    {
        tooltipText.text = itemName;

        // Fix height
        tooltipText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight + padding);

        // Fit width
        float width = tooltipText.preferredWidth + padding;
        tooltipText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + padding);

        Vector3[] corners = new Vector3[4];
        itemTransform.GetWorldCorners(corners); // [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right

        // You can pick top-left or top-right depending on preference
        transform.position = corners[2];

        gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}
