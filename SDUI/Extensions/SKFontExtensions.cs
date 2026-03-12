using System;
using SkiaSharp;

namespace SDUI;

public static class SKFontExtensions
{
    public static bool FontEquals(this SKFont? left, SKFont? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Size == right.Size
               && left.Subpixel == right.Subpixel
               && left.Edging == right.Edging
               && left.Hinting == right.Hinting
               && left.Embolden == right.Embolden
               && left.ScaleX == right.ScaleX
               && left.SkewX == right.SkewX
               && left.LinearMetrics == right.LinearMetrics
               && left.Typeface?.FamilyName == right.Typeface?.FamilyName
               && left.Typeface?.FontStyle.Weight == right.Typeface?.FontStyle.Weight
               && left.Typeface?.FontStyle.Width == right.Typeface?.FontStyle.Width
               && left.Typeface?.FontStyle.Slant == right.Typeface?.FontStyle.Slant;
    }

    public static SKFont CloneFont(this SKFont font)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        return new SKFont(font.Typeface ?? SKTypeface.Default, font.Size)
        {
            Subpixel = font.Subpixel,
            Edging = font.Edging,
            Hinting = font.Hinting,
            Embolden = font.Embolden,
            ScaleX = font.ScaleX,
            SkewX = font.SkewX,
            LinearMetrics = font.LinearMetrics,
        };
    }

    public static string GetFamilyName(this SKFont font)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        return font.Typeface?.FamilyName ?? "Segoe UI";
    }

    public static bool IsBold(this SKFont font)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        var weight = font.Typeface?.FontStyle.Weight ?? (int)SKFontStyleWeight.Normal;
        return weight >= (int)SKFontStyleWeight.Bold;
    }

    public static bool IsItalic(this SKFont font)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));

        return (font.Typeface?.FontStyle.Slant ?? SKFontStyleSlant.Upright) == SKFontStyleSlant.Italic;
    }
}