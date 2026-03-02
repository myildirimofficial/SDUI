using SkiaSharp;
using System;

using System.Threading;
using System.Threading.Tasks;

namespace SDUI;

public class ColorScheme
{
    private static bool _isDarkMode;
    private static double _themeTransitionProgress = 1.0;
    private static bool _isTransitioning;
    private static readonly object _lock = new();

    private static SynchronizationContext? _uiContext;
    private static int _themeChangedQueued;

    // Theme background transition (drives surface + derived colors)
    private static SKColor _themeBackgroundFrom = new SKColor(250, 250, 250);
    private static SKColor _themeBackgroundTo = new SKColor(250, 250, 250);
    private static int _themeTransitionId;

    private static double _accentTransitionProgress = 1.0;
    private static bool _isAccentTransitioning;
    private static readonly object _accentLock = new();

    // Accent palette transition (light/dark variants)
    private static SKColor _primaryLightFrom = new (33, 150, 243); // #2196F3
    private static SKColor _primaryLightTo = new(33, 150, 243);
    private static SKColor _primaryDarkFrom = new SKColor(30, 136, 229); // #1E88E5
    private static SKColor _primaryDarkTo = new SKColor(30, 136, 229);

    private static SKColor _primaryContainerLightFrom = new SKColor(227, 242, 253); // #E3F2FD
    private static SKColor _primaryContainerLightTo = new SKColor(227, 242, 253);
    private static SKColor _primaryContainerDarkFrom = new SKColor(25, 118, 210); // #1976D2
    private static SKColor _primaryContainerDarkTo = new SKColor(25, 118, 210);

    private static SKColor _onPrimaryContainerLightFrom = new SKColor(13, 71, 161);
    private static SKColor _onPrimaryContainerLightTo = new SKColor(13, 71, 161);
    private static SKColor _onPrimaryContainerDarkFrom = new SKColor(227, 242, 253);
    private static SKColor _onPrimaryContainerDarkTo = new SKColor(227, 242, 253);

    /// <summary>
    ///     Gets or sets debug borders for layout inspection
    /// </summary>
    public static bool DrawDebugBorders;

    /// <summary>
    ///     When enabled, the UI favors a flat look (no elevation / heavy shadows).
    /// </summary>
    public static bool FlatDesign { get; set; } = true;

    /// <summary>
    ///     Toggle between light and dark theme
    /// </summary>
    public static bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _uiContext ??= SynchronizationContext.Current;

            // Even if the flag is already equal, user intent may be to jump back to the
            // canonical light/dark background (e.g. after random background theme).
            var targetBackground = value ? new SKColor(28, 28, 30) : new SKColor(250, 250, 250);
            if (_isDarkMode == value && ThemeTransitionProgress >= 0.999 &&
                ColorsClose(CurrentThemeBackground, targetBackground))
                return;

