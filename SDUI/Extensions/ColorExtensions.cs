using SkiaSharp;
using System;
using System.Runtime.CompilerServices;

namespace SDUI;

public static class ColorExtensions
{
    public static bool IsEmpty(this SKColor color)
    {
        return color == SKColors.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor InterpolateColor(this SKColor start, SKColor end, float progress)
    {
        var t = Math.Clamp(progress, 0f, 1f);

        byte r = (byte)Math.Round(start.Red + (end.Red - start.Red) * t);
        byte g = (byte)Math.Round(start.Green + (end.Green - start.Green) * t);
        byte b = (byte)Math.Round(start.Blue + (end.Blue - start.Blue) * t);
        byte a = (byte)Math.Round(start.Alpha + (end.Alpha - start.Alpha) * t);

        return new SKColor(r, g, b, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor Determine(this SKColor color)
    {
        var value = 0;

        var luminance = (0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue) / 255;

        if (luminance > 0.5)
            value = 0; // bright colors - black font
        else
            value = 255; // dark colors - white font

        return new SKColor((byte)value, (byte)value, (byte)value).WithAlpha(color.Alpha);
    }

    /// <summary>
    ///     Creates color with corrected brightness.
    /// </summary>
    /// <param name="color">Color to correct.</param>
    /// <param name="correctionFactor">
    ///     The brightness correction factor. Must be between -1 and 1.
    ///     Negative values produce darker colors.
    /// </param>
    /// <returns>
    ///     Corrected <see cref="Color" /> structure.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor Brightness(this SKColor color, float factor)
    {
        // factor: -1 .. +1

        float r = color.Red / 255f;
        float g = color.Green / 255f;
        float b = color.Blue / 255f;

        // Gamma expand (sRGB -> linear)
        r = MathF.Pow(r, 2.2f);
        g = MathF.Pow(g, 2.2f);
        b = MathF.Pow(b, 2.2f);

        // Apply brightness
        r = Math.Clamp(r + factor, 0f, 1f);
        g = Math.Clamp(g + factor, 0f, 1f);
        b = Math.Clamp(b + factor, 0f, 1f);

        // Gamma compress (linear -> sRGB)
        r = MathF.Pow(r, 1f / 2.2f);
        g = MathF.Pow(g, 1f / 2.2f);
        b = MathF.Pow(b, 1f / 2.2f);

        return new SKColor(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255),
            color.Alpha
        );
    }

    /// <summary>
    ///     Is the color dark <c>true</c>; otherwise <c>false</c>
    /// </summary>
    /// <param name="color">The color</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDark(this SKColor color)
    {
        return 384 - color.Red - color.Green - color.Blue > 0;
    }

    /// <summary>
    ///     Removes the alpha component of a color.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static SKColor RemoveAlpha(this SKColor color)
    {
        color.WithAlpha(255);
        return color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor BlendWith(this SKColor bg, SKColor fg, double blend)
    {
        double alpha = blend * (fg.Alpha / 255.0);

        double inv = 1 - alpha;

        byte r = (byte)(bg.Red * inv + fg.Red * alpha);
        byte g = (byte)(bg.Green * inv + fg.Green * alpha);
        byte b = (byte)(bg.Blue * inv + fg.Blue * alpha);
        byte a = (byte)(bg.Alpha * inv + fg.Alpha * alpha);

        return new SKColor(r, g, b, a);
    }
}