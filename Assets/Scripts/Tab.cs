using UnityEngine;
using UnityEngine.UI;

public class Tab : MonoBehaviour
{
    public Image image;
    public Sprite selectedImage, unselectedImage;
    public GameObject menu;

    private TabArea tabArea;

    private void Start()
    {
        image.raycastTarget = true;
        tabArea = GetComponentInParent<TabArea>();
    }

    // Called by button
    public void OnTabClicked() => tabArea.SelectTab(this);

    public void Select()
    {
        image.sprite = selectedImage;
        menu.SetActive(true);
    }

    public void Deselect()
    {
        image.sprite = unselectedImage;
        menu.SetActive(false);
    }
}
