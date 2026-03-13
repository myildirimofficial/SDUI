using System;

namespace SDUI.Rendering;

internal static class RendererFactory
{
    internal static IWindowRenderer CreateRenderer(RenderBackend backend, nint hwnd)
    {
        var renderer = backend switch
        {
            RenderBackend.Software => new SoftwareRenderer(),
            RenderBackend.OpenGL => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!"),
            RenderBackend.DirectX11 => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!"),
            RenderBackend.Vulkan => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!"),
            RenderBackend.Metal => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!"),
            _ => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!")
        };

        renderer.Initialize(hwnd);
        return renderer;
    }
}
