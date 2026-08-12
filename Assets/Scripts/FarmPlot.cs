using Game.Inventory;
using Game.Registries;
using Game.Saving;
using UnityEngine;

namespace Game.Food
{
    public class FarmPlot : MonoBehaviour, IInteractable, ISaveableObject
    {
        [SerializeField] private Transform plantSpawnPoint;
        private GameObject currentPlantInstance;

        [HideInInspector] public PlantData plantedData;
        [HideInInspector] public float growthTimer;
        [HideInInspector] public int currentStage = -1;
        [HideInInspector] public bool isPlanted;

        private Item selectedItem;

        [SerializeField] private Sprite farmPlotIcon;
        public Sprite ObjectIcon => farmPlotIcon;

        public void Plant()
        {
            selectedItem = InventoryManager.Instance.GetSelectedItem(delete: false);
            if (selectedItem == null || selectedItem.plantData == null) return;

            plantedData = selectedItem.plantData;
            growthTimer = plantedData.totalGrowthTime; // countdown starts at total time
            currentStage = -1;
            isPlanted = true;

            InventoryManager.Instance.UseSelectedItem();
            UpdateVisual();
        }

        void Update()
        {
            if (!isPlanted || plantedData == null) return;

            if (!IsFullyGrown())
            {
                growthTimer -= Time.deltaTime; // countdown
                growthTimer = Mathf.Max(growthTimer, 0f);

                string growthTime = GetGrowthTime();
                InteractableItemManager.Instance.OnInteractTextChanged?.Invoke(growthTime);
            }

            int stageCount = plantedData.growthStages.Length;
            float normalizedProgress = 1f - (growthTimer / plantedData.totalGrowthTime);
            int newStage = Mathf.FloorToInt(normalizedProgress * stageCount);
            newStage = Mathf.Clamp(newStage, 0, stageCount - 1);

            if (newStage == currentStage) return;

            currentStage = newStage;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (currentPlantInstance != null)
                Destroy(currentPlantInstance);

            if (currentStage >= 0 && plantedData.growthStages.Length > currentStage)
            {
                currentPlantInstance = Instantiate(
                    plantedData.growthStages[currentStage],
                    plantSpawnPoint.position,
                    Quaternion.identity,
                    plantSpawnPoint
                );
            }
        }

        private bool IsFullyGrown() => isPlanted && growthTimer <= 0f;

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
            if (!isPlanted) return "Sow Seed";
            if (IsFullyGrown()) return "Harvest";

            string growthTime = GetGrowthTime();
            return growthTime;
        }

        private string GetGrowthTime()
        {
            int totalSeconds = Mathf.CeilToInt(growthTimer);
            if (totalSeconds < 60) return $"{totalSeconds}";

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }

        public Transform GetTransform() => transform;

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
            if (data == null) return;

            plantedData = PlantRegistry.Instance.GetByKey(data.plantName);
            growthTimer = data.growthTimer;
            currentStage = data.currentStage;
            isPlanted = data.isPlanted;

            UpdateVisual();
        }
    }
}
