using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SDUI.Native.Windows;
using SkiaSharp;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Helpers;

/// <summary>
/// Represents a display device or multiple display devices on a single system.
/// Provides DPI-aware screen metrics and multi-monitor support.
/// </summary>
public sealed class Screen
{
    private static Screen[] _allScreens;
    private static Screen _primaryScreen;
    private static readonly object _lockObject = new object();

    private readonly IntPtr _hMonitor;
    private readonly string _deviceName;
    private readonly SKRectI _bounds;
    private readonly SKRectI _workingArea;
    private readonly bool _isPrimary;
    private readonly uint _dpiX;
    private readonly uint _dpiY;

    private Screen(IntPtr hMonitor, MONITORINFOEX info)
    {
        _hMonitor = hMonitor;
        _deviceName = info.szDevice;
        
        var monitorRect = info.rcMonitor;
        _bounds = new SKRectI(
            monitorRect.Left,
            monitorRect.Top,
            monitorRect.Right,
            monitorRect.Bottom
        );
        
        var workRect = info.rcWork;
        _workingArea = new SKRectI(
            workRect.Left,
            workRect.Top,
            workRect.Right,
            workRect.Bottom
        );
        
        _isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

        // Get DPI for this monitor
        try
        {
            if (GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out _dpiX, out _dpiY) != 0)
            {
                _dpiX = 96;
                _dpiY = 96;
            }
        }
        catch
        {
            _dpiX = 96;
            _dpiY = 96;
        }
    }

    /// <summary>
    /// Gets the monitor handle for this screen.
    /// </summary>
    public IntPtr MonitorHandle => _hMonitor;

    /// <summary>
    /// Gets the device name associated with a display.
    /// </summary>
    public string DeviceName => _deviceName;

    /// <summary>
    /// Gets the bounds of the display in pixels (physical coordinates).
    /// </summary>
    public SKRectI Bounds => _bounds;

    /// <summary>
    /// Gets the working area of the display (excludes taskbars, docked windows, and toolbars).
    /// </summary>
    public SKRectI WorkingArea => _workingArea;

    /// <summary>
    /// Gets a value indicating whether this display is the primary device.
    /// </summary>
    public bool IsPrimary => _isPrimary;

    /// <summary>
    /// Gets the horizontal DPI of the screen.
    /// </summary>
    public uint DpiX => _dpiX;

    /// <summary>
    /// Gets the vertical DPI of the screen.
    /// </summary>
    public uint DpiY => _dpiY;

    /// <summary>
    /// Gets the DPI scale factor (relative to 96 DPI).
    /// </summary>
    public float ScaleFactor => _dpiX / 96f;

    /// <summary>
    /// Gets the primary display screen.
    /// </summary>
    public static Screen PrimaryScreen
    {
        get
        {
            lock (_lockObject)
            {
                if (_primaryScreen == null)
                {
                    RefreshScreens();
                }
                return _primaryScreen;
            }
        }
    }

    /// <summary>
    /// Gets an array of all displays on the system.
    /// </summary>
    public static Screen[] AllScreens
    {
        get
        {
            lock (_lockObject)
            {
                if (_allScreens == null)
                {
                    RefreshScreens();
                }
                return _allScreens;
            }
        }
    }

    /// <summary>
    /// Retrieves a Screen for the display that contains the specified window.
    /// </summary>
    public static Screen FromHandle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return PrimaryScreen;

        IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        return FromMonitor(hMonitor);
    }

    /// <summary>
    /// Retrieves a Screen for the display that contains the specified point.
    /// </summary>
    public static Screen FromPoint(SKPoint point)
    {
        var pt = new POINT { X = (int)point.X, Y = (int)point.Y };
        IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        return FromMonitor(hMonitor);
    }

    /// <summary>
    /// Retrieves a Screen for the display that contains the largest portion of the rectangle.
    /// </summary>
    public static Screen FromRectangle(SKRect rect)
    {
        // Use center point as approximation
        var center = new SKPoint(rect.MidX, rect.MidY);
        return FromPoint(center);
    }

    /// <summary>
    /// Gets the working area of the primary screen in DPI-scaled logical units.
    /// </summary>
    public static SKRect GetWorkingArea()
    {
        return PrimaryScreen.GetLogicalWorkingArea();
    }

    /// <summary>
    /// Gets the working area in DPI-scaled logical units.
    /// </summary>
    public SKRect GetLogicalWorkingArea()
    {
        if (ScaleFactor <= 1f)
            return new SKRect(_workingArea.Left, _workingArea.Top, _workingArea.Right, _workingArea.Bottom);

        return new SKRect(
            _workingArea.Left / ScaleFactor,
            _workingArea.Top / ScaleFactor,
            _workingArea.Right / ScaleFactor,
            _workingArea.Bottom / ScaleFactor
        );
    }

    /// <summary>
    /// Gets the bounds in DPI-scaled logical units.
    /// </summary>
    public SKRect GetLogicalBounds()
    {
        if (ScaleFactor <= 1f)
            return new SKRect(_bounds.Left, _bounds.Top, _bounds.Right, _bounds.Bottom);

        return new SKRect(
            _bounds.Left / ScaleFactor,
            _bounds.Top / ScaleFactor,
            _bounds.Right / ScaleFactor,
            _bounds.Bottom / ScaleFactor
        );
    }

    /// <summary>
    /// Refreshes the screen cache. Call this when display configuration changes.
    /// </summary>
    public static void RefreshScreens()
    {
        lock (_lockObject)
        {
            var screens = new List<Screen>();
            Screen primary = null;

            MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
            {
                var info = new MONITORINFOEX();
                info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFOEX));

                if (GetMonitorInfo(hMonitor, ref info))
                {
                    var screen = new Screen(hMonitor, info);
                    screens.Add(screen);

                    if (screen.IsPrimary)
                        primary = screen;
                }

                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            _allScreens = screens.ToArray();
            _primaryScreen = primary ?? (screens.Count > 0 ? screens[0] : null);
        }
    }

    private static Screen FromMonitor(IntPtr hMonitor)
    {
        if (hMonitor == IntPtr.Zero)
            return PrimaryScreen;

        var screens = AllScreens;
        foreach (var screen in screens)
        {
            if (screen._hMonitor == hMonitor)
                return screen;
        }

        return PrimaryScreen;
    }

    public override string ToString()
    {
        return $"Screen[{DeviceName}, Bounds={Bounds}, DPI={DpiX}x{DpiY}, Primary={IsPrimary}]";
    }

    #region DPI Helpers

    private const int LOGPIXELSX = 88;
    private const int LOGPIXELSY = 90;

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Gets the system-wide DPI (primary monitor, legacy fallback).
    /// </summary>
    public static int GetSystemDpi()
    {
        var screenDC = GetDC(IntPtr.Zero);
        try
        {
            return Math.Max(
                GetDeviceCaps(screenDC, LOGPIXELSX),
                GetDeviceCaps(screenDC, LOGPIXELSY));
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, screenDC);
        }
    }

    /// <summary>
    /// Gets the effective DPI for a specific window handle.
    /// Falls back to monitor DPI, then system DPI.
    /// </summary>
    public static int GetDpiForWindowHandle(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            try
            {
                return checked((int)GetDpiForWindow(handle));
            }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
        }

        var monitor = handle != IntPtr.Zero
            ? MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST)
            : IntPtr.Zero;

        if (monitor != IntPtr.Zero)
        {
            try
            {
                if (GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out var dpiX, out _) == 0)
                    return (int)dpiX;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        return GetSystemDpi();
    }

    /// <summary>
    /// Gets the scale factor for a specific window handle (1.0 = 96 DPI).
    /// </summary>
    public static float GetScaleFactorForWindow(IntPtr handle)
    {
        return GetDpiForWindowHandle(handle) / 96f;
    }

    #endregion
}
