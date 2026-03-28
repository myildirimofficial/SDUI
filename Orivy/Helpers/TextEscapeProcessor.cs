using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Orivy.Helpers;

internal static class TextEscapeProcessor
{
    private static readonly Regex UnicodeEscapeRegex = new(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);
    private static readonly Regex HexEscapeRegex = new(@"\\x([0-9A-Fa-f]{1,4})", RegexOptions.Compiled);

    public static string ProcessEscapeSequences(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                var nextChar = text[i + 1];
                
                switch (nextChar)
                {
                    case 'n':
                        result.Append('\n');
                        i += 2;
                        continue;
                    
                    case 't':
                        result.Append('\t');
                        i += 2;
                        continue;
                    
                    case 'r':
                        result.Append('\r');
                        i += 2;
                        continue;
                    
                    case '\\':
                        result.Append('\\');
                        i += 2;
                        continue;
                    
                    case '"':
                        result.Append('"');
                        i += 2;
                        continue;
                    
                    case '\'':
                        result.Append('\'');
                        i += 2;
                        continue;
                    
                    case 'b':
                        result.Append('\b');
                        i += 2;
                        continue;
                    
                    case 'f':
                        result.Append('\f');
                        i += 2;
                        continue;
                    
                    case 'v':
                        result.Append('\v');
                        i += 2;
                        continue;
                    
                    case '0':
                        result.Append('\0');
                        i += 2;
                        continue;
                    
                    case 'u':
                        if (TryParseUnicodeEscape(text, i, out var unicodeChar, out var unicodeLength))
                        {
                            result.Append(unicodeChar);
                            i += unicodeLength;
                            continue;
                        }
                        break;
                    
                    case 'x':
                        if (TryParseHexEscape(text, i, out var hexChar, out var hexLength))
                        {
                            result.Append(hexChar);
                            i += hexLength;
                            continue;
                        }
                        break;
                }
            }

            result.Append(text[i]);
            i++;
        }

        return result.ToString();
    }

    private static bool TryParseUnicodeEscape(string text, int startIndex, out char result, out int length)
    {
        result = '\0';
        length = 0;

        if (startIndex + 5 >= text.Length)
            return false;

        var escapeSequence = text.Substring(startIndex, 6);
        var match = UnicodeEscapeRegex.Match(escapeSequence);

        if (!match.Success)
            return false;

        var hexValue = match.Groups[1].Value;
        if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
        {
            result = (char)codePoint;
            length = 6;
            return true;
        }

        return false;
    }

    private static bool TryParseHexEscape(string text, int startIndex, out char result, out int length)
    {
        result = '\0';
        length = 0;

        var maxLength = Math.Min(6, text.Length - startIndex);
        if (maxLength < 3)
            return false;

        for (var len = maxLength; len >= 3; len--)
        {
            var escapeSequence = text.Substring(startIndex, len);
            var match = HexEscapeRegex.Match(escapeSequence);

            if (match.Success)
            {
                var hexValue = match.Groups[1].Value;
                if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                {
                    result = (char)codePoint;
                    length = len;
                    return true;
                }
            }
        }

        return false;
    }

    public static string UnescapeString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = ProcessEscapeSequences(text);
        
        text = UnicodeEscapeRegex.Replace(text, m =>
        {
            var hexValue = m.Groups[1].Value;
            var codePoint = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            return char.ConvertFromUtf32(codePoint);
        });

        text = HexEscapeRegex.Replace(text, m =>
        {
            var hexValue = m.Groups[1].Value;
            var codePoint = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            return char.ConvertFromUtf32(codePoint);
        });

        return text;
    }
}
