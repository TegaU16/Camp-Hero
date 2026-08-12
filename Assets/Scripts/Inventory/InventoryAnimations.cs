using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory
{
    public static class InventoryAnimations
    {
        public static Tween AnimateTransfer(
            InventoryItem sourceItem,
            InventorySlot targetSlot,
            float duration,
            Action onComplete = null)
        {
            Canvas canvas = sourceItem.GetComponentInParent<Canvas>();

            GameObject ghost = new("TransferGhost");
            ghost.transform.SetParent(canvas.transform, false);

            Image image = ghost.AddComponent<Image>();
            image.sprite = sourceItem.image.sprite;
            image.raycastTarget = false;

            RectTransform ghostRect = ghost.GetComponent<RectTransform>();
            RectTransform sourceRect = sourceItem.GetComponent<RectTransform>();
            RectTransform targetRect = targetSlot.GetComponent<RectTransform>();

            ghostRect.position = sourceRect.position;
            ghostRect.sizeDelta = sourceRect.rect.size;

            return ghostRect
                .DOMove(targetRect.position, duration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    UnityEngine.Object.Destroy(ghost);
                    onComplete?.Invoke();
                });
        }
    }
}
