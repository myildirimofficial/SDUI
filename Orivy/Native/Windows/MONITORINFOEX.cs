using System.Runtime.InteropServices;
using static Orivy.Native.Windows.Methods;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct MONITORINFOEX
{
    public uint cbSize;
    public Rect rcMonitor;
    public Rect rcWork;
    public uint dwFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;
}
