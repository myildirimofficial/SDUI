using System;

namespace Orivy.Rendering;

internal static class RendererFactory
{
    internal static IWindowRenderer CreateRenderer(RenderBackend backend, nint hwnd)
    {
        IWindowRenderer renderer = backend switch
        {
            RenderBackend.Software => new SoftwareRenderer(),
            RenderBackend.OpenGL => new OpenGLRenderer(),
            RenderBackend.DirectX11 or RenderBackend.Vulkan or RenderBackend.Metal => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!"),
            _ => throw new NotSupportedException($"{backend} backend is not yet supported on this platform!")
        };

        renderer.Initialize(hwnd);
        return renderer;
    }
}
