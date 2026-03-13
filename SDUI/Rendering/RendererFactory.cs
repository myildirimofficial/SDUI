using System;

namespace SDUI.Rendering;

internal static class RendererFactory
{
    internal static IWindowRenderer CreateRenderer(RenderBackend backend, IntPtr hwnd)
    {
        IWindowRenderer renderer = backend switch
        {
            RenderBackend.Software => throw new NotSupportedException($"{backend} backend does not supporting on this platform!"),
            RenderBackend.OpenGL => throw new NotSupportedException($"{backend} backend does not supporting on this platform!"),
            RenderBackend.DirectX11 => throw new NotSupportedException($"{backend} backend does not supporting on this platform!"),
            RenderBackend.Vulkan => throw new NotSupportedException($"{backend} backend does not supporting on this platform!"),
            RenderBackend.Metal => throw new NotSupportedException($"{backend} backend does not supporting on this platform!"),
            _ => throw new NotSupportedException($"{backend} backend does not supporting on this platform!")
        };

        renderer.Initialize(hwnd);
        return renderer;
    }
}
