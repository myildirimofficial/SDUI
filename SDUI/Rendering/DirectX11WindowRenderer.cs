using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SkiaSharp;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace SDUI.Rendering;

/// <summary>
///     DirectX11 presenter that uses a D3D swapchain and prefers GPU-backed Skia rendering.
///     Rendering is done with Skia; when GPU path is unavailable we fall back to a CPU-rendered bitmap
///     and present it via a D3D upload (no GDI blit fallback).
/// </summary>
internal sealed class DirectX11WindowRenderer : IWindowRenderer, IGpuWindowRenderer
{
    // Lock to protect access to CPU cache resources during present/upload
    private readonly object _cpuCacheLock = new();
    private ID3D11Texture2D? _backBuffer;

    private SKBitmap? _cacheBitmap;
    private SKSurface? _cacheSurface;
    private ID3D11DeviceContext? _context;
    private uint _cpuUploadHeight;
    private ID3D11Texture2D? _cpuUploadTexture;
    private uint _cpuUploadWidth;

    private ID3D11Device? _device;

    private GRBackendRenderTarget? _grRenderTarget;
    private SKSurface? _grSurface;
    private int _height;

    private nint _hwnd;
    private ID3D11Texture2D? _presentBackBuffer;
    private IDXGISwapChain1? _swapChain;
    private bool _useSkiaGpu;

    private int _width;

    /// <summary>
    ///     Maximum retained bytes for the CPU backbuffer cache (SKBitmap + SKSurface).
    ///     Set to 0 (or less) to disable the limit (unlimited).
    /// </summary>
    public static long MaxCpuBackBufferBytes { get; set; } = 24L * 1024 * 1024;

    public bool IsSkiaGpuActive => _useSkiaGpu && GrContext != null && _grSurface != null;


    public string? LastInitError { get; private set; }

    // If SkiaSharp has a D3D backend in this build, we render directly to the swapchain backbuffer.
    // Otherwise we fall back to the CPU upload (D3D) path.
    public GRContext? GrContext { get; private set; }

    public RenderBackend Backend => RenderBackend.DirectX11;

    public void Initialize(nint hwnd)
    {
        _hwnd = hwnd;

        LastInitError = null;

        // Create D3D11 device
        var flags = DeviceCreationFlags.BgraSupport;
        var featureLevels = new[]
        {
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        };

        try
        {
            (_device, _context) = CreateDeviceWithFallback(flags, featureLevels);
        }
        catch (Exception ex)
        {
            LastInitError = ex.Message;
            throw;
        }

        _useSkiaGpu = TryInitializeSkiaD3D();

        try
        {
            // Create a normal swapchain; we will prefer GPU path when available
            // and fall back to CPU upload (D3D texture) when not.
            _swapChain = CreateSwapChainForHwnd(1, 1, false);
        }
        catch (Exception ex)
        {
            LastInitError = ex.Message;
            throw;
        }

        Resize(1, 1);
    }

