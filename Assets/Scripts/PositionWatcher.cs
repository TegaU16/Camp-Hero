using UnityEngine;
using System.Diagnostics;

public class PositionWatcher : MonoBehaviour
{
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (transform.position != lastPosition)
        {
            UnityEngine.Debug.Log($"Position changed from {lastPosition} to {transform.position}", this);
            UnityEngine.Debug.Log(StackTraceToString(new StackTrace(true)));
            lastPosition = transform.position;
        }
    }

    string StackTraceToString(StackTrace stackTrace)
    {
        var frames = stackTrace.GetFrames();
        System.Text.StringBuilder sb = new();
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method.DeclaringType != typeof(PositionWatcher))
            {
                sb.AppendLine($"{method.DeclaringType}.{method.Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}");
            }
        }
        return sb.ToString();
    }
}
