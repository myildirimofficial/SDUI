using System.Runtime.InteropServices;
using static Orivy.Native.Windows.Methods;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential)]
public struct MONITORINFO
{
    public uint cbSize;
    public Rect rcMonitor;
    public Rect rcWork;
    public uint dwFlags;
}
