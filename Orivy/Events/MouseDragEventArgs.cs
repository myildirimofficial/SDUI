using System;
using SkiaSharp;

namespace Orivy;

public class MouseDragEventArgs : EventArgs
{
    public MouseDragEventArgs(MouseButtons button, int x, int y, int deltaX, int deltaY)
    {
        Button = button;
        X = x;
        Y = y;
        DeltaX = deltaX;
        DeltaY = deltaY;
    }

    public MouseButtons Button { get; }

    public int X { get; }

    public int Y { get; }

    public int DeltaX { get; }

    public int DeltaY { get; }

    public SKPoint Location => new SKPoint(X, Y);
}
