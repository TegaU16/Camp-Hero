using Game.Players;
using UnityEngine;

namespace Game.Inventory
{
    public class ItemEquip : MonoBehaviour
    {
        public static ItemEquip Instance;
        private GameObject currentEquippedItem;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void EquipItem(Item item)
        {
            if (currentEquippedItem != null)
                Destroy(currentEquippedItem);

            if (item != null && item.equippedPrefab != null && GameManager.Instance.playerInstance != null)
            {
                Player playerScript = GameManager.Instance.playerInstance.GetComponent<Player>();
                currentEquippedItem = Instantiate(item.equippedPrefab);
                currentEquippedItem.transform.SetParent(playerScript.itemHolder, false); // keep prefab offsets
            }
        }
    }
}
