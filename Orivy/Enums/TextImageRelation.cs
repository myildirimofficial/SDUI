namespace Orivy;

/// <summary>
/// Specifies the position of the text and image relative to each other on a control.
/// </summary>
public enum TextImageRelation
{
    /// <summary>
    /// Specifies that the image and text share the same space on a control.
    /// </summary>
    Overlay = 0,

    /// <summary>
    /// Specifies that the image is displayed horizontally before the text of a control.
    /// </summary>
    ImageBeforeText = 4,

    /// <summary>
    /// Specifies that the text is displayed horizontally before the image of a control.
    /// </summary>
    TextBeforeImage = 8,

    /// <summary>
    /// Specifies that the image is displayed vertically above the text of a control.
    /// </summary>
    ImageAboveText = 1,

    /// <summary>
    /// Specifies that the text is displayed vertically above the image of a control.
    /// </summary>
    TextAboveImage = 2
}