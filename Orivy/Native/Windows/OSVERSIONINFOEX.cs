using System.Runtime.InteropServices;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct OSVERSIONINFOEX
{
    // The OSVersionInfoSize field must be set to Marshal.SizeOf(typeof(OSVERSIONINFOEX))
    public int OSVersionInfoSize;
    public int MajorVersion;
    public int MinorVersion;
    public int BuildNumber;
    public int PlatformId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string CSDVersion;

    public ushort ServicePackMajor;
    public ushort ServicePackMinor;
    public ushort SuiteMask;
    public byte ProductType;
    public byte Reserved;
}
