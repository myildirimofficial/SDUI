using SkiaSharp;

namespace Orivy;

public class SKPaintSurfaceEventArgs(SKSurface surface, SKImageInfo info)
{
    public SKSurface Surface => surface;
    public SKImageInfo Info => info;
}