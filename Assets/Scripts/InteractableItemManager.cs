using System.Collections.Generic;
using UnityEngine;

public class InteractableItemManager : MonoBehaviour
{
    public static InteractableItemManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    private readonly List<InteractableItem> items = new();
    private float mergeTimer = 0f;
    public float mergeInterval = 1f;  // Check every second

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
        items.RemoveAll(item => item == null);
    }

    private void MergeAllItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            InteractableItem item = items[i];
            if (item != null)
            {
                item.MergeNearbyObjects();
            }
        }
    }
}
