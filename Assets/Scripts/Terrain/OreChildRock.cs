using Game.Inventory;
using UnityEngine;

namespace Game.Terrain
{
    public class OreChildRock : MonoBehaviour, IInteractable
    {
        [SerializeField] private int index;
        [SerializeField] private OreRockGroup parentOre;
        [SerializeField] private GameObject pickupPrefab;

        [SerializeField] private Sprite stoneIcon;
        public Sprite ObjectIcon => stoneIcon;

        public string GetInteractText()
        {
            IInteractable interactable = this;
            return $"Stone\n(1x)\n{interactable.InteractKeyText}";
        }

        public Transform GetTransform() => transform;

        public void Interact()
        {
            parentOre.RemoveRock(index);
            InventoryManager.Instance.AddItem(pickupPrefab.GetComponent<InteractableItem>().item, 1);
        }
    }
}
