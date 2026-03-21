using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using SDUI.Native.Windows;

namespace SDUI.Rendering;

/// <summary>
/// OpenGL-based GPU renderer using SkiaSharp's GRContext for hardware-accelerated rendering.
/// Provides high-performance rendering when OpenGL is available and enabled.
/// </summary>
internal sealed class OpenGLRenderer : IWindowRenderer
{
    private nint _hwnd;
    private nint _glContext;
    private nint _deviceContext;
    private GRContext? _grContext;
    private SKSurface? _surface;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _isInitialized;
    public bool IsSkiaGpuActive => _grContext != null && !_disposed;
    public RenderBackend Backend => RenderBackend.OpenGL;
    public GRContext? GrContext => _grContext;

    public void Initialize(nint hwnd)
    {
        if (hwnd == IntPtr.Zero)
            throw new ArgumentException("Invalid window handle", nameof(hwnd));

        if (_isInitialized)
            return;

        _hwnd = hwnd;
        _deviceContext = WglNativeMethods.GetDC(_hwnd);
        if (_deviceContext == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get device context for OpenGL rendering.");

        try
        {
            // Setup pixel format for OpenGL
            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (short)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                // PFD sabitlerinin numeric değerleri
                dwFlags = 0x00000004 | 0x00000020 | 0x00000001, // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
                iPixelType = 0, // PFD_TYPE_RGBA
                cColorBits = 32,
                cDepthBits = 24,
                cStencilBits = 8,
                iLayerType = 0 // PFD_MAIN_PLANE
            };

            int pixelFormat = WglNativeMethods.ChoosePixelFormat(_deviceContext, ref pfd);
            if (pixelFormat == 0)
                throw new InvalidOperationException("Failed to choose OpenGL pixel format.");

            if (!WglNativeMethods.SetPixelFormat(_deviceContext, pixelFormat, ref pfd))
                throw new InvalidOperationException("Failed to set OpenGL pixel format.");

            // Create temporary OpenGL context for function loading
            nint tempRc = WglNativeMethods.wglCreateContext(_deviceContext);
            if (tempRc == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create temporary OpenGL context.");

            if (!WglNativeMethods.wglMakeCurrent(_deviceContext, tempRc))
            {
                WglNativeMethods.wglDeleteContext(tempRc);
                throw new InvalidOperationException("Failed to make temporary OpenGL context current.");
            }

            // Create the actual OpenGL context
            _glContext = WglNativeMethods.wglCreateContext(_deviceContext);
            if (_glContext == IntPtr.Zero)
            {
                WglNativeMethods.wglDeleteContext(tempRc);
                throw new InvalidOperationException("Failed to create OpenGL rendering context.");
            }

            // Make the new context current
            if (!WglNativeMethods.wglMakeCurrent(_deviceContext, _glContext))
            {
                WglNativeMethods.wglDeleteContext(_glContext);
                WglNativeMethods.wglDeleteContext(tempRc);
                throw new InvalidOperationException("Failed to make OpenGL context current.");
            }

            // Clean up temporary context
            WglNativeMethods.wglDeleteContext(tempRc);

            // Create SkiaSharp GRContext for OpenGL
            var glInterface = GRGlInterface.Create();
            if (glInterface == null)
                throw new InvalidOperationException("Failed to assemble OpenGL interface for SkiaSharp.");

            _grContext = GRContext.CreateGl(glInterface);
            if (_grContext == null)
                throw new InvalidOperationException("Failed to create SkiaSharp GRContext for OpenGL.");

            _isInitialized = true;
        }
        finally
        {
            // Context current durumda kalır, rendering için hazır
        }
    }

    public void Resize(int width, int height)
    {
        if (_disposed || !_isInitialized)
            return;

        if (width <= 0 || height <= 0)
            return;

        // Update OpenGL viewport
        if (_glContext != IntPtr.Zero && _deviceContext != IntPtr.Zero)
        {
            WglNativeMethods.wglMakeCurrent(_deviceContext, _glContext);
            WglNativeMethods.glViewport(0, 0, width, height);
        }

        // Invalidate existing surface - recreated on next Render call
        _surface?.Dispose();
        _surface = null;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Renders a frame using OpenGL and SkiaSharp GPU backend.
    /// Returns true if the frame was successfully rendered and presented, false otherwise.
    /// </summary>
    public bool Render(int width, int height, Action<SKCanvas, SKImageInfo> draw)
    {
        if (_disposed || !_isInitialized || _hwnd == IntPtr.Zero)
            return false;

        if (width <= 0 || height <= 0 || _grContext == null)
            return false;

        // Ensure OpenGL context is current
        if (!WglNativeMethods.wglMakeCurrent(_deviceContext, _glContext))
            return false;

        try
        {
            // Recreate surface if needed (size changed or first render)
            if (_surface == null || _width != width || _height != height)
            {
                _surface?.Dispose();
                _surface = null;

                // GRBackendRenderTarget oluştur: default framebuffer (0), GL_RGBA8 format
                var framebufferInfo = new GRGlFramebufferInfo(0, 0x8058); // 0x8058 = GL_RGBA8
                var backendRenderTarget = new GRBackendRenderTarget(
                    width,
                    height,
                    0,  // sample count
                    0,  // stencil bits
                    framebufferInfo);

                // SKSurface.Create overload: GRContext, GRBackendRenderTarget, origin, colorType
                _surface = SKSurface.Create(
                    _grContext,
                    backendRenderTarget,
                    GRSurfaceOrigin.BottomLeft,
                    SKColorType.Rgba8888);
                
                if (_surface == null)
                    return false;

                _width = width;
                _height = height;
            }

            // Execute user drawing code
            var canvas = _surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            draw(canvas, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            canvas.Flush();

            // Present the frame by swapping buffers
            WglNativeMethods.SwapBuffers(_deviceContext);

            // Flush GRContext for resource management
            _grContext.Flush();

            return true;
        }
        catch
        {
            // On rendering error, clean up surface to force recreation
            _surface?.Dispose();
            _surface = null;
            return false;
        }
    }

    /// <summary>
    /// Trims GPU resources managed by SkiaSharp's GRContext.
    /// Call this when the application is backgrounded or memory pressure is high.
    /// </summary>
    public void TrimCaches()
    {
        if (_disposed || _grContext == null)
            return;

        // Trim SkiaSharp's GPU resource cache (soft trim)
        _grContext.PurgeResources();
        
        // Dispose surface to force recreation with fresh resources
        _surface?.Dispose();
        _surface = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Clean up SkiaSharp resources
        _surface?.Dispose();
        _surface = null;
        _grContext?.Dispose();
        _grContext = null;

        // Clean up OpenGL context
        if (_glContext != IntPtr.Zero)
        {
            if (_deviceContext != IntPtr.Zero)
            {
                WglNativeMethods.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            }
            WglNativeMethods.wglDeleteContext(_glContext);
            _glContext = IntPtr.Zero;
        }

        // Release device context
        if (_deviceContext != IntPtr.Zero && _hwnd != IntPtr.Zero)
        {
            WglNativeMethods.ReleaseDC(_hwnd, _deviceContext);
            _deviceContext = IntPtr.Zero;
        }

        _disposed = true;
        _isInitialized = false;
    }
}