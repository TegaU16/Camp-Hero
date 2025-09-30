using UnityEngine;

public class FarmPlot : MonoBehaviour, IInteractable, ISaveableObject
{
    public Transform plantSpawnPoint;
    private GameObject currentPlantInstance;

    [HideInInspector] public PlantData plantedData;
    [HideInInspector] public float growthTimer;
    [HideInInspector] public int currentStage = -1;
    [HideInInspector] public bool isPlanted;

    private Item selectedItem;

    public void Plant()
    {
        selectedItem = InventoryManager.Instance.GetSelectedItem(false);
        if (selectedItem == null || selectedItem.plantData == null) return;

        plantedData = selectedItem.plantData;
        growthTimer = 0f;
        currentStage = -1;
        isPlanted = true;

        InventoryManager.Instance.UseSelectedItem();
        UpdateVisual();
    }

    void Update()
    {
        if (!isPlanted || plantedData == null) return;

        if (!IsFullyGrown()) growthTimer += Time.deltaTime;

        int stageCount = plantedData.growthStages.Length;
        int newStage = Mathf.FloorToInt((growthTimer / plantedData.totalGrowthTime) * stageCount);
        newStage = Mathf.Clamp(newStage, 0, stageCount - 1);

        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpdateVisual();
        }
    }

    void UpdateVisual()
    {
        if (currentPlantInstance != null)
            Destroy(currentPlantInstance);

        if (currentStage >= 0 && plantedData.growthStages.Length > currentStage)
        {
            currentPlantInstance = Instantiate(plantedData.growthStages[currentStage], plantSpawnPoint.position, Quaternion.identity, plantSpawnPoint);
        }
    }

    private bool IsFullyGrown() => isPlanted && growthTimer >= plantedData.totalGrowthTime;

    private void Harvest()
    {
        if (!IsFullyGrown()) return;
        if (InventoryManager.Instance.IsInventoryFullForItem(plantedData.plant, plantedData.harvestAmount)) return;

        Destroy(currentPlantInstance);

        InventoryManager.Instance.AddItem(plantedData.plant, plantedData.harvestAmount);

        plantedData = null;
        isPlanted = false;
        growthTimer = 0f;
        currentStage = -1;
    }

    public void Interact()
    {
        if (!isPlanted)
            Plant();
        else if (IsFullyGrown())
            Harvest();
    }

    public string GetInteractText()
    {
        if (!isPlanted)
            return "Sow Seed";

        if (IsFullyGrown())
            return "Harvest";

        return "";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public string SaveState()
    {
        FarmPlotData data = new()
        {
            plantName = plantedData != null ? plantedData.plant.itemName : null,
            growthTimer = growthTimer,
            currentStage = currentStage,
            isPlanted = isPlanted
        };

        return JsonUtility.ToJson(data);
    }

    public void LoadState(string json)
    {
        FarmPlotData data = JsonUtility.FromJson<FarmPlotData>(json);

        if (string.IsNullOrEmpty(data.plantName))
        {
            // No plant = reset
            plantedData = null;
            isPlanted = false;
            growthTimer = 0f;
            currentStage = -1;
            if (currentPlantInstance != null)
                Destroy(currentPlantInstance);
        }
        else
        {
            plantedData = PlantRegistry.GetPlantByKey(data.plantName);
            growthTimer = data.growthTimer;
            currentStage = data.currentStage;
            isPlanted = data.isPlanted;

            UpdateVisual();
        }
    }
}
