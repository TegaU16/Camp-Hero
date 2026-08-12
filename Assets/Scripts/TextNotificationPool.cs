using System.Collections.Generic;
using Game;
using UnityEngine;

public class TextNotificationPool : MultiObjectPool<TextNotification>
{
    [SerializeField] private List<GameObject> notificationPrefabs = new();
    [SerializeField] private int sizePerPool = 10;

    protected override void Awake()
    {
        base.Awake();
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
                TextNotification textNotification = CreatePooledObject(notificationPrefab);
                if (textNotification == null) continue;

                textNotifications.Enqueue(textNotification);
            }

            pools[notificationPrefab] = textNotifications;
        }
    }

    protected override TextNotification CreatePooledObject(GameObject prefab)
    {
        if (!prefab.TryGetComponent(out TextNotification _)) return null;

        GameObject notificationInstance = Instantiate(prefab);
        TextNotification textNotification = notificationInstance.GetComponent<TextNotification>();
        notificationInstance.SetActive(false);

        return textNotification;
    }
}
