using UnityEngine;

public class Furnace : MonoBehaviour, IInteractable
{
    private FurnaceUnit furnaceUnit;

    private void Awake()
    {
        furnaceUnit = GetComponent<FurnaceUnit>();
    }

    public void Interact()
    {
        FurnaceManager.Instance.Open(furnaceUnit); // opens this specific one
        InventoryManager.Instance.mainInventory.SetActive(true);
    }

    public string GetInteractText() => "Use Furnace";

    public Transform GetTransform() => transform;
}
