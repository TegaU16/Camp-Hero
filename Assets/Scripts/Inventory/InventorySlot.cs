using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    public enum SlotType
    {
        General,
        Armor,
        Weapon,
        Crafting,
        Smelting,
        Fuel,
        Reforge
    }

    public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image image;
        public SlotType slotType;
        private Coroutine followCoroutine;

        private void Awake()
        {
            if (TryGetComponent(out SelectableImage selectableImage))
                selectableImage.Deselect();
        }

        private void Start() => image.raycastTarget = true;

        #region Click Handlers

        public void HandleLeftClick()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                QuickTransfer();
                return;
            }

            if (InventoryItem.selectedItem != null)
            {
                OnSecondClick(this);
                return;
            }

            if (transform.childCount == 0) return;

            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>(this);
            if (selectedItem == null) return;

            PickUpItem(selectedItem);
        }

        public void HandleRightClick()
        {
            if (InventoryItem.selectedItem != null)
            {
                OnSecondClick(this);
                return;
            }

            if (transform.childCount == 0) return;

            InventoryItem originalItem = GetComponentInChildren<InventoryItem>(this);
            if (originalItem == null) return;

            if (originalItem.count <= 1)
            {
                PickUpItem(originalItem);
                return;
            }

            int splitCount = originalItem.count / 2;

            // Reduce original count and refresh
            originalItem.count -= splitCount;
            originalItem.RefreshCount();

            // Instantiate the split item directly under the UI root to avoid a single-frame race
            GameObject splitItemGo = Instantiate(InventoryManager.Instance.inventoryItemPrefab, transform);
            splitItemGo.name = "SplitItem_" + originalItem.item.name;

            InventoryItem splitItem = splitItemGo.GetComponent<InventoryItem>();
            splitItem.SetItem(originalItem.item, splitCount);

            PickUpItem(splitItem, setParentRoot: false);
        }

        private void OnSecondClick(InventorySlot targetSlot)
        {
            if (InventoryItem.selectedItem == null) return;

            InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;
            RectTransform deleteSlotTransform = InventoryManager.Instance.inventoryUIHandler.deleteSlot;

            if (InventoryManager.InventoryUI == null || IsItemInsideDeleteSlot(deleteSlotTransform, Input.mousePosition))
            {
                DropSelectedItem();
                InventoryItem.selectedItem = null;
                return;
            }

            if (targetSlot == null || !targetSlot.IsItemAllowedInSlot(InventoryItem.selectedItem.item))
            {
                InventoryItem.selectedItem.RevertToOriginalSlot();
                DisableItemDrag();
                return;
            }

            if (targetSlot.transform.childCount == 0)
            {
                InventoryItem.selectedItem.PlaceInSlot(targetSlot.transform);
                DisableItemDrag();
                return;
            }

            InventoryItem existingItem = targetSlot.GetComponentInChildren<InventoryItem>(this);
            if (existingItem != null && existingItem.item == InventoryItem.selectedItem.item)
                StackItems(existingItem, InventoryItem.selectedItem);
            else
                InventoryItem.selectedItem.RevertToOriginalSlot();

            DisableItemDrag();
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            // IMPORTANT: if an item is already being carried (selectedItem), don't start a new drag.
            if (InventoryItem.selectedItem != null || transform.childCount <= 0) return;

            InventoryItem selectedItem = GetComponentInChildren<InventoryItem>(this);
            if (selectedItem == null || selectedItem.isBeingDragged) return;

            PickUpItem(selectedItem);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (InventoryItem.selectedItem != null)
                InventoryItem.selectedItem.transform.position = eventData.position;
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
            RectTransform deleteSlotTransform = InventoryManager.Instance.inventoryUIHandler.deleteSlot;
            if (InventoryManager.InventoryUI != null && !IsItemInsideDeleteSlot(deleteSlotTransform, Input.mousePosition))
                HandleItemPlaced(eventData);
            else
                DropSelectedItem();

            InventoryItem.selectedItem = null;
        }

        private void DisableItemDrag()
        {
            InventoryItem.selectedItem.DisableDragLayering();
            StopFollowCursor();
            InventoryItem.selectedItem = null;
        }

        #endregion

        private void PickUpItem(InventoryItem inventoryItem, bool setParentRoot = true)
        {
            InventoryItem.selectedItem = inventoryItem;
            inventoryItem.isBeingDragged = true;

            if (setParentRoot)
                inventoryItem.transform.SetParent(transform.root);

            inventoryItem.GetComponent<Image>().raycastTarget = false;
            inventoryItem.EnableDragLayering();
            StartFollowCursor(inventoryItem);

            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
        }

        private void HandleItemPlaced(PointerEventData eventData)
        {
            GameObject targetObject = eventData.pointerEnter;
            InventorySlot targetSlot = FindTargetSlot(targetObject);

            if (targetSlot == null || !targetSlot.IsItemAllowedInSlot(InventoryItem.selectedItem.item))
            {
                InventoryItem.selectedItem.RevertToOriginalSlot();
                return;
            }

            if (targetSlot.transform.childCount == 0)
            {
                InventoryItem.selectedItem.PlaceInSlot(targetSlot.transform);
                return;
            }

            InventoryItem existingItem = targetSlot.GetComponentInChildren<InventoryItem>(this);
            if (existingItem != null && existingItem.item == InventoryItem.selectedItem.item)
                StackItems(existingItem, InventoryItem.selectedItem);
            else
                InventoryItem.selectedItem.RevertToOriginalSlot();
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
            => RectTransformUtility.RectangleContainsScreenPoint(deleteSlotRectTransform, itemPosition, cam: null);

        #region Placing In Slots

        private void StackItems(InventoryItem existingItem, InventoryItem selectedItem)
        {
            int maxStack = selectedItem.item.maxStack;
            int combinedCount = existingItem.count + selectedItem.count;

            if (combinedCount <= maxStack)
            {
                existingItem.count = combinedCount;
                existingItem.RefreshCount();
                Destroy(selectedItem.gameObject);
                InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
                return;
            }

            existingItem.count = maxStack;
            existingItem.RefreshCount();
            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);

            int overflow = combinedCount - maxStack;

            // Try to auto-place overflow in other slots
            overflow = PlaceOverflowInSlots(selectedItem.item, overflow);
            if (overflow <= 0)
            {
                Destroy(selectedItem.gameObject);
                return;
            }

            selectedItem.count = overflow;
            selectedItem.RefreshCount();
            selectedItem.RevertToOriginalSlot();
            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
        }

        private void QuickTransfer()
        {
            InventoryItem inventoryItem = GetComponentInChildren<InventoryItem>();
            if (inventoryItem == null) return;

            Item item = inventoryItem.item;
            int remaining = inventoryItem.count;

            // Remove source immediately
            Destroy(inventoryItem.gameObject);

            // Move count elsewhere
            remaining = StackIntoExistingSlots(item, remaining, this);
            remaining = PlaceIntoEmptySlots(item, remaining, this);

            // If leftover exists, recreate in original slot
            if (remaining > 0)
                InventoryManager.Instance.SpawnNewItem(item, this, remaining);
            else
                ItemTooltipUI.Instance.HideTooltip();

            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
        }

        private int PlaceOverflowInSlots(Item item, int overflow)
        {
            overflow = StackIntoExistingSlots(item, overflow);
            overflow = PlaceIntoEmptySlots(item, overflow);
            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);

            return overflow;
        }

        private int StackIntoExistingSlots(Item item, int amount, InventorySlot excludeSlot = null)
        {
            if (item == null || amount <= 0) return 0;

            foreach (InventorySlot slot in InventoryManager.Instance.InventorySlots)
            {
                if (slot == excludeSlot || slot.transform.childCount <= 0) continue;

                InventoryItem existing = slot.GetComponentInChildren<InventoryItem>(this);
                if (existing.item.itemName != item.itemName || existing.count >= item.maxStack) continue;

                int space = item.maxStack - existing.count;
                int toMove = Mathf.Min(space, amount);

                existing.count += toMove;
                existing.RefreshCount();

                amount -= toMove;
                if (amount <= 0) return 0;
            }

            return amount;
        }

        private int PlaceIntoEmptySlots(Item item, int amount, InventorySlot excludeSlot = null)
        {
            if (item == null || amount <= 0) return 0;

            foreach (InventorySlot slot in InventoryManager.Instance.InventorySlots)
            {
                if (slot == excludeSlot || slot.transform.childCount != 0) continue;

                int toPlace = Mathf.Min(amount, item.maxStack);
                InventoryManager.Instance.SpawnNewItem(item, slot, toPlace);

                amount -= toPlace;
                if (amount <= 0) return 0;
            }

            return amount;
        }

        #endregion

        public bool IsItemAllowedInSlot(Item item)
        {
            return slotType switch
            {
                SlotType.Fuel => (item.itemTypes & ItemType.Fuel) != 0,
                SlotType.Smelting => (item.itemTypes & ItemType.Smelting) != 0,
                _ => true,
            };
        }

        private void DropSelectedItem()
        {
            if (InventoryItem.selectedItem == null) return;

            InventoryManager.Instance.DropItem(InventoryItem.selectedItem.item, InventoryItem.selectedItem.count);
            Destroy(InventoryItem.selectedItem.gameObject);
            ItemTooltipUI.Instance.HideTooltip();
            InventoryItem.selectedItem.DisableDragLayering();
            StopFollowCursor();

            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
        }

        #region Cursor Follow

        // --- Cursor follow helpers (single coroutine)
        private void StartFollowCursor(InventoryItem item)
        {
            StopFollowCursor();
            followCoroutine = StartCoroutine(FollowCursor(item));
        }

        private void StopFollowCursor()
        {
            if (followCoroutine == null) return;

            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }

        private IEnumerator FollowCursor(InventoryItem item)
        {
            while (item != null && item == InventoryItem.selectedItem)
            {
                item.transform.position = Input.mousePosition;
                yield return null;
            }
        }

        #endregion

        public void ClearSlot()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            InventoryManager.Instance.OnInventoryItemChanged?.Invoke(this);
        }
    }
}
