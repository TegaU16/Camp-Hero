using TMPro;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DayTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private float typeSpeed = 0.05f; // seconds between each letter
    [SerializeField] private float visibleDuration = 2f; // how long text stays fully visible
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowDay(int dayNumber)
    {
        string fullText = $"Day {dayNumber}";

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(TypeTextRoutine(fullText));
    }

    private IEnumerator TypeTextRoutine(string fullText)
    {
        canvasGroup.alpha = 1f;
        dayText.text = "";

        // Typewriter effect
        foreach (char c in fullText)
        {
            dayText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Wait before fading out
        yield return new WaitForSeconds(visibleDuration);

        // Smooth fade out
        float fadeDuration = 1f;
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
