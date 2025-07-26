using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Debug;

public static class PerformanceMeasurment
{
    private static readonly ConcurrentDictionary<string, double> measurements = new();

    public static IReadOnlyDictionary<string, double> Measurements => measurements;

    public static void Add(string key, double value)
    {
        measurements[key] = value;
    }
}