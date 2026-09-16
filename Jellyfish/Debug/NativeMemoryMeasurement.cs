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

        measurements.AddOrUpdate(key, amount, (k, v) => v + amount);

        return new NativeMemoryTracker((key, amount), static sender => RemoveMemory(sender.key, sender.amount));
    }

    private static void RemoveMemory(string key, long amount)
    {
        measurements.AddOrUpdate(key, 0, (k, v) => v - amount);
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