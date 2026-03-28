using System;
using System.Diagnostics;
using System.IO;

namespace Orivy.Helpers;

public static class DebugSettings
{
    private static readonly string s_logFile = Path.Combine(Path.GetTempPath(), "Orivy-render.log");
    public static bool EnableRenderLogging { get; set; } = false;

    public static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:O}] {message}";
            Debug.WriteLine(line);
            try
            {
                Console.WriteLine(line);
            }
            catch
            {
            }

            try
            {
                File.AppendAllText(s_logFile, line + Environment.NewLine);
            }
            catch
            {
            }
        }
        catch
        {
            // swallow any logging errors
        }
    }
}