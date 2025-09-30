using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [Header("General UI")]
    public Transform furnaceItemParent;
    public GameObject furnaceItemPrefab;
    public FurnaceUI furnaceUI;
    public GameObject recipeSection;
    public GameObject nullItemSelectText;

    [Header("Process Section")]
    public Image progressFill;

    [Header("Info Section")]
    public Image resultImage;
    public TextMeshProUGUI resultName;
    public Image requiredImage;
    public TextMeshProUGUI requiredName;

    public const float fuelDecreaseRate = -0.01f;

    [Header("State")]
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

        recipeSection.SetActive(false);
        nullItemSelectText.SetActive(true);
    }

    public void TryUnlockRecipes(List<Item> discoveredItems)
    {
        foreach (SmeltingRecipe recipe in smeltingDatabase.allRecipes)
        {
            if (!unlockedRecipes.Contains(recipe) && recipe.ShouldUnlock(discoveredItems))
                UnlockRecipe(recipe);
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
            FurnaceUnit furnaceUnit = furnaceUI.linkedFurnace;
            if (furnaceUnit != null)
                furnaceUnit.selectedItem = furnaceItem;
        }
    }

    public void Open(FurnaceUnit unit)
    {
        furnaceUI.Open(unit);
    }

    public void SaveSmeltingProgress()
    {
        SaveSystem.SaveSmelting(GameManager.Instance.currentWorldName, unlockedRecipes);
    }

    public void LoadSmeltingProgress()
    {
        unlockedRecipes.Clear();
        foreach (Transform child in furnaceItemParent)
            Destroy(child.gameObject);

        List<SmeltingRecipe> loadedRecipes = SaveSystem.LoadSmelting(GameManager.Instance.currentWorldName, smeltingDatabase);
        unlockedRecipes.AddRange(loadedRecipes);

        foreach (SmeltingRecipe recipe in unlockedRecipes)
        {
            GameObject itemGO = Instantiate(furnaceItemPrefab, furnaceItemParent);
            FurnaceItem uiItem = itemGO.GetComponent<FurnaceItem>();
            uiItem.recipe = recipe;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
        }
    }
}
