using SDUI.Styling;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace SDUI.Controls;

public abstract partial class ElementBase
{
    private readonly Stopwatch _motionEffectsClock = Stopwatch.StartNew();
    private readonly Timer _motionEffectsTimer = new(33) { AutoReset = true };
    private bool _motionEffectsEnabled;

    [Browsable(false)]
    public ElementMotionScene MotionEffects { get; }

    [Browsable(false)]
    public bool MotionEffectsEnabled => _motionEffectsEnabled;

    private void InitializeMotionEffectsSystem()
    {
        _motionEffectsTimer.Elapsed += HandleMotionEffectsTick;
    }

    public ElementBase ConfigureMotionEffects(Action<ElementMotionSceneBuilder> configure, bool clearExisting = true)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ElementMotionSceneBuilder(this);
        if (clearExisting)
            builder.Clear();

        configure(builder);
        UpdateMotionEffectsState();
        Invalidate();
        return this;
    }

    public void ClearMotionEffects()
    {
        MotionEffects.Clear();
    }

    internal void OnMotionEffectsChanged()
    {
        UpdateMotionEffectsState();
        Invalidate();
    }

    private void UpdateMotionEffectsState()
    {
        _motionEffectsEnabled = MotionEffects.Count > 0;

        if (_motionEffectsEnabled)
        {
            if (!_motionEffectsClock.IsRunning)
                _motionEffectsClock.Start();

            if (!_motionEffectsTimer.Enabled)
                _motionEffectsTimer.Start();
        }
        else
        {
            _motionEffectsTimer.Stop();
        }
    }

    private void HandleMotionEffectsTick(object? sender, ElapsedEventArgs e)
    {
        if (!_motionEffectsEnabled || !Visible || Width <= 0 || Height <= 0 || IsDisposed)
            return;

        Invalidate();
    }

    private void RenderMotionEffects(SKCanvas canvas, SKRect bounds)
    {
        if (!_motionEffectsEnabled || MotionEffects.Count == 0)
            return;

        var elapsedSeconds = _motionEffectsClock.Elapsed.TotalSeconds;
        var state = VisualState;

        for (var i = 0; i < MotionEffects.Count; i++)
        {
            var effect = MotionEffects[i];
            var duration = Math.Max(0.2d, effect.DurationSeconds);
            var speedMultiplier = 1f;
            if (state.IsPointerOver)
                speedMultiplier *= effect.HoverSpeedMultiplier;
            if (state.IsPressed)
                speedMultiplier *= effect.PressedSpeedMultiplier;
            if (state.IsFocused)
                speedMultiplier *= effect.FocusedSpeedMultiplier;

            speedMultiplier = Math.Max(0.1f, speedMultiplier);
            var time = ((elapsedSeconds + effect.DelaySeconds) * speedMultiplier) / duration;
            var wave = (float)Math.Sin(time * Math.PI * 2d);
            var waveSecondary = (float)Math.Cos(time * Math.PI * 1.4d);
            var progress = 0.5f + 0.5f * wave;
            var scale = Lerp(effect.ScaleMin, effect.ScaleMax, progress);
            var opacity = Lerp(effect.OpacityMin, effect.OpacityMax, progress);

            var baseX = bounds.Left + bounds.Width * effect.Anchor.X;
            var baseY = bounds.Top + bounds.Height * effect.Anchor.Y;
            var position = ResolveMotionPosition(effect, baseX, baseY, wave, waveSecondary, progress);
            var centerX = position.X;
            var centerY = position.Y;
            var width = effect.Size.Width * scale;
            var height = effect.Size.Height * scale;
            var color = effect.Color.WithAlpha((byte)Math.Clamp((int)Math.Round(effect.Color.Alpha * opacity), 0, 255));

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = color,
                Style = SKPaintStyle.Fill
            };

            if (effect.ShapeKind == ElementMotionShapeKind.Circle)
            {
                canvas.DrawCircle(centerX, centerY, Math.Max(width, height) * 0.5f, paint);
                continue;
            }

            var saveCount = canvas.Save();
            canvas.Translate(centerX, centerY);
            canvas.RotateDegrees(effect.RotationDegrees * waveSecondary);
            var rect = SKRect.Create(-width / 2f, -height / 2f, width, height);
            if (effect.CornerRadius > 0f)
                canvas.DrawRoundRect(rect, effect.CornerRadius, effect.CornerRadius, paint);
            else
                canvas.DrawRect(rect, paint);
            canvas.RestoreToCount(saveCount);
        }
    }

    private static SKPoint ResolveMotionPosition(
        ElementMotionEffect effect,
        float baseX,
        float baseY,
        float wave,
        float waveSecondary,
        float progress)
    {
        return effect.MovementKind switch
        {
            ElementMotionMovementKind.Orbit => new SKPoint(
                baseX + effect.OrbitRadius.X * wave,
                baseY + effect.OrbitRadius.Y * waveSecondary),
            ElementMotionMovementKind.Bezier => EvaluateBezier(
                baseX,
                baseY,
                effect.PathStart,
                effect.PathControl1,
                effect.PathControl2,
                effect.PathEnd,
                progress),
            _ => new SKPoint(
                baseX + effect.Drift.X * wave,
                baseY + effect.Drift.Y * waveSecondary)
        };
    }

    private static SKPoint EvaluateBezier(
        float baseX,
        float baseY,
        SKPoint start,
        SKPoint control1,
        SKPoint control2,
        SKPoint end,
        float progress)
    {
        var t = progress;
        var inv = 1f - t;
        var p0 = new SKPoint(baseX + start.X, baseY + start.Y);
        var p1 = new SKPoint(baseX + control1.X, baseY + control1.Y);
        var p2 = new SKPoint(baseX + control2.X, baseY + control2.Y);
        var p3 = new SKPoint(baseX + end.X, baseY + end.Y);

        var x = inv * inv * inv * p0.X +
                3f * inv * inv * t * p1.X +
                3f * inv * t * t * p2.X +
                t * t * t * p3.X;
        var y = inv * inv * inv * p0.Y +
                3f * inv * inv * t * p1.Y +
                3f * inv * t * t * p2.Y +
                t * t * t * p3.Y;
        return new SKPoint(x, y);
    }

    private static float Lerp(float from, float to, float progress)
    {
        return from + (to - from) * progress;
    }

    private void DisposeMotionEffectsSystem()
    {
        _motionEffectsTimer.Elapsed -= HandleMotionEffectsTick;
        _motionEffectsTimer.Stop();
        _motionEffectsTimer.Dispose();
    }
}