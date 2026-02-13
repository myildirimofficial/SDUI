using SkiaSharp;

namespace SDUI;

/// <summary>
/// CSS-like box shadow definition. Supports offset, blur, per-edge spread, color, and inset mode.
/// Usage: <c>new BoxShadow(offsetX: 0, offsetY: 4, blur: 12, spread: new Thickness(8), color: SKColors.Black.WithAlpha(60))</c>
/// </summary>
public readonly struct BoxShadow
{
    /// <summary>
    /// Horizontal offset of the shadow. Positive = right, negative = left.
    /// </summary>
    public float OffsetX { get; }

    /// <summary>
    /// Vertical offset of the shadow. Positive = down, negative = up.
    /// </summary>
    public float OffsetY { get; }

    /// <summary>
    /// Gaussian blur radius. 0 = sharp edge, higher = softer.
    /// </summary>
    public float Blur { get; }

    /// <summary>
    /// Per-edge spread distance. Expands the shadow shape outward (or inward for negative values).
    /// Use uniform spread: <c>new Thickness(8)</c> or per-edge: <c>new Thickness(left, top, right, bottom)</c>.
    /// </summary>
    public Thickness Spread { get; }

    /// <summary>
    /// Shadow color with alpha for opacity control.
    /// </summary>
    public SKColor Color { get; }

    /// <summary>
    /// When true, the shadow is drawn inside the element (inset shadow).
    /// </summary>
    public bool Inset { get; }

    /// <summary>
    /// Returns true if this shadow has no visible effect.
    /// </summary>
    public bool IsEmpty => Color.Alpha == 0 || (Blur == 0 && Spread.IsEmpty && OffsetX == 0 && OffsetY == 0);

    public BoxShadow(float offsetX, float offsetY, float blur, Thickness spread, SKColor color, bool inset = false)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        Blur = blur >= 0 ? blur : 0;
        Spread = spread;
        Color = color;
        Inset = inset;
    }

    public BoxShadow(float offsetX, float offsetY, float blur, int spread, SKColor color, bool inset = false)
        : this(offsetX, offsetY, blur, new Thickness(spread), color, inset) { }

    public BoxShadow(float blur, SKColor color)
        : this(0, 0, blur, new Thickness(0), color) { }

    public BoxShadow(float blur, int spread, SKColor color)
        : this(0, 0, blur, new Thickness(spread), color) { }

    public static readonly BoxShadow None = default;
}
