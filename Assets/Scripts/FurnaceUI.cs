using UnityEngine;
using UnityEngine.UI;

public class FurnaceUI : MonoBehaviour
{
    public InventorySlot inputSlot;
    public InventorySlot outputSlot;
    public InventorySlot fuelSlot;

    public Slider fuelBar;
    public Image progressBar;

    private FurnaceUnit linkedFurnace;

    public void Open(FurnaceUnit unit)
    {
        linkedFurnace = unit;
        gameObject.SetActive(true);
        InventoryManager.Instance.mainInventory.SetActive(true);
        InventoryManager.Instance.OnInventoryOpen();

        linkedFurnace.inputSlot = inputSlot;
        linkedFurnace.outputSlot = outputSlot;
        linkedFurnace.fuelSlot = fuelSlot;

        linkedFurnace.LoadUI(); // Load furnace data into the UI
    }

    public void Close()
    {
        if (linkedFurnace != null)
        {
            linkedFurnace.SaveUI(); // Save the UI data back into furnace
            linkedFurnace.inputSlot = null;
            linkedFurnace.outputSlot = null;
            linkedFurnace.fuelSlot = null;
        }

        linkedFurnace = null;
        gameObject.SetActive(false);
        InventoryManager.Instance.mainInventory.SetActive(false);
        InventoryManager.Instance.darkBackground.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (linkedFurnace != null)
        {
            if (fuelBar != null)
                fuelBar.value = linkedFurnace.GetFuelRatio();
            if (progressBar != null)
                progressBar.fillAmount = linkedFurnace.smeltProgress;
        }
    }
}
