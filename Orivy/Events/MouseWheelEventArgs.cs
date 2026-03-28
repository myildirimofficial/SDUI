using System;

namespace Orivy;

public class MouseWheelEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the MouseWheelEventArgs class with the specified wheel delta and mouse
    /// coordinates.
    /// </summary>
    /// <param name="delta">The amount the mouse wheel has rotated, measured in detents. A positive value indicates upward rotation; a
    /// negative value indicates downward rotation.</param>
    /// <param name="x">The x-coordinate of the mouse pointer, in pixels, at the time the event occurred.</param>
    /// <param name="y">The y-coordinate of the mouse pointer, in pixels, at the time the event occurred.</param>
    public MouseWheelEventArgs(int delta, int x, int y)
    {
        Delta = delta;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the difference between two related values.
    /// </summary>
    public int Delta { get; }

    /// <summary>
    /// Gets the value of X.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y-coordinate value.
    /// </summary>
    public int Y { get; }
}