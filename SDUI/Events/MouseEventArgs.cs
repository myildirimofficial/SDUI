using System;
using SkiaSharp;

namespace SDUI;

public class MouseEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the MouseEventArgs class with the specified mouse button, number of clicks, cursor
    /// coordinates, and scroll wheel delta.
    /// </summary>
    /// <param name="button">The mouse button that was pressed or released.</param>
    /// <param name="clicks">The number of times the mouse button was pressed and released.</param>
    /// <param name="x">The x-coordinate of the mouse, in pixels, relative to the control.</param>
    /// <param name="y">The y-coordinate of the mouse, in pixels, relative to the control.</param>
    /// <param name="delta">The number of detents the mouse wheel has rotated, where a positive value indicates forward rotation and a
    /// negative value indicates backward rotation.</param>
    /// <param name="isHorizontalWheel">Indicates whether the delta came from a horizontal wheel gesture.</param>
    public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta, bool isHorizontalWheel = false)
    {
        Button = button;
        Clicks = clicks;
        X = x;
        Y = y;
        Delta = delta;
        IsHorizontalWheel = isHorizontalWheel;
    }
    
    /// <summary>
    /// Gets the mouse button associated with the event.
    /// </summary>
    public MouseButtons Button { get; }

    /// <summary>
    /// Gets the total number of times the associated item has been clicked.
    /// </summary>
    public int Clicks { get; }

    /// <summary>
    /// Gets the value of X.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y-coordinate value.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the difference between two related values.
    /// </summary>
    public int Delta { get; }

    /// <summary>
    /// Gets a value indicating whether the wheel delta represents a horizontal scrolling gesture.
    /// </summary>
    public bool IsHorizontalWheel { get; }

    /// <summary>
    /// Gets the location represented by the current X and Y coordinates as an SKPoint structure.
    /// </summary>
    public SKPoint Location => new SKPoint(X, Y);

    /// <summary>
    /// Gets or sets a value indicating whether the event has been handled and should not be processed further.
    /// </summary>
    public bool Handled { get; set; }
}
