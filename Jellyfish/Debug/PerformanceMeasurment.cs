using System.Collections.Generic;

namespace Jellyfish.Debug;

public static class PerformanceMeasurment
{
    private static readonly SortedDictionary<string, double> measurements = new();

    public static IReadOnlyDictionary<string, double> Measurements => measurements;

    public static void Add(string key, double value)
    {
        measurements[key] = value;
    }
}