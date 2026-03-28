using System;

namespace Orivy;

/// <summary>
/// Represents padding values for UI elements with left, top, right, and bottom components.
/// </summary>
public struct Thickness
{
    /// <summary>
    /// Gets or sets the horizontal distance, in pixels, between the left edge of the control and the left edge of its
    /// container.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return in a query result.
    /// </summary>
    /// <remarks>Set this property to limit the number of results retrieved. If not set, the default behavior
    /// may return all available items, depending on the implementation.</remarks>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the distance, in pixels, between the right edge of the element and the left edge of its container.
    /// </summary>
    public int Right { get; set; }

    /// <summary>
    /// Gets or sets the y-coordinate of the lower edge of the rectangle.
    /// </summary>
    public int Bottom { get; set; }

    /// <summary>
    /// Gets the total horizontal padding (left + right).
    /// </summary>
    public int Horizontal => Left + Right;

    /// <summary>
    /// Gets the total vertical padding (top + bottom).
    /// </summary>
    public int Vertical => Top + Bottom;

    /// <summary>
    /// Gets or sets the padding value for all edges.
    /// </summary>
    public int All
    {
        get => Left == Top && Top == Right && Right == Bottom ? Left : -1;
        set
        {
            Left = value;
            Top = value;
            Right = value;
            Bottom = value;
        }
    }

    /// <summary>
    /// Gets an empty padding with all values set to zero.
    /// </summary>
    public static readonly Thickness Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Initializes a new instance of the Padding record with all sides set to the same value.
    /// </summary>
    /// <param name="all">The value to set for left, top, right, and bottom.</param>
    public Thickness(int all) : this(all, all, all, all) { }

    /// <summary>
    /// Gets a value indicating whether all padding values are zero.
    /// </summary>
    public bool IsEmpty => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>
    /// Initializes a new instance of the Thickness structure with the specified left, top, right, and bottom values.
    /// </summary>
    /// <param name="left">The thickness, in pixels, for the left side.</param>
    /// <param name="top">The thickness, in pixels, for the top side.</param>
    /// <param name="right">The thickness, in pixels, for the right side.</param>
    /// <param name="bottom">The thickness, in pixels, for the bottom side.</param>
    public Thickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Initializes a new instance of the Thickness structure with all sides set to zero.
    /// </summary>
    /// <remarks>This constructor creates a Thickness where the left, top, right, and bottom values are all
    /// zero, representing no thickness. It is equivalent to specifying Thickness(0, 0, 0, 0).</remarks>
    public Thickness() : this(0, 0, 0, 0) { }

    /// <summary>
    /// Scales the padding by a given factor.
    /// </summary>
    /// <param name="factor">The scaling factor.</param>
    /// <returns>A new Padding scaled by the factor.</returns>
    public Thickness Scale(float factor)
    {
        if (Math.Abs(factor - 1f) < 0.001f)
            return this;

        static int Scale(int value, float factor)
        {
            return Math.Max(0, (int)Math.Round(value * factor));
        }

        return new Thickness(
            Scale(Left, factor),
            Scale(Top, factor),
            Scale(Right, factor),
            Scale(Bottom, factor));
    }

    /// <summary>
    /// Negates the padding values component-wise.
    /// </summary>
    /// <param name="padding">The padding to negate.</param>
    /// <returns>A new Padding with negated components.</returns>
    public static Thickness operator -(Thickness padding)
    {
        return new Thickness(-padding.Left, -padding.Top, -padding.Right, -padding.Bottom);
    }

    /// <summary>
    /// Adds two Padding values component-wise.
    /// </summary>
    /// <param name="left">The first Padding.</param>
    /// <param name="right">The second Padding.</param>
    /// <returns>A new Padding with summed components.</returns>
    public static Thickness operator +(Thickness left, Thickness right)
    {
        return new Thickness(left.Left + right.Left, left.Top + right.Top, left.Right + right.Right, left.Bottom + right.Bottom);
    }

    /// <summary>
    /// Subtracts one Padding from another component-wise.
    /// </summary>
    /// <param name="left">The Padding to subtract from.</param>
    /// <param name="right">The Padding to subtract.</param>
    /// <returns>A new Padding with subtracted components.</returns>
    public static Thickness operator -(Thickness left, Thickness right)
    {
        return new Thickness(left.Left - right.Left, left.Top - right.Top, left.Right - right.Right, left.Bottom - right.Bottom);
    }

    /// <summary>
    /// Multiplies a Padding by an integer factor component-wise.
    /// </summary>
    /// <param name="padding">The Padding to multiply.</param>
    /// <param name="factor">The integer factor.</param>
    /// <returns>A new Padding with multiplied components.</returns>
    public static Thickness operator *(Thickness padding, int factor)
    {
        return new Thickness(padding.Left * factor, padding.Top * factor, padding.Right * factor, padding.Bottom * factor);
    }

    /// <summary>
    /// Multiplies an integer factor by a Padding component-wise.
    /// </summary>
    /// <param name="factor">The integer factor.</param>
    /// <param name="padding">The Padding to multiply.</param>
    /// <returns>A new Padding with multiplied components.</returns>
    public static Thickness operator *(int factor, Thickness padding)
    {
        return padding * factor;
    }

    /// <summary>
    /// Divides a Padding by an integer divisor component-wise.
    /// </summary>
    /// <param name="padding">The Padding to divide.</param>
    /// <param name="divisor">The integer divisor.</param>
    /// <returns>A new Padding with divided components.</returns>
    public static Thickness operator /(Thickness padding, int divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Thickness(padding.Left / divisor, padding.Top / divisor, padding.Right / divisor, padding.Bottom / divisor);
    }

    public static bool operator ==(Thickness left, Thickness right)
    {
        return left.Left == right.Left &&
               left.Top == right.Top &&
               left.Right == right.Right &&
               left.Bottom == right.Bottom;
    }

    public static bool operator !=(Thickness left, Thickness right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is Thickness other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Top, Right, Bottom);
    }
}