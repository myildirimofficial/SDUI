using System;

namespace Orivy.Native.Windows;

public class CreateParams
{
    public int X { get; set; } = 0; // CW_USEDEFAULT
    public int Y { get; set; } = 0; // CW_USEDEFAULT
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public string Caption { get; set; } = "";
    public string ClassName { get; set; }
    public int Style { get; set; }
    public uint ExStyle { get; set; }
    public int ClassStyle { get; set; }
    public IntPtr Parent { get; set; } = IntPtr.Zero;
}
