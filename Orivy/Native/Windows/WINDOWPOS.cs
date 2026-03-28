using System;
using System.Runtime.InteropServices;

namespace Orivy.Native.Windows;


[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPOS
{
    public IntPtr HWND;
    public IntPtr hwndAfter;
    public int x;
    public int y;
    public int cx;
    public int cy;
    public SetWindowPosFlags flags;
}
