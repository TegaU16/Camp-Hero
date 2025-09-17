using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public Item item;
    public bool isGold = false;
    [HideInInspector] public int itemCount;

    private bool isInteracted = false;
    private bool canPickup = true;

    [Header("For Item Drops")]
    public float mergeDistance = 2f;
    public int maxItemCount = 100;
    public float mergeCheckInterval = 1f;

    private int pickableLayerMask;
    
    private Rigidbody rb;

    [HideInInspector] public VoxelChunk owningChunk;
    [HideInInspector] public int savedObjectIndex;

    private void Start()
    {
        pickableLayerMask = LayerMask.GetMask("Pickable");
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearDamping = 2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void OnEnable()
    {
        InteractableItemManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (InteractableItemManager.HasInstance)
            InteractableItemManager.Instance.Unregister(this);
    }

    public void Interact()
    {
        if (isInteracted) return;
        if (!canPickup) return;

        isInteracted = true;
        bool toDestroy = false;

        if (!isGold)
        {
            bool added = InventoryManager.Instance.AddItem(item, itemCount);
            if (added)
                toDestroy = true;
            else
                itemCount = InventoryManager.Instance.LastRemainingCount;
        }
        else
        {
            bool added = InventoryManager.Instance.AddGold(itemCount);
            if (added)
                toDestroy = true;
        }

        InventoryManager.Instance.EquipSelectedItem();

        if (toDestroy)
            DestroyInteractableItem(this);
    }

    public void MergeNearbyObjects()
    {
        if (rb == null) return;

        Collider[] nearbyColliders = new Collider[20];

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, mergeDistance, nearbyColliders, pickableLayerMask);

        if (hitCount == nearbyColliders.Length)
        {
            Collider[] expandedArray = new Collider[hitCount * 2];
            hitCount = Physics.OverlapSphereNonAlloc(transform.position, mergeDistance, expandedArray, pickableLayerMask);
            nearbyColliders = expandedArray;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = nearbyColliders[i];

            if (collider.gameObject == gameObject) continue;

            InteractableItem other = collider.gameObject.GetComponent<InteractableItem>();

            if (other != null && !other.isGold && other.item.itemName == item.itemName)
            {
                if (other.TryGetComponent(out Rigidbody _))
                {
                    int totalItemCount = itemCount + other.itemCount;

                    if (totalItemCount <= maxItemCount)
                    {
                        itemCount = totalItemCount;
                        DestroyInteractableItem(other);

                        break;
                    }
                }
            }
        }
    }

    private void DestroyInteractableItem(InteractableItem interactable)
    {
        if (interactable.owningChunk != null && interactable.savedObjectIndex >= 0 && interactable.savedObjectIndex < interactable.owningChunk.savedObjects.Count)
        {
            interactable.owningChunk.savedObjects.RemoveAt(interactable.savedObjectIndex);
            interactable.owningChunk.isDirty = true;
        }
        Destroy(interactable.gameObject);
    }

    public void EnablePickupAfterDelay(float delay)
    {
        canPickup = false;
        Invoke(nameof(EnablePickup), delay);
    }

    private void EnablePickup() => canPickup = true;

    public string GetInteractText()
    {
        return isGold ? $"Press E to pick up {itemCount} gold" : $"Press E to pick up {itemCount}x {item.itemName}";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void LoadInteractableItemData(InteractableItemData interactableItemData)
    {
        item = ItemRegistry.GetItemByName(interactableItemData.itemName);
        itemCount = interactableItemData.count;
    }
}
