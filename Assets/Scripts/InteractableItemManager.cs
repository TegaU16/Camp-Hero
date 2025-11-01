using System.Collections.Generic;
using UnityEngine;

public class InteractableItemManager : MonoBehaviour
{
    public static InteractableItemManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    private readonly List<InteractableItem> items = new();

    // Batch processing indices
    private int groundIndex = 0;
    private int mergeIndex = 0;

    [Header("Performance Settings")]
    public int itemsPerFrame = 200;      // how many ground checks per frame
    public int mergesPerFrame = 100;     // how many merges per frame

    [Header("Intervals (seconds)")]
    public float mergeInterval = 1f;     // only run merges every second
    public float groundCheckInterval = 0.5f; // ground checks every half second

    private float mergeTimer = 0f;
    private float groundCheckTimer = 0f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(InteractableItem item)
    {
        if (!items.Contains(item))
            items.Add(item);
    }

    public void Unregister(InteractableItem item)
    {
        items.Remove(item);
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameManagerReady()) return;
        if (items.Count == 0) return;

        mergeTimer += Time.deltaTime;
        groundCheckTimer += Time.deltaTime;

        // Handle merging in small batches, only at set interval
        if (mergeTimer >= mergeInterval)
        {
            int processed = 0;
            while (processed < mergesPerFrame && mergeIndex < items.Count)
            {
                InteractableItem item = items[mergeIndex];
                if (item != null && item.gameObject.activeInHierarchy)
                    item.TryMergeNearby();

                mergeIndex++;
                processed++;
            }

            if (mergeIndex >= items.Count)
            {
                mergeIndex = 0;
                CleanupList(); // cleanup after a full pass
            }
        }

        // Handle ground checks in small batches, only at set interval
        if (groundCheckTimer >= groundCheckInterval)
        {
            int processed = 0;
            while (processed < itemsPerFrame && groundIndex < items.Count)
            {
                InteractableItem item = items[groundIndex];
                if (item != null && item.gameObject.activeInHierarchy)
                    item.CheckGround();

                groundIndex++;
                processed++;
            }

            if (groundIndex >= items.Count)
            {
                groundIndex = 0;
                CleanupList();
            }
        }
    }

    private void CleanupList()
    {
        // Remove destroyed items safely
        items.RemoveAll(item => item == null);
    }

    /// <summary>
    /// Optional utility: Force-update all items instantly
    /// </summary>
    public void ForceMergeAll()
    {
        CleanupList();
        foreach (InteractableItem item in items)
        {
            if (item == null) continue;
            if (!item.gameObject.activeInHierarchy) continue;
            item.TryMergeNearby();
        }
    }
}
