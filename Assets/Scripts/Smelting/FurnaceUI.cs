using System.Collections.Generic;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Smelting
{
    public class FurnaceUI : MonoBehaviour
    {
        public InventorySlot inputSlot;
        public InventorySlot outputSlot;
        public InventorySlot fuelSlot;

        [Header("General UI")]
        public Transform furnaceItemParent;
        public GameObject furnaceItemPrefab;
        public List<Button> smeltingTabs;

        [Header("Process Section")]
        public ArrowFillController progressBar;
        public Slider fuelBar;

        [Header("Recipe Section")]
        public GameObject recipeSection;
        public GameObject nullItemSelectText;
        public Image resultImage;
        public TextMeshProUGUI resultName;
        public Image requiredImage;
        public TextMeshProUGUI requiredName;

        [HideInInspector] public FurnaceUnit linkedFurnace;

        // Start is called before the first frame update
        void Start()
        {
            progressBar.fillAmount = 0;

            recipeSection.SetActive(false);
            nullItemSelectText.SetActive(true);
        }

        public void Open(FurnaceUnit unit)
        {
            linkedFurnace = unit;
            gameObject.SetActive(true);
            InventoryManager.Instance.mainInventory.SetActive(true);
            InventoryManager.Instance.OnInventoryOpen();

            linkedFurnace.inputSlot = inputSlot;
            linkedFurnace.outputSlot = outputSlot;
            linkedFurnace.fuelSlot = fuelSlot;

            linkedFurnace.inputSlot.parentFurnace = linkedFurnace;
            linkedFurnace.outputSlot.parentFurnace = linkedFurnace;
            linkedFurnace.fuelSlot.parentFurnace = linkedFurnace;

            linkedFurnace.inventorySlots.Clear();
            linkedFurnace.inventorySlots.Add(linkedFurnace.inputSlot);
            linkedFurnace.inventorySlots.Add(linkedFurnace.outputSlot);
            linkedFurnace.inventorySlots.Add(linkedFurnace.fuelSlot);

            linkedFurnace.LoadUI(); // Load furnace data into the UI

            if (fuelBar != null)
                fuelBar.value = linkedFurnace.GetFuelRatio();

            if (progressBar != null)
                progressBar.fillAmount = linkedFurnace.smeltProgress;

            InventoryManager.Instance.activeFurnace = linkedFurnace;
        }

        public void Close()
        {
            if (linkedFurnace != null)
            {
                linkedFurnace.SaveUI(); // Save the UI data back into furnace
                linkedFurnace.inputSlot = null;
                linkedFurnace.outputSlot = null;
                linkedFurnace.fuelSlot = null;

                linkedFurnace = null;
            }

            gameObject.SetActive(false);
            InventoryManager.Instance.mainInventory.SetActive(false);
            InventoryManager.Instance.darkBackground.SetActive(false);
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (linkedFurnace == null) return;

            if (fuelBar != null)
                fuelBar.value = linkedFurnace.GetFuelRatio();

            if (progressBar != null)
                progressBar.fillAmount = linkedFurnace.smeltProgress;
        }

        public void AddUnlockedRecipe(SmeltingRecipe recipe)
        {
            GameObject itemGO = Instantiate(furnaceItemPrefab, furnaceItemParent);
            FurnaceItem uiItem = itemGO.GetComponent<FurnaceItem>();
            uiItem.recipe = recipe;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
        }

        public void SetSelectedFurnaceItem(FurnaceItem furnaceItem = null, bool onOpened = false)
        {
            bool itemSelected = furnaceItem != null;

            if (itemSelected)
            {
                resultImage.sprite = furnaceItem.recipe.resultItem.icon;
                requiredImage.sprite = furnaceItem.recipe.requiredItem.icon;

                resultName.text = furnaceItem.recipe.resultItem.name;
                requiredName.text = furnaceItem.recipe.requiredItem.name;
            }

            recipeSection.SetActive(itemSelected);
            nullItemSelectText.SetActive(!itemSelected);

            if (!onOpened)
            {
                FurnaceUnit furnaceUnit = linkedFurnace;
                if (furnaceUnit != null)
                    furnaceUnit.selectedItem = furnaceItem;
            }
        }

        public void FilterByCategory(SmeltingCategory category, Button button)
        {
            foreach (Transform child in furnaceItemParent)
            {
                if (!child.TryGetComponent(out FurnaceItem item)) continue;

                bool shouldShow = category == SmeltingCategory.All || item.recipe.category == category;
                child.gameObject.SetActive(shouldShow);
            }

            foreach (Button b in smeltingTabs)
            {
                if (!b.TryGetComponent(out InteractiveButton interactiveButton)) continue;

                if (b == button)
                    interactiveButton.Select();
                else
                    interactiveButton.Deselect();
            }
        }
    }
}
