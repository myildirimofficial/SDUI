using SkiaSharp;
using System;
using System.Collections.Concurrent;

namespace Orivy.Helpers;

internal sealed class TypefaceCache : IDisposable
{
    private const int MaxCacheSize = 256;
    private readonly ConcurrentDictionary<int, SKTypeface> _cache = new();
    private readonly object _cleanupLock = new();
    private bool _disposed;

    public SKTypeface GetOrAdd(int codepoint, Func<SKTypeface> factory)
    {
        if (_cache.TryGetValue(codepoint, out var cached))
            return cached;

        var typeface = factory();
        
        if (_cache.Count >= MaxCacheSize)
            CleanupOldEntries();

        _cache.TryAdd(codepoint, typeface);
        return typeface;
    }

    private void CleanupOldEntries()
    {
        lock (_cleanupLock)
        {
            if (_cache.Count < MaxCacheSize)
                return;

            var entriesToRemove = _cache.Count / 4;
            var removed = 0;

            foreach (var key in _cache.Keys)
            {
                if (_cache.TryRemove(key, out var typeface))
                {
                    typeface?.Dispose();
                    removed++;
                }

                if (removed >= entriesToRemove)
                    break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var typeface in _cache.Values)
            typeface?.Dispose();

        _cache.Clear();
        _disposed = true;
    }
}
