using System.Collections.Generic;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Smelting
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class FurnaceUI : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        [SerializeField] private InventorySlot inputSlot;
        [SerializeField] private InventorySlot outputSlot;
        [SerializeField] private InventorySlot fuelSlot;

        [Header("General UI")]
        public Transform furnaceItemParent;
        [SerializeField] private GameObject furnaceItemPrefab;
        [SerializeField] private List<Button> smeltingTabs;

        [Header("Process Section")]
        [SerializeField] private ArrowFillController progressBar;
        [SerializeField] private Slider fuelBar;

        [Header("Recipe Section")]
        [SerializeField] private GameObject recipeSection;
        [SerializeField] private GameObject nullItemSelectText;
        [SerializeField] private Image resultImage;
        [SerializeField] private TextMeshProUGUI resultName;
        [SerializeField] private Image requiredImage;
        [SerializeField] private TextMeshProUGUI requiredName;

        private bool isOpen;
        private FurnaceUnit linkedFurnace;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        // Start is called before the first frame update
        void Start()
        {
            progressBar.fillAmount = 0;

            recipeSection.SetActive(false);
            nullItemSelectText.SetActive(true);
        }

        public void Open(FurnaceUnit unit)
        {
            if (isOpen) return;

            isOpen = true;
            linkedFurnace = unit;
            InventoryManager.Instance.OpenInventory();

            linkedFurnace.inputSlot = this.inputSlot;
            linkedFurnace.outputSlot = this.outputSlot;
            linkedFurnace.fuelSlot = this.fuelSlot;

            linkedFurnace.LoadUI(); // Load furnace data into the UI

            if (fuelBar != null)
                fuelBar.value = linkedFurnace.GetFuelRatio();

            if (progressBar != null)
                progressBar.fillAmount = linkedFurnace.smeltProgress;

            UITween.DefaultOpenMenu(canvasGroup, rectTransform);
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            if (linkedFurnace == null) return;

            linkedFurnace.SaveUI(inputSlot);
            linkedFurnace.SaveUI(outputSlot);
            linkedFurnace.SaveUI(fuelSlot);

            linkedFurnace.inputSlot = null;
            linkedFurnace.outputSlot = null;
            linkedFurnace.fuelSlot = null;

            linkedFurnace = null;
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
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

        public void SetSelectedFurnaceItem(FurnaceItem furnaceItem = null)
        {
            if (linkedFurnace == null) return;

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

            linkedFurnace.selectedItem = furnaceItem;
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
