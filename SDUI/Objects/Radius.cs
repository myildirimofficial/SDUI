using SkiaSharp;
using System;

namespace SDUI;

/// <summary>
/// Represents padding values for UI elements with left, top, right, and bottom components.
/// </summary>
public struct Radius
{
    /// <summary>
    /// Gets or sets the horizontal distance, in pixels, between the left edge of the control and the left edge of its
    /// container.
    /// </summary>
    public float TopLeft { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return in a query result.
    /// </summary>
    /// <remarks>Set this property to limit the number of results retrieved. If not set, the default behavior
    /// may return all available items, depending on the implementation.</remarks>
    public float TopRight { get; set; }

    /// <summary>
    /// Gets or sets the distance, in pixels, between the right edge of the element and the left edge of its container.
    /// </summary>
    public float BottomLeft { get; set; }

    /// <summary>
    /// Gets or sets the y-coordinate of the lower edge of the rectangle.
    /// </summary>
    public float BottomRight { get; set; }

    /// <summary>
    /// Gets or sets the padding value for all edges.
    /// </summary>
    public float All
    {
        get => BottomLeft == TopLeft && 
               TopRight == BottomRight &&
               BottomLeft == BottomRight && 
               TopLeft == TopRight 
            ? BottomRight : -1;
        set
        {
            TopLeft = value;
            TopRight = value;
            BottomLeft = value;
            BottomRight = value;
        }
    }

    /// <summary>
    /// Gets an empty padding with all values set to zero.
    /// </summary>
    public static readonly Radius Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Initializes a new instance of the Padding record with all sides set to the same value.
    /// </summary>
    /// <param name="all">The value to set for left, top, right, and bottom.</param>
    public Radius(int all) : this(all, all, all, all) { }

    /// <summary>
    /// Gets a value indicating whether all padding values are zero.
    /// </summary>
    public bool IsEmpty => BottomLeft == 0 && BottomRight == 0 && TopLeft == 0 && TopRight == 0;

    /// <summary>
    /// Initializes a new instance of the Thickness structure with the specified left, top, right, and bottom values.
    /// </summary>
    /// <param name="left">The thickness, in pixels, for the left side.</param>
    /// <param name="top">The thickness, in pixels, for the top side.</param>
    /// <param name="right">The thickness, in pixels, for the right side.</param>
    /// <param name="bottom">The thickness, in pixels, for the bottom side.</param>
    public Radius(float topLeft, float topRight, float bottomLeft, float bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }

    /// <summary>
    /// Initializes a new instance of the Thickness structure with all sides set to zero.
    /// </summary>
    /// <remarks>This constructor creates a Thickness where the left, top, right, and bottom values are all
    /// zero, representing no thickness. It is equivalent to specifying Thickness(0, 0, 0, 0).</remarks>
    public Radius() : this(0, 0, 0, 0) { }

    /// <summary>
    /// Scales the padding by a given factor.
    /// </summary>
    /// <param name="factor">The scaling factor.</param>
    /// <returns>A new Padding scaled by the factor.</returns>
    public Radius Scale(float factor)
    {
        if (Math.Abs(factor - 1f) < 0.001f)
            return this;

        static float Scale(float value, float factor)
        {
            return MathF.Max(0, MathF.Round(value * factor));
        }

        return new Radius(
            Scale(TopLeft, factor),
            Scale(TopRight, factor),
            Scale(BottomLeft, factor),
            Scale(BottomRight, factor));
    }

    /// <summary>
    /// Negates the padding values component-wise.
    /// </summary>
    /// <param name="padding">The padding to negate.</param>
    /// <returns>A new Padding with negated components.</returns>
    public static Radius operator -(Radius padding)
    {
        return new Radius(-padding.TopLeft, -padding.TopRight, -padding.BottomLeft, -padding.BottomRight);
    }

    /// <summary>
    /// Adds two Padding values component-wise.
    /// </summary>
    /// <param name="left">The first Padding.</param>
    /// <param name="right">The second Padding.</param>
    /// <returns>A new Padding with summed components.</returns>
    public static Radius operator +(Radius left, Radius right)
    {
        return new Radius(left.TopLeft + right.TopLeft, left.TopRight + right.TopRight, left.BottomLeft + right.BottomLeft, left.BottomRight + right.BottomRight);
    }

    /// <summary>
    /// Subtracts one Padding from another component-wise.
    /// </summary>
    /// <param name="left">The Padding to subtract from.</param>
    /// <param name="right">The Padding to subtract.</param>
    /// <returns>A new Padding with subtracted components.</returns>
    public static Radius operator -(Radius left, Radius right)
    {
        return new Radius(left.TopLeft - right.TopLeft, left.TopRight - right.TopRight, left.BottomLeft - right.BottomLeft, left.BottomRight - right.BottomRight);
    }

    /// <summary>
    /// Multiplies a Padding by an integer factor component-wise.
    /// </summary>
    /// <param name="padding">The Padding to multiply.</param>
    /// <param name="factor">The integer factor.</param>
    /// <returns>A new Padding with multiplied components.</returns>
    public static Radius operator *(Radius padding, int factor)
    {
        return new Radius(padding.TopLeft * factor, padding.TopRight * factor, padding.BottomLeft * factor, padding.BottomRight * factor);
    }

    /// <summary>
    /// Multiplies an integer factor by a Padding component-wise.
    /// </summary>
    /// <param name="factor">The integer factor.</param>
    /// <param name="padding">The Padding to multiply.</param>
    /// <returns>A new Padding with multiplied components.</returns>
    public static Radius operator *(int factor, Radius padding)
    {
        return padding * factor;
    }

    /// <summary>
    /// Divides a Padding by an integer divisor component-wise.
    /// </summary>
    /// <param name="padding">The Padding to divide.</param>
    /// <param name="divisor">The integer divisor.</param>
    /// <returns>A new Padding with divided components.</returns>
    public static Radius operator /(Radius padding, int divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Radius(padding.TopLeft / divisor, padding.TopRight / divisor, padding.BottomLeft / divisor, padding.BottomRight / divisor);
    }

    /// <summary>
    /// Determines whether two Radius instances are equal by comparing their corresponding corner values.
    /// </summary>
    /// <remarks>This operator compares the TopLeft, TopRight, BottomLeft, and BottomRight properties of each
    /// Radius instance to determine equality.</remarks>
    /// <param name="left">The first Radius instance to compare.</param>
    /// <param name="right">The second Radius instance to compare.</param>
    /// <returns>true if the specified Radius instances are equal; otherwise, false.</returns>
    public static bool operator ==(Radius left, Radius right)
    {
        return left.TopLeft == right.TopLeft &&
               left.TopRight == right.TopRight &&
               left.BottomLeft == right.BottomLeft &&
               left.BottomRight == right.BottomRight;
    }

    /// <summary>
    /// Determines whether two Radius instances are not equal.
    /// </summary>
    /// <param name="left">The first Radius instance to compare.</param>
    /// <param name="right">The second Radius instance to compare.</param>
    /// <returns>true if the specified Radius instances are not equal; otherwise, false.</returns>
    public static bool operator !=(Radius left, Radius right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Creates a new SKRoundRect with corner radii corresponding to the current instance and the specified rectangle
    /// bounds.
    /// </summary>
    /// <remarks>The corner radii for each corner are taken from the TopLeft, TopRight, BottomRight, and
    /// BottomLeft properties of the current instance. Each corner will have a radius equal to the corresponding
    /// property value.</remarks>
    /// <param name="rect">The rectangle that defines the bounds of the rounded rectangle.</param>
    /// <returns>An SKRoundRect whose bounds are set to the specified rectangle and whose corner radii are determined by the
    /// current instance.</returns>
    public readonly SKRoundRect ToRoundRect(SKRect rect)
    {
        var rr = new SKRoundRect();
        rr.SetRectRadii(rect,
        [
            new SKPoint(TopLeft, TopLeft),
            new SKPoint(TopRight, TopRight),
            new SKPoint(BottomRight, BottomRight),
            new SKPoint(BottomLeft, BottomLeft)
        ]);

        return rr;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Radius instance.
    /// </summary>
    /// <remarks>Use this method to compare two Radius instances for value equality. This method overrides
    /// Object.Equals and supports comparison with objects of type Radius only.</remarks>
    /// <param name="obj">The object to compare with the current Radius instance.</param>
    /// <returns>true if the specified object is a Radius and is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Radius other && this == other;
    }

    /// <summary>
    /// Serves as the default hash function for the Radius object.
    /// </summary>
    /// <remarks>The hash code is computed based on the values of the TopLeft, TopRight, BottomLeft, and
    /// BottomRight properties. This method is suitable for use in hashing algorithms and data structures such as a hash
    /// table.</remarks>
    /// <returns>A 32-bit signed integer hash code that represents the current object.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TopLeft, TopRight, BottomLeft, BottomRight);
    }

    /// <summary>
    /// Returns a string that represents the current radius values for each corner.
    /// </summary>
    /// <remarks>This method is useful for debugging or logging purposes to quickly view the state of all
    /// corner radii.</remarks>
    /// <returns>A string containing the values of the TopLeft, TopRight, BottomLeft, and BottomRight properties in a formatted
    /// representation.</returns>
    public override string ToString()
    {
        return $"Radius(TopLeft: {TopLeft}, TopRight: {TopRight}, BottomLeft: {BottomLeft}, BottomRight: {BottomRight})";
    }
}