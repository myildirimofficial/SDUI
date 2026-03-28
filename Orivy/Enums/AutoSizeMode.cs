namespace Orivy;

/// <summary>
/// Specifies how a control automatically adjusts its size to fit its content.
/// </summary>
/// <remarks>Use this enumeration to control whether a control resizes automatically when its contents change. The
/// selected mode determines if the control can grow, shrink, or remain fixed in size in response to content
/// changes.</remarks>
public enum AutoSizeMode
{
    /// <summary>
    /// Indicates that no options are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the operation should only reduce the size or capacity, and not increase it.
    /// </summary>
    ShrinkOnly,

    /// <summary>
    /// Specifies a mode in which an element can both grow and shrink to fit its content or available space.
    /// </summary>
    GrowAndShrink,

    /// <summary>
    /// /// Specifies a mode in which an element can only grow to fit its content but cannot shrink smaller than its
    /// </summary>
    GrowOnly
}
