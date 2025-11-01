using UnityEngine;

public class FoodManager : MonoBehaviour
{
    private GameObject playerObj;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;

            if (InventoryManager.Instance.IsExtensionOpen()) return;

            Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
            if (selectedItem == null || (selectedItem.itemTypes & ItemType.Food) == 0) return;

            Eat(selectedItem);
        }
    }

    private void Eat(Item food)
    {
        if (!playerObj.TryGetComponent(out Health playerHealth)) return;

        if (playerHealth.GetHealth() < playerHealth.maxHealth)
        {
            playerHealth.AddHealth((int)food.foodValue);
            InventoryManager.Instance.UseSelectedItem();
        }
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
            playerObj = player;
    }
}
