using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

public class PickupNotificationManager : MonoBehaviour
{
    public static PickupNotificationManager Instance;

    public PickupNotification pickupPrefab;
    public Transform notificationsParent;
    public float verticalSpacing = 60f;

    private readonly List<PickupNotification> activeNotifications = new();
    private readonly Queue<PickupNotification> notificationPool = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowPickup(Item item, int count)
    {
        // Only stack if not fading
        PickupNotification existing = activeNotifications.Find(n =>
            n.Item == item && !n.IsFading
        );

        if (existing != null)
        {
            existing.AddCount(count);
        }
        else
        {
            PickupNotification newNotification = GetFromPool();

            newNotification.CaptureBasePosition();
            newNotification.Initialize(item, count, () => ReturnToPool(newNotification));

            activeNotifications.Add(newNotification);
        }

        RepositionNotifications();
    }

    private PickupNotification GetFromPool()
    {
        PickupNotification notification;

        if (notificationPool.Count > 0)
        {
            notification = notificationPool.Dequeue();
            notification.gameObject.SetActive(true);
        }
        else
        {
            notification = Instantiate(pickupPrefab, notificationsParent);
        }

        return notification;
    }

    private void ReturnToPool(PickupNotification notification)
    {
        if (activeNotifications.Contains(notification))
        {
            activeNotifications.Remove(notification);
            RepositionNotifications();
        }

        notification.gameObject.SetActive(false);
        notificationPool.Enqueue(notification);
    }

    private void RepositionNotifications()
    {
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            PickupNotification n = activeNotifications[i];
            Vector2 targetPos = new(n.BaseAnchoredPos.x, n.BaseAnchoredPos.y + i * verticalSpacing);
            n.MoveTo(targetPos);
        }
    }
}
