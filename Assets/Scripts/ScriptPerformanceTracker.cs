using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ScriptPerformanceTracker
{
    private sealed class TimingData
    {
        public double TotalMilliseconds;
        public double PeakMilliseconds;
        public int CallCount;
    }

    public readonly struct Measurement : IDisposable
    {
        private readonly string name;
        private readonly long startTimestamp;

        public Measurement(string name)
        {
            this.name = name;
            startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            double elapsedMilliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;

            Record(name, elapsedMilliseconds);
        }
    }

    private static readonly Dictionary<string, TimingData> CurrentWindow = new();
    private static readonly Dictionary<string, TimingData> DisplayWindow = new();

    private static readonly StringBuilder DisplayBuilder = new();

    private static double nextRefreshTime;
    private const double RefreshInterval = 0.5;

    public static Measurement Measure(string name) => new Measurement(name);

    private static void Record(string name, double milliseconds)
    {
        if (!CurrentWindow.TryGetValue(name, out TimingData data))
        {
            data = new TimingData();
            CurrentWindow.Add(name, data);
        }

        data.TotalMilliseconds += milliseconds;
        data.CallCount++;

        if (milliseconds > data.PeakMilliseconds)
            data.PeakMilliseconds = milliseconds;
    }

    public static string GetDisplayText()
    {
        double currentTime = Time.realtimeSinceStartupAsDouble;

        if (currentTime >= nextRefreshTime)
        {
            nextRefreshTime = currentTime + RefreshInterval;

            DisplayWindow.Clear();

            foreach (KeyValuePair<string, TimingData> pair in CurrentWindow)
            {
                DisplayWindow.Add(pair.Key, new TimingData
                {
                    TotalMilliseconds = pair.Value.TotalMilliseconds,
                    PeakMilliseconds = pair.Value.PeakMilliseconds,
                    CallCount = pair.Value.CallCount
                });
            }

            CurrentWindow.Clear();
        }

        DisplayBuilder.Clear();

        foreach (KeyValuePair<string, TimingData> pair in DisplayWindow
                     .OrderByDescending(entry => entry.Value.TotalMilliseconds)
                     .Take(10))
        {
            TimingData data = pair.Value;
            double average = data.CallCount > 0 ? data.TotalMilliseconds / data.CallCount : 0.0;

            DisplayBuilder
                .Append('\n')
                .Append(pair.Key)
                .Append(": ")
                .Append(average.ToString("F3"))
                .Append(" ms avg | ")
                .Append(data.PeakMilliseconds.ToString("F3"))
                .Append(" ms peak | ")
                .Append(data.CallCount)
                .Append(" calls");
        }

        if (DisplayWindow.Count == 0)
            DisplayBuilder.Append("\nNo measured scripts");

        return DisplayBuilder.ToString();
    }
}