            // Smooth transition request
            StartDarkModeTransition(value);
        }
    }

    /// <summary>
    ///     Progress of current accent (primary color) transition in range [0,1].
    /// </summary>
    public static double AccentTransitionProgress
    {
        get => _accentTransitionProgress;
        private set
        {
            _accentTransitionProgress = Math.Clamp(value, 0.0, 1.0);
            RequestThemeChanged();
        }
    }

    /// <summary>
    ///     Progress of current theme transition in range [0,1].
    ///     0 = previous theme, 1 = target theme.
    /// </summary>
    public static double ThemeTransitionProgress
    {
        get => _themeTransitionProgress;
        private set
        {
            _themeTransitionProgress = Math.Clamp(value, 0.0, 1.0);
            RequestThemeChanged();
        }
    }

    private static SKColor CurrentThemeBackground =>
        Lerp(_themeBackgroundFrom, _themeBackgroundTo, ThemeTransitionProgress);

    private static bool CurrentIsDark => RelativeLuminance(CurrentThemeBackground) < 0.45;

    // Surface colors (derived from current transitioning background)
    public static SKColor Surface => CurrentThemeBackground;
    public static SKColor BackColor => CurrentThemeBackground;
    public static SKColor ForeColor => ReadableOn(CurrentThemeBackground);

    public static SKColor SurfaceVariant => SurfaceAdjust(Surface, 0.10);
    public static SKColor SurfaceContainerLowest => SurfaceAdjust(Surface, 0.03);
    public static SKColor SurfaceContainerLow => SurfaceAdjust(Surface, 0.06);
    public static SKColor SurfaceContainer => SurfaceAdjust(Surface, 0.08);
    public static SKColor SurfaceContainerHigh => SurfaceAdjust(Surface, 0.12);

    // Accent colors - DodgerBlue-ish modern palette
    private static SKColor PrimaryLightCurrent => Lerp(_primaryLightFrom, _primaryLightTo, AccentTransitionProgress);
    private static SKColor PrimaryDarkCurrent => Lerp(_primaryDarkFrom, _primaryDarkTo, AccentTransitionProgress);

    private static SKColor PrimaryContainerLightCurrent =>
        Lerp(_primaryContainerLightFrom, _primaryContainerLightTo, AccentTransitionProgress);

    private static SKColor PrimaryContainerDarkCurrent =>
        Lerp(_primaryContainerDarkFrom, _primaryContainerDarkTo, AccentTransitionProgress);

    private static SKColor OnPrimaryContainerLightCurrent => Lerp(_onPrimaryContainerLightFrom,
        _onPrimaryContainerLightTo, AccentTransitionProgress);

    private static SKColor OnPrimaryContainerDarkCurrent => Lerp(_onPrimaryContainerDarkFrom, _onPrimaryContainerDarkTo,
        AccentTransitionProgress);

    public static SKColor Primary => CurrentIsDark ? PrimaryDarkCurrent : PrimaryLightCurrent;
    public static SKColor PrimaryContainer => CurrentIsDark ? PrimaryContainerDarkCurrent : PrimaryContainerLightCurrent;
    public static SKColor OnPrimary => new SKColor(255, 255, 255);

    public static SKColor OnPrimaryContainer =>
        CurrentIsDark ? OnPrimaryContainerDarkCurrent : OnPrimaryContainerLightCurrent;

    public static SKColor Secondary => CurrentIsDark ? new SKColor(120, 144, 156) : new SKColor(96, 125, 139);

    public static SKColor SecondaryContainer =>
        CurrentIsDark ? new SKColor(55, 71, 79) : new SKColor(236, 239, 241);

    public static SKColor Tertiary => CurrentIsDark ? new SKColor(255, 183, 77) : new SKColor(255, 152, 0);

    public static SKColor TertiaryContainer =>
        CurrentIsDark ? new SKColor(239, 108, 0) : new SKColor(255, 243, 224);

    // Semantic colors
    public static SKColor Error => CurrentIsDark ? new SKColor(255, 100, 100) : new SKColor(220, 50, 50);
    public static SKColor ErrorContainer => CurrentIsDark ? new SKColor(100, 40, 40) : new SKColor(255, 230, 230);
    public static SKColor OnError => new SKColor(255, 255, 255);

    public static SKColor Success => CurrentIsDark ? new SKColor(100, 255, 150) : new SKColor(50, 200, 100);
    public static SKColor SuccessContainer => CurrentIsDark ? new SKColor(30, 80, 40) : new SKColor(230, 255, 240);

    public static SKColor Warning => CurrentIsDark ? new SKColor(255, 200, 100) : new SKColor(220, 150, 50);
    public static SKColor WarningContainer => CurrentIsDark ? new SKColor(100, 70, 30) : new SKColor(255, 245, 230);

    // Outlines and borders
    public static SKColor Outline => SurfaceAdjust(Surface, 0.22);
    public static SKColor OutlineVariant => SurfaceAdjust(Surface, 0.16);

    // Shadows and elevation
    public static SKColor Shadow => new SKColor(0, 0, 0);
    public static SKColor Scrim => new SKColor(0, 0, 0);

    // Text colors with proper contrast
    public static SKColor OnBackground => ForeColor;
    public static SKColor OnSurface => ForeColor;
    public static SKColor OnSurfaceVariant => Blend(ForeColor, Surface, 0.35);

    // Interactive states
    public static SKColor StateLayerHover => ForeColor.WithAlpha(8);
    public static SKColor StateLayerFocus => ForeColor.WithAlpha(12);
    public static SKColor StateLayerPressed => ForeColor.WithAlpha(16);
    public static SKColor StateLayerDragged => ForeColor.WithAlpha(16);

    // Legacy aliases for backward compatibility
    public static SKColor AccentColor => Primary;
    public static SKColor PrimaryColor => Primary;
    public static SKColor BorderColor => Outline;
    public static SKColor BackColor2 => SurfaceVariant;
    public static SKColor ShadowColor => FlatDesign ? SKColors.Transparent : Shadow.WithAlpha(30);

    /// <summary>
    ///     Theme transition entry point: animates from current accent to target accent color.
    /// </summary>
    public static void StartThemeTransition(SKColor targetColor)
    {
        _uiContext ??= SynchronizationContext.Current;

        // Random/background-driven theme: adapt foreground + surfaces automatically.
        // Also choose a reasonable accent derived from the background so controls remain visible.
        var targetBackground = targetColor;
        var targetIsDark = RelativeLuminance(targetBackground) < 0.45;
        _isDarkMode = targetIsDark;

        var accentSeed = targetBackground.Brightness(targetIsDark ? 0.35f : -0.35f);
        SetPrimarySeedColor(accentSeed);

        StartThemeBackgroundTransition(targetBackground);
    }

    public static event EventHandler? ThemeChanged;

    /// <summary>
    ///     Immediately sets theme without animation (used internally after transition).
    /// </summary>
    public static void SetThemeInstant(bool dark)
    {
        _uiContext ??= SynchronizationContext.Current;
        _isDarkMode = dark;
        var bg = dark ? new SKColor(28, 28, 30) : new SKColor(250, 250, 250);
        _themeBackgroundFrom = bg;
        _themeBackgroundTo = bg;
        ThemeTransitionProgress = 1.0;
    }

    private static void StartDarkModeTransition(bool targetDark)
    {
        _isDarkMode = targetDark;
        var targetBackground = targetDark ? new SKColor(28, 28, 30) : new SKColor(250, 250, 250);
        _ = StartThemeBackgroundTransition(targetBackground);  // Fire-and-forget async task
    }

    private static void RequestThemeChanged()
    {
        if (Interlocked.Exchange(ref _themeChangedQueued, 1) == 1)
            return;

        void Raise()
        {
            Interlocked.Exchange(ref _themeChangedQueued, 0);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        var ctx = _uiContext;
        if (ctx != null)
            ctx.Post(_ => Raise(), null);
        else
            Raise();
    }

    private static bool ColorsClose(SKColor a, SKColor b, int threshold = 2)
    {
        return Math.Abs(a.Red - b.Red) <= threshold
               && Math.Abs(a.Green - b.Green) <= threshold
               && Math.Abs(a.Blue - b.Blue) <= threshold
               && Math.Abs(a.Alpha - b.Alpha) <= threshold;
    }

    private static SKColor SurfaceAdjust(SKColor baseColor, double amount)
    {
        // For dark backgrounds we lift surfaces slightly, for light we shade slightly.
        return CurrentIsDark
            ? Blend(baseColor, new SKColor(255, 255, 255), amount)
            : Blend(baseColor, new SKColor(0, 0, 0), amount);
    }

    private static async Task StartThemeBackgroundTransition(SKColor targetBackground)
    {
        int transitionId;

        lock (_lock)
        {
            _themeTransitionId++;
            transitionId = _themeTransitionId;

            _themeBackgroundFrom = CurrentThemeBackground;
            _themeBackgroundTo = targetBackground;
            _isTransitioning = true;
            ThemeTransitionProgress = 0.0;
        }
        


        const int durationMs = 220;
        const int stepMs = 16;
        var steps = Math.Max(1, durationMs / stepMs);

        for (var i = 0; i <= steps; i++)
        {
            lock (_lock)
            {
                if (transitionId != _themeTransitionId)
                    return;
            }

            var t = (double)i / steps;
            t = t * t * (3 - 2 * t);
            ThemeTransitionProgress = t;
            await Task.Delay(stepMs);
        }

        ThemeTransitionProgress = 1.0;
        lock (_lock)
        {
            if (transitionId == _themeTransitionId)
                _isTransitioning = false;
        }
    }

    // Helper for smooth color lerp
    private static SKColor Lerp(SKColor from, SKColor to, double t)
    {
        var r = (byte)Math.Round(from.Red + (to.Red - from.Red) * t);
        var g = (byte)Math.Round(from.Green + (to.Green - from.Green) * t);
        var b = (byte)Math.Round(from.Blue + (to.Blue - from.Blue) * t);
        var a = (byte)Math.Round(from.Alpha + (to.Alpha - from.Alpha) * t);
        return new SKColor(a, r, g, b);
    }

    private static SKColor Blend(SKColor a, SKColor b, double t)
    {
        return Lerp(a, b, t);
    }

    private static double RelativeLuminance(SKColor c)
    {
        static double SrgbToLinear(double v)
        {
            v /= 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        var r = SrgbToLinear(c.Red);
        var g = SrgbToLinear(c.Green);
        var b = SrgbToLinear(c.Blue);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static SKColor ReadableOn(SKColor background)
    {
        // Simple WCAG-ish threshold; good enough for our palette.
        return RelativeLuminance(background) < 0.45 ? new SKColor(255, 255, 255) : new SKColor(28, 28, 28);
    }

    private static void ComputePrimaryPalette(SKColor seed,
        out SKColor lightPrimary,
        out SKColor darkPrimary,
        out SKColor lightContainer,
        out SKColor darkContainer,
        out SKColor onLightContainer,
        out SKColor onDarkContainer)
    {
        // Light primary: seed
        lightPrimary = new SKColor(seed.Red, seed.Green, seed.Blue);
        // Dark primary: slightly darkened
        darkPrimary = Blend(lightPrimary, new SKColor(0, 0, 0), 0.12);

        // Containers: light gets a very light tint, dark gets a stronger tint.
        lightContainer = Blend(lightPrimary, new SKColor(255, 255, 255), 0.88);
        darkContainer = Blend(lightPrimary, new SKColor(0, 0, 0), 0.28);

        onLightContainer = ReadableOn(lightContainer);
        onDarkContainer = ReadableOn(darkContainer);
    }

    /// <summary>
    ///     Sets a new primary (accent) seed color with smooth animation.
    /// </summary>
    public static void SetPrimarySeedColor(SKColor seed)
    {
        _uiContext ??= SynchronizationContext.Current;

        // Capture current interpolated palette as the "from" state so we can retarget mid-animation.
        var curPrimaryLight = Lerp(_primaryLightFrom, _primaryLightTo, AccentTransitionProgress);
        var curPrimaryDark = Lerp(_primaryDarkFrom, _primaryDarkTo, AccentTransitionProgress);
        var curContainerLight = Lerp(_primaryContainerLightFrom, _primaryContainerLightTo, AccentTransitionProgress);
        var curContainerDark = Lerp(_primaryContainerDarkFrom, _primaryContainerDarkTo, AccentTransitionProgress);
        var curOnContainerLight =
            Lerp(_onPrimaryContainerLightFrom, _onPrimaryContainerLightTo, AccentTransitionProgress);
        var curOnContainerDark = Lerp(_onPrimaryContainerDarkFrom, _onPrimaryContainerDarkTo, AccentTransitionProgress);

        ComputePrimaryPalette(seed,
            out var nextPrimaryLight,
            out var nextPrimaryDark,
            out var nextContainerLight,
            out var nextContainerDark,
            out var nextOnContainerLight,
            out var nextOnContainerDark);

        lock (_accentLock)
        {
            _primaryLightFrom = curPrimaryLight;
            _primaryDarkFrom = curPrimaryDark;
            _primaryContainerLightFrom = curContainerLight;
            _primaryContainerDarkFrom = curContainerDark;
            _onPrimaryContainerLightFrom = curOnContainerLight;
            _onPrimaryContainerDarkFrom = curOnContainerDark;

            _primaryLightTo = nextPrimaryLight;
            _primaryDarkTo = nextPrimaryDark;
            _primaryContainerLightTo = nextContainerLight;
            _primaryContainerDarkTo = nextContainerDark;
            _onPrimaryContainerLightTo = nextOnContainerLight;
            _onPrimaryContainerDarkTo = nextOnContainerDark;

            _isAccentTransitioning = true;
        }

        StartAccentTransition();  // Fire-and-forget async task
    }

    private static async Task StartAccentTransition()
    {
        lock (_accentLock)
        {
            if (!_isAccentTransitioning)
                _isAccentTransitioning = true;
        }

        const int durationMs = 220;
        const int stepMs = 16;
        var steps = Math.Max(1, durationMs / stepMs);

        for (var i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            t = t * t * (3 - 2 * t);
            AccentTransitionProgress = t;
            await Task.Delay(stepMs);
        }

        AccentTransitionProgress = 1.0;
        lock (_accentLock)
        {
            _isAccentTransitioning = false;
        }
    }

    /// <summary>
    ///     Elevation levels (0-5) returning appropriate shadow/tint
    /// </summary>
    public static SKColor GetElevationTint(int level)
    {
        if (!CurrentIsDark) return SKColors.Transparent;

        return level switch
        {
            0 => SKColors.Transparent,
            1 => new SKColor(5, 255, 255, 255),
            2 => new SKColor(8, 255, 255, 255),
            3 => new SKColor(11, 255, 255, 255),
            4 => new SKColor(14, 255, 255, 255),
            5 => new SKColor(17, 255, 255, 255),
            _ => new SKColor(20, 255, 255, 255)
        };
    }

    /// <summary>
    ///     Returns shadow blur radius for elevation level
    /// </summary>
    public static float GetElevationBlur(int level)
    {
        if (FlatDesign)
            return 0f;

        return level switch
        {
            0 => 0f,
            1 => 2f,
            2 => 4f,
            3 => 8f,
            4 => 12f,
            5 => 16f,
            _ => 20f
        };
    }

    /// <summary>
    ///     Returns shadow offset for elevation level
    /// </summary>
    public static float GetElevationOffset(int level)
    {
        if (FlatDesign)
            return 0f;

        return level switch
        {
            0 => 0f,
            1 => 1f,
            2 => 2f,
            3 => 4f,
            4 => 6f,
            5 => 8f,
            _ => 10f
        };
    }
}