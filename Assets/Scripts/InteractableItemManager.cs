using System.Collections.Generic;
using UnityEngine;

public class InteractableItemManager : MonoBehaviour
{
    public static InteractableItemManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    private readonly List<InteractableItem> items = new();
    private float mergeTimer = 0f;
    public float mergeInterval = 1f;  // Merge every second

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
        if (GameManager.Instance.isPaused) return;

        mergeTimer += Time.deltaTime;
        if (mergeTimer >= mergeInterval)
        {
            mergeTimer = 0f;
            CleanupList();
            MergeAllItems();
        }
    }

    private void CleanupList()
    {
        // Remove destroyed items safely
        items.RemoveAll(item => item == null);
    }

    private void MergeAllItems()
    {
        // Create a temporary copy to avoid modifying list during iteration
        List<InteractableItem> snapshot = new(items);

        foreach (InteractableItem item in snapshot)
        {
            if (item == null) continue;
            if (!item.gameObject.activeInHierarchy) continue; // Skip inactive items
            item.MergeNearbyObjects();
        }
    }

    /// <summary>
    /// Optional utility: Force-update an item when player dies or drops items
    /// </summary>
    public void ForceMergeAll()
    {
        CleanupList();
        MergeAllItems();
    }
}
