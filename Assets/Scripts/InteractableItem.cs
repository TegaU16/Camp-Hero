using System.Collections.Generic;
using Game.Inventory;
using Game.Players;
using Game.Quests;
using Game.Registries;
using Game.Saving;
using Game.Terrain;
using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable, ISaveableObject
{
    public Item item;
    [HideInInspector] public int itemCount = 1;

    private bool isInteracted = false;
    private bool canPickup = true;
    private bool isMerging = false;

    public int maxItemCount = 100;

    private Rigidbody rb;

    [HideInInspector] public VoxelChunk owningChunk;

    [HideInInspector] public Vector2Int CurrentCell;

    private static readonly List<InteractableItem> mergeBuffer = new();

    private void Start()
    {
        if (TryGetComponent(out Rigidbody rigidBody))
        {
            rb = rigidBody;
            rb.linearDamping = 2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void OnEnable()
    {
        ItemGrid.Register(this);
        InteractableItemManager.Instance.Register(this);
    }
    private void OnDisable() 
    {
        ItemGrid.Unregister(this);
        if (InteractableItemManager.HasInstance) 
            InteractableItemManager.Instance.Unregister(this); 
    }

    private void Update()
    {
        ItemGrid.UpdateItemCell(this);
    }

    public void CheckGround()
    {
        if (transform.position.y < -0.1f)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 50f, LayerMask.GetMask("Ground")))
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                transform.position = hit.point + Vector3.up * 0.25f;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public void Interact()
    {
        if (isInteracted || !canPickup) return;
        isInteracted = true;


        bool added = InventoryManager.Instance.AddItem(item, itemCount);
        if (added)
        {
            foreach (Quest quest in QuestManager.Instance.GetActiveQuests())
                quest.OnItemCollected(item);
        }

        itemCount = InventoryManager.Instance.LastRemainingCount;

        InventoryManager.Instance.EquipSelectedItem();

        if (added)
            DestroyInteractableItem(this);
    }

    public void TryMergeNearby()
    {
        if (rb == null) return;
        if (isMerging) return;

        isMerging = true;

        mergeBuffer.Clear();

        foreach (InteractableItem other in ItemGrid.GetNearby(transform.position))
        {
            if (other == this || other.rb == null || other.item.itemName != item.itemName || other.isMerging) continue;

            int totalItemCount = itemCount + other.itemCount;

            if (totalItemCount <= maxItemCount)
            {
                itemCount = totalItemCount;
                other.isMerging = true;   // mark so it won't be processed
                mergeBuffer.Add(other);
            }
            else
            {
                int transferable = maxItemCount - itemCount;
                if (transferable > 0)
                {
                    itemCount += transferable;
                    other.itemCount -= transferable;
                }
            }

            if (itemCount >= maxItemCount) break;
        }

        foreach (InteractableItem merged in mergeBuffer)
        {
            merged.gameObject.SetActive(false); // triggers OnDisable -> unregister
            DestroyInteractableItem(merged);
        }

        isMerging = false;
    }

    private void DestroyInteractableItem(InteractableItem interactable)
    {
        // Notify interactor if needed
        PlayerInteractor interactor = FindAnyObjectByType<PlayerInteractor>();
        if (interactor != null && interactor.CurrentInteractable == interactable.GetComponent<IInteractable>())
            interactor.ClearInteractable();

        VoxelGrid.Instance.RemoveObjectFromChunk(interactable.owningChunk, interactable.gameObject);
        interactable.owningChunk.isDirty = true;

        Destroy(interactable.gameObject);
    }

    public void EnablePickupAfterDelay(float delay)
    {
        canPickup = false;
        Invoke(nameof(EnablePickup), delay);
    }

    private void EnablePickup() => canPickup = true;

    public string GetInteractText()
        => $"Press E to pick up {itemCount}x {item.itemName}";

    public Transform GetTransform() => transform;

    public string SaveState()
    {
        return JsonUtility.ToJson(new InteractableItemData
        {
            itemName = item != null ? item.itemName : "",
            count = itemCount
        });
    }

    public void LoadState(string json)
    {
        InteractableItemData data = JsonUtility.FromJson<InteractableItemData>(json);
        if (data != null)
        {
            item = ItemRegistry.GetItemByName(data.itemName);
            itemCount = data.count;
        }
    }
}
