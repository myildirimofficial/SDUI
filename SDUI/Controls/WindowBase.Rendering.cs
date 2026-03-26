using SDUI.Native.Windows;
using SDUI.Rendering;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Controls;

public partial class WindowBase
{
    private readonly object _rendererSync = new();
    private bool _showPerfOverlay = true;
    private bool _softwareUpdateQueued;
    private Timer? _idleMaintenanceTimer;
    private int _suppressImmediateUpdateCount;
    private int _backendSwitchPaintTraceFrames;
    private long _perfLastTimestamp;
    private double _perfSmoothedFrameMs;
    private SKPaint? _perfOverlayPaint;
    protected SKBitmap _cacheBitmap;
    private SKSurface _cacheSurface;

    // Render loop optimization: cache GRContext and GPU state check
    private GRContext? _cachedGrContext;
    private bool _cachedGrContextIsValid;
    private int _grContextValidationFrame;
    private bool _paintInvalidatedPending;

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

    private RenderBackend _renderBackend = RenderBackend.Software;
    private IWindowRenderer _renderer;

    [System.ComponentModel.DefaultValue(RenderBackend.Software)]
    [System.ComponentModel.Description("Selects how WindowBase presents frames: Software (GDI), OpenGL, or DirectX11 (DXGI/GDI-compatible swapchain).")]
    public RenderBackend RenderBackend
    {
        get => _renderBackend;
        set
        {
            if (_renderBackend == value)
                return;

            Debug.WriteLine($"[WindowBase] Switching renderer: {_renderBackend} -> {value}");
            _renderBackend = value;

            if (!IsHandleCreated)
                return;

            _idleMaintenanceTimer?.Stop();
            NeedsFullChildRedraw = true;
            _perfLastTimestamp = 0;
            _perfSmoothedFrameMs = 0;
            _backendSwitchPaintTraceFrames = 4;
            ReleaseRetainedRenderResources();
            RecreateRenderer();
            ApplyNativeWindowStyles();
            ApplyThemeToNativeWindow();
            InvalidateWindow();
            ForceBackendSwitchRedraw();
        }
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        IWindowRenderer? rendererSnapshot;
        lock (_rendererSync)
            rendererSnapshot = _renderBackend != RenderBackend.Software ? _renderer : null;

        rendererSnapshot?.Resize((int)ClientSize.Width, (int)ClientSize.Height);

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
        if (!EnableIdleMaintenance)
            return;

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

        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        IntPtr lParam = IntPtr.Zero;
        _ = PostMessage(Handle, (int)WM_APP_IDLEMAINTENANCE, 0, ref lParam);
    }

    private void RunIdleMaintenance()
    {
        // 1. Trim renderer caches (DirectX / OpenGL) on UI thread.
        IWindowRenderer? rendererSnapshot;
        lock (_rendererSync)
            rendererSnapshot = _renderer;

        rendererSnapshot?.TrimCaches();

        // 2. Purge global Skia resource cache if requested
        if (PurgeSkiaResourceCacheOnIdle) SKGraphics.PurgeResourceCache();
    }

    protected void DisposeSoftwareBackBuffer()
    {
        _cacheSurface?.Dispose();
        _cacheSurface = null;

        _cacheBitmap?.Dispose();
        _cacheBitmap = null;
    }

    private void ReleaseRetainedRenderResources()
    {
        DisposeSoftwareBackBuffer();
        _perfOverlayPaint?.Dispose();
        _perfOverlayPaint = null;
    }

    private void RecreateRenderer()
    {
        if (!IsHandleCreated)
            return;

        Debug.WriteLine($"[WindowBase] RecreateRenderer begin. Target={_renderBackend}");

        IWindowRenderer? oldRenderer;
        lock (_rendererSync)
        {
            oldRenderer = _renderer;
            _renderer = null;
            _cachedGrContext = null;
            _cachedGrContextIsValid = false;  // Invalidate GRContext cache on renderer change
        }
        DisposeRendererSafely(oldRenderer);
        ReleaseRetainedRenderResources();

        IWindowRenderer? newRenderer = null;
        try
        {
            newRenderer = RendererFactory.CreateRenderer(_renderBackend, Handle);

            lock (_rendererSync)
            {
                _renderer = newRenderer;
                _cachedGrContext = null;
                _cachedGrContextIsValid = false;  // Invalidate GRContext cache on renderer initialization
            }

            Debug.WriteLine($"[WindowBase] RecreateRenderer completed. Active={_renderBackend}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowBase] Failed to initialize {_renderBackend} renderer. Falling back to Software. Error: {ex.Message}");
        }
    }

    private void ApplyNativeWindowStyles()
    {
        if (!IsHandleCreated)
            return;

        var hwnd = Handle;
        var stylePtr = GetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE);
        var style = stylePtr;
        var clipFlags = (nint)(uint)(SetWindowLongFlags.WS_CLIPCHILDREN | SetWindowLongFlags.WS_CLIPSIBLINGS);
        style = _renderer.IsSkiaGpuActive ? style | clipFlags : style & ~clipFlags;

