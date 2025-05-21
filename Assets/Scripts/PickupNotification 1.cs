using UnityEngine;
using UnityEngine.UI;

public class PickupNotification : MonoBehaviour
{
    public Image itemIcon;
    public Text itemCountText;

    public void ShowPickup(Item item, int count)
    {
        itemIcon.sprite = item.icon;
        itemCountText.text = count.ToString() + "x " + item.name;
        gameObject.SetActive(true);
        // Optionally, add logic to hide after a few seconds
        Invoke(nameof(HideNotification), 3f); // Hide after 3 seconds
    }

    private void HideNotification()
    {
        gameObject.SetActive(false);
    }
}
