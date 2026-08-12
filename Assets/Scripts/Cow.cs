using Game.Inventory;
using UnityEngine;
using Worlds;

namespace Game.AI.Animals
{
    public class Cow : Animal, IInteractable
    {
        [SerializeField] private Sprite milkIcon;
        public Sprite ObjectIcon => milkIcon;

        [SerializeField] private Item bucket;
        [SerializeField] private Item milkBucket;

        public string GetInteractText() => "Milk\n<color=#27ef60>\"E\"</color>";

        public Transform GetTransform() => transform;

        public void Interact()
        {
            Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
            if (selectedItem == null || selectedItem.itemName != bucket.itemName) return;

            InventoryManager.Instance.UseSelectedItem();

            bool added = InventoryManager.Instance.AddItem(milkBucket);
            if (!added)
            {
                ItemSpawner.Spawn(
                    milkBucket,
                    transform.position + Vector3.up * 2.5f,
                    itemCount: 1,
                    scatter: true,
                    scatterDistance: 2f,
                    scatterForce: 3f
                );
            }

            WorldSession.CurrentRunStats.cowsViolated++;
        }        
    }
}
