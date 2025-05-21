using UnityEngine;
using System.Collections;

public class HitEffectManager : MonoBehaviour
{
    public float shrinkFactor = 0.9f; // Percentage of original size to shrink to
    public float duration = 0.2f;

    // Call this method with the GameObject that was hit
    public void ApplyHitEffect(GameObject hitObject)
    {
        StartCoroutine(ShrinkAndExpand(hitObject));
    }

    private IEnumerator ShrinkAndExpand(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;
        Vector3 targetScale = originalScale * shrinkFactor;

        // Scale down
        yield return ScaleOverTime(obj, originalScale, targetScale, duration);

        // Scale back up
        yield return ScaleOverTime(obj, targetScale, originalScale, duration);
    }

    private IEnumerator ScaleOverTime(GameObject obj, Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0;
        while (elapsed < time)
        {
            obj.transform.localScale = Vector3.Lerp(from, to, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.transform.localScale = to;
    }
}
