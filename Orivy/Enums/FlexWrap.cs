namespace Orivy;

/// <summary>
///     Item wrapping behavior
/// </summary>
public enum FlexWrap
{
    /// <summary>
    /// Specifies that text should not automatically wrap to the next line.
    /// </summary>
    NoWrap,

    /// <summary>
    /// Gets or sets a value indicating whether text wrapping is enabled.
    /// </summary>
    /// <remarks>When enabled, text that exceeds the available width will automatically continue on the next
    /// line. Disabling wrapping may cause overflowing text to be clipped or truncated, depending on the control's
    /// layout behavior.</remarks>
    Wrap,

    /// <summary>
    /// Specifies that items should wrap in the reverse direction when there is insufficient space.
    /// </summary>
    WrapReverse
}
