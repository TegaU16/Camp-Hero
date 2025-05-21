using UnityEngine;

public class Campfire : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        InventoryManager.Instance.campfireMenuUI.SetActive(true);
    }

    public string GetInteractText() => "Open Campfire Menu";

    public Transform GetTransform() => transform;

    public void Die()
    {
        GameManager.Instance.GameOver();
    }
}
