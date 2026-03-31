using System;
using System.Collections;
using System.Collections.Generic;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tutorial
{
    public class RecipeUnlockPopupQueue : MonoBehaviour
    {
        public static RecipeUnlockPopupQueue Instance;

        private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI itemNameText;

        private readonly Queue<RecipeUnlockNotification> queue = new();
        private bool isShowing;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void Enqueue(RecipeUnlockNotification notification)
        {
            queue.Enqueue(notification);

            if (!isShowing)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            isShowing = true;

            while (queue.Count > 0)
            {
                RecipeUnlockNotification notification = queue.Dequeue();

                icon.sprite = notification.icon;
                itemNameText.text = notification.resultItem.itemName;

                popupRoot.SetActive(true);
                yield return new WaitForSeconds(notification.duration);
                popupRoot.SetActive(false);

                yield return _waitForSeconds0_1; // spacing
            }

            isShowing = false;
        }
    }

    [Serializable]
    public class RecipeUnlockNotification
    {
        public Item resultItem;
        public Sprite icon;
        public float duration = 2.4f;
    }
}
