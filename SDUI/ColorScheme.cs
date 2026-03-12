using SkiaSharp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SDUI;

public static class ColorScheme
{
    private static SKColor _backgroundFrom = new(250, 250, 250);
    private static SKColor _backgroundTo = new(250, 250, 250);

    private static double _progress = 1.0;
    private static bool _isDark;

    private static int _transitionId;

    private static int _themeQueued;

    public static bool FlatDesign { get; set; } = true;
    public static bool DrawDebugBorders;

    public static event EventHandler? ThemeChanged;

    public static bool IsDarkMode
    {
        get => _isDark;
        set
        {
            var target = value
                ? new SKColor(28, 28, 30)
                : new SKColor(250, 250, 250);

            if (_isDark == value && ColorsClose(CurrentBackground, target))
                return;

            StartBackgroundTransition(target);
        }
    }

    private static SKColor CurrentBackground
        => Lerp(_backgroundFrom, _backgroundTo, _progress);

    public static SKColor Surface => CurrentBackground;
    public static SKColor BackColor => CurrentBackground;
    public static SKColor ForeColor => Surface.Determine();

    public static SKColor SurfaceVariant => SurfaceAdjust(Surface, .10);
    public static SKColor SurfaceContainer => SurfaceAdjust(Surface, .08);
    public static SKColor SurfaceContainerHigh => SurfaceAdjust(Surface, .12);

    private static SKColor _primary = new(33,150,243);

    public static SKColor Primary => _primary;

    public static SKColor Outline => SurfaceAdjust(Surface, .22);
    public static SKColor BorderColor => Outline;

    public static SKColor Error => _isDark
        ? new SKColor(255,100,100)
        : new SKColor(220,50,50);

    public static SKColor Success => _isDark
        ? new SKColor(100,255,150)
        : new SKColor(50,200,100);

    public static SKColor Warning => _isDark
        ? new SKColor(255,200,100)
        : new SKColor(220,150,50);

    public static SKColor ShadowColor
        => FlatDesign ? SKColors.Transparent : SKColors.Black.WithAlpha(30);

    public static void SetPrimarySeedColor(SKColor seed)
    {
        _primary = seed;
        RaiseThemeChanged();
    }

    public static void SetThemeInstant(bool dark)
    {
        _isDark = dark;

        var bg = dark
            ? new SKColor(28,28,30)
            : new SKColor(250,250,250);

        _backgroundFrom = bg;
        _backgroundTo = bg;

        _progress = 1;

        RaiseThemeChanged();
    }

    public static void StartThemeTransition(SKColor targetBackground)
    {
        _isDark = RelativeLuminance(targetBackground) < .45;

        var accent = targetBackground.Brightness(_isDark ? .35f : -.35f);

        SetPrimarySeedColor(accent);

        StartBackgroundTransition(targetBackground);
    }

    private static async void StartBackgroundTransition(SKColor target)
    {
        var id = ++_transitionId;

        _backgroundFrom = CurrentBackground;
        _backgroundTo = target;

        const int duration = 220;
        const int step = 16;

        var steps = duration / step;

        for (int i = 0; i <= steps; i++)
        {
            if (id != _transitionId)
                return;

            var t = (double)i / steps;
            t = t * t * (3 - 2 * t);

            _progress = t;

            _isDark = RelativeLuminance(CurrentBackground) < .45;

            RaiseThemeChanged();

            await Task.Delay(step);
        }

        _progress = 1;
    }

    private static void RaiseThemeChanged()
    {
        if (Interlocked.Exchange(ref _themeQueued, 1) == 1)
            return;

        ThemeChanged?.Invoke(null, EventArgs.Empty);

        Interlocked.Exchange(ref _themeQueued, 0);
    }

    private static SKColor SurfaceAdjust(SKColor baseColor, double amount)
    {
        return _isDark
            ? Blend(baseColor, SKColors.White, amount)
            : Blend(baseColor, SKColors.Black, amount);
    }

    private static SKColor Blend(SKColor a, SKColor b, double t)
        => Lerp(a, b, t);

    private static SKColor Lerp(SKColor a, SKColor b, double t)
    {
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    private static double RelativeLuminance(SKColor c)
    {
        static double Linear(double v)
        {
            v /= 255.0;
            return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
        }

        var r = Linear(c.Red);
        var g = Linear(c.Green);
        var b = Linear(c.Blue);

        return .2126 * r + .7152 * g + .0722 * b;
    }

    private static bool ColorsClose(SKColor a, SKColor b)
    {
        const int t = 2;

        return Math.Abs(a.Red - b.Red) <= t
        && Math.Abs(a.Green - b.Green) <= t
        && Math.Abs(a.Blue - b.Blue) <= t;
    }

    public static SKColor GetElevationTint(int level)
    {
        if (!_isDark)
            return SKColors.Transparent;

        return SKColors.White.WithAlpha((byte)(level * 4 + 4));
    }

    public static float GetElevationBlur(int level)
    {
        if (FlatDesign)
            return 0;

        return level switch
        {
            1 => 2,
            2 => 4,
            3 => 8,
            4 => 12,
            5 => 16,
            _ => 20
        };
    }

    public static float GetElevationOffset(int level)
    {
        if (FlatDesign)
            return 0;

        return level * 2;
    }
}