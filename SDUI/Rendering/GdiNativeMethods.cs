
using System;
using System.Runtime.InteropServices;
using SDUI.Native.Windows;

/// <summary>
/// Native P/Invoke methods for GDI operations.
/// </summary>
internal static class GdiNativeMethods
{
    private const string gdi32 = "gdi32.dll";

    [DllImport(gdi32, SetLastError = true)]
    internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport(gdi32, SetLastError = true)]
    internal static extern bool DeleteDC(IntPtr hdc);

    [DllImport(gdi32, SetLastError = true)]
    internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

    [DllImport(gdi32, SetLastError = true)]
    internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport(gdi32, SetLastError = true)]
    internal static extern bool DeleteObject(IntPtr hObject);

    [DllImport(gdi32, SetLastError = true)]
    internal static extern bool BitBlt(IntPtr hdcDest, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    internal const uint SRCCOPY = 0x00CC0020;
}
