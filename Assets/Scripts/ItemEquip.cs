using UnityEngine;

public class ItemEquip : MonoBehaviour
{
    private GameObject currentEquippedItem;

    public void EquipItem(Item item)
    {
        if (currentEquippedItem != null)
        {
            Destroy(currentEquippedItem);
        }

        if (item != null && item.equippedPrefab != null && GameManager.Instance.playerInstance != null)
        {
            Player playerScript = GameManager.Instance.playerInstance.GetComponent<Player>();
            currentEquippedItem = Instantiate(item.equippedPrefab, playerScript.itemHolder);
            currentEquippedItem.transform.localPosition = Vector3.zero;
        }
    }
}