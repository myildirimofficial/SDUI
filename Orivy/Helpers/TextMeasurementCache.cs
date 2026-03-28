using SkiaSharp;
using System;
using System.Collections.Concurrent;

namespace Orivy.Helpers;

internal sealed class TextMeasurementCache
{
    private const int MaxCacheSize = 512;
    private readonly ConcurrentDictionary<MeasurementKey, SKRect> _cache = new();
    private readonly object _cleanupLock = new();

    public SKRect GetOrMeasure(string text, SKFont font, Func<SKRect> measureFunc)
    {
        var key = new MeasurementKey(text, font.Typeface?.FamilyName ?? "Default", font.Size);

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var bounds = measureFunc();

        if (_cache.Count >= MaxCacheSize)
            CleanupOldEntries();

        _cache.TryAdd(key, bounds);
        return bounds;
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
                if (_cache.TryRemove(key, out _))
                    removed++;

                if (removed >= entriesToRemove)
                    break;
            }
        }
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private readonly struct MeasurementKey : IEquatable<MeasurementKey>
    {
        private readonly string _text;
        private readonly string _fontFamily;
        private readonly float _fontSize;
        private readonly int _hashCode;

        public MeasurementKey(string text, string fontFamily, float fontSize)
        {
            _text = text;
            _fontFamily = fontFamily;
            _fontSize = fontSize;
            _hashCode = HashCode.Combine(text, fontFamily, fontSize);
        }

        public bool Equals(MeasurementKey other)
        {
            return _text == other._text &&
                   _fontFamily == other._fontFamily &&
                   Math.Abs(_fontSize - other._fontSize) < 0.001f;
        }

        public override bool Equals(object obj)
        {
            return obj is MeasurementKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;
    }
}
