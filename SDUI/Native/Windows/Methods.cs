using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SDUI.Native.Windows;

public partial class Methods
{
    private const string user32 = "user32.dll";
    private const string kernel32 = "kernel32.dll";
    private const string dwmapi = "dwmapi.dll";
    private const string uxtheme = "uxtheme.dll";
    private const string gdi32 = "gdi32.dll";

    [DllImport(user32, SetLastError = true)]
    public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwc);

    [DllImport(user32, SetLastError = true)]
    public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport(user32)]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport(user32)]
    public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport(user32)]
    public static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport(user32)]
    public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport(user32)]
    public static extern void PostQuitMessage(int nExitCode);

    [DllImport(user32)]
    public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SetCursor(IntPtr hCursor);

    [DllImport(user32)]
    public static extern bool ClipCursor(ref Rect lpRect);

    [DllImport(user32)]
    public static extern bool ClipCursor(IntPtr lpRect);

    [DllImport(user32)]
    public static extern bool GetClipCursor(out Rect lpRect);

    [DllImport(kernel32)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport(user32)]
    public static extern bool ReleaseCapture();

    [DllImport(user32)]
    public static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [SecurityCritical]
    [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern NTSTATUS RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

    [DllImport(dwmapi, CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute,
        ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, int cbAttribute);

    [DllImport(dwmapi)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref uint pvAttribute,
        int cbAttribute);

    [DllImport(dwmapi)]
    public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

    [DllImport(uxtheme, ExactSpelling = true)]
    public static extern int DrawThemeParentBackground(IntPtr hWnd, IntPtr hdc, ref SkiaSharp.SKRect pRect);

    [DllImport(user32, EntryPoint = "SendMessageW", SetLastError = true)]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref Rect lParam);

    [DllImport(user32, EntryPoint = "SendMessageW", SetLastError = true)]
    public static extern IntPtr SendMessagePtr(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport(user32, EntryPoint = "PostMessageW", SetLastError = true)]
    public static extern int PostMessage(IntPtr hWnd, int Msg, int wParam, ref IntPtr lParam);

    [DllImport(uxtheme, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

    [DllImport(uxtheme, EntryPoint = "#133", SetLastError = true)]
    internal static extern bool AllowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    
    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport(user32)]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport(user32)]
    public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport(user32, CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport(user32)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("shcore.dll", SetLastError = true)]
    public static extern int SetProcessDpiAwareness(int awareness);

    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);

    public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
}
