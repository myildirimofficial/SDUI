using System;
using SkiaSharp;

namespace Orivy;

public class MouseHoverEventArgs : EventArgs
{
    public MouseHoverEventArgs(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public SKPoint Location => new SKPoint(X, Y);
}
