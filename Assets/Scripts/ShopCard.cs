using UnityEngine;
using UnityEngine.UI;

public class ShopCard : MonoBehaviour
{
    public Item item;
    public Image image;
    public Text itemName;
    public int cost;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image.sprite = item.icon;
        itemName.text = item.name;
    }

    public void BuyItem()
    {
        if (!InventoryManager.Instance.IsInventoryFullForItem(item)) 
        {
            if (InventoryManager.Instance.RemoveGold(cost))
            {
                InventoryManager.Instance.AddItem(item);
            }
        }
    }
}
