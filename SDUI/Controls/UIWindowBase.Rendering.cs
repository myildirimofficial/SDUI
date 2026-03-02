using SDUI.Native.Windows;
using SDUI.Rendering;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Timers;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Controls;

public partial class UIWindowBase
{
    private bool _showPerfOverlay = true;
    private IntPtr _cachedMemDC;
    private IntPtr _cachedBitmap;
    private IntPtr _cachedPixels;
    private int _cachedWidth;
    private int _cachedHeight;
    private bool _softwareUpdateQueued;
    private Timer? _idleMaintenanceTimer;
    private int _suppressImmediateUpdateCount;
    private long _perfLastTimestamp;
    private double _perfSmoothedFrameMs;
    protected SKBitmap _cacheBitmap;
    private SKSurface _cacheSurface;

    /// <summary>
    ///     Maximum retained bytes for the software backbuffer (SKBitmap + SKSurface + GDI Bitmap wrapper).
    ///     This prevents 4K/8K windows from permanently retaining very large pixel buffers.
    ///     Set to 0 (or less) to disable the limit (unlimited).
    /// </summary>
    public static long MaxSoftwareBackBufferBytes { get; set; } = 24L * 1024 * 1024;

    public static bool EnableIdleMaintenance { get; set; } = true;

    /// <summary>
    ///     Delay (ms) after the last repaint request before trimming retained backbuffers and
    ///     asking Skia to purge resource caches.
    /// </summary>
    public static int IdleMaintenanceDelayMs { get; set; } = 1500;


    public static bool PurgeSkiaResourceCacheOnIdle { get; set; } = true;

    [DefaultValue(false)]
    [Description("Shows a small FPS/frame-time overlay for measuring renderer performance.")]
    public bool ShowPerfOverlay
    {
        get => _showPerfOverlay;
        set
        {
            if (_showPerfOverlay == value)
                return;
            _showPerfOverlay = value;
            _perfLastTimestamp = 0;
            _perfSmoothedFrameMs = 0;
            Invalidate();
        }
    }

    private SDUI.Rendering.RenderBackend _renderBackend = SDUI.Rendering.RenderBackend.Software;
    private SDUI.Rendering.IWindowRenderer _renderer;

    [System.ComponentModel.DefaultValue(SDUI.Rendering.RenderBackend.Software)]
    [System.ComponentModel.Description("Selects how UIWindowBase presents frames: Software (GDI), OpenGL, or DirectX11 (DXGI/GDI-compatible swapchain).")]
    public SDUI.Rendering.RenderBackend RenderBackend
    {
        get => _renderBackend;
        set
        {
            if (_renderBackend == value)
                return;

            _renderBackend = value;
            ApplyRenderStyles();
            RecreateRenderer();
            InvalidateWindow();
        }
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (_renderBackend != RenderBackend.Software && _renderer != null)
            _renderer.Resize((int)ClientSize.Width, (int)ClientSize.Height);

        Invalidate();
    }


    private void QueueSoftwareUpdate()
    {
        if (_softwareUpdateQueued)
            return;

        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        _softwareUpdateQueued = true;
        try
        {
            _softwareUpdateQueued = false;
            if (!IsHandleCreated || IsDisposed || Disposing)
                return;

            if (_renderBackend == RenderBackend.Software)
                InvalidateWindow(); // ✅ Use native invalidate to avoid recursion
        }
        catch
        {
            _softwareUpdateQueued = false;
        }
    }

    public override void Invalidate()
    {
        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        if (_renderBackend == RenderBackend.Software)
        {
            if (_suppressImmediateUpdateCount <= 0 && ShouldForceSoftwareUpdate())
                QueueSoftwareUpdate();
            else
                InvalidateWindow();
        }
        else
        {
            InvalidateWindow();
        }
    }

    protected virtual bool ShouldForceSoftwareUpdate()
    {
        return true;
    }

    protected void BeginImmediateUpdateSuppression()
    {
        _suppressImmediateUpdateCount++;
    }

    protected void EndImmediateUpdateSuppression()
    {
        if (_suppressImmediateUpdateCount > 0)
            _suppressImmediateUpdateCount--;
    }

