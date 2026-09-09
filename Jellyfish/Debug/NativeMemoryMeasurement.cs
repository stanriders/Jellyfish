using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Jellyfish.Debug;

public static class NativeMemoryMeasurement
{
    private static readonly ConcurrentDictionary<string, double> measurements = new();
    public static IReadOnlyDictionary<string, double> Measurements => measurements;

    public static NativeMemoryTracker AddMemory(object source, long amount)
    {
        var key = source.GetType().Name;

        if (!measurements.TryAdd(key, 1))
            measurements[key] += amount;

        return new NativeMemoryTracker((key, amount), static sender => RemoveMemory(sender.key, sender.amount));
    }

    private static void RemoveMemory(string key, long amount)
    {
        if (measurements.ContainsKey(key))
            measurements[key] -= amount;
    }

    public class NativeMemoryTracker : IDisposable
    {
        private readonly Action<(string key, long amount)> _action;
        private readonly (string key, long amount) _sender;

        public NativeMemoryTracker((string key, long amount) sender, Action<(string key, long amount)> action)
        {
            _sender = sender;
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _action(_sender);
            _isDisposed = true;
        }
    }
}