using System;
using System.Runtime.InteropServices;

namespace Orivy.Native.Windows;

[StructLayout(LayoutKind.Sequential)]
public struct WNDCLASSEX
{
    public uint cbSize;
    public uint style;
    [MarshalAs(UnmanagedType.FunctionPtr)] 
    public WndProc lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    public string lpszMenuName;
    public string lpszClassName;
    public IntPtr hIconSm;
}