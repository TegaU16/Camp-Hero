using UnityEngine;

[System.Serializable]
public class FurnaceSlot
{
    public string itemName;
    public int count;

    [System.NonSerialized]
    public Item item;

    public void SyncNameFromItem()
    {
        itemName = item != null ? item.name : null;
    }

    public void ResolveItemFromName()
    {
        item = !string.IsNullOrEmpty(itemName) ? ItemRegistry.GetItemByName(itemName) : null;
    }
}

public class FurnaceUnit : MonoBehaviour, ISaveableObject
{
    public FuelComponent fuel = new();
    public FurnaceSlot inputData = new();
    public FurnaceSlot outputData = new();
    public FurnaceSlot fuelData = new();

    [HideInInspector] public InventorySlot inputSlot;
    [HideInInspector] public InventorySlot outputSlot;
    [HideInInspector] public InventorySlot fuelSlot;

    [HideInInspector] public float smeltProgress;

    public float smeltDuration = 5f;
    private float smeltRate;

    private void Start()
    {
        smeltRate = 1.0f / smeltDuration;
    }

    private void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (inputSlot != null)
            SaveUI();

        // Always use our own data, not the UI
        if (fuel.currentFuel < fuel.maxFuel && fuelData.item != null && fuelData.count > 0)
        {
            if (fuelData.item.fuelValue + fuel.currentFuel <= fuel.maxFuel)
            {
                fuel.AddFuel(fuelData.item);
                fuelData.count--;

                if (fuelData.count <= 0)
                    fuelData.item = null;

                if (inputSlot != null)
                    LoadUI();
            }
        }

        if (fuel.currentFuel > 0 && inputData.item != null && inputData.count > 0)
        {
            fuel.Consume(Time.deltaTime * fuel.fuelUseRate);

            smeltProgress += smeltRate * Time.deltaTime;

            if (smeltProgress >= 1.0f)
            {
                SmeltItem();
                smeltProgress = 0f;
            }
        }
    }

    private void SmeltItem()
    {
        if (inputData.item != null)
        {
            if (outputData.item == null)
            {
                outputData.item = inputData.item.output;
                outputData.count = inputData.item.outputCount;
            }
            else if (outputData.item == inputData.item.output)
            {
                outputData.count += inputData.item.outputCount;
            }

            inputData.count--;
            if (inputData.count <= 0)
            {
                inputData.item = null;
            }

            if (inputSlot != null)
                LoadUI();
        }
    }

    public float GetFuelRatio()
    {
        return fuel?.GetFuelRatio() ?? 0f;
    }

    public void LoadUI()
    {
        LoadSlot(inputData, inputSlot);
        LoadSlot(outputData, outputSlot);
        LoadSlot(fuelData, fuelSlot);
    }

    public void SaveUI()
    {
        SaveSlot(inputSlot, ref inputData);
        SaveSlot(outputSlot, ref outputData);
        SaveSlot(fuelSlot, ref fuelData);
    }

    private void LoadSlot(FurnaceSlot data, InventorySlot slot)
    {
        slot.ClearSlot();
        if (data.item != null && data.count > 0)
        {
            InventoryManager.Instance.SpawnNewItem(data.item, slot, data.count);
        }
    }

    private void SaveSlot(InventorySlot slot, ref FurnaceSlot data)
    {
        InventoryItem item = slot.GetComponentInChildren<InventoryItem>();
        if (item != null && item.item != null)
        {
            data.item = item.item;
            data.count = item.count;
        }
        else
        {
            data.item = null;
            data.count = 0;
        }
    }

    public string SaveState()
    {
        inputData.SyncNameFromItem();
        outputData.SyncNameFromItem();
        fuelData.SyncNameFromItem();

        return JsonUtility.ToJson(this);
    }

    public void LoadState(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);

        inputData.ResolveItemFromName();
        outputData.ResolveItemFromName();
        fuelData.ResolveItemFromName();
    }
}
