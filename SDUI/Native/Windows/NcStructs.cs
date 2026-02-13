using System;
using System.Runtime.InteropServices;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Native.Windows;

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
