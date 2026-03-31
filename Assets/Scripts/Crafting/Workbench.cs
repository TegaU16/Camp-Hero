using UnityEngine;

namespace Game.Crafting
{
    public class Workbench : MonoBehaviour, IInteractable
    {
        public Sprite workbenchIcon;
        public Sprite ObjectIcon => workbenchIcon;

        public void Interact()
        {
            if (CraftingManager.Instance != null)
                CraftingUI.Instance.ToggleCraftingMenu(CraftingSource.Workbench);
        }

        public string GetInteractText() => "Craft\n<color=#27ef60>\"E\"</color>";

        public Transform GetTransform() => transform;
    }
}
