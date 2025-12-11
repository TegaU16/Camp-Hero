using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class InteractiveButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public Image image;
    public List<TextMeshProUGUI> buttonTexts;
    public ButtonVisualProfile visualProfile;

    [SerializeField] private bool isSelected = false;
    private bool isPressed = false;

    [HideInInspector] public bool isActive;
    private readonly string clickSound = "click-sound-432501 (mp3cut.net)";

    void Start()
    {
        if (image == null)
            image = GetComponent<Image>();

        isActive = true;
        UpdateVisualState();
    }

    public void Select()
    {
        isSelected = true;
        UpdateVisualState();
    }

    public void Deselect()
    {
        isSelected = false;
        UpdateVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable || isPressed) return;

        SetVisualState(visualProfile.hoverSprite, visualProfile.hoverTextColor, visualProfile.hoverOutlineColor, visualProfile.hoverUnderlayColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelected) return;

        isPressed = true;
        UpdateVisualState();

        AudioManager.Instance.PlaySFX(clickSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (isPressed)
            SetVisualState(visualProfile.pressedSprite, visualProfile.pressedTextColor, visualProfile.pressedOutlineColor, visualProfile.pressedUnderlayColor);
        else if (isSelected)
            SetVisualState(visualProfile.selectedSprite, visualProfile.selectedTextColor, visualProfile.selectedOutlineColor, visualProfile.selectedUnderlayColor);
        else
            SetVisualState(visualProfile.unselectedSprite, visualProfile.unselectedTextColor, visualProfile.unselectedOutlineColor, visualProfile.unselectedUnderlayColor);
    }

    private void SetVisualState(Sprite sprite, Color textColor, Color outlineColor, Color underlayColor)
    {
        if (!isActive) return;

        if (image != null && sprite != null)
            image.sprite = sprite;

        foreach (TextMeshProUGUI buttonText in buttonTexts)
        {
            if (buttonText != null)
            {
                buttonText.color = textColor;

                // Update outline colour
                buttonText.outlineColor = outlineColor;
                buttonText.outlineWidth = outlineColor.a > 0 ? 0.2f : 0f;

                // Update underlay color (requires underlay enabled in TMP shader)
                buttonText.fontMaterial.SetColor("_UnderlayColor", underlayColor);
            }
        }
    }
}
