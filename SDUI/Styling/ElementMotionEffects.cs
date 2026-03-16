using SDUI.Controls;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;

namespace SDUI.Styling;

public enum ElementMotionShapeKind
{
    Circle,
    Rectangle
}

public enum ElementMotionMovementKind
{
    Drift,
    Orbit,
    Bezier
}

public sealed class ElementMotionEffect
{
    public ElementMotionShapeKind ShapeKind { get; set; }
    public ElementMotionMovementKind MovementKind { get; set; } = ElementMotionMovementKind.Drift;
    public SKPoint Anchor { get; set; } = new(0.5f, 0.5f);
    public SKSize Size { get; set; } = new(40f, 40f);
    public SKPoint Drift { get; set; } = new(18f, 12f);
    public SKPoint OrbitRadius { get; set; } = new(22f, 18f);
    public SKPoint PathStart { get; set; }
    public SKPoint PathControl1 { get; set; } = new(16f, -22f);
    public SKPoint PathControl2 { get; set; } = new(-12f, 26f);
    public SKPoint PathEnd { get; set; } = new(28f, 8f);
    public SKColor Color { get; set; } = SKColors.White.WithAlpha(24);
    public float OpacityMin { get; set; } = 0.18f;
    public float OpacityMax { get; set; } = 0.52f;
    public float ScaleMin { get; set; } = 0.94f;
    public float ScaleMax { get; set; } = 1.12f;
    public float RotationDegrees { get; set; } = 8f;
    public float CornerRadius { get; set; } = 12f;
    public double DurationSeconds { get; set; } = 4.2d;
    public double DelaySeconds { get; set; }
    public float HoverSpeedMultiplier { get; set; } = 1f;
    public float PressedSpeedMultiplier { get; set; } = 1f;
    public float FocusedSpeedMultiplier { get; set; } = 1f;
}

public sealed class ElementMotionEffectBuilder
{
    private readonly ElementMotionEffect _effect;

    internal ElementMotionEffectBuilder(ElementMotionEffect effect)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }

    public ElementMotionEffectBuilder Anchor(float x, float y)
    {
        _effect.Anchor = new SKPoint(Math.Clamp(x, 0f, 1f), Math.Clamp(y, 0f, 1f));
        return this;
    }

    public ElementMotionEffectBuilder Size(float width, float height)
    {
        _effect.Size = new SKSize(Math.Max(1f, width), Math.Max(1f, height));
        return this;
    }

    public ElementMotionEffectBuilder Drift(float x, float y)
    {
        _effect.MovementKind = ElementMotionMovementKind.Drift;
        _effect.Drift = new SKPoint(x, y);
        return this;
    }

    public ElementMotionEffectBuilder Orbit(float radiusX, float radiusY)
    {
        _effect.MovementKind = ElementMotionMovementKind.Orbit;
        _effect.OrbitRadius = new SKPoint(radiusX, radiusY);
        return this;
    }

    public ElementMotionEffectBuilder Bezier(SKPoint start, SKPoint control1, SKPoint control2, SKPoint end)
    {
        _effect.MovementKind = ElementMotionMovementKind.Bezier;
        _effect.PathStart = start;
        _effect.PathControl1 = control1;
        _effect.PathControl2 = control2;
        _effect.PathEnd = end;
        return this;
    }

    public ElementMotionEffectBuilder Color(SKColor color)
    {
        _effect.Color = color;
        return this;
    }

    public ElementMotionEffectBuilder Opacity(float minimum, float maximum)
    {
        _effect.OpacityMin = Math.Clamp(Math.Min(minimum, maximum), 0f, 1f);
        _effect.OpacityMax = Math.Clamp(Math.Max(minimum, maximum), 0f, 1f);
        return this;
    }

    public ElementMotionEffectBuilder Scale(float minimum, float maximum)
    {
        _effect.ScaleMin = Math.Max(0.1f, Math.Min(minimum, maximum));
        _effect.ScaleMax = Math.Max(_effect.ScaleMin, Math.Max(minimum, maximum));
        return this;
    }

    public ElementMotionEffectBuilder Rotate(float degrees)
    {
        _effect.RotationDegrees = degrees;
        return this;
    }

    public ElementMotionEffectBuilder CornerRadius(float radius)
    {
        _effect.CornerRadius = Math.Max(0f, radius);
        return this;
    }

    public ElementMotionEffectBuilder Duration(double seconds)
    {
        _effect.DurationSeconds = Math.Max(0.2d, seconds);
        return this;
    }

    public ElementMotionEffectBuilder Delay(double seconds)
    {
        _effect.DelaySeconds = Math.Max(0d, seconds);
        return this;
    }

    public ElementMotionEffectBuilder SpeedOnHover(float multiplier)
    {
        _effect.HoverSpeedMultiplier = Math.Max(0.1f, multiplier);
        return this;
    }

    public ElementMotionEffectBuilder SpeedOnPressed(float multiplier)
    {
        _effect.PressedSpeedMultiplier = Math.Max(0.1f, multiplier);
        return this;
    }

    public ElementMotionEffectBuilder SpeedOnFocused(float multiplier)
    {
        _effect.FocusedSpeedMultiplier = Math.Max(0.1f, multiplier);
        return this;
    }
}

public sealed class ElementMotionScene : Collection<ElementMotionEffect>
{
    private readonly ElementBase _owner;

    internal ElementMotionScene(ElementBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override void InsertItem(int index, ElementMotionEffect item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item);
        _owner.OnMotionEffectsChanged();
    }

    protected override void SetItem(int index, ElementMotionEffect item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.SetItem(index, item);
        _owner.OnMotionEffectsChanged();
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _owner.OnMotionEffectsChanged();
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        _owner.OnMotionEffectsChanged();
    }
}

public sealed class ElementMotionSceneBuilder
{
    private readonly ElementBase _owner;

    internal ElementMotionSceneBuilder(ElementBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public ElementMotionSceneBuilder Clear()
    {
        _owner.MotionEffects.Clear();
        return this;
    }

    public ElementMotionSceneBuilder Circle(Action<ElementMotionEffectBuilder> configure)
    {
        return Add(ElementMotionShapeKind.Circle, configure);
    }

    public ElementMotionSceneBuilder Rectangle(Action<ElementMotionEffectBuilder> configure)
    {
        return Add(ElementMotionShapeKind.Rectangle, configure);
    }

    private ElementMotionSceneBuilder Add(ElementMotionShapeKind kind, Action<ElementMotionEffectBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var effect = new ElementMotionEffect { ShapeKind = kind };
        configure(new ElementMotionEffectBuilder(effect));
        _owner.MotionEffects.Add(effect);
        return this;
    }
}