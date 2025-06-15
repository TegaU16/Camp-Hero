using UnityEngine;

public class FarmPlot : MonoBehaviour, IInteractable
{
    public Transform plantSpawnPoint;
    private GameObject currentPlantInstance;

    private PlantData plantedData;
    private float growthTimer;
    private int currentStage = -1;
    private bool isPlanted;
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

        if (growthTimer >= plantedData.totalGrowthTime)
        {
            
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
}
