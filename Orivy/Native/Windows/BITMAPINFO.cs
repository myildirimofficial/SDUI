using System.Runtime.InteropServices;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFO
{
    public BITMAPINFOHEADER bmiHeader;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public uint[] bmiColors;
}