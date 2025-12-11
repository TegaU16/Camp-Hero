using System.Collections;
using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LevelUpPopup : MonoBehaviour
    {
        public float floatUpDistance = 1.5f;
        public float duration = 0.7f;
        public float fadeDuration = 0.4f;

        public float uiHeightOffset = 1.5f;
        public float surfaceOffset = 0.15f;

        private Vector3 initialPosition;
        private Vector3 floatDirection;
        private CanvasGroup canvasGroup;

        private Transform player;

        public void Setup()
        {
            player = GameManager.Instance.playerInstance.transform;
            if (player == null) return;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;

            initialPosition = player.position + Vector3.up * 2f;

            floatDirection = (Vector3.up * 1.2f).normalized;

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = elapsed / duration;

                // stronger curve: ease-out interpolation
                float easedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);

                transform.position = initialPosition + floatUpDistance * easedProgress * floatDirection;
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0f, 180f, 0f);

                if (elapsed > duration - fadeDuration)
                {
                    float fadeProgress = (elapsed - (duration - fadeDuration)) / fadeDuration;
                    canvasGroup.alpha = 1f - fadeProgress;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
