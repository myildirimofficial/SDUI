namespace Orivy;

/// <summary>
///     Main axis alignment
/// </summary>
public enum JustifyContent
{
    /// <summary>
    /// Specifies that items are aligned to the start of the flex container's main axis.
    /// </summary>
    FlexStart,

    /// <summary>
    /// Specifies that child elements are aligned to the end of the flex container's main axis.
    /// </summary>
    FlexEnd,

    /// <summary>
    /// Specifies the center alignment option.
    /// </summary>
    Center,

    /// <summary>
    /// Specifies that space should be distributed evenly between elements, with no space at the start or end.
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// Specifies that space should be distributed evenly around items in a layout or container.
    /// </summary>
    SpaceAround,

    /// <summary>
    /// Specifies that child elements are distributed evenly, with equal space between, before, and after each element.
    /// </summary>
    /// <remarks>Use this value to arrange items so that the spacing before the first item, between items, and
    /// after the last item is equal. This is commonly used in layout scenarios where uniform distribution of space is
    /// desired.</remarks>
    SpaceEvenly
}