        var exStylePtr = GetWindowLong(hwnd, WindowLongIndexFlags.GWL_EXSTYLE);
        var exStyle = exStylePtr;
        var noRedirect = (nint)(uint)SetWindowLongFlags.WS_EX_NOREDIRECTIONBITMAP;
        var composited = (nint)(uint)SetWindowLongFlags.WS_EX_COMPOSITED;

        // WS_EX_NOREDIRECTIONBITMAP is required for GPU rendering.
        // Software (GDI) renderer must NOT have this flag — GetDC+BitBlt requires DWM's
        // redirection bitmap; setting this flag causes the window to render as white/blank.
        if (_renderer.IsSkiaGpuActive)
        {
            exStyle |= noRedirect;
            exStyle &= ~composited;
        }
        else
        {
            exStyle &= ~noRedirect;
        }

        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(hwnd, (int)WindowLongIndexFlags.GWL_STYLE, style);
            SetWindowLongPtr64(hwnd, (int)WindowLongIndexFlags.GWL_EXSTYLE, exStyle);
        }
        else
        {
            SetWindowLong32(hwnd, (int)WindowLongIndexFlags.GWL_STYLE, (int)style);
            SetWindowLong32(hwnd, (int)WindowLongIndexFlags.GWL_EXSTYLE, (int)exStyle);
        }

        // Runtime backend switches may happen while input handlers are active.
        // Avoid SWP_FRAMECHANGED here to prevent expensive non-client recalculation/reentrancy.
        SetWindowPos(
            hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE |
            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

    }


    /// <summary>
    /// Handles WM_PAINT message - uses the appropriate renderer (Software or GPU).
    /// </summary>
    private IntPtr HandlePaint(IntPtr hWnd)
    {
        if (_backendSwitchPaintTraceFrames > 0)
            Debug.WriteLine($"[WindowBase] HandlePaint begin. Backend={_renderBackend}, Renderer={_renderer?.Backend.ToString() ?? "null"}");

        PAINTSTRUCT ps;
        var hdc = BeginPaint(hWnd, out ps);

        if (hdc == IntPtr.Zero)
            return IntPtr.Zero;

        try
        {
            _paintInvalidatedPending = false;

            var clientRect = new Rect();
            GetClientRect(hWnd, ref clientRect);

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;

            if (width <= 0 || height <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[HandlePaint] Invalid dimensions: {width}x{height}");
                return IntPtr.Zero;
            }

            // Try to render with active renderer (GPU or Software)
            if (_renderer.Render(width, height, RenderScene))
            {
                if (_backendSwitchPaintTraceFrames > 0)
                {
                    Debug.WriteLine($"[WindowBase] HandlePaint completed. Backend={_renderBackend}");
                    _backendSwitchPaintTraceFrames--;
                }
                return IntPtr.Zero;
            }

            ArmIdleMaintenance();
        }
        finally
        {
            EndPaint(hWnd, ref ps);
        }

        return IntPtr.Zero;
    }

    private static void DisposeRendererSafely(IWindowRenderer? renderer)
    {
        if (renderer == null)
            return;

        var backend = renderer.Backend;
        var sw = Stopwatch.StartNew();
        try
        {
            renderer.Dispose();
            Debug.WriteLine($"[WindowBase] Renderer disposed: {backend} ({sw.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowBase] Renderer dispose failed: {ex.Message}");
        }
    }

    private void RenderScene(SKCanvas canvas, SKImageInfo info)
    {
        // Lazy-validate GRContext: on cache miss or renderer change, look it up
        GRContext? gr = _cachedGrContext;
        if (!_cachedGrContextIsValid)
        {
            gr = null;
            lock (_rendererSync)
            {
                if (_renderer is IWindowRenderer renderer && renderer.IsSkiaGpuActive)
                    gr = renderer.GrContext;
            }

            _cachedGrContext = gr;
            _cachedGrContextIsValid = true;
        }

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
        canvas.Clear(ColorScheme.Surface);
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

        _perfOverlayPaint ??= new SKPaint { IsAntialias = true };
        var paint = _perfOverlayPaint;
        paint.Color = ColorScheme.ForeColor;
        paint.TextSize = 12;


        var backendLabel = RenderBackend + (_renderer.IsSkiaGpuActive ? "GPU" : "CPU");

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

        if (_paintInvalidatedPending)
            return;

        _paintInvalidatedPending = true;

        InvalidateRect(Handle, IntPtr.Zero, false);
    }

    private void ForceBackendSwitchRedraw()
    {
        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        const int rdwInvalidate = 0x0001;
        const int rdwAllChildren = 0x0080;
        const int rdwUpdateNow = 0x0100;
        const int rdwFrame = 0x0400;
        const int flags = rdwInvalidate | rdwAllChildren | rdwUpdateNow | rdwFrame;

        try
        {
            _ = RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, flags);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowBase] ForceBackendSwitchRedraw failed: {ex.Message}");
            Update();
        }
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
