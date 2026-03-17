using SkiaSharp;
using System;

namespace SDUI;

public static class ColorExtensions
{
    public static bool IsEmpty(this SKColor color)
    {
        return color == SKColors.Empty;
    }

    public static SKColor InterpolateColor(this SKColor start, SKColor end, float progress)
    {
        var r = (byte)(start.Red + (end.Red - start.Red) * progress);
        var g = (byte)(start.Green + (end.Green - start.Green) * progress);
        var b = (byte)(start.Blue + (end.Blue - start.Blue) * progress);
        var a = (byte)(start.Alpha + (end.Alpha - start.Alpha) * progress);
        return new SKColor(r, g, b, a);
    }

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

    public static SKColor BlendWith(this SKColor backgroundColor, SKColor frontColor, double blend)
    {
        var ratio = blend / 255d;
        var invRatio = 1d - ratio;
        byte r = (byte)Math.Clamp((int)(backgroundColor.Red * invRatio + frontColor.Red * ratio), 0, 255);
        byte g = (byte)Math.Clamp((int)(backgroundColor.Green * invRatio + frontColor.Green * ratio), 0, 255);
        byte b = (byte)Math.Clamp((int)(backgroundColor.Blue * invRatio + frontColor.Blue * ratio), 0, 255);
        byte a = (byte)Math.Clamp((int)Math.Abs(frontColor.Alpha - backgroundColor.Alpha), 0, 255);


        return new SKColor(r, g, b, a);
    }
}