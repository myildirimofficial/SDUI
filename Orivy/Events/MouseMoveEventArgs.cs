using System;
using SkiaSharp;

namespace Orivy;

public class MouseMoveEventArgs : EventArgs
{
    public MouseMoveEventArgs(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public SKPoint Location => new SKPoint(X, Y);
}