    protected void ArmIdleMaintenance()
    {
        if (_idleMaintenanceTimer == null)
        {
            _idleMaintenanceTimer = new Timer();
            _idleMaintenanceTimer.Interval = IdleMaintenanceDelayMs;
            _idleMaintenanceTimer.Elapsed += IdleMaintenanceTimer_Tick;
        }
        
        if (_idleMaintenanceTimer != null)
        {
            _idleMaintenanceTimer.Stop();
            _idleMaintenanceTimer.Start();
        }
    }

    private void IdleMaintenanceTimer_Tick(object? sender, EventArgs e)
    {
        _idleMaintenanceTimer?.Stop();

        // 1. Trim renderer caches (DirectX / OpenGL)
        _renderer?.TrimCaches();

        // 2. Trim software backbuffer if using software rendering
        if (_renderer == null)
        {
            DisposeSoftwareBackBuffer();
            NeedsFullChildRedraw = true;
        }

        // 3. Purge global Skia resource cache if requested
        if (PurgeSkiaResourceCacheOnIdle) SKGraphics.PurgeResourceCache();
    }

    protected void DisposeSoftwareBackBuffer()
    {
        _cacheSurface?.Dispose();
        _cacheSurface = null;

        _cacheBitmap?.Dispose();
        _cacheBitmap = null;
    }

    private void RecreateRenderer()
    {
        if (!IsHandleCreated)
            return;

        if (_renderBackend == RenderBackend.Software)
        {
            _renderer?.Dispose();
            _renderer = null;
            return;
        }

        try
        {
            _renderer?.Dispose();
            _renderer = _renderBackend switch
            {
                SDUI.Rendering.RenderBackend.DirectX11 => new SDUI.Rendering.DirectX11WindowRenderer(),
                SDUI.Rendering.RenderBackend.OpenGL => new SDUI.Rendering.OpenGlWindowRenderer(),
                _ => null
            };
            
            if (_renderer != null)
                _renderer.Initialize(Handle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UIWindowBase] Failed to initialize {_renderBackend} renderer. Falling back to Software. Error: {ex.Message}");
            _renderer?.Dispose();
            _renderer = null;
            _renderBackend = SDUI.Rendering.RenderBackend.Software;
        }
    }

    private void ApplyRenderStyles()
    {
        var gpu = _renderBackend != SDUI.Rendering.RenderBackend.Software;
        ApplyNativeWindowStyles(gpu);
    }

    private void ApplyNativeWindowStyles(bool gpu)
    {
        if (!IsHandleCreated)
            return;

        var hwnd = Handle;
        var stylePtr = GetWindowLong(hwnd, SDUI.Native.Windows.WindowLongIndexFlags.GWL_STYLE);
        var style = stylePtr;
        var clipFlags = (nint)(uint)(SDUI.Native.Windows.SetWindowLongFlags.WS_CLIPCHILDREN | SDUI.Native.Windows.SetWindowLongFlags.WS_CLIPSIBLINGS);
        style = gpu ? style | clipFlags : style & ~clipFlags;

        var exStylePtr = GetWindowLong(hwnd, SDUI.Native.Windows.WindowLongIndexFlags.GWL_EXSTYLE);
        var exStyle = exStylePtr;
        var noRedirect = (nint)(uint)SDUI.Native.Windows.SetWindowLongFlags.WS_EX_NOREDIRECTIONBITMAP;
        var composited = (nint)(uint)SDUI.Native.Windows.SetWindowLongFlags.WS_EX_COMPOSITED;
        if (gpu)
        {
            if (_renderBackend == SDUI.Rendering.RenderBackend.OpenGL)
                exStyle |= noRedirect;
            else
                exStyle &= ~noRedirect;
            exStyle &= ~composited;
        }
        else
        {
            exStyle &= ~noRedirect;
        }

        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(hwnd, (int)SDUI.Native.Windows.WindowLongIndexFlags.GWL_STYLE, style);
            SetWindowLongPtr64(hwnd, (int)SDUI.Native.Windows.WindowLongIndexFlags.GWL_EXSTYLE, exStyle);
        }
        else
        {
            SetWindowLong32(hwnd, (int)SDUI.Native.Windows.WindowLongIndexFlags.GWL_STYLE, (int)style);
            SetWindowLong32(hwnd, (int)SDUI.Native.Windows.WindowLongIndexFlags.GWL_EXSTYLE, (int)exStyle);
        }

