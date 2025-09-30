using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("UI")]
    public GameObject craftingItemPrefab;
    public Transform craftingItemParent;
    public TextMeshProUGUI craftingItemName;
    public Image craftingItemIcon;
    public GameObject requirementPrefabParent;

    [Header("State")]
    private CraftingItem selectedItem;
    public CraftingDatabase craftingDatabase;
    private readonly List<CraftingRecipe> unlockedRecipes = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        DOTween.Init();
        selectedItem = null;
    }

    public void ToggleCraftingMenu()
    {
        if (!InventoryManager.Instance.IsExtensionOpen())
        {
            if (InventoryManager.Instance.mainInventory != null) InventoryManager.Instance.mainInventory.SetActive(true);
            if (InventoryManager.Instance.craftingMenuUI != null) InventoryManager.Instance.craftingMenuUI.SetActive(true);

            if (selectedItem == null)
            {
                craftingItemIcon.gameObject.SetActive(false);
                craftingItemName.text = "Select an item to craft";
            }
        }
    }

    public void TryUnlockRecipes(List<Item> discoveredItems)
    {
        foreach (CraftingRecipe recipe in craftingDatabase.allRecipes)
        {
            if (!unlockedRecipes.Contains(recipe) && recipe.ShouldUnlock(discoveredItems))
            {
                UnlockRecipe(recipe);
            }
        }
    }

    /// <summary>
    /// Adds a new recipe to the list and instantiates its UI element.
    /// </summary>
    public void UnlockRecipe(CraftingRecipe recipe)
    {
        if (unlockedRecipes.Contains(recipe)) return;

        unlockedRecipes.Add(recipe);

        GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
        CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();
        uiItem.recipe = recipe;
        uiItem.itemImage.sprite = recipe.resultItem.icon;
    }

    /// <summary>
    /// Updates the crafting item UI and shows its requirements.
    /// </summary>
    public void SetSelectedCraftingItem(CraftingItem craftingItem)
    {
        // Clear all existing requirement entries
        foreach (Transform child in requirementPrefabParent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (CraftingRecipe.Requirement item in craftingItem.recipe.requirements)
        {
            if (item.requiredItem == null) continue;

            GameObject reqGO = Instantiate(craftingItem.requirementPrefab, requirementPrefabParent.transform);

            // Set requirement background
            Image bg = reqGO.GetComponent<Image>();
            if (bg != null && craftingItem.requirementBackground != null)
                bg.sprite = Instantiate(craftingItem.requirementBackground.sprite);

            // Set requirement icon
            Transform iconTransform = reqGO.transform.Find("Req. Icon");
            if (iconTransform != null)
            {
                if (iconTransform.TryGetComponent(out Image iconImage))
                    iconImage.sprite = item.requiredItem.icon;
            }

            // Set requirement count
            Transform count = reqGO.transform.Find("Req. Count");
            if (count != null)
            {
                if (count.TryGetComponent(out TextMeshProUGUI countText))
                    countText.text = item.count.ToString();
            }

            // Set requirement name
            Transform name = reqGO.transform.Find("Req. Name");
            if (name != null)
            {
                if (name.TryGetComponent(out TextMeshProUGUI nameText))
                    nameText.text = item.requiredItem.name.ToString();
            }
        }

        // Update selected item
        selectedItem = craftingItem;

        craftingItemName.text = selectedItem.recipe.resultItem.itemName;
        craftingItemIcon.gameObject.SetActive(true);
        craftingItemIcon.sprite = selectedItem.recipe.resultItem.icon;
    }

    /// <summary>
    /// Crafts the currently selected item if requirements are met.
    /// </summary>
    public void Craft()
    {
        if (selectedItem == null || !selectedItem.HasItems())
            return;

        // Remove required items
        foreach (CraftingRecipe.Requirement requirement in selectedItem.recipe.requirements)
        {
            int toRemove = requirement.count;

            InventoryManager.Instance.ConsumeItem(requirement.requiredItem, toRemove);
        }

        StartCoroutine(DelayedAddCraftedItem(selectedItem.recipe.resultItem));
    }

    private IEnumerator DelayedAddCraftedItem(Item item)
    {
        yield return null; // wait 1 frame
        InventoryManager.Instance.AddItem(item);
        InventoryManager.Instance.EquipSelectedItem();
    }

    public void SaveCraftingProgress()
    {
        SaveSystem.SaveCrafting(GameManager.Instance.currentWorldName, unlockedRecipes);
    }

    public void LoadCraftingProgress()
    {
        unlockedRecipes.Clear();

        foreach (Transform child in craftingItemParent)
            Destroy(child.gameObject);

        List<CraftingRecipe> loadedRecipes = SaveSystem.LoadCrafting(GameManager.Instance.currentWorldName, craftingDatabase);
        unlockedRecipes.AddRange(loadedRecipes);

        foreach (CraftingRecipe recipe in unlockedRecipes)
        {
            GameObject itemGO = Instantiate(craftingItemPrefab, craftingItemParent);
            CraftingItem uiItem = itemGO.GetComponent<CraftingItem>();
            uiItem.recipe = recipe;
            uiItem.itemImage.sprite = recipe.resultItem.icon;
        }
    }
}
