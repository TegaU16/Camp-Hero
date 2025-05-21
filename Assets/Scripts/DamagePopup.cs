using TMPro;
using UnityEngine;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float floatUpDistance = 1f;
    public float duration = 1f;
    public float floatSpeed = 1f;
    public float fadeDuration = 0.5f;

    private Vector3 initialPosition;
    private Vector3 floatDirection;
    private CanvasGroup canvasGroup;

    public void Setup(int damageAmount, bool crit)
    {
        damageText.text = damageAmount.ToString();

        damageText.color = crit ? Color.yellow : new Color32(53, 230, 213, 255);
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        initialPosition = transform.position;
        float randomAngle = Random.Range(-30f, 30f); // Side arc direction
        Vector3 sideOffset = Quaternion.Euler(0, randomAngle, 0) * Vector3.right;
        floatDirection = (Vector3.up + sideOffset).normalized;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            transform.position = initialPosition + floatUpDistance * progress * floatDirection;

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
