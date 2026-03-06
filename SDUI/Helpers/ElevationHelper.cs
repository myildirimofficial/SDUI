
using SkiaSharp;

namespace SDUI.Helpers;

/// <summary>
///     Modern elevation system for Material Design 3 style depth
/// </summary>
public static class ElevationHelper
{
    /// <summary>
    ///     Draws elevation shadow and tint for a surface
    /// </summary>
    public static void DrawElevation(SKCanvas canvas, SkiaSharp.SKRect bounds, float cornerRadius, int elevation)
    {
        if (ColorScheme.FlatDesign) return;
        if (elevation <= 0) return;

        var blur = ColorScheme.GetElevationBlur(elevation);
        var offset = ColorScheme.GetElevationOffset(elevation);
        var shadowColor = ColorScheme.ShadowColor.WithAlpha((byte)(ColorScheme.IsDarkMode ? 40 : 15));

        // Draw shadow
        using (var shadowMaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur / 2))
        using (var shadowPaint = new SKPaint
               {
                   IsAntialias = true,
                   Color = shadowColor,
                   MaskFilter = shadowMaskFilter
               })
        {
            var shadowBounds = new SkiaSharp.SKRect(
                bounds.Left,
                bounds.Top + offset,
                bounds.Right,
                bounds.Bottom + offset
            );
            canvas.DrawRoundRect(shadowBounds, cornerRadius, cornerRadius, shadowPaint);
        }

        // Draw elevation tint (for dark mode)
        if (ColorScheme.IsDarkMode && elevation > 0)
        {
            var tint = ColorScheme.GetElevationTint(elevation);
            using var tintPaint = new SKPaint
            {
                IsAntialias = true,
                Color = tint
            };
            canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, tintPaint);
        }
    }

    /// <summary>
    ///     Draws a smooth gradient overlay for glassmorphism effect
    /// </summary>
    public static void DrawGlassEffect(SKCanvas canvas, SkiaSharp.SKRect bounds, float cornerRadius, float opacity = 0.1f)
    {
        if (ColorScheme.FlatDesign) return;
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(bounds.Left, bounds.Top),
            new SKPoint(bounds.Right, bounds.Bottom),
            new[]
            {
                new SkiaSharp.SKColor(255, 255, 255, (byte)(opacity * 50)),
                new SkiaSharp.SKColor(255, 255, 255, (byte)(opacity * 10))
            },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp
        );

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader
        };

        canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, paint);
    }

    /// <summary>
    ///     Draws modern ripple effect at specified position
    /// </summary>
    public static void DrawRipple(SKCanvas canvas, SKPoint center, float radius, float progress, SKColor color)
    {
        var alpha = (byte)(255 * (1 - progress));
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = color.WithAlpha(alpha)
        };

        canvas.DrawCircle(center, radius * progress, paint);
    }

    /// <summary>
    ///     Creates a smooth state layer for hover/focus/press states
    /// </summary>
    public static void DrawStateLayer(SKCanvas canvas, SkiaSharp.SKRect bounds, float cornerRadius, SKColor stateColor)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = stateColor
        };

        canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, paint);
    }
}