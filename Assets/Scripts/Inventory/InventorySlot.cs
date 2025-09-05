using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NUnit.Framework;
using System.Collections.Generic;

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

    private Coroutine followCoroutine;

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

    // --- Left click pick / place
    public void HandleLeftClick()
    {
        if (InventoryItem.selectedItem != null)
        {
            OnSecondClick(this);
            UpdateInventorySlot();
            return;
        }

        if (transform.childCount > 0)
        {
            // record original slot on the item before we move it
            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>();
            if (selectedItem == null) return;

            selectedItem.originalParentSlot = transform;
            InventoryItem.selectedItem = selectedItem;
            selectedItem.isBeingDragged = true;
            selectedItem.transform.SetParent(transform.root);
            selectedItem.GetComponent<Image>().raycastTarget = false;

            selectedItem.EnableDragLayering();
            StartFollowCursor(selectedItem);
        }

        UpdateInventorySlot();
    }

    // --- Right click split / pick
    public void HandleRightClick()
    {
        Debug.Log($"[HandleRightClick] Called on slot: {name}");

        if (InventoryItem.selectedItem != null)
        {
            OnSecondClick(this);
            UpdateInventorySlot();
            return;
        }

        if (transform.childCount > 0)
        {
            InventoryItem originalItem = GetComponentInChildren<InventoryItem>();
            if (originalItem == null) return;

            Debug.Log($"[HandleRightClick] Found original item: {originalItem.name}, count: {originalItem.count}, parent: {originalItem.transform.parent.name}");

            if (originalItem.count > 1)
            {
                int splitCount = originalItem.count / 2;

                // Reduce original count and refresh
                originalItem.count -= splitCount;
                originalItem.RefreshCount();

                // Instantiate the split item directly under the UI root to avoid a single-frame race
                GameObject splitItemGo = Instantiate(InventoryManager.Instance.inventoryItemPrefab, transform);
                splitItemGo.name = "SplitItem_" + originalItem.item.name;

                InventoryItem splitItem = splitItemGo.GetComponent<InventoryItem>();
                splitItem.SetItem(originalItem.item, splitCount);

                // store the slot the split came from so RevertToOriginalSlot can work
                splitItem.originalParentSlot = transform;

                // mark as the currently selected (being dragged) item
                InventoryItem.selectedItem = splitItem;
                splitItem.isBeingDragged = true;
                splitItem.GetComponent<Image>().raycastTarget = false;

                splitItem.EnableDragLayering();
                StartFollowCursor(splitItem);
            }
            else
            {
                InventoryItem.selectedItem = originalItem;
                originalItem.originalParentSlot = transform;
                originalItem.isBeingDragged = true;
                originalItem.transform.SetParent(transform.root);
                originalItem.GetComponent<Image>().raycastTarget = false;

                InventoryItem.selectedItem.EnableDragLayering();
                StartFollowCursor(InventoryItem.selectedItem);
            }
        }

        UpdateInventorySlot();
    }

    private void OnSecondClick(InventorySlot targetSlot)
    {
        if (InventoryItem.selectedItem == null) return;

        InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;

        if (InventoryManager.InventoryUI != null &&
            !IsItemInsideDeleteSlot(InventoryManager.Instance.inventoryUIHandler.deleteSlot, Input.mousePosition))
        {
            if (targetSlot != null && targetSlot.IsItemAllowedInSlot(InventoryItem.selectedItem.item))
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

            InventoryItem.selectedItem.DisableDragLayering();
            StopFollowCursor();
        }
        else
        {
            DropSelectedItem();
        }

        InventoryItem.selectedItem = null;
    }

    // --- DRAG HANDLERS ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // IMPORTANT: if an item is already being carried (selectedItem), don't start a new drag.
        if (InventoryItem.selectedItem != null) return;

        if (transform.childCount > 0)
        {
            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>();
            if (selectedItem == null) return;

            // if the child is already flagged as being dragged, ignore
            if (selectedItem.isBeingDragged) return;

            InventoryItem.selectedItem = selectedItem;
            selectedItem.originalParentSlot = transform;
            selectedItem.isBeingDragged = true;
            selectedItem.transform.SetParent(transform.root);
            selectedItem.GetComponent<Image>().raycastTarget = false;

            selectedItem.EnableDragLayering();
            StartFollowCursor(selectedItem);

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

        // clear dragged flag
        InventoryItem.selectedItem.isBeingDragged = false;
        InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;

        InventoryItem.selectedItem.DisableDragLayering();
        StopFollowCursor();

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
            if (targetObject.TryGetComponent(out InventorySlot targetSlot)) return targetSlot;
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

            // Try to put leftover back to its original slot if possible, otherwise revert.
            if (selectedItem.originalParentSlot != null && selectedItem.originalParentSlot.childCount == 0)
            {
                selectedItem.PlaceInSlot(selectedItem.originalParentSlot);
            }
            else
            {
                selectedItem.RevertToOriginalSlot();
            }
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
        if (InventoryItem.selectedItem == null) return;

        InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
        Destroy(InventoryItem.selectedItem.gameObject);

        InventoryItem.selectedItem.DisableDragLayering();
        StopFollowCursor();
    }

    // --- Cursor follow helpers (single coroutine)
    private void StartFollowCursor(InventoryItem item)
    {
        StopFollowCursor();
        followCoroutine = StartCoroutine(FollowCursor(item));
    }

    private void StopFollowCursor()
    {
        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
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

    private InventorySlot GetSlotUnderMouse()
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject.TryGetComponent(out InventorySlot slot))
                return slot;
        }

        return null;
    }
}
