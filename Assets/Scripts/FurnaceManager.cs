using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [Header("UI")]
    public Transform furnaceItemParent;
    public GameObject requirementPrefabParent;
    public Image progressFill;
    public InventorySlot inputSlot;
    public InventorySlot outputSlot;
    public GameObject furnaceItemPrefab;
    public FurnaceUI furnaceUI;
    public GameObject inventoryMenu;

    public const float fuelDecreaseRate = -0.01f;

    [Header("State")]
    private FurnaceItem selectedItem;
    public SmeltingDatabase smeltingDatabase;
    private readonly List<SmeltingRecipe> unlockedRecipes = new();

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        progressFill.fillAmount = 0;
        selectedItem = null;
    }

    public void TryUnlockRecipes(List<Item> discoveredItems)
    {
        foreach (var recipe in smeltingDatabase.allRecipes)
        {
            if (!unlockedRecipes.Contains(recipe) && recipe.ShouldUnlock(discoveredItems))
            {
                UnlockRecipe(recipe);
            }
        }
    }

    public void UnlockRecipe(SmeltingRecipe recipe)
    {
        if (unlockedRecipes.Contains(recipe)) return;

        unlockedRecipes.Add(recipe);

        GameObject itemGO = Instantiate(furnaceItemPrefab, furnaceItemParent);
        FurnaceItem uiItem = itemGO.GetComponent<FurnaceItem>();
        uiItem.recipe = recipe;
        uiItem.itemImage.sprite = recipe.resultItem.icon;
    }

    public void SetSelectedFurnaceItem(FurnaceItem furnaceItem)
    {
        if (furnaceItem.recipe.requiredItem != null)
        {
            // Clear all existing requirement entries
            foreach (Transform child in requirementPrefabParent.transform)
            {
                Destroy(child.gameObject);
            }

            GameObject reqGO = Instantiate(furnaceItem.requirementPrefab, requirementPrefabParent.transform);

            // Set requirement background
            Image bg = reqGO.GetComponent<Image>();
            if (bg != null && furnaceItem.requirementBackground != null)
                bg.sprite = Instantiate(furnaceItem.requirementBackground.sprite);

            // Set requirement icon
            Transform iconTransform = reqGO.transform.Find("Req. Icon");
            if (iconTransform != null)
            {
                if (iconTransform.TryGetComponent<Image>(out var iconImage))
                    iconImage.sprite = furnaceItem.recipe.requiredItem.icon;
            }

            // Update selected item
            selectedItem = furnaceItem;
        }
    }

    public void Open(FurnaceUnit unit)
    {
        furnaceUI.Open(unit);
    }

    public void Close()
    {
        furnaceUI.Close();
    }
}
