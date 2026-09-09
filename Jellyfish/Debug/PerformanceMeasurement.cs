using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jellyfish.Debug;

public static class PerformanceMeasurement
{
    private static readonly ConcurrentDictionary<string, double> timedMeasurements = new();
    private static readonly ConcurrentDictionary<string, double> incrementalMeasurements = new();

    public static IReadOnlyDictionary<string, double> TimedMeasurements => timedMeasurements;
    public static IReadOnlyDictionary<string, double> IncrementalMeasurements => incrementalMeasurements;

    public static void Add(string key, double value)
    {
        timedMeasurements[key] = value;
    }

    public static void Increment(string key)
    {
        if (!incrementalMeasurements.TryAdd(key, 1))
            incrementalMeasurements[key]++;
    }

    public static void Reset(string key)
    {
        incrementalMeasurements[key] = 0;
    }
}

public class PerformanceMeasure : IDisposable
{
    private readonly string _key;
    private readonly Stopwatch _stopwatch;

    public PerformanceMeasure(string key)
    {
        _key = key;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        PerformanceMeasurement.Add(_key, _stopwatch.Elapsed.TotalMilliseconds);
    }
}