        SetWindowPos(
            hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SDUI.Native.Windows.SetWindowPosFlags.SWP_NOMOVE | SDUI.Native.Windows.SetWindowPosFlags.SWP_NOSIZE |
            SDUI.Native.Windows.SetWindowPosFlags.SWP_NOZORDER | SDUI.Native.Windows.SetWindowPosFlags.SWP_FRAMECHANGED);
    }


    /// <summary>
    /// Handles WM_PAINT message - creates Skia surface and renders to native HDC.
    /// Uses a cached Memory DC + DIB section for flicker-free double buffering.
    /// The GDI resources are retained between frames and only re-allocated on resize.
    /// </summary>
    private IntPtr HandlePaint(IntPtr hWnd)
    {
        PAINTSTRUCT ps;
        var hdc = BeginPaint(hWnd, out ps);

        if (hdc == IntPtr.Zero)
            return IntPtr.Zero;

        try
        {
            var clientRect = new Rect();
            GetClientRect(hWnd, ref clientRect);

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;

            if (width <= 0 || height <= 0)
                return IntPtr.Zero;

            // Re-create cached DIB only when size changes
            if (_cachedMemDC == IntPtr.Zero || width != _cachedWidth || height != _cachedHeight)
            {
                DisposeCachedDIB();

                _cachedMemDC = CreateCompatibleDC(hdc);
                if (_cachedMemDC == IntPtr.Zero)
                    return IntPtr.Zero;

                var bmi = new BITMAPINFO
                {
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0
                };

                _cachedBitmap = CreateDIBSection(hdc, ref bmi, 0, out _cachedPixels, IntPtr.Zero, 0);
                if (_cachedBitmap == IntPtr.Zero || _cachedPixels == IntPtr.Zero)
                {
                    DisposeCachedDIB();
                    return IntPtr.Zero;
                }

                SelectObject(_cachedMemDC, _cachedBitmap);
                _cachedWidth = width;
                _cachedHeight = height;
            }

            // Render via Skia directly into the cached DIB pixels
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var surface = SKSurface.Create(info, _cachedPixels, width * 4))
            {
                if (surface != null)
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(BackColor);
                    OnPaintCanvas(canvas, info);
                    canvas.Flush();
                }
            }

            // Single
            BitBlt(hdc, 0, 0, width, height, _cachedMemDC, 0, 0, SRCCOPY);
        }
        finally
        {
            EndPaint(hWnd, ref ps);
        }

        return IntPtr.Zero;
    }

    private void DisposeCachedDIB()
    {
        if (_cachedBitmap != IntPtr.Zero)
        {
            DeleteObject(_cachedBitmap);
            _cachedBitmap = IntPtr.Zero;
        }

        if (_cachedMemDC != IntPtr.Zero)
        {
            DeleteDC(_cachedMemDC);
            _cachedMemDC = IntPtr.Zero;
        }

        _cachedPixels = IntPtr.Zero;
        _cachedWidth = 0;
        _cachedHeight = 0;
    }

    protected virtual void OnPaintCanvas(SKCanvas canvas, SKImageInfo info)
    {
        if (_renderBackend != RenderBackend.Software && _renderer != null)
        {
            try
            {
                _renderer.Render((int)info.Width, (int)info.Height, RenderScene);
                ArmIdleMaintenance();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UIWindowBase] Hardware rendering failed ({ex.GetType().Name}). Falling back to Software. Error: {ex.Message}");
                _renderer?.Dispose();
                _renderer = null;
                _renderBackend = RenderBackend.Software;
            }
        }

        RenderScene(canvas, info);
        ArmIdleMaintenance();
    }

    private void RenderScene(SKCanvas canvas, SKImageInfo info)
    {
        GRContext? gr = null;

        if (_renderer is DirectX11WindowRenderer dx && dx.IsSkiaGpuActive)
            gr = dx.GrContext;
        else if (_renderer is OpenGlWindowRenderer gl && gl.IsSkiaGpuActive)
            gr = gl.GrContext;
        else if (_renderer is IGpuWindowRenderer gpuRenderer)
            gr = gpuRenderer.GrContext;

        using var gpuScope = gr != null ? PushGpuContext(gr) : null;

        canvas.Save();
        canvas.ResetMatrix();
        canvas.ClipRect(SKRect.Create(info.Width, info.Height));

        RenderWindowFrame(canvas, info);
        RenderChildren(canvas);

        if (ShowPerfOverlay)
            DrawPerfOverlay(canvas);

        canvas.Restore();
    }

    protected virtual void RenderWindowFrame(SKCanvas canvas, SKImageInfo info)
    {
        canvas.Clear(ColorScheme.BackColor);
    }

    private void DrawPerfOverlay(SKCanvas canvas)
    {
        var now = Stopwatch.GetTimestamp();
        if (_perfLastTimestamp == 0)
        {
            _perfLastTimestamp = now;
            return;
        }

        var dt = (now - _perfLastTimestamp) / (double)Stopwatch.Frequency;
        _perfLastTimestamp = now;
        if (dt <= 0)
            return;

        var frameMs = dt * 1000.0;
        _perfSmoothedFrameMs = _perfSmoothedFrameMs <= 0
            ? frameMs
            : _perfSmoothedFrameMs * 0.90 + frameMs * 0.10;

        var fps = 1000.0 / Math.Max(0.001, _perfSmoothedFrameMs);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ColorScheme.ForeColor,
            TextSize = 12
        };

        var backendLabel = RenderBackend.ToString();
        if (RenderBackend == RenderBackend.DirectX11 && _renderer is DirectX11WindowRenderer dx)
            backendLabel = dx.IsSkiaGpuActive ? "DX:GPU" : "DX:CPU";
        else if (RenderBackend == RenderBackend.OpenGL && _renderer is OpenGlWindowRenderer gl)
            backendLabel = gl.IsSkiaGpuActive ? "GL:GPU" : "GL";

        var text = $"{backendLabel}  {fps:0} FPS  {_perfSmoothedFrameMs:0.0} ms";
        canvas.DrawText(text, 8, 16, paint);
    }

    #region Native Structures and Methods for GDI Drawing

    /// <summary>
    /// Invalidates the window and requests a repaint on the next message loop iteration.
    /// </summary>
    public virtual void InvalidateWindow()
    {
        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        InvalidateRect(Handle, IntPtr.Zero, false);
    }

    /// <summary>
    /// Converts a window-relative rectangle to screen coordinates.
    /// </summary>
    public SKRect RectangleToScreen(SKRect clientRect)
    {
        var topLeft = PointToScreen(clientRect.Location);
        return SKRect.Create(topLeft, clientRect.Size);
    }

    /// <summary>
    /// Converts a screen-space rectangle to window client coordinates.
    /// </summary>
    public SKRect RectangleToClient(SKRect screenRect)
    {
        var topLeft = PointToClient(screenRect.Location);
        return SKRect.Create(topLeft, screenRect.Size);
    }

    /// <summary>
    /// Gets the window rectangle in screen coordinates (including frame/borders).
    /// </summary>
    public SKRectI GetWindowRect()
    {
        if (!IsHandleCreated)
            return SKRectI.Empty;

        Rect rect;
        Methods.GetWindowRect(Handle, out rect);
        
        return SKRectI.Create(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Gets the client rectangle in screen coordinates.
    /// </summary>
    public SKRect GetClientRectScreen()
    {
        if (!IsHandleCreated)
            return SKRect.Empty;

        return RectangleToScreen(ClientRectangle);
    }

    /// <summary>
    /// Forces an immediate synchronous paint of the window.
    /// Only use when absolutely necessary - prefer Invalidate() for normal updates.
    /// </summary>
    public virtual void Update()
    {
        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        UpdateWindow(Handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BITMAPINFO pbmi,
        uint iUsage,
        out IntPtr ppvBits,
        IntPtr hSection,
        uint dwOffset);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(
        IntPtr hdcDest,
        int nXDest,
        int nYDest,
        int nWidth,
        int nHeight,
        IntPtr hdcSrc,
        int nXSrc,
        int nYSrc,
        int dwRop);

    private const int SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public Rect rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    #endregion
}
