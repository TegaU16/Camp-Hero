using UnityEngine;
using UnityEngine.UI;

public class OverlayImageManager : MonoBehaviour
{
    public Image[] overlayImages;

    void Start()
    {
        // Disable raycasting on the overlay images
        for (int i = 0; i < overlayImages.Length; i++)
        {
            if (overlayImages[i] != null)
                overlayImages[i].raycastTarget = false;
        }
    }
}
