using SDUI.Animation;
using SDUI.Controls;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;

namespace SDUI.Styling;

[Flags]
public enum ElementVisualStates
{
    None = 0,
    PointerOver = 1 << 0,
    Pressed = 1 << 1,
    Focused = 1 << 2,
    Disabled = 1 << 3,
    Hidden = 1 << 4
}

public readonly record struct ElementVisualStateContext(ElementBase Element, ElementVisualStates States)
{
    public bool IsPointerOver => States.HasFlag(ElementVisualStates.PointerOver);
    public bool IsPressed => States.HasFlag(ElementVisualStates.Pressed);
    public bool IsFocused => States.HasFlag(ElementVisualStates.Focused);
    public bool IsEnabled => !States.HasFlag(ElementVisualStates.Disabled);
    public bool IsVisible => !States.HasFlag(ElementVisualStates.Hidden);
}

public sealed class ElementVisualTransition
{
    private TimeSpan _duration = TimeSpan.FromMilliseconds(180);

    public bool Enabled { get; set; } = true;

    public TimeSpan Duration
    {
        get => _duration;
        set => _duration = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

    public AnimationType AnimationType { get; set; } = AnimationType.EaseInOut;

    internal double GetIncrement()
    {
        if (!Enabled || _duration <= TimeSpan.Zero)
            return 1d;

        return Math.Clamp(16d / Math.Max(16d, _duration.TotalMilliseconds), 0.01d, 1d);
    }
}

public sealed class ElementVisualStyle
{
    public float? Width { get; set; }
    public float? Height { get; set; }
    public SKColor? BackColor { get; set; }
    public SKColor? ForeColor { get; set; }
    public Thickness? Border { get; set; }
    public SKColor? BorderColor { get; set; }
    public Radius? Radius { get; set; }
    public BoxShadow[]? Shadows { get; set; }
    public float? Opacity { get; set; }

    internal void ApplyTo(ref ElementVisualStyleSnapshot snapshot)
    {
        if (Width.HasValue)
            snapshot = snapshot.WithSize(new SKSize(Width.Value, snapshot.Size.Height));

        if (Height.HasValue)
            snapshot = snapshot.WithSize(new SKSize(snapshot.Size.Width, Height.Value));

        if (BackColor.HasValue)
            snapshot = snapshot.WithBackColor(BackColor.Value);

        if (ForeColor.HasValue)
            snapshot = snapshot.WithForeColor(ForeColor.Value);

        if (Border.HasValue)
            snapshot = snapshot.WithBorder(Border.Value);

        if (BorderColor.HasValue)
            snapshot = snapshot.WithBorderColor(BorderColor.Value);

        if (Radius.HasValue)
            snapshot = snapshot.WithRadius(Radius.Value);

        if (Shadows != null)
            snapshot = snapshot.WithShadows(ElementVisualStyleInterpolator.CloneShadows(Shadows));

        if (Opacity.HasValue)
            snapshot = snapshot.WithOpacity(Math.Clamp(Opacity.Value, 0f, 1f));
    }
}

public sealed class ElementVisualStyleRule
{
    private readonly Func<ElementBase, ElementVisualStateContext, bool>? _predicate;

    public ElementVisualStyleRule(ElementVisualStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    private ElementVisualStyleRule(
        ElementVisualStyle style,
        Func<ElementBase, ElementVisualStateContext, bool> predicate) : this(style)
    {
        _predicate = predicate;
    }

    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public ElementVisualStates RequiredStates { get; set; }
    public ElementVisualStates ExcludedStates { get; set; }
    public ElementVisualStyle Style { get; }
    public ElementVisualTransition? Transition { get; set; }

    public static ElementVisualStyleRule When(ElementVisualStates requiredStates, Action<ElementVisualStyle> configure)
    {
        var style = new ElementVisualStyle();
        configure(style);
        return new ElementVisualStyleRule(style) { RequiredStates = requiredStates };
    }

    public static ElementVisualStyleRule When(
        Func<ElementBase, ElementVisualStateContext, bool> predicate,
        Action<ElementVisualStyle> configure)
    {
        var style = new ElementVisualStyle();
        configure(style);
        return new ElementVisualStyleRule(style, predicate);
    }

    public bool Matches(ElementBase element, ElementVisualStateContext state)
    {
        if (!Enabled)
            return false;

        if ((state.States & RequiredStates) != RequiredStates)
            return false;

        if ((state.States & ExcludedStates) != 0)
            return false;

        return _predicate == null || _predicate(element, state);
    }
}

public sealed class ElementVisualStyleCollection : Collection<ElementVisualStyleRule>
{
    private readonly ElementBase _owner;

