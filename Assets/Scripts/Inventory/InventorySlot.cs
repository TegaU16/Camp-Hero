using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Smelting;

namespace Game.Inventory
{
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
        public SlotType slotType;
        private Coroutine followCoroutine;

        [HideInInspector] public FurnaceUnit parentFurnace;

        private void Awake()
        {
            if (TryGetComponent(out SelectableImage selectableImage))
                selectableImage.Deselect();
        }

        private void Start()
        {
            image.raycastTarget = true;
        }

        // --- Left click pick / place
        public void HandleLeftClick()
        {
            if (parentFurnace != null)
                parentFurnace.SaveUI();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                QuickTransfer();
                UpdateInventorySlot();
                return;
            }

            if (InventoryItem.selectedItem != null)
            {
                OnSecondClick(this);
                UpdateInventorySlot();
                return;
            }

            if (transform.childCount > 0)
            {
                InventoryItem selectedItem = GetComponentInChildren<InventoryItem>();
                if (selectedItem == null) return;

                selectedItem.originalParentSlot = transform;
                InventoryItem.selectedItem = selectedItem;
                selectedItem.isBeingDragged = true;
                selectedItem.transform.SetParent(transform.root, true); // keep position
                selectedItem.GetComponent<Image>().raycastTarget = false;

                selectedItem.EnableDragLayering();
                StartFollowCursor(selectedItem);
            }

            UpdateInventorySlot();
        }

        // --- Right click split / pick
        public void HandleRightClick()
        {
            if (parentFurnace != null)
                parentFurnace.SaveUI();

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
                    StartFollowCursor(InventoryItem.selectedItem);
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
                            StackItems(existingItem, InventoryItem.selectedItem);
                        else
                            InventoryItem.selectedItem.RevertToOriginalSlot();
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
            if (parentFurnace != null)
                parentFurnace.SaveUI();

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
                InventoryItem.selectedItem.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (parentFurnace != null)
                parentFurnace.SaveUI();

            if (InventoryItem.selectedItem == null) return;

            // clear dragged flag
            InventoryItem.selectedItem.isBeingDragged = false;
            InventoryItem.selectedItem.GetComponent<Image>().raycastTarget = true;

            InventoryItem.selectedItem.DisableDragLayering();
            StopFollowCursor();

            // Use the centralized InventoryUI reference to check if the item is inside the inventory
            if (InventoryManager.InventoryUI != null && !IsItemInsideDeleteSlot(InventoryManager.Instance.inventoryUIHandler.deleteSlot, Input.mousePosition))
                HandleItemDrop(eventData);
            else
                DropSelectedItem();

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
                        StackItems(existingItem, InventoryItem.selectedItem);
                    else
                        InventoryItem.selectedItem.RevertToOriginalSlot();
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
            InventoryManager.Instance.EquipSelectedItem();
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

                int overflow = combinedCount - maxStack;

                // Try to auto-place overflow in other slots
                overflow = PlaceOverflowInSlots(selectedItem.item, overflow);

                if (overflow > 0)
                {
                    // Revert only if no free slot found
                    selectedItem.count = overflow;
                    selectedItem.RefreshCount();
                    selectedItem.RevertToOriginalSlot();
                }
                else
                {
                    Destroy(selectedItem.gameObject);
                }
            }
        }

        private void QuickTransfer()
        {
            InventoryItem inventoryItem = GetComponentInChildren<InventoryItem>();
            if (inventoryItem == null) return;

            int remaining = inventoryItem.count;

            // Try to stack into existing slots first
            foreach (InventorySlot slot in InventoryManager.Instance.inventoryUIHandler.inventorySlots)
            {
                if (slot == this) continue;
                if (slot.transform.childCount > 0)
                {
                    InventoryItem existing = slot.GetComponentInChildren<InventoryItem>();
                    if (existing.item == inventoryItem.item && existing.count < inventoryItem.item.maxStack)
                    {
                        int space = inventoryItem.item.maxStack - existing.count;
                        int toMove = Mathf.Min(space, remaining);

                        existing.count += toMove;
                        existing.RefreshCount();
                        remaining -= toMove;

                        if (remaining <= 0) break;
                    }
                }
            }

            // If leftover, try empty slots
            if (remaining > 0)
            {
                foreach (InventorySlot slot in InventoryManager.Instance.inventoryUIHandler.inventorySlots)
                {
                    if (slot == this) continue;
                    if (slot.transform.childCount == 0)
                    {
                        int toPlace = Mathf.Min(remaining, inventoryItem.item.maxStack);

                        InventoryManager.Instance.SpawnNewItem(inventoryItem.item, slot, toPlace);

                        remaining -= toPlace;
                        if (remaining <= 0) break;
                    }
                }
            }

            // Update or remove original slot
            if (remaining > 0)
            {
                inventoryItem.count = remaining;
                inventoryItem.RefreshCount();
            }
            else
            {
                Destroy(inventoryItem.gameObject);
                ItemTooltipUI.Instance.HideTooltip();
            }
        }

        private int PlaceOverflowInSlots(Item item, int overflow)
        {
            // First, try to stack into other existing items
            foreach (InventorySlot slot in InventoryManager.Instance.inventoryUIHandler.inventorySlots)
            {
                if (slot.transform.childCount > 0)
                {
                    InventoryItem existing = slot.GetComponentInChildren<InventoryItem>();
                    if (existing.item == item && existing.count < item.maxStack)
                    {
                        int space = item.maxStack - existing.count;
                        int toMove = Mathf.Min(space, overflow);

                        existing.count += toMove;
                        existing.RefreshCount();
                        overflow -= toMove;
                        if (overflow <= 0) return 0;
                    }
                }
            }

            // Then, put into empty slots
            foreach (InventorySlot slot in InventoryManager.Instance.inventoryUIHandler.inventorySlots)
            {
                if (slot.transform.childCount == 0)
                {
                    int toPlace = Mathf.Min(overflow, item.maxStack);

                    InventoryManager.Instance.SpawnNewItem(item, slot, toPlace);

                    overflow -= toPlace;
                    if (overflow <= 0) return 0;
                }
            }

            return overflow; // leftover if no slots
        }

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
                    item.transform.position = Input.mousePosition;
                else
                    yield break;
                yield return null;
            }
        }

        public void ClearSlot()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
        }
    }
}
