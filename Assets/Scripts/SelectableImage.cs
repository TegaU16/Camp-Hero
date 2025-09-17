using UnityEngine;
using UnityEngine.UI;

public class SelectableImage : MonoBehaviour
{
    public Image image;
    public Sprite selectedImage, unselectedImage;

    public void Select()
    {
        image.sprite = selectedImage;
    }

    public void Deselect()
    {
        image.sprite = unselectedImage;
    }
}