    internal ElementVisualStyleCollection(ElementBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override void InsertItem(int index, ElementVisualStyleRule item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item);
        _owner.OnVisualStyleDefinitionsChanged();
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _owner.OnVisualStyleDefinitionsChanged();
    }

    protected override void SetItem(int index, ElementVisualStyleRule item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.SetItem(index, item);
        _owner.OnVisualStyleDefinitionsChanged();
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        _owner.OnVisualStyleDefinitionsChanged();
    }
}

internal readonly struct ElementVisualStyleSnapshot : IEquatable<ElementVisualStyleSnapshot>
{
    public ElementVisualStyleSnapshot(
        SKSize size,
        SKColor backColor,
        SKColor foreColor,
        Thickness border,
        SKColor borderColor,
        Radius radius,
        BoxShadow[] shadows,
        float opacity)
    {
        Size = size;
        BackColor = backColor;
        ForeColor = foreColor;
        Border = border;
        BorderColor = borderColor;
        Radius = radius;
        Shadows = shadows ?? Array.Empty<BoxShadow>();
        Opacity = Math.Clamp(opacity, 0f, 1f);
    }

    public SKSize Size { get; }
    public SKColor BackColor { get; }
    public SKColor ForeColor { get; }
    public Thickness Border { get; }
    public SKColor BorderColor { get; }
    public Radius Radius { get; }
    public BoxShadow[] Shadows { get; }
    public float Opacity { get; }

    public ElementVisualStyleSnapshot WithSize(SKSize size) => new(size, BackColor, ForeColor, Border, BorderColor, Radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithBackColor(SKColor color) => new(Size, color, ForeColor, Border, BorderColor, Radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithForeColor(SKColor color) => new(Size, BackColor, color, Border, BorderColor, Radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithBorder(Thickness border) => new(Size, BackColor, ForeColor, border, BorderColor, Radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithBorderColor(SKColor color) => new(Size, BackColor, ForeColor, Border, color, Radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithRadius(Radius radius) => new(Size, BackColor, ForeColor, Border, BorderColor, radius, Shadows, Opacity);
    public ElementVisualStyleSnapshot WithShadows(BoxShadow[] shadows) => new(Size, BackColor, ForeColor, Border, BorderColor, Radius, shadows, Opacity);
    public ElementVisualStyleSnapshot WithOpacity(float opacity) => new(Size, BackColor, ForeColor, Border, BorderColor, Radius, Shadows, opacity);

    public bool Equals(ElementVisualStyleSnapshot other)
    {
        return Size == other.Size &&
               BackColor == other.BackColor &&
               ForeColor == other.ForeColor &&
             Border == other.Border &&
             BorderColor == other.BorderColor &&
               Radius == other.Radius &&
             Math.Abs(Opacity - other.Opacity) < 0.0001f &&
               ElementVisualStyleInterpolator.AreShadowsEqual(Shadows, other.Shadows);
    }

    public override bool Equals(object? obj)
    {
        return obj is ElementVisualStyleSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Size);
        hash.Add(BackColor);
        hash.Add(ForeColor);
        hash.Add(Border);
        hash.Add(BorderColor);
        hash.Add(Radius);
        hash.Add(Opacity);
        for (var i = 0; i < Shadows.Length; i++)
            hash.Add(Shadows[i]);
        return hash.ToHashCode();
    }
}

internal static class ElementVisualStyleInterpolator
{
    public static ElementVisualStyleSnapshot Interpolate(
        in ElementVisualStyleSnapshot from,
        in ElementVisualStyleSnapshot to,
        float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);

        return new ElementVisualStyleSnapshot(
            new SKSize(
                Lerp(from.Size.Width, to.Size.Width, progress),
                Lerp(from.Size.Height, to.Size.Height, progress)),
            from.BackColor.InterpolateColor(to.BackColor, progress),
            from.ForeColor.InterpolateColor(to.ForeColor, progress),
            InterpolateThickness(from.Border, to.Border, progress),
            from.BorderColor.InterpolateColor(to.BorderColor, progress),
            InterpolateRadius(from.Radius, to.Radius, progress),
            InterpolateShadows(from.Shadows, to.Shadows, progress),
            Lerp(from.Opacity, to.Opacity, progress));
    }

    public static BoxShadow[] CloneShadows(BoxShadow[]? shadows)
    {
        if (shadows == null || shadows.Length == 0)
            return Array.Empty<BoxShadow>();

        var clone = new BoxShadow[shadows.Length];
        Array.Copy(shadows, clone, shadows.Length);
        return clone;
    }

    public static bool AreShadowsEqual(BoxShadow[]? left, BoxShadow[]? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return left == null && right == null;

        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            var a = left[i];
            var b = right[i];
            if (a.OffsetX != b.OffsetX ||
                a.OffsetY != b.OffsetY ||
                a.Blur != b.Blur ||
                a.Spread != b.Spread ||
                a.Color != b.Color ||
                a.Inset != b.Inset)
            {
                return false;
            }
        }

        return true;
    }

    private static BoxShadow[] InterpolateShadows(BoxShadow[] from, BoxShadow[] to, float progress)
    {
        if (AreShadowsEqual(from, to))
            return CloneShadows(progress >= 1f ? to : from);

        var count = Math.Max(from.Length, to.Length);
        if (count == 0)
            return Array.Empty<BoxShadow>();

        var result = new BoxShadow[count];
        for (var i = 0; i < count; i++)
        {
            var start = i < from.Length ? from[i] : CreateTransparentShadow(to[i]);
            var end = i < to.Length ? to[i] : CreateTransparentShadow(from[i]);
            result[i] = new BoxShadow(
                Lerp(start.OffsetX, end.OffsetX, progress),
                Lerp(start.OffsetY, end.OffsetY, progress),
                Lerp(start.Blur, end.Blur, progress),
                InterpolateRadius(start.Spread, end.Spread, progress),
                start.Color.InterpolateColor(end.Color, progress),
                end.Inset);
        }

        return result;
    }

    private static BoxShadow CreateTransparentShadow(in BoxShadow source)
    {
        return new BoxShadow(source.OffsetX, source.OffsetY, source.Blur, source.Spread, source.Color.WithAlpha(0), source.Inset);
    }

    private static Radius InterpolateRadius(Radius from, Radius to, float progress)
    {
        return new Radius(
            Lerp(from.TopLeft, to.TopLeft, progress),
            Lerp(from.TopRight, to.TopRight, progress),
            Lerp(from.BottomLeft, to.BottomLeft, progress),
            Lerp(from.BottomRight, to.BottomRight, progress));
    }

    private static Thickness InterpolateThickness(Thickness from, Thickness to, float progress)
    {
        return new Thickness(
            Lerp(from.Left, to.Left, progress),
            Lerp(from.Top, to.Top, progress),
            Lerp(from.Right, to.Right, progress),
            Lerp(from.Bottom, to.Bottom, progress));
    }

    private static int Lerp(int start, int end, float progress)
    {
        return (int)MathF.Round(start + (end - start) * progress);
    }

    private static float Lerp(float start, float end, float progress)
    {
        return start + (end - start) * progress;
    }
}

public sealed class ElementVisualStyleValueBuilder
{
    private readonly ElementVisualStyle _style;

    internal ElementVisualStyleValueBuilder(ElementVisualStyle style)
    {
        _style = style ?? throw new ArgumentNullException(nameof(style));
    }

    public ElementVisualStyleValueBuilder Width(float width)
    {
        _style.Width = Math.Max(0f, width);
        return this;
    }

    public ElementVisualStyleValueBuilder Height(float height)
    {
        _style.Height = Math.Max(0f, height);
        return this;
    }

    public ElementVisualStyleValueBuilder Size(float width, float height)
    {
        _style.Width = Math.Max(0f, width);
        _style.Height = Math.Max(0f, height);
        return this;
    }

    public ElementVisualStyleValueBuilder Background(SKColor color)
    {
        _style.BackColor = color;
        return this;
    }

    public ElementVisualStyleValueBuilder Foreground(SKColor color)
    {
        _style.ForeColor = color;
        return this;
    }

    public ElementVisualStyleValueBuilder Border(Thickness border)
    {
        _style.Border = border;
        return this;
    }

    public ElementVisualStyleValueBuilder Border(int all)
    {
        _style.Border = new Thickness(Math.Max(0, all));
        return this;
    }

    public ElementVisualStyleValueBuilder BorderColor(SKColor color)
    {
        _style.BorderColor = color;
        return this;
    }

    public ElementVisualStyleValueBuilder Radius(Radius radius)
    {
        _style.Radius = radius;
        return this;
    }

    public ElementVisualStyleValueBuilder Radius(float all)
    {
        _style.Radius = new Radius(all, all, all, all);
        return this;
    }

    public ElementVisualStyleValueBuilder Shadow(BoxShadow shadow)
    {
        _style.Shadows = shadow.IsEmpty ? Array.Empty<BoxShadow>() : [shadow];
        return this;
    }

    public ElementVisualStyleValueBuilder Shadows(params BoxShadow[] shadows)
    {
        _style.Shadows = ElementVisualStyleInterpolator.CloneShadows(shadows);
        return this;
    }

    public ElementVisualStyleValueBuilder Opacity(float opacity)
    {
        _style.Opacity = Math.Clamp(opacity, 0f, 1f);
        return this;
    }
}

public sealed class ElementVisualStyleRuleBuilder
{
    private readonly ElementVisualStyleRule _rule;
    private readonly ElementVisualStyleValueBuilder _values;

    internal ElementVisualStyleRuleBuilder(ElementVisualStyleRule rule)
    {
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        _values = new ElementVisualStyleValueBuilder(rule.Style);
    }

    public ElementVisualStyleRuleBuilder Named(string name)
    {
        _rule.Name = name ?? string.Empty;
        return this;
    }

    public ElementVisualStyleRuleBuilder Requires(ElementVisualStates states)
    {
        _rule.RequiredStates |= states;
        return this;
    }

    public ElementVisualStyleRuleBuilder Excludes(ElementVisualStates states)
    {
        _rule.ExcludedStates |= states;
        return this;
    }

    public ElementVisualStyleRuleBuilder Transition(TimeSpan duration, AnimationType animationType = AnimationType.EaseInOut)
    {
        _rule.Transition = new ElementVisualTransition
        {
            Duration = duration,
            AnimationType = animationType,
            Enabled = duration > TimeSpan.Zero
        };
        return this;
    }

    public ElementVisualStyleRuleBuilder Style(Action<ElementVisualStyleValueBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_values);
        return this;
    }

    public ElementVisualStyleRuleBuilder Width(float width) => Style(values => values.Width(width));
    public ElementVisualStyleRuleBuilder Height(float height) => Style(values => values.Height(height));
    public ElementVisualStyleRuleBuilder Size(float width, float height) => Style(values => values.Size(width, height));
    public ElementVisualStyleRuleBuilder Background(SKColor color) => Style(values => values.Background(color));
    public ElementVisualStyleRuleBuilder Foreground(SKColor color) => Style(values => values.Foreground(color));
    public ElementVisualStyleRuleBuilder Border(Thickness border) => Style(values => values.Border(border));
    public ElementVisualStyleRuleBuilder Border(int all) => Style(values => values.Border(all));
    public ElementVisualStyleRuleBuilder BorderColor(SKColor color) => Style(values => values.BorderColor(color));
    public ElementVisualStyleRuleBuilder Radius(Radius radius) => Style(values => values.Radius(radius));
    public ElementVisualStyleRuleBuilder Radius(float all) => Style(values => values.Radius(all));
    public ElementVisualStyleRuleBuilder Shadow(BoxShadow shadow) => Style(values => values.Shadow(shadow));
    public ElementVisualStyleRuleBuilder Shadows(params BoxShadow[] shadows) => Style(values => values.Shadows(shadows));
    public ElementVisualStyleRuleBuilder Opacity(float opacity) => Style(values => values.Opacity(opacity));
}

public sealed class ElementVisualStyleBuilder
{
    private readonly ElementBase _owner;

    internal ElementVisualStyleBuilder(ElementBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public ElementVisualStyleBuilder ClearRules()
    {
        _owner.VisualStyles.Clear();
        return this;
    }

    public ElementVisualStyleBuilder DefaultTransition(TimeSpan duration, AnimationType animationType = AnimationType.EaseInOut)
    {
        _owner.VisualTransition.Duration = duration;
        _owner.VisualTransition.AnimationType = animationType;
        _owner.VisualTransition.Enabled = duration > TimeSpan.Zero;
        return this;
    }

    public ElementVisualStyleBuilder Base(Action<ElementVisualStyleValueBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var style = new ElementVisualStyle();
        configure(new ElementVisualStyleValueBuilder(style));
        _owner.ApplyVisualStyleBase(style);
        return this;
    }

    public ElementVisualStyleBuilder When(ElementVisualStates requiredStates, Action<ElementVisualStyleRuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var rule = new ElementVisualStyleRule(new ElementVisualStyle())
        {
            RequiredStates = requiredStates
        };
        configure(new ElementVisualStyleRuleBuilder(rule));
        _owner.VisualStyles.Add(rule);
        return this;
    }

    public ElementVisualStyleBuilder OnHover(Action<ElementVisualStyleRuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var rule = new ElementVisualStyleRule(new ElementVisualStyle())
        {
            RequiredStates = ElementVisualStates.PointerOver,
            ExcludedStates = ElementVisualStates.Pressed
        };
        configure(new ElementVisualStyleRuleBuilder(rule));
        _owner.VisualStyles.Add(rule);
        return this;
    }

    public ElementVisualStyleBuilder OnPressed(Action<ElementVisualStyleRuleBuilder> configure)
    {
        return When(ElementVisualStates.Pressed, configure);
    }

    public ElementVisualStyleBuilder OnFocused(Action<ElementVisualStyleRuleBuilder> configure)
    {
        return When(ElementVisualStates.Focused, configure);
    }

    public ElementVisualStyleBuilder OnDisabled(Action<ElementVisualStyleRuleBuilder> configure)
    {
        return When(ElementVisualStates.Disabled, configure);
    }

    public ElementVisualStyleBuilder OnHidden(Action<ElementVisualStyleRuleBuilder> configure)
    {
        return When(ElementVisualStates.Hidden, configure);
    }

    public ElementVisualStyleBuilder When(
        Func<ElementBase, ElementVisualStateContext, bool> predicate,
        Action<ElementVisualStyleRuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configure);
        var rule = ElementVisualStyleRule.When(predicate, _ => { });
        configure(new ElementVisualStyleRuleBuilder(rule));
        _owner.VisualStyles.Add(rule);
        return this;
    }
}