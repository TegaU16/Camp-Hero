using TMPro;
using UnityEngine;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float floatUpDistance = 1.5f;
    public float duration = 0.7f;
    public float fadeDuration = 0.4f;

    public Color baseColor = Color.white;
    public Color critColor = Color.yellow;

    private Vector3 initialPosition;
    private Vector3 floatDirection;
    private CanvasGroup canvasGroup;

    public void Setup(int damageAmount, bool crit)
    {
        damageText.text = damageAmount.ToString();
        damageText.color = crit ? critColor : baseColor;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        initialPosition = transform.position;

        float randomAngle = Random.Range(-60f, 60f);
        Vector3 sideOffset = Quaternion.Euler(0, randomAngle, 0) * Vector3.right;

        floatDirection = (Vector3.up * 1.2f + sideOffset * 2f).normalized;

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

            if (elapsed > duration - fadeDuration)
            {
                float fadeProgress = (elapsed - (duration - fadeDuration)) / fadeDuration;
                canvasGroup.alpha = 1f - fadeProgress;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        DamagePopupPool.Instance.ReturnPopup(gameObject);
    }
}
