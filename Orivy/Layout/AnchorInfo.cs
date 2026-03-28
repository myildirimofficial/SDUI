using SkiaSharp;

namespace Orivy.Layout;

/// <summary>
///     Stores anchor distances from parent edges for a control.
///     Used to calculate control position/size when parent is resized.
/// </summary>
internal sealed class AnchorInfo
{
    public SkiaSharp.SKRect DisplayRectangle { get; set; }

    /// <summary>Distance from left edge of parent's DisplayRectangle</summary>
    public float Left { get; set; }

    /// <summary>Distance from top edge of parent's DisplayRectangle</summary>
    public float Top { get; set; }

    /// <summary>Distance from right edge of parent's DisplayRectangle (negative when control is anchored right)</summary>
    public float Right { get; set; }

    /// <summary>Distance from bottom edge of parent's DisplayRectangle (negative when control is anchored bottom)</summary>
    public float Bottom { get; set; }
}
