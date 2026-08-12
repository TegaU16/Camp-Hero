using System.Text;
using UnityEngine;

public sealed class PerformanceOverlay : MonoBehaviour
{
    [SerializeField, Min(0.05f)]
    private float refreshInterval = 0.25f;

    [SerializeField]
    private KeyCode toggleKey = KeyCode.F3;

    private float accumulatedTime;
    private int accumulatedFrames;

    private float displayedFps;
    private float displayedFrameTimeMs;
    private bool visible = false;

    private readonly StringBuilder textBuilder = new();

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;

        // Not affected by Time.timeScale or pausing.
        accumulatedTime += Time.unscaledDeltaTime;
        accumulatedFrames++;

        if (accumulatedTime < refreshInterval) return;

        displayedFps = accumulatedFrames / accumulatedTime;
        displayedFrameTimeMs = accumulatedTime * 1000f / accumulatedFrames;

        accumulatedTime = 0f;
        accumulatedFrames = 0;
    }

    private void OnGUI()
    {
        if (!visible) return;

        textBuilder.Clear();
        textBuilder.Append("FPS: ").Append(displayedFps.ToString("F1"));
        textBuilder.Append("\nFrame: ").Append(displayedFrameTimeMs.ToString("F2"));
        textBuilder.Append(" ms");
        textBuilder.Append("\n\nScript timings:");
        textBuilder.Append(ScriptPerformanceTracker.GetDisplayText());

        GUI.Box(
            new Rect(10f, 10f, 390f, 360f),
            textBuilder.ToString()
        );
    }
}
