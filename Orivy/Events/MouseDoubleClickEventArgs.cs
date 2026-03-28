using System;
using SkiaSharp;

namespace Orivy;

public class MouseDoubleClickEventArgs : EventArgs
{
    public MouseDoubleClickEventArgs(MouseButtons button, int clicks, int x, int y)
    {
        Button = button;
        Clicks = clicks;
        X = x;
        Y = y;
    }

    public MouseButtons Button { get; }

    public int Clicks { get; }

    public int X { get; }

    public int Y { get; }

    public SKPoint Location => new SKPoint(X, Y);
}
