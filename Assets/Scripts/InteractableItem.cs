using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public Item item;
    public bool isGold = false;
    [HideInInspector] public int itemCount;
    private bool isInteracted = false; // Flag to prevent multiple interactions

    [Header("For Item Drops")]
    public float mergeDistance = 2f; // Distance at which objects will merge
    public int maxItemCount = 100; // Maximum item count before merging stops
    private float mergeCheckTimer = 0f;
    public float mergeCheckInterval = 1f; // Reduced check interval (1 second)

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null )
            rb.linearDamping = 2f;
    }

    void Update()
    {
        // Only attempt merging if the object has a Rigidbody and we're past the interval
        if (rb != null)
        {
            mergeCheckTimer += Time.deltaTime;
            if (mergeCheckTimer >= mergeCheckInterval)
            {
                mergeCheckTimer = 0f;
                MergeNearbyObjects();
            }
        }
    }

    public void Interact()
    {
        if (isInteracted) return; // Prevent interaction if already done

        isInteracted = true; // Set flag to true on interaction

        // Add this item to the inventory
        if (!isGold)
        {
            bool added = InventoryManager.Instance.AddItem(item, itemCount);
            if (added)
            {
                Destroy(gameObject); // Destroy the object in the world if added to the inventory
            }
            else
            {
                itemCount -= InventoryManager.Instance.LastRemainingCount;
            }
        }
        else
        {
            bool added = InventoryManager.Instance.AddGold(itemCount);
            if (added)
            {
                Destroy(gameObject); // Destroy the object in the world if added to the inventory
            }
        }

        InventoryManager.Instance.EquipSelectedItem();
    }

    private void MergeNearbyObjects()
    {
        // Filter nearby objects using a layer mask to only check for interactable objects
        int layerMask = LayerMask.GetMask("Pickable");
        Collider[] nearbyColliders = new Collider[20];

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, mergeDistance, nearbyColliders, layerMask);

        if (hitCount == nearbyColliders.Length)
        {
            // Resize the array if it's full
            Collider[] expandedArray = new Collider[hitCount * 2];  // Double the size
            hitCount = Physics.OverlapSphereNonAlloc(transform.position, mergeDistance, expandedArray, layerMask);
            nearbyColliders = expandedArray;  // Assign the expanded array
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = nearbyColliders[i];
            // Skip merging with itself
            if (collider.gameObject == gameObject) continue;

            InteractableItem other = collider.gameObject.GetComponent<InteractableItem>();

            if (other != null && !other.isGold && other.item == item)
            {
                // Only merge if the other object has a Rigidbody (it can be merged)
                if (other.TryGetComponent<Rigidbody>(out _))
                {
                    int totalItemCount = itemCount + other.itemCount;

                    // Check if the total count exceeds the max item count
                    if (totalItemCount <= maxItemCount)
                    {
                        // Merge item counts and destroy the other object
                        itemCount = totalItemCount;
                        Destroy(other.gameObject);

                        break; // Exit the loop once a merge is done
                    }
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if the other object is within range, same item, not gold, and has a Rigidbody
        InteractableItem otherObject = other.gameObject.GetComponent<InteractableItem>();
        if (otherObject != null && otherObject.rb != null && otherObject.item == item && !otherObject.isGold)
        {
            int totalItemCount = itemCount + otherObject.itemCount;

            // Check if the total count exceeds the max item count
            if (totalItemCount <= maxItemCount)
            {
                // Merge item counts and destroy the other object
                itemCount = totalItemCount;
                Destroy(other.gameObject);
            }
        }
    }

    public string GetInteractText()
    {
        return isGold ? $"Press E to pick up {itemCount} gold" : $"Press E to pick up {itemCount}x {item.name}";
    }

    public Transform GetTransform()
    {
        return transform;
    }
}