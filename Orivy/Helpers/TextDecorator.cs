using SkiaSharp;

namespace Orivy.Helpers;

internal static class TextDecorator
{
    public static void DrawDecorations(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont font,
        SKPaint paint,
        TextDecoration decoration,
        float thickness,
        SKColor color)
    {
        if (decoration == TextDecoration.None || string.IsNullOrEmpty(text))
            return;

        var textWidth = font.MeasureText(text);
        font.MeasureText(text, out var bounds);

        var decorationColor = color == SKColors.Transparent ? paint.Color : color;

        using var decorationPaint = new SKPaint
        {
            Color = decorationColor,
            StrokeWidth = thickness,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        if ((decoration & TextDecoration.Underline) != 0)
        {
            var underlineY = y + thickness;
            canvas.DrawLine(x, underlineY, x + textWidth, underlineY, decorationPaint);
        }

        if ((decoration & TextDecoration.Strikethrough) != 0)
        {
            var strikethroughY = y - (bounds.Height / 2);
            canvas.DrawLine(x, strikethroughY, x + textWidth, strikethroughY, decorationPaint);
        }

        if ((decoration & TextDecoration.Overline) != 0)
        {
            var overlineY = y + bounds.Top - thickness;
            canvas.DrawLine(x, overlineY, x + textWidth, overlineY, decorationPaint);
        }
    }
}
