using Game.Inventory;
using Game.Saving;
using Game.Storage;
using UnityEngine;

namespace Game.Smelting
{
    public class FurnaceUnit : StorageContainer, ISaveableObject, IInteractable
    {
        private enum SlotIndex
        {
            Input,
            Output,
            Fuel
        }

        public FuelComponent fuel = new();

        [HideInInspector] public InventorySlot inputSlot;
        [HideInInspector] public InventorySlot outputSlot;
        [HideInInspector] public InventorySlot fuelSlot;

        [HideInInspector] public FurnaceItem selectedItem;

        [HideInInspector] public float smeltProgress;

        public float smeltDuration = 5f;
        private float smeltRate;

        public Sprite furnaceIcon;
        public Sprite ObjectIcon => furnaceIcon;

        private int Input => (int)SlotIndex.Input;
        private int Output => (int)SlotIndex.Output;
        private int Fuel => (int)SlotIndex.Fuel;

        public Item InputItem => GetItemFromSlot(Input);
        public Item OutputItem => GetItemFromSlot(Output);
        public Item FuelItem => GetItemFromSlot(Fuel);

        private bool suppressSave;

        private void Awake()
        {
            items = new ItemData[3];
        }

        private void Start()
        {
            smeltRate = 1f / smeltDuration;
            InventoryManager.Instance.OnInventoryItemChanged += SaveUI;
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;

            if (ShouldAddFuel())
            {
                fuel.AddFuel(FuelItem);
                items[Fuel].count--;

                if (items[Fuel].count <= 0)
                    items[Fuel] = null;

                RefreshSlot(fuelSlot, items[Fuel]);
            }

            if (!ShouldConsumeFuel()) return;

            fuel.Consume(Time.deltaTime * fuel.fuelUseRate);
            smeltProgress += smeltRate * Time.deltaTime;

            if (smeltProgress >= 1f)
            {
                SmeltItem();
                smeltProgress = 0f;
            }
        }

        private bool ShouldAddFuel()
        {
            return items[Fuel] != null &&
                   items[Fuel].count > 0 &&
                   FuelItem != null &&
                   FuelItem.fuelValue + fuel.currentFuel <= fuel.maxFuel;
        }

        private bool ShouldConsumeFuel()
        {
            bool hasFuel = fuel.currentFuel > 0f;
            if (!hasFuel) return false;

            bool hasInput = InputItem != null && items[Input].count > 0;
            if (!hasInput) return false;

            if (OutputItem == null || items[Output].count == 0) return true;

            string matchingOutputName = InputItem.output.itemName;
            bool hasMatchingOutput = OutputItem.itemName == matchingOutputName && items[Output].count < OutputItem.maxStack;
            if (!hasMatchingOutput) return false;

            return true;
        }

        private void SmeltItem()
        {
            if (items[Input] == null || InputItem == null) return;

            Item result = InputItem.output;

            if (OutputItem != null && OutputItem != result) return;

            if (items[Output] == null)
                items[Output] = new ItemData();

            items[Output].itemName = result.itemName;
            items[Output].count += InputItem.outputCount;

            items[Input].count--;

            if (items[Input].count <= 0)
                items[Input] = null;

            suppressSave = true;

            RefreshSlot(inputSlot, items[Input]);
            RefreshSlot(outputSlot, items[Output]);

            suppressSave = false;
        }

        public float GetFuelRatio() => fuel?.GetFuelRatio() ?? 0f;

        public void LoadUI()
        {
            suppressSave = true;

            RefreshSlot(inputSlot, items[Input]);
            RefreshSlot(outputSlot, items[Output]);
            RefreshSlot(fuelSlot, items[Fuel]);

            suppressSave = false;
        }

        public void SaveUI(InventorySlot changedSlot = null)
        {
            if (suppressSave) return;

            if (changedSlot == inputSlot)
                SaveSlot(inputSlot, Input);

            else if (changedSlot == outputSlot)
                SaveSlot(outputSlot, Output);

            else if (changedSlot == fuelSlot)
                SaveSlot(fuelSlot, Fuel);
        }

        public void Interact() => FurnaceManager.Instance.furnaceUI.Open(this);

        public string GetInteractText() => "Use Furnace";

        public Transform GetTransform() => transform;

        public string SaveState()
        {
            FurnaceSaveData data = new()
            {
                input = items[Input],
                output = items[Output],
                fuel = items[Fuel],
                smeltProgress = smeltProgress,
                currentFuel = fuel.currentFuel
            };

            return JsonUtility.ToJson(data);
        }

        public void LoadState(string json)
        {
            FurnaceSaveData data = JsonUtility.FromJson<FurnaceSaveData>(json);
            if (data == null) return;

            items[Input] = data.input;
            items[Output] = data.output;
            items[Fuel] = data.fuel;

            smeltProgress = data.smeltProgress;
            fuel.currentFuel = data.currentFuel;
        }
    }
}
