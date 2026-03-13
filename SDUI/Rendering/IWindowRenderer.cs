using System;
using SkiaSharp;

namespace SDUI.Rendering;

internal interface IWindowRenderer : IDisposable
{
    bool IsSkiaGpuActive { get; }
    RenderBackend Backend { get; }
    GRContext? GrContext { get; }

    void Initialize(nint hwnd);

    void Resize(int width, int height);

    bool Render(int width, int height, Action<SKCanvas, SKImageInfo> draw);

    void TrimCaches();
}
