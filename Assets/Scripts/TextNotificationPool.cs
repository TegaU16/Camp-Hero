using System.Collections.Generic;
using Game.Registries;
using UnityEngine;

public class TextNotificationPool : MonoBehaviour
{
    public static TextNotificationPool Instance;

    private readonly Dictionary<GameObject, Queue<TextNotification>> pools = new();
    [SerializeField] private List<GameObject> notificationPrefabs = new();
    [SerializeField] private int sizePerPool = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject notificationPrefab in notificationPrefabs)
        {
            Queue<TextNotification> textNotifications = new();

            for (int i = 0; i < sizePerPool; i++)
            {
                TextNotification textNotification = CreatePooledNotification(notificationPrefab);
                if (textNotification == null) continue;

                textNotifications.Enqueue(textNotification);
            }

            pools[notificationPrefab] = textNotifications;
        }
    }

    private TextNotification CreatePooledNotification(GameObject notificationPrefab)
    {
        if (!notificationPrefab.TryGetComponent(out TextNotification _)) return null;

        GameObject notificationInstance = Instantiate(notificationPrefab);
        TextNotification textNotification = notificationInstance.GetComponent<TextNotification>();
        notificationInstance.SetActive(false);

        return textNotification;
    }

    public TextNotification GetTextNotification(GameObject notification)
    {
        if (!pools.ContainsKey(notification))
        {
            Debug.LogWarning("No pool found for prefab: " + notification.name);
            return null;
        }

        Queue<TextNotification> textNotifications = pools[notification];

        if (textNotifications.Count == 0)
        {
            TextNotification newTextNotification = CreatePooledNotification(notification);
            return newTextNotification;
        }

        return textNotifications.Dequeue();
    }

    public void ReturnTextNotification(TextNotification notification)
    {
        notification.gameObject.SetActive(false);

        PrefabID id = notification.GetComponent<PrefabID>();
        GameObject prefab = PrefabRegistry.GetPrefabByKey(id.prefabKey);

        if (pools.TryGetValue(prefab, out Queue<TextNotification> pool))
            pool.Enqueue(notification);
    }
}
