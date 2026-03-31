using System;
using System.Collections.Generic;
using Game.Players;
using UnityEngine;

public class InteractableItemManager : MonoBehaviour
{
    public static InteractableItemManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    private readonly List<InteractableItem> items = new();

    public PlayerInteractor PlayerInteractor { get; private set; }

    [SerializeField] private int itemsPerFrame = 200;
    [SerializeField] private int mergesPerFrame = 100;
    [SerializeField] private float mergeInterval = 1f;

    public RarityColorConfig rarityColorConfig;

    private float mergeTimer;
    private int updateIndex;
    private int mergeIndex;

    public Action<string> OnInteractTextChanged;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        if (items.Count == 0) return;

        float dt = Time.deltaTime;

        ProcessItems(dt);
        ProcessMerges();
    }

    private void ProcessItems(float dt)
    {
        int processed = 0;

        while (processed < itemsPerFrame && updateIndex < items.Count)
        {
            InteractableItem item = items[updateIndex];

            if (item != null && item.gameObject.activeInHierarchy)
                item.ManagerUpdate(dt);

            updateIndex++;
            processed++;
        }

        if (updateIndex >= items.Count)
        {
            updateIndex = 0;
            Cleanup();
        }
    }

    private void ProcessMerges()
    {
        mergeTimer += Time.deltaTime;
        if (mergeTimer < mergeInterval) return;

        mergeTimer = 0;
        int processed = 0;

        while (processed < mergesPerFrame && mergeIndex < items.Count)
        {
            InteractableItem item = items[mergeIndex];

            if (item != null && item.gameObject.activeInHierarchy)
                item.TryMergeNearby();

            mergeIndex++;
            processed++;
        }

        if (mergeIndex < items.Count) return;

        mergeIndex = 0;
        Cleanup();
    }

    private void Cleanup() => items.RemoveAll(i => i == null);

    public void SetPlayer(GameObject player)
    {
        if (player == null) return;

        PlayerInteractor = player.GetComponent<PlayerInteractor>();
    }
}