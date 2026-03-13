using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using SDUI.Native.Windows;

namespace SDUI.Rendering;





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

/// <summary>
/// Software renderer using GDI and memory DIB sections for CPU-based rendering.
/// Provides a fallback when GPU rendering is unavailable or disabled.
/// </summary>
internal class SoftwareRenderer : IWindowRenderer
{
    private nint _hwnd;
    private IntPtr _cachedMemDC;
    private IntPtr _cachedBitmap;
    private IntPtr _cachedPixels;
    private int _cachedWidth;
    private int _cachedHeight;
    private bool _disposed;
    public bool IsSkiaGpuActive => false;
    public RenderBackend Backend => RenderBackend.Software;
    public GRContext? GrContext => null;

    public void Initialize(nint hwnd)
    {
        if (hwnd == IntPtr.Zero)
            throw new ArgumentException("Invalid window handle", nameof(hwnd));

        _hwnd = hwnd;
    }

    public void Resize(int width, int height)
    {
        // Software renderer doesn't pre-allocate; DIB is created on-demand during Render
        DisposeCachedDIB();
    }

    /// <summary>
    /// Renders to a software backbuffer using GDI memory DIB.
    /// Returns true if the frame was successfully presented, false otherwise.
    /// </summary>
    public bool Render(int width, int height, Action<SKCanvas, SKImageInfo> draw)
    {
        if (_disposed || _hwnd == IntPtr.Zero)
            return false;

        if (width <= 0 || height <= 0)
            return false;

        IntPtr hdc = GdiNativeMethods.GetDC(_hwnd);
        if (hdc == IntPtr.Zero)
            return false;

        try
        {
            // Re-create cached DIB only when size changes
            if (_cachedMemDC == IntPtr.Zero || width != _cachedWidth || height != _cachedHeight)
            {
                DisposeCachedDIB();

                _cachedMemDC = GdiNativeMethods.CreateCompatibleDC(hdc);
                if (_cachedMemDC == IntPtr.Zero)
                    return false;

                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = width,
                        biHeight = -height,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0
                    },
                    bmiColors = new uint[1]
                };

                _cachedBitmap = GdiNativeMethods.CreateDIBSection(hdc, ref bmi, 0, out _cachedPixels, IntPtr.Zero, 0);
                if (_cachedBitmap == IntPtr.Zero || _cachedPixels == IntPtr.Zero)
                {
                    DisposeCachedDIB();
                    return false;
                }

                GdiNativeMethods.SelectObject(_cachedMemDC, _cachedBitmap);
                _cachedWidth = width;
                _cachedHeight = height;
            }

            // Render via Skia directly into the cached DIB pixels
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var surface = SKSurface.Create(info, _cachedPixels, width * 4))
            {
                if (surface == null)
                    return false;

                var canvas = surface.Canvas;
                draw(canvas, info);
                canvas.Flush();
            }

            // Blit the memory DC to the screen
            GdiNativeMethods.BitBlt(hdc, 0, 0, width, height, _cachedMemDC, 0, 0, GdiNativeMethods.SRCCOPY);
            return true;
        }
        finally
        {
            GdiNativeMethods.ReleaseDC(_hwnd, hdc);
        }
    }

    /// <summary>
    /// Trims cached resources (memory DIB).
    /// </summary>
    public void TrimCaches()
    {
        DisposeCachedDIB();
    }

    private void DisposeCachedDIB()
    {
        if (_cachedBitmap != IntPtr.Zero)
        {
            GdiNativeMethods.DeleteObject(_cachedBitmap);
            _cachedBitmap = IntPtr.Zero;
        }

        if (_cachedMemDC != IntPtr.Zero)
        {
            GdiNativeMethods.DeleteDC(_cachedMemDC);
            _cachedMemDC = IntPtr.Zero;
        }

        _cachedPixels = IntPtr.Zero;
        _cachedWidth = 0;
        _cachedHeight = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DisposeCachedDIB();
        _disposed = true;
    }
}