    public void TrimCaches()
    {
        try
        {
            if (GrContext == null)
            {
                Debug.WriteLine("[DX11 TrimCaches] _grContext is null");
                return;
            }

            GrContext.PurgeResources();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DX11 TrimCaches] Exception: {ex}");
        }
    }

    public void Resize(int width, int height)
    {
        if (_swapChain == null)
            return;

        width = Math.Max(1, width);
        height = Math.Max(1, height);

        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;

        ReleaseGpuResources();
        ReleaseCpuUploadResources();
        DisposeCpuCacheResources();

        // If GPU init was attempted and later disabled, we may still have a flip-model swapchain.
        // Ensure we have a swapchain and CPU cache resources for the CPU upload path.
        if (!_useSkiaGpu && EnsureCpuSwapChain(width, height))
            return;

        try
        {
            // Release potentially bound resources ensuring ResizeBuffers succeeds
            _context?.ClearState();
            _context?.Flush();

            // ResizeBuffers flags must be 0; the GDI-compatible flag is specified at creation time.
            _swapChain.ResizeBuffers(0, (uint)width, (uint)height, Format.Unknown, SwapChainFlags.None);
        }
        catch (Exception ex)
        {
            LastInitError = $"ResizeBuffers failed: {ex.Message}";
            throw;
        }

        if (_useSkiaGpu && GrContext != null)
        {
            TryCreateSkiaBackBufferSurface(width, height);
            return;
        }

        try
        {
            CreateCpuCacheResources(width, height);
        }
        catch (Exception ex)
        {
            LastInitError = $"CPU cache init failed: {ex.Message}";

            _cacheSurface?.Dispose();
            _cacheSurface = null;
            _cacheBitmap?.Dispose();
            _cacheBitmap = null;
            // If CPU cache creation fails, we will fall back to per-frame uncached rendering.
        }
    }

    public bool Render(int width, int height, Action<SKCanvas, SKImageInfo> draw)
    {
        if (_swapChain == null)
            return false;

        Resize(width, height);

        if (_useSkiaGpu && _grSurface != null && GrContext != null)
        {
            var gpuInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul,
                SKColorSpace.CreateSrgb());
            draw(_grSurface.Canvas, gpuInfo);
            _grSurface.Canvas.Flush();
            GrContext.Flush();
            GrContext.Submit();
            // Use SyncInterval 0 (no VSync) to prevent potential blocking/freezing on some systems.
            _swapChain.Present(0, PresentFlags.None);
            return true;
        }

        // #####################
        var info = new SKImageInfo((int)width, height, SKColorType.Bgra8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());

        // If we have a cached CPU backbuffer (within MaxCpuBackBufferBytes), use it.
        if (_cacheSurface != null && _cacheBitmap != null)
            lock (_cpuCacheLock)
            {
                draw(_cacheSurface.Canvas, info);
                _cacheSurface.Canvas.Flush();

                // Prefer D3D upload path for presenting CPU-rendered cache.
                try
                {
                    PresentFromCpuCacheViaD3D11Upload(_cacheBitmap, width, height);
                    return true;
                }
                catch (Exception ex)
                {
                    LastInitError = $"D3D11 upload present failed: {ex.Message}; falling back to per-frame rendering.";
                    DisposeCpuCacheResources();
                    // Fall through to per-frame temporary rendering below.
                }
            }

        // Otherwise render into a temporary buffer per frame to keep resident memory low.
        using var tempBitmap = new SKBitmap(info);
        var pixels = tempBitmap.GetPixels();
        using var tempSurface = SKSurface.Create(info, pixels, tempBitmap.RowBytes);
        if (tempSurface == null)
            return false;

        draw(tempSurface.Canvas, info);
        tempSurface.Canvas.Flush();

        try
        {
            PresentFromCpuCacheViaD3D11Upload(tempBitmap, width, height);
            return true;
        }
        catch (Exception ex)
        {
            LastInitError = $"D3D11 upload present failed: {ex.Message}; no fallback available for this frame.";
            return false;
        }
        // #####################
    }

    public void Dispose()
    {
        ReleaseGpuResources();
        ReleaseCpuUploadResources();

        GrContext?.Dispose();
        GrContext = null;

        DisposeCpuCacheResources();

        _swapChain?.Dispose();
        _swapChain = null;

        _context?.Dispose();
        _context = null;

        _device?.Dispose();
        _device = null;

        _hwnd = 0;
    }

    private void ReleaseGpuResources()
    {
        _grSurface?.Dispose();
        _grSurface = null;

        _grRenderTarget?.Dispose();
        _grRenderTarget = null;

        _backBuffer?.Dispose();
        _backBuffer = null;
    }

    private void ReleaseCpuUploadResources()
    {
        _presentBackBuffer?.Dispose();
        _presentBackBuffer = null;

        _cpuUploadTexture?.Dispose();
        _cpuUploadTexture = null;

        _cpuUploadWidth = 0;
        _cpuUploadHeight = 0;
    }

    private IDXGISwapChain1 CreateSwapChainForHwnd(int width, int height, bool gdiCompatible)
    {
        if (_device == null)
            throw new InvalidOperationException("D3D device is not initialized.");
        if (_hwnd == 0)
            throw new InvalidOperationException("HWND is not initialized.");

        width = Math.Max(1, width);
        height = Math.Max(1, height);

        using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetAdapter();
        using var factory = adapter.GetParent<IDXGIFactory2>();

        var desc = gdiCompatible
            ? new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                // GDI-compatible swapchains must use the legacy "blt" model.
                // With SwapEffect.Discard, DXGI requires BufferCount == 1.
                BufferCount = 1,
                // Legacy swap effects use the blt model; Stretch is the safest value.
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.Discard,
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.GdiCompatible
            }
            : new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                // Stretch is more broadly compatible for HWND flip-model swapchains.
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.None
            };

        var sc = factory.CreateSwapChainForHwnd(_device, _hwnd, desc);
        factory.MakeWindowAssociation(_hwnd, WindowAssociationFlags.IgnoreAltEnter);
        return sc;
    }

    private bool EnsureCpuSwapChain(int width, int height)
    {
        // Ensure a swapchain exists for CPU upload path (non-GDI flip model).
        if (_swapChain == null)
            try
            {
                _swapChain?.Dispose();
                _swapChain = CreateSwapChainForHwnd(width, height, false);
                // Create or refresh CPU cache resources
                CreateCpuCacheResources(width, height);
                return true;
            }
            catch (Exception ex)
            {
                LastInitError = $"Failed to recreate CPU swapchain: {ex.Message}";
                throw;
            }

        return false;
    }

    private void CreateCpuCacheResources(int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
        var bytes = EstimateBackBufferBytes(width, height);
        var max = MaxCpuBackBufferBytes;

        // If the requested buffer is too large, do not retain a cached backbuffer.
        // We'll render into a temporary buffer per frame.
        if (max > 0 && bytes > max)
        {
            lock (_cpuCacheLock)
            {
                _cacheSurface?.Dispose();
                _cacheSurface = null;

                _cacheBitmap?.Dispose();
                _cacheBitmap = null;
            }

            return;
        }

        lock (_cpuCacheLock)
        {
            _cacheBitmap = new SKBitmap(info);
            var pixels = _cacheBitmap.GetPixels();
            _cacheSurface = SKSurface.Create(info, pixels, _cacheBitmap.RowBytes);

            // Note: GDI fallback removed. We use D3D upload (PresentFromCpuCacheViaD3D11Upload) when GPU is unavailable.
        }
    }

    private void DisposeCpuCacheResources()
    {
        lock (_cpuCacheLock)
        {
            _cacheSurface?.Dispose();
            _cacheSurface = null;

            _cacheBitmap?.Dispose();
            _cacheBitmap = null;
        }
    }

    private static long EstimateBackBufferBytes(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 0;

        try
        {
            checked
            {
                return (long)width * height * 4;
            }
        }
        catch
        {
            return long.MaxValue;
        }
    }

    private void EnsureCpuUploadResources(int width, int height)
    {
        if (_device == null || _context == null || _swapChain == null)
            throw new InvalidOperationException("D3D device/context/swapchain is not initialized.");

        if (_cpuUploadTexture != null && _presentBackBuffer != null && _cpuUploadWidth == width &&
            _cpuUploadHeight == height)
            return;

        _presentBackBuffer?.Dispose();
        _presentBackBuffer = null;
        _cpuUploadTexture?.Dispose();
        _cpuUploadTexture = null;

        _presentBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            // CPU upload texture: stage on CPU, then copy into swapchain backbuffer.
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.None
        };

        _cpuUploadTexture = _device.CreateTexture2D(desc);
        _cpuUploadWidth = (uint)width;
        _cpuUploadHeight = (uint)height;
    }

    private unsafe void PresentFromCpuCacheViaD3D11Upload(SKBitmap srcBitmap, int width, int height)
    {
        if (_context == null || _swapChain == null || srcBitmap == null)
            return;

        var stage = "EnsureCpuUploadResources";
        try
        {
            EnsureCpuUploadResources(width, height);
            if (_cpuUploadTexture == null || _presentBackBuffer == null)
                return;

            lock (_cpuCacheLock)
            {
                stage = "Map";
                var mapped = _context.Map(_cpuUploadTexture, 0, MapMode.Write);
                try
                {
                    // Defensive guard: re-check srcBitmap and pixel pointer
                    var pixelsPtr = srcBitmap.GetPixels();
                    if (pixelsPtr == IntPtr.Zero)
                        return;

                    var srcBase = (byte*)pixelsPtr.ToPointer();
                    var dstBase = (byte*)mapped.DataPointer.ToPointer();

                    var srcStride = srcBitmap.RowBytes;
                    var dstStride = mapped.RowPitch;
                    var rowBytes = width * 4;

                    for (var y = 0; y < height; y++)
                        Buffer.MemoryCopy(srcBase + y * srcStride, dstBase + y * dstStride, dstStride, rowBytes);
                }
                finally
                {
                    _context.Unmap(_cpuUploadTexture, 0);
                }

                stage = "CopyResource";
                _context.CopyResource(_presentBackBuffer, _cpuUploadTexture);

                stage = "Present";
                // Use SyncInterval 0 (no VSync) to prevent potential blocking/freezing on some systems.
                _swapChain.Present(0, PresentFlags.None);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"DX11 CPU upload failed at {stage}: {ex.Message}", ex);
        }
    }

    private static (ID3D11Device device, ID3D11DeviceContext context) CreateDeviceWithFallback(
        DeviceCreationFlags flags, FeatureLevel[] featureLevels)
    {
        var errors = new List<Exception>(6);

        var without11_1 = featureLevels.Contains(FeatureLevel.Level_11_1)
            ? featureLevels.Where(f => f != FeatureLevel.Level_11_1).ToArray()
            : featureLevels;

        var wideFeatureLevels = featureLevels.Concat(new[]
        {
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1
        }).Distinct().ToArray();

        var wideWithout11_1 = wideFeatureLevels.Where(f => f != FeatureLevel.Level_11_1).ToArray();

        var attempts = new (DriverType Driver, FeatureLevel[] Levels)[]
        {
            (DriverType.Hardware, featureLevels),
            (DriverType.Hardware, without11_1),
            (DriverType.Warp, featureLevels),
            (DriverType.Warp, without11_1),
            (DriverType.Warp, wideFeatureLevels),
            (DriverType.Warp, wideWithout11_1)
        };

        foreach (var attempt in attempts)
        {
            if (attempt.Levels == null || attempt.Levels.Length == 0)
                continue;

            if (TryCreateDevice(attempt.Driver, flags, attempt.Levels, out var device, out var context, out var error))
                return (device, context);

            if (error != null)
                errors.Add(error);
        }

        var message = "Failed to create a D3D11 device (Hardware and WARP).";
        if (errors.Count > 0)
            message += " Last error: " + errors[^1].Message;
        throw new InvalidOperationException(message, errors.Count > 0 ? errors[^1] : null);
    }

    private static bool TryCreateDevice(
        DriverType driverType,
        DeviceCreationFlags flags,
        FeatureLevel[] featureLevels,
        out ID3D11Device device,
        out ID3D11DeviceContext context,
        out Exception? error)
    {
        device = null!;
        context = null!;
        error = null;

        try
        {
            D3D11.D3D11CreateDevice(
                null,
                driverType,
                flags,
                featureLevels,
                out device,
                out context);
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    private bool TryInitializeSkiaD3D()
    {
        try
        {
            if (_device == null || _context == null)
                return false;

            // SkiaSharp D3D backend APIs are not available in all builds; use reflection.
            var skiaAsm = typeof(GRContext).Assembly;
            var backendContextType = skiaAsm.GetType("SkiaSharp.GRD3DBackendContext", false);
            if (backendContextType == null)
                return false;

            var backendContext = Activator.CreateInstance(backendContextType);
            if (backendContext == null)
                return false;

            var devicePtr = _device.NativePointer;
            var contextPtr = _context.NativePointer;

            SetPropertyIfExists(backendContext, "Device", devicePtr);
            SetPropertyIfExists(backendContext, "Context", contextPtr);
            SetPropertyIfExists(backendContext, "Queue", contextPtr);

            var create = typeof(GRContext).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "CreateDirect3D" && m.GetParameters().Length >= 1 &&
                    m.GetParameters()[0].ParameterType == backendContextType);

            if (create == null)
                return false;

            var gr = create.GetParameters().Length == 1
                ? (GRContext?)create.Invoke(null, new[] { backendContext })
                : (GRContext?)create.Invoke(null, new[] { backendContext, null });

            if (gr == null)
                return false;

            GrContext = gr;
            return true;
        }
        catch
        {
            GrContext = null;
            return false;
        }
    }

    private static void SetPropertyIfExists(object target, string name, nint value)
    {
        var prop = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null || !prop.CanWrite)
            return;

        try
        {
            if (prop.PropertyType == typeof(IntPtr) || prop.PropertyType == typeof(nint))
                prop.SetValue(target, value);
            else if (prop.PropertyType == typeof(long))
                prop.SetValue(target, (long)value);
            else if (prop.PropertyType == typeof(ulong))
                prop.SetValue(target, (ulong)value);
        }
        catch
        {
            // ignore
        }
    }

    private void TryCreateSkiaBackBufferSurface(int width, int height)
    {
        try
        {
            if (_swapChain == null || GrContext == null)
                return;

            _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            var skiaAsm = typeof(GRContext).Assembly;
            var texInfoType = skiaAsm.GetType("SkiaSharp.GRD3DTextureInfo", false);
            if (texInfoType == null)
                throw new MissingMemberException("SkiaSharp.GRD3DTextureInfo not found.");

            var texInfo = Activator.CreateInstance(texInfoType) ??
                          throw new InvalidOperationException("Failed to create GRD3DTextureInfo.");
            SetPropertyIfExists(texInfo, "Texture", _backBuffer.NativePointer);
            SetPropertyIfExists(texInfo, "Resource", _backBuffer.NativePointer);

            // Some Skia builds require a DXGI format value.
            var formatProp = texInfoType.GetProperty("Format", BindingFlags.Public | BindingFlags.Instance);
            if (formatProp != null && formatProp.CanWrite)
                try
                {
                    var dxgiFormat = (int)Format.B8G8R8A8_UNorm;
                    if (formatProp.PropertyType == typeof(int)) formatProp.SetValue(texInfo, dxgiFormat);
                    else if (formatProp.PropertyType == typeof(uint)) formatProp.SetValue(texInfo, (uint)dxgiFormat);
                }
                catch
                {
                }

            // Create a backend render target wrapping the swapchain buffer.
            var ctor = typeof(GRBackendRenderTarget).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 5 && p[0].ParameterType == typeof(int) && p[1].ParameterType == typeof(int) &&
                           p[4].ParameterType == texInfoType;
                });

            if (ctor == null)
                throw new MissingMemberException(
                    "GRBackendRenderTarget ctor(width,height,samples,stencil,GRD3DTextureInfo) not found.");

            _grRenderTarget = (GRBackendRenderTarget)ctor.Invoke(new[] { width, height, 1, 8, texInfo });
            _grSurface = SKSurface.Create(
                GrContext,
                _grRenderTarget,
                GRSurfaceOrigin.TopLeft,
                SKColorType.Bgra8888,
                SKColorSpace.CreateSrgb());
        }
        catch (Exception ex)
        {
            // If anything fails, fall back to the CPU upload path for this session.
            LastInitError = $"Skia D3D backbuffer init failed: {ex.Message}";
            _useSkiaGpu = false;
            ReleaseGpuResources();

            // Ensure we have a swapchain suitable for CPU upload/present.
            if (EnsureCpuSwapChain(width, height))
                return;
        }
    }
}
