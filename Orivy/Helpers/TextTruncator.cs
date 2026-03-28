using SkiaSharp;

namespace Orivy.Helpers;

internal static class TextTruncator
{
    private const string Ellipsis = "...";

    public static string TruncateText(string text, SKFont font, float maxWidth, TextTrimming trimming)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0 || trimming == TextTrimming.None)
            return text;

        var fullWidth = font.MeasureText(text);
        if (fullWidth <= maxWidth)
            return text;

        var ellipsisWidth = font.MeasureText(Ellipsis);
        var availableWidth = maxWidth - ellipsisWidth;

        if (availableWidth <= 0)
            return Ellipsis;

        if (trimming == TextTrimming.CharacterEllipsis)
            return TruncateByCharacter(text, font, availableWidth);

        return TruncateByWord(text, font, availableWidth);
    }

    private static string TruncateByCharacter(string text, SKFont font, float availableWidth)
    {
        var result = string.Empty;

        foreach (var c in text)
        {
            var testText = result + c;
            var width = font.MeasureText(testText);

            if (width > availableWidth)
                break;

            result = testText;
        }

        return result + Ellipsis;
    }

    private static string TruncateByWord(string text, SKFont font, float availableWidth)
    {
        var words = text.Split(' ');
        var result = string.Empty;

        foreach (var word in words)
        {
            var testText = string.IsNullOrEmpty(result) ? word : result + " " + word;
            var width = font.MeasureText(testText);

            if (width > availableWidth)
                break;

            result = testText;
        }

        if (string.IsNullOrEmpty(result) && words.Length > 0)
            result = TruncateByCharacter(words[0], font, availableWidth).TrimEnd('.', ' ');

        return result + Ellipsis;
    }
}
