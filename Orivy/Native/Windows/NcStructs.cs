using System;
using System.Runtime.InteropServices;
using static Orivy.Native.Windows.Methods;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential)]
public struct NCCALCSIZE_PARAMS
{
    public Rect rgrc0;
    public Rect rgrc1;
    public Rect rgrc2;
    public IntPtr lppos;
}

[StructLayout(LayoutKind.Sequential)]
public struct MINMAXINFO
{
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
}
