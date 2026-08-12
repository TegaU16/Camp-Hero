using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            canvasGroup = popupRoot.GetComponent<CanvasGroup>();
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

            RectTransform rect = (RectTransform)popupRoot.transform;

            while (queue.Count > 0)
            {
                RecipeUnlockNotification notification = queue.Dequeue();

                icon.sprite = notification.icon;
                itemNameText.text = notification.resultItem.itemName;

                popupRoot.SetActive(true);

                rect.DOKill();
                canvasGroup.DOKill();

                yield return UITween.PopIn(rect, canvasGroup).WaitForCompletion();

                itemNameText.transform.DOKill();
                itemNameText.transform.localScale = Vector3.one * 0.9f;
                itemNameText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

                Tween idle = UITween.FloatY(rect);

                yield return new WaitForSeconds(notification.duration);

                idle.Kill();

                yield return UITween.PopOut(rect, canvasGroup).WaitForCompletion();

                popupRoot.SetActive(false);

                yield return _waitForSeconds0_1;
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
