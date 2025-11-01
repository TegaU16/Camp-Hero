using UnityEngine;

[CreateAssetMenu(fileName = "ButtonVisualProfile", menuName = "UI/Button Visual Profile")]
public class ButtonVisualProfile : ScriptableObject
{
    [Header("Sprites")]
    public Sprite unselectedSprite;
    public Sprite selectedSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;

    [Header("Text Colors")]
    public Color unselectedTextColor = Color.white;
    public Color selectedTextColor = Color.white;
    public Color hoverTextColor = Color.white;
    public Color pressedTextColor = Color.white;

    [Header("Outline Colors")]
    public Color unselectedOutlineColor = Color.black;
    public Color selectedOutlineColor = Color.black;
    public Color hoverOutlineColor = Color.black;
    public Color pressedOutlineColor = Color.black;

    [Header("Underlay Colors")]
    public Color unselectedUnderlayColor = Color.clear;
    public Color selectedUnderlayColor = Color.clear;
    public Color hoverUnderlayColor = Color.clear;
    public Color pressedUnderlayColor = Color.clear;
}
