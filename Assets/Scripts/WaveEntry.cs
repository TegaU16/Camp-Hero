using Unity.VectorGraphics;
using UnityEngine;

public class WaveEntry : MonoBehaviour
{
    public SVGImage image;
    public Sprite selectedImage, unselectedImage;
    public Transform enemyListContainer;

    public bool IsSelected() => image.sprite == selectedImage;

    public void Select()
    {
        image.sprite = selectedImage;
    }

    public void Deselect()
    {
        image.sprite = unselectedImage;
    }
}
