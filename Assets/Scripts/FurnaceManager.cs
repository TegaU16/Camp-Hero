using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnaceManager : MonoBehaviour
{
    public static FurnaceManager Instance;

    [Header("General UI")]
    public Transform furnaceItemParent;
    public GameObject requirementPrefabParent;
    public GameObject furnaceItemPrefab;
    public FurnaceUI furnaceUI;
    public GameObject inventoryMenu;

    [Header("Process Section")]
    public Image progressFill;
    public InventorySlot inputSlot;
    public InventorySlot outputSlot;

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
            resultImage.sprite = furnaceItem.recipe.resultItem.icon;
            requiredImage.sprite = furnaceItem.recipe.requiredItem.icon;

            resultName.text = furnaceItem.recipe.resultItem.name;
            requiredName.text = furnaceItem.recipe.requiredItem.name;
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

    public void SaveSmeltingProgress()
    {
        SaveSystem.SaveSmelting(GameManager.Instance.currentWorldName, unlockedRecipes);
    }

    public void LoadSmeltingProgress()
    {
        unlockedRecipes.Clear();
        unlockedRecipes.AddRange(
            SaveSystem.LoadSmelting(GameManager.Instance.currentWorldName, smeltingDatabase)
        );
    }
}
