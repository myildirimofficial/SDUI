using SkiaSharp;
using System;
using System.Collections.Generic;
using Orivy;

namespace Orivy.Helpers;

public static class TextRenderer
{
    private static readonly SKFontManager _fontManager = SKFontManager.Default;
    private static readonly TypefaceCache _fallbackCache = new();
    private static readonly TextMeasurementCache _measurementCache = new();

    private static readonly string[] _fallbackFonts =
    {
        "Segoe UI Emoji",
        "Segoe UI Symbol",
        "Arial Unicode MS",
        "Noto Sans",
        "Noto Color Emoji"
    };

    public static void DrawText(SKCanvas canvas, string? text, float x, float y, SKColor color)
    {
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true
        };

        DrawText(canvas, text, x, y, SKTextAlign.Left, null, paint);
    }

    public static void DrawText(SKCanvas canvas, string? text, float x, float y, SKPaint paint)
    {
        DrawText(canvas, text, x, y, SKTextAlign.Left, null, paint);
    }

    public static void DrawText(SKCanvas canvas, string? text, float x, float y, SKFont font, SKPaint paint)
    {
        DrawText(canvas, text, x, y, SKTextAlign.Left, font, paint);
    }

    public static void DrawText(SKCanvas canvas, string? text, float x, float y, SKTextAlign alignment, SKPaint paint)
    {
        DrawText(canvas, text, x, y, alignment, null, paint);
    }

    public static void DrawText(SKCanvas canvas, string? text, float x, float y, SKTextAlign alignment, SKFont? font, SKPaint paint)
    {
        DrawText(canvas, text, x, y, alignment, font, paint, new TextRenderOptions());
    }

    public static void DrawText(
        SKCanvas canvas,
        string? text,
        float x,
        float y,
        SKTextAlign alignment,
        SKFont? font,
        SKPaint paint,
        TextRenderOptions options)
    {
        if (!ShouldRender(canvas, paint, text))
            return;

        using var disposableFont = font is null ? CreateFontFromPaint(paint) : null;
        var effectiveFont = font ?? disposableFont!;
        using var configuredFont = CreateConfiguredFontCopy(effectiveFont, options);
        effectiveFont = configuredFont ?? effectiveFont;

        // Use text as-is - escape processing should be done at property level, not render level
        var processedText = text!;

        if (options.UseMnemonic && options.Wrap == TextWrap.None && options.Trimming == TextTrimming.None)
        {
            DrawTextWithMnemonicFallback(canvas, processedText, x, y, alignment, effectiveFont, paint);
            if (options.Decoration != TextDecoration.None)
            {
                TextDecorator.DrawDecorations(canvas, processedText.Replace("&", ""), x, y, effectiveFont, paint,
                    options.Decoration, options.DecorationThickness, options.DecorationColor);
            }
            return;
        }

        if (options.Wrap != TextWrap.None)
        {
            DrawWrappedText(canvas, processedText, x, y, alignment, effectiveFont, paint, options);
        }
        else if (options.Trimming != TextTrimming.None && options.MaxWidth < float.MaxValue)
        {
            var truncated = TextTruncator.TruncateText(processedText, effectiveFont, options.MaxWidth, options.Trimming);
            DrawTextWithFallback(canvas, truncated, x, y, alignment, effectiveFont, paint);
            
            if (options.Decoration != TextDecoration.None)
            {
                TextDecorator.DrawDecorations(canvas, truncated, x, y, effectiveFont, paint, 
                    options.Decoration, options.DecorationThickness, options.DecorationColor);
            }
        }
        else
        {
            DrawTextWithFallback(canvas, processedText, x, y, alignment, effectiveFont, paint);
            
            if (options.Decoration != TextDecoration.None)
            {
                TextDecorator.DrawDecorations(canvas, processedText, x, y, effectiveFont, paint, 
                    options.Decoration, options.DecorationThickness, options.DecorationColor);
            }
        }
    }

    private static void DrawTextWithMnemonicFallback(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKTextAlign alignment,
        SKFont font,
        SKPaint paint)
    {
        if (!text.Contains('&'))
        {
            DrawTextWithFallback(canvas, text, x, y, alignment, font, paint);
            return;
        }

        // Build cleaned text and locate mnemonic index in cleaned text
        var cleanBuilder = new System.Text.StringBuilder(text.Length);
        int mnemonicIndex = -1;
        int cleanIndex = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '&')
            {
                // Escaped && -> produce single &
                if (i + 1 < text.Length && text[i + 1] == '&')
                {
                    cleanBuilder.Append('&');
                    i++; // skip next &
                    cleanIndex++;
                    continue;
                }

                // Single & marks mnemonic for next character
                if (i + 1 < text.Length)
                {
                    mnemonicIndex = cleanIndex;
                    continue; // do not append &
                }

                // Trailing &, just append
                cleanBuilder.Append('&');
                cleanIndex++;
                continue;
            }

            cleanBuilder.Append(c);
            cleanIndex++;
        }

        var cleanText = cleanBuilder.ToString();

        // Build runs with fallback detection (same approach as DrawTextWithFallback)
        var runs = new List<(string text, SKTypeface typeface)>();
        var primaryTypeface = font.Typeface ?? SKTypeface.Default;

        var currentRun = string.Empty;
        var currentTypeface = primaryTypeface;

        foreach (var c in cleanText)
        {
            var glyphId = primaryTypeface.GetGlyph(c);
            SKTypeface? requiredTypeface = null;

            if (glyphId == 0)
            {
                requiredTypeface = GetFallbackTypeface(c);
            }

            var charTypeface = requiredTypeface ?? primaryTypeface;

            if (charTypeface != currentTypeface)
            {
                if (currentRun.Length > 0)
                {
                    runs.Add((currentRun, currentTypeface));
                }
                currentRun = c.ToString();
                currentTypeface = charTypeface;
            }
            else
            {
                currentRun += c;
            }
        }

        if (currentRun.Length > 0)
        {
            runs.Add((currentRun, currentTypeface));
        }

        var currentX = x;

        if (alignment == SKTextAlign.Center || alignment == SKTextAlign.Right)
        {
            var totalWidth = 0f;
            foreach (var run in runs)
            {
                using var runFont = CreateRunFont(run.typeface, font);
                totalWidth += runFont.MeasureText(run.text);
            }

            if (alignment == SKTextAlign.Center)
                currentX -= totalWidth / 2;
            else if (alignment == SKTextAlign.Right)
                currentX -= totalWidth;
        }

        // Draw runs and underline mnemonic
        float cumulative = 0f;
        int remainingMnemonic = mnemonicIndex;
        float underlineStart = -1f;
        float underlineEnd = -1f;

        foreach (var run in runs)
        {
            using var runFont = CreateRunFont(run.typeface, font);
            var runText = run.text;

            if (remainingMnemonic >= 0 && remainingMnemonic < runText.Length)
            {
                // mnemonic is inside this run
                var before = runText.Substring(0, remainingMnemonic);
                var mnemonicChar = runText[remainingMnemonic].ToString();
                var beforeWidth = runFont.MeasureText(before);
                var charWidth = runFont.MeasureText(mnemonicChar);

                underlineStart = currentX + beforeWidth;
                underlineEnd = underlineStart + charWidth;
            }

            canvas.DrawText(runText, currentX, y, runFont, paint);
            var w = runFont.MeasureText(runText);
            currentX += w;
            cumulative += w;

            if (remainingMnemonic >= 0)
            {
                remainingMnemonic -= runText.Length;
                if (remainingMnemonic < 0)
                    remainingMnemonic = -999999; // mark handled
            }
        }

        if (underlineStart >= 0 && underlineEnd > underlineStart)
        {
            using var underlinePaint = new SKPaint
            {
                Color = paint.Color,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            // small offset for underline relative to baseline
            canvas.DrawLine(underlineStart, y + 2, underlineEnd, y + 2, underlinePaint);
        }
    }

    /// <summary>
    /// Processes escape sequences in text. Use this at property-set time, NOT during rendering.
    /// </summary>
    public static string ProcessEscapeSequences(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Quick check - avoid processing if no backslash
        if (!text.Contains('\\'))
            return text;

        return TextEscapeProcessor.ProcessEscapeSequences(text);
    }

    private static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKTextAlign alignment,
        SKFont font,
        SKPaint paint,
        TextRenderOptions options)
    {
        var lines = TextWrapper.WrapText(text, font, options.MaxWidth, options.Wrap);
        font.MeasureText("X", out var sampleBounds);
        var lineHeight = sampleBounds.Height * options.LineSpacing;
        var currentY = y;

        foreach (var line in lines)
        {
            if (currentY - y > options.MaxHeight)
                break;

            DrawTextWithFallback(canvas, line, x, currentY, alignment, font, paint);
            
            if (options.Decoration != TextDecoration.None)
            {
                TextDecorator.DrawDecorations(canvas, line, x, currentY, font, paint, 
                    options.Decoration, options.DecorationThickness, options.DecorationColor);
            }

            currentY += lineHeight;
        }
    }

    private static void DrawTextWithFallback(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKTextAlign alignment,
        SKFont font,
        SKPaint paint)
    {
        var currentX = x;
        var runs = new List<(string text, SKTypeface typeface)>();
        var primaryTypeface = font.Typeface ?? SKTypeface.Default;

        var currentRun = string.Empty;
        var currentTypeface = primaryTypeface;

        foreach (var c in text)
        {
            var glyphId = primaryTypeface.GetGlyph(c);
            SKTypeface? requiredTypeface = null;

            if (glyphId == 0)
            {
                requiredTypeface = GetFallbackTypeface(c);
            }

            var charTypeface = requiredTypeface ?? primaryTypeface;

            if (charTypeface != currentTypeface)
            {
                if (currentRun.Length > 0)
                {
                    runs.Add((currentRun, currentTypeface));
                }
                currentRun = c.ToString();
                currentTypeface = charTypeface;
            }
            else
            {
                currentRun += c;
            }
        }

        if (currentRun.Length > 0)
        {
            runs.Add((currentRun, currentTypeface));
        }

        if (alignment == SKTextAlign.Center || alignment == SKTextAlign.Right)
        {
            var totalWidth = 0f;
            foreach (var run in runs)
            {
                using var runFont = CreateRunFont(run.typeface, font);
                totalWidth += runFont.MeasureText(run.text);
            }

            if (alignment == SKTextAlign.Center)
                currentX -= totalWidth / 2;
            else if (alignment == SKTextAlign.Right)
                currentX -= totalWidth;
        }

        foreach (var run in runs)
        {
            using var runFont = CreateRunFont(run.typeface, font);
            canvas.DrawText(run.text, currentX, y, runFont, paint);
            currentX += runFont.MeasureText(run.text);
        }
    }

    private static SKFont CreateRunFont(SKTypeface typeface, SKFont baseFont)
    {
        return new SKFont(typeface, baseFont.Size)
        {
            Edging = baseFont.Edging,
            Hinting = baseFont.Hinting,
            Subpixel = baseFont.Subpixel
        };
    }

    private static SKTypeface GetFallbackTypeface(char c)
    {
        var codepoint = (int)c;

        return _fallbackCache.GetOrAdd(codepoint, () =>
        {
            foreach (var fontFamily in _fallbackFonts)
            {
                var typeface = SKTypeface.FromFamilyName(fontFamily);
                if (typeface != null && typeface.GetGlyph(c) != 0)
                {
                    return typeface;
                }
                typeface?.Dispose();
            }

            var matched = _fontManager.MatchCharacter(codepoint);
            if (matched != null)
            {
                return matched;
            }

            return SKTypeface.Default;
        });
    }

    public static SKRect MeasureText(string text, SKFont font)
    {
        return _measurementCache.GetOrMeasure(text, font, () =>
        {
            font.MeasureText(text, out var bounds);
            return bounds;
        });
    }

    public static float MeasureTextWidth(string text, SKFont font)
    {
        return font.MeasureText(text);
    }

    /// <summary>
    /// Measures text with the specified font and constraints.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="font">The font to use for measurement</param>
    /// <param name="proposedSize">The proposed size constraints</param>
    /// <returns>The measured size of the text</returns>
    public static SKSize MeasureText(string? text, SKFont? font, SKSize proposedSize)
    {
        return MeasureText(text, font, proposedSize, new TextRenderOptions());
    }

    /// <summary>
    /// Measures text with the specified font, constraints, and render options.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="font">The font to use for measurement</param>
    /// <param name="proposedSize">The proposed size constraints</param>
    /// <param name="options">Text rendering options</param>
    /// <returns>The measured size of the text</returns>
    public static SKSize MeasureText(string? text, SKFont? font, SKSize proposedSize, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return SKSize.Empty;

        font ??= Application.DefaultFont;

        using var configuredFont = CreateConfiguredFontCopy(font, options);
        var effectiveFont = configuredFont ?? font;

        return MeasureTextWithOptions(text, effectiveFont, proposedSize, options);
    }

    private static SKFont? CreateConfiguredFontCopy(SKFont font, TextRenderOptions options)
    {
        if (!RequiresFontConfiguration(font, options))
            return null;

        var configuredFont = font.CloneFont();
        ApplyFontOptions(configuredFont, options);
        return configuredFont;
    }

    private static bool RequiresFontConfiguration(SKFont font, TextRenderOptions options)
    {
        return (options.Subpixel.HasValue && options.Subpixel.Value != font.Subpixel)
               || (options.Edging.HasValue && options.Edging.Value != font.Edging)
               || (options.Hinting.HasValue && options.Hinting.Value != font.Hinting);
    }

    private static void ApplyFontOptions(SKFont font, TextRenderOptions options)
    {
        if (options.Subpixel.HasValue)
            font.Subpixel = options.Subpixel.Value;

        if (options.Edging.HasValue)
            font.Edging = options.Edging.Value;

        if (options.Hinting.HasValue)
            font.Hinting = options.Hinting.Value;
    }

    /// <summary>
    /// Measures text with SKFont and render options.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="font">The SKFont to use for measurement</param>
    /// <param name="proposedSize">The proposed size constraints</param>
    /// <param name="options">Text rendering options</param>
    /// <returns>The measured size of the text</returns>
    public static SKSize MeasureTextWithOptions(string text, SKFont font, SKSize proposedSize, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return SKSize.Empty;

        // Apply constraints from options
        var maxWidth = options.MaxWidth < float.MaxValue ? options.MaxWidth : proposedSize.Width;
        var maxHeight = options.MaxHeight < float.MaxValue ? options.MaxHeight : proposedSize.Height;

        // Handle text wrapping
        if (options.Wrap != TextWrap.None && maxWidth < float.MaxValue)
        {
            return MeasureWrappedText(text, font, maxWidth, maxHeight, options);
        }

        // Handle text trimming
        if (options.Trimming != TextTrimming.None && maxWidth < float.MaxValue)
        {
            var truncated = TextTruncator.TruncateText(text, font, maxWidth, options.Trimming);
            font.MeasureText(truncated, out var bounds);
            return new SKSize(bounds.Width, bounds.Height);
        }

        // Simple measurement
        font.MeasureText(text, out var simpleBounds);
        return new SKSize(simpleBounds.Width, simpleBounds.Height);
    }

    /// <summary>
    /// Measures wrapped text.
    /// </summary>
    private static SKSize MeasureWrappedText(string text, SKFont font, float maxWidth, float maxHeight, TextRenderOptions options)
    {
        var lines = TextWrapper.WrapText(text, font, maxWidth, options.Wrap);
        
        font.MeasureText("X", out var sampleBounds);
        var lineHeight = sampleBounds.Height * options.LineSpacing;

        float totalHeight = 0;
        float maxLineWidth = 0;
        int lineCount = 0;

        foreach (var line in lines)
        {
            if (totalHeight + lineHeight > maxHeight)
                break;

            font.MeasureText(line, out var lineBounds);
            maxLineWidth = Math.Max(maxLineWidth, lineBounds.Width);
            totalHeight += lineHeight;
            lineCount++;
        }

        return new SKSize(maxLineWidth, totalHeight);
    }

    public static void ClearCaches()
    {
        _measurementCache.Clear();
    }

    private static SKFont CreateFontFromPaint(SKPaint paint)
    {
#pragma warning disable CS0618
        var font = new SKFont(paint.Typeface ?? SKTypeface.Default, paint.TextSize);
#pragma warning restore CS0618
        font.Edging = SKFontEdging.SubpixelAntialias;
        font.Subpixel = true;
        font.Hinting = SKFontHinting.Full;
        return font;
    }

    private static bool ShouldRender(SKCanvas canvas, SKPaint paint, string? text)
    {
        return canvas is not null && paint is not null && !string.IsNullOrEmpty(text);
    }
}
