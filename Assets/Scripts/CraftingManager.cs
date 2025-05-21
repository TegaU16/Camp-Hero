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
        }
    }
    public void TryUnlockRecipes(List<Item> discoveredItems)
    {
        foreach (var recipe in craftingDatabase.allRecipes)
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

        foreach (var item in craftingItem.recipe.requirements)
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
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                    iconImage.sprite = item.requiredItem.icon;
            }

            // Set requirement count
            TextMeshProUGUI countText = reqGO.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
                countText.text = item.count.ToString();
        }

        // Update selected item
        selectedItem = craftingItem;
    }

    /// <summary>
    /// Crafts the currently selected item if requirements are met.
    /// </summary>
    public void Craft()
    {
        if (selectedItem == null || !selectedItem.HasItems())
            return;

        // Remove required items
        foreach (var requirement in selectedItem.recipe.requirements)
        {
            int toRemove = requirement.count;

            foreach (InventorySlot slot in InventoryManager.Instance.inventoryUIHandler.inventorySlots)
            {
                InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

                if (itemInSlot != null && itemInSlot.item == requirement.requiredItem)
                {
                    if (itemInSlot.count <= toRemove)
                    {
                        toRemove -= itemInSlot.count;
                        Destroy(itemInSlot.gameObject);
                    }
                    else
                    {
                        itemInSlot.count -= toRemove;
                        itemInSlot.RefreshCount();
                        break;
                    }
                }

                if (toRemove <= 0) break;
            }
        }

        StartCoroutine(DelayedAddCraftedItem(selectedItem.recipe.resultItem));
    }

    private IEnumerator DelayedAddCraftedItem(Item item)
    {
        yield return null; // wait 1 frame
        InventoryManager.Instance.AddItem(item);
        InventoryManager.Instance.EquipSelectedItem();
    }
}
