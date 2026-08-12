using System.Collections;
using System.Collections.Generic;
using Game.Inventory;
using Game.Players;
using Game.Quests;
using Game.Registries;
using Game.Saving;
using Game.Terrain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable, ISaveableObject
{
    public Item item;
    [HideInInspector] public int itemCount = 1;
    [SerializeField] private float outlineWidth = 5f;

    public int maxItemCount = 100;

    private bool isInteracted;
    private bool canPickup = true;

    private Rigidbody rb;
    private Collider col;

    [HideInInspector] public VoxelChunk owningChunk;
    [HideInInspector] public Vector2Int currentCell;

    private bool isSleeping;
    private float sleepTimer;

    private const float SleepDelay = 3f;
    private const float SleepVelocityThreshold = 0.05f;

    private static readonly List<InteractableItem> mergeBuffer = new();

    public Sprite ObjectIcon => item.icon;

    private void Start()
    {
        if (!TryGetComponent(out Rigidbody rb)) return;

        this.rb = rb;
        rb.linearDamping = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        col = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterNextFrame());
    }

    IEnumerator RegisterNextFrame()
    {
        yield return null;

        ItemGrid.Register(this);
        InteractableItemManager.Instance.Register(this);

        foreach (InteractableItem nearby in ItemGrid.GetNearby(transform.position))
            nearby.Wake();
    }

    private void OnDisable()
    {
        ItemGrid.Unregister(this);

        if (InteractableItemManager.HasInstance)
            InteractableItemManager.Instance.Unregister(this);
    }

    public void ManagerUpdate(float deltaTime)
    {
        if (isSleeping) return;

        ItemGrid.UpdateItemCell(this);
        CheckSleep(deltaTime);
        CheckGround();
    }

    private void CheckSleep(float dt)
    {
        if (rb == null) return;

        if (rb.linearVelocity.sqrMagnitude < SleepVelocityThreshold)
            sleepTimer += dt;
        else
            sleepTimer = 0f;

        if (sleepTimer > SleepDelay)
            Sleep();
    }

    private void Sleep()
    {
        if (isSleeping) return;

        isSleeping = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        InteractableItemManager.Instance.Unregister(this);
    }

    public void Wake()
    {
        if (!isSleeping) return;

        isSleeping = false;
        sleepTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        InteractableItemManager.Instance.Register(this);
    }

    private void CheckGround()
    {
        if (rb == null || isSleeping || col == null) return;

        int x = Mathf.FloorToInt(transform.position.x);
        int z = Mathf.FloorToInt(transform.position.z);

        float groundY = Utility.GetHeightAt(x, z);

        float bottomY = col.bounds.min.y;
        float distanceToGround = bottomY - groundY;

        // Small tolerance
        if (distanceToGround < -1f)
            SnapToGround(groundY);
    }

    private void SnapToGround(float y)
    {
        transform.position = new Vector3(
            transform.position.x,
            y,
            transform.position.z
        );

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;     // ← key part
            rb.isKinematic = true;     // ← stop physics completely
        }

        Sleep();
    }

    public void TryMergeNearby()
    {
        if (isSleeping || rb == null) return;

        mergeBuffer.Clear();

        foreach (InteractableItem other in ItemGrid.GetNearby(transform.position))
        {
            if (other == this) continue;
            if (other.item != item) continue;

            int total = itemCount + other.itemCount;

            if (total <= maxItemCount)
            {
                itemCount = total;
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
            merged.gameObject.SetActive(false);
            DestroyInteractableItem(merged);
        }
    }

    private void DestroyInteractableItem(InteractableItem interactable)
    {
        PlayerInteractor interactor = InteractableItemManager.Instance.PlayerInteractor;

        if (interactor != null && interactor.CurrentInteractable == interactable.GetComponent<IInteractable>())
            interactor.ClearInteractable();

        VoxelGrid.Instance.RemoveObjectFromChunk(interactable.owningChunk, interactable.gameObject);

        Destroy(interactable.gameObject);
    }

    public void Interact()
    {
        if (isInteracted || !canPickup) return;

        isInteracted = true;

        bool added = InventoryManager.Instance.AddItem(item, itemCount);
        itemCount = InventoryManager.Instance.LastRemainingCount;

        if (!added) return;

        foreach (Quest quest in QuestManager.Instance.GetActiveQuests())
            quest.OnItemCollected(item);

        DestroyInteractableItem(this);
    }

    public void EnablePickupAfterDelay(float delay)
    {
        canPickup = false;
        Invoke(nameof(EnablePickup), delay);
    }

    private void EnablePickup() => canPickup = true;

    public Transform GetTransform() => transform;

    public string GetInteractText()
    {
        IInteractable interactable = this;
        return $"{item.itemName}\n({itemCount}x)\n{interactable.InteractKeyText}";
    }

    public void SetItem(Item newItem)
    {
        item = newItem;
        if (item == null) return;
        if (!TryGetComponent(out Outline outline)) return;
        
        Color outlineColor = InteractableItemManager.Instance.rarityColorConfig.GetColor(item.rarity);
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
    }

    public string SaveState()
    {
        return JsonUtility.ToJson(new InteractableItemData
        {
            itemName = item != null ? item.itemName : "",
            toolAttribute = item.toolAttribute != null ? item.toolAttribute.attributeID : "",
            count = itemCount
        });
    }

    public void LoadState(string json)
    {
        InteractableItemData data = JsonUtility.FromJson<InteractableItemData>(json);

        if (data == null) return;

        item = ItemRegistry.Instance.GetByKey(data.itemName);
        item.toolAttribute = ToolAttributeRegistry.Instance.GetByKey(data.toolAttribute);
        itemCount = data.count;
    }
}
