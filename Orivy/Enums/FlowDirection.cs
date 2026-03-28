namespace Orivy;

/// <summary>
/// Specifies the direction in which content is laid out or displayed.
/// </summary>
/// <remarks>Use this enumeration to indicate the flow direction for user interface elements or text, particularly
/// when supporting languages with different reading directions such as left-to-right (e.g., English) or right-to-left
/// (e.g., Arabic, Hebrew).</remarks>
public enum FlowDirection
{
    /// <summary>
    /// Specifies that content is laid out from left to right.
    /// </summary>
    LeftToRight,

    /// <summary>
    /// Specifies whether content is displayed from right to left, such as for languages that use right-to-left scripts.
    /// </summary>
    /// <remarks>Use this value to indicate layout or text direction for languages such as Arabic or Hebrew.
    /// When set, content flows from the right edge to the left edge.</remarks>
    RightToLeft
}
