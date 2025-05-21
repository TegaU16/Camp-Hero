using UnityEngine;

public class Workbench : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.ToggleCraftingMenu();
        }
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
