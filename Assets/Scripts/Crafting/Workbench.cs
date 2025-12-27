using UnityEngine;

namespace Game.Crafting
{
    public class Workbench : MonoBehaviour, IInteractable
    {
        public void Interact()
        {
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.craftingUI.ToggleCraftingMenu(CraftingSource.Workbench);
        }

        public string GetInteractText()
        {
            return "";
        }

        public Transform GetTransform()
        {
            return transform;
        }
    }
}
