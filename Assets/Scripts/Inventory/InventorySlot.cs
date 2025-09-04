using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum SlotType
{
    General,
    Armor,
    Weapon,
    Crafting,
    Smelting,
    Fuel
}

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image image;
    public Sprite selectedImage, unselectedImage;

    public SlotType slotType;

    private void Awake()
    {
        Deselect();
    }

    private void Start()
    {
        image.raycastTarget = true;
    }

    public void Select()
    {
        image.sprite = selectedImage;
    }

    public void Deselect()
    {
        image.sprite = unselectedImage;
    }

    public void HandleLeftClick()
    {
        if (transform.childCount > 0)
        {
            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>();
            InventoryItem.selectedItem = selectedItem;
            selectedItem.transform.SetParent(transform.root);
            selectedItem.GetComponent<Image>().raycastTarget = false;
            StartCoroutine(FollowCursor(InventoryItem.selectedItem));
        }
        else
        {
            OnSecondClick();
        }

        UpdateInventorySlot();
    }

    public void HandleRightClick()
    {
        Debug.Log($"[HandleRightClick] Called on slot: {name}");

        if (transform.childCount > 0)
        {
            InventoryItem originalItem = GetComponentInChildren<InventoryItem>();
            Debug.Log($"[HandleRightClick] Found original item: {originalItem.name}, count: {originalItem.count}, parent: {originalItem.transform.parent.name}");

            if (originalItem.count > 1)
            {
                int splitCount = originalItem.count / 2;

                // Reduce original count and refresh
                originalItem.count -= splitCount;
                originalItem.RefreshCount();

                // Create new split item
                GameObject splitItemGo = Instantiate(InventoryManager.Instance.inventoryItemPrefab, transform);
                splitItemGo.name = "SplitItem_" + originalItem.item.name;

                InventoryItem splitItem = splitItemGo.GetComponent<InventoryItem>();
                splitItem.SetItem(originalItem.item, splitCount);

                splitItem.transform.SetParent(transform.root);
                splitItem.transform.SetAsLastSibling();

                InventoryItem.selectedItem = originalItem;
                originalItem.GetComponent<Image>().raycastTarget = false;

                StartCoroutine(FollowCursor(InventoryItem.selectedItem));
            }
            else
            {
                InventoryItem.selectedItem = originalItem;
                originalItem.transform.SetParent(transform.root);
                originalItem.transform.SetAsLastSibling();
                originalItem.GetComponent<Image>().raycastTarget = false;
                StartCoroutine(FollowCursor(InventoryItem.selectedItem));
            }
        }
        else
        {
            Debug.Log("[HandleRightClick] Slot empty, calling OnSecondClick.");
            OnSecondClick();
        }

        UpdateInventorySlot();
    }

    private void OnSecondClick()
    {
        if (InventoryItem.selectedItem == null) return;

        InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;

        if (InventoryManager.InventoryUI != null && !IsItemInsideDeleteSlot(InventoryManager.Instance.inventoryUIHandler.deleteSlot, Input.mousePosition))
        {
            if (IsItemAllowedInSlot(InventoryItem.selectedItem.item))
            {
                if (transform.childCount == 0)
                {
                    InventoryItem.selectedItem.PlaceInSlot(transform);
                }
                else
                {
                    InventoryItem existingItem = GetComponentInChildren<InventoryItem>();
                    if (existingItem.item == InventoryItem.selectedItem.item)
                    {
                        StackItems(existingItem, InventoryItem.selectedItem);
                    }
                    else
                    {
                        InventoryItem.selectedItem.RevertToOriginalSlot();
                    }
                }

                StopAllCoroutines();
            }
            else
            {
                InventoryItem.selectedItem.RevertToOriginalSlot();
                StopAllCoroutines();
            }
        }
        else
        {
            DropSelectedItem();
        }

        InventoryItem.selectedItem = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (transform.childCount > 0)
        {
            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>();
            InventoryItem.selectedItem = selectedItem;
            selectedItem.transform.SetParent(transform.root);
            selectedItem.GetComponent<Image>().raycastTarget = false;
            UpdateInventorySlot();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryItem.selectedItem != null)
        {
            InventoryItem.selectedItem.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryItem.selectedItem == null) return;

        InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;

        // Use the centralized InventoryUI reference to check if the item is inside the inventory
        if (InventoryManager.InventoryUI != null && !IsItemInsideDeleteSlot(InventoryManager.Instance.inventoryUIHandler.deleteSlot, Input.mousePosition))
        {
            HandleItemDrop(eventData);
        }
        else
        {
            // Drop the item if it's not inside inventory UI
            DropSelectedItem();
        }

        InventoryItem.selectedItem = null;
        UpdateInventorySlot();
    }

    private void HandleItemDrop(PointerEventData eventData)
    {
        GameObject targetObject = eventData.pointerEnter;
        InventorySlot targetSlot = FindTargetSlot(targetObject);

        if (targetSlot == null)
        {
            InventoryItem.selectedItem.RevertToOriginalSlot();
            return;
        }

        if (targetSlot.IsItemAllowedInSlot(InventoryItem.selectedItem.item))
        {
            if (targetSlot.transform.childCount == 0)
            {
                InventoryItem.selectedItem.PlaceInSlot(targetSlot.transform);
            }
            else
            {
                InventoryItem existingItem = targetSlot.GetComponentInChildren<InventoryItem>();
                if (existingItem != null && existingItem.item == InventoryItem.selectedItem.item)
                {
                    StackItems(existingItem, InventoryItem.selectedItem);
                }
                else
                {
                    InventoryItem.selectedItem.RevertToOriginalSlot();
                }
            }
        }
        else
        {
            InventoryItem.selectedItem.RevertToOriginalSlot();
        }
    }

    private InventorySlot FindTargetSlot(GameObject targetObject)
    {
        while (targetObject != null)
        {
            if (targetObject.TryGetComponent<InventorySlot>(out var targetSlot)) return targetSlot;
            targetObject = targetObject.transform.parent != null ? targetObject.transform.parent.gameObject : null;
        }
        return null;
    }

    private bool IsItemInsideDeleteSlot(RectTransform deleteSlotRectTransform, Vector2 itemPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(deleteSlotRectTransform, itemPosition, null);
    }

    private void UpdateInventorySlot()
    {
        Item selectedItem = InventoryManager.Instance.GetSelectedItem(false);
        if (selectedItem == null)
        {
            InventoryManager.Instance.itemEquip.EquipItem(null);
        }
        else
        {
            InventoryManager.Instance.EquipSelectedItem();
        }
    }

    private void StackItems(InventoryItem existingItem, InventoryItem selectedItem)
    {
        int maxStack = selectedItem.item.maxStack;
        int combinedCount = existingItem.count + selectedItem.count;

        if (combinedCount <= maxStack)
        {
            existingItem.count = combinedCount;
            existingItem.RefreshCount();
            Destroy(selectedItem.gameObject);
        }
        else
        {
            existingItem.count = maxStack;
            existingItem.RefreshCount();
            selectedItem.count = combinedCount - maxStack;
            selectedItem.RefreshCount();
            selectedItem.RevertToOriginalSlot();
        }
    }

    public bool IsItemAllowedInSlot(Item item)
    {
        return slotType switch
        {
            SlotType.Armor => item.itemType == ItemType.Armor,
            SlotType.Weapon => item.itemType == ItemType.Weapon,
            SlotType.Crafting => item.itemType == ItemType.Crafting,
            SlotType.Fuel => item.itemType == ItemType.Fuel,
            SlotType.Smelting => item.itemType == ItemType.Smelting,
            _ => true,
        };
    }

    private void DropSelectedItem()
    {
        InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
        Destroy(InventoryItem.selectedItem.gameObject);
    }

    private IEnumerator FollowCursor(InventoryItem item)
    {
        while (item == InventoryItem.selectedItem && item != null)
        {
            if (item != null)
            {
                item.transform.position = Input.mousePosition;
            }
            else
            {
                yield break;
            }
            yield return null;
        }
    }

    public void ClearSlot()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
