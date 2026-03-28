using System;

namespace Orivy;

[Flags]
public enum BoundsSpecified
{
    /// <summary>
    /// No bounds are specified. The layout engine will not update any geometry.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies that the X-coordinate (left edge) of the control is defined.
    /// </summary>
    X = 1,

    /// <summary>
    /// Specifies that the Y-coordinate (top edge) of the control is defined.
    /// </summary>
    Y = 2,

    /// <summary>
    /// Specifies that both X and Y coordinates (position) are defined.
    /// Equivalent to (X | Y).
    /// </summary>
    Location = 3,

    /// <summary>
    /// Specifies that the width of the control is defined.
    /// </summary>
    Width = 4,

    /// <summary>
    /// Specifies that the height of the control is defined.
    /// </summary>
    Height = 8,

    /// <summary>
    /// Specifies that both Width and Height (dimensions) are defined.
    /// Equivalent to (Width | Height).
    /// </summary>
    Size = 12,

    /// <summary>
    /// Specifies that all bounds (X, Y, Width, and Height) are defined.
    /// Equivalent to (Location | Size).
    /// </summary>
    All = 15
}