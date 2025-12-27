using Game.Inventory;
using UnityEngine;

namespace Game.Food
{
    public class FoodManager : MonoBehaviour
    {
        public static FoodManager Instance;

        private GameObject playerObj;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (!Input.GetMouseButtonDown(1)) return;
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (InventoryManager.Instance.IsExtensionOpen()) return;

            Item selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
            if (selectedItem == null || (selectedItem.itemTypes & ItemType.Food) == 0) return;

            Eat(selectedItem);
        }

        private void Eat(Item food)
        {
            if (!playerObj.TryGetComponent(out Health playerHealth)) return;
            if (playerHealth.GetHealth() == playerHealth.maxHealth) return;

            playerHealth.AddHealth((int)food.foodValue);
            InventoryManager.Instance.UseSelectedItem();
        }

        public void SetPlayer(GameObject player)
        {
            if (player != null)
                playerObj = player;
        }
    }
}
