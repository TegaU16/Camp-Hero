using System.Diagnostics;

public static class PerfTimer
{
    public static void Measure(string label, System.Action action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        action.Invoke();
        sw.Stop();

        UnityEngine.Debug.Log($"{label}: {sw.ElapsedMilliseconds} ms");
    }
}