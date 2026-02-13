namespace SDUI.Native.Windows;

public enum WindowMessage : uint
{
    WM_DESTROY = 0x0002,
    WM_CLOSE = 0x0010,
    WM_ACTIVATE = 0x0006,
    WM_NCACTIVATE = 0x0086,
    WM_KEYDOWN = 0x0100,
    WM_KEYUP = 0x0101,
    WM_CHAR = 0x0102,
    WM_SYSKEYDOWN = 0x0104,
    WM_SYSKEYUP = 0x0105,
    WM_LBUTTONDOWN = 0x0201,
    WM_LBUTTONUP = 0x0202,
    WM_LBUTTONDBLCLK = 0x0203,
    WM_MOUSEMOVE = 0x0200,
    WM_RBUTTONDOWN = 0x0204,
    WM_RBUTTONUP = 0x0205,
    WM_RBUTTONDBLCLK = 0x0206,
    WM_MBUTTONDOWN = 0x0207,
    WM_MBUTTONUP = 0x0208,
    WM_MBUTTONDBLCLK = 0x0209,
    WM_MOUSEWHEEL = 0x020A,
    WM_MOUSEHWHEEL = 0x020E,
    WM_MOUSEHOVER = 0x02A1,
    WM_MOUSELEAVE = 0x02A3,
    WM_PRINT = 0x0317,
    WM_REFLECT = 0x2000,
    WM_NOFITY = 0x4E,
    WM_SETFOCUS = 0x0007,
    WM_KILLFOCUS = 0x8,
    WM_MOVE = 0x0003,
    WM_SIZE = 0x0005,
    WM_VSCROLL = 0x115,
    WM_HSCROLL = 0x114,
    WM_THEMECHANGED = 0x031A,
    WM_NOTIFY = 0x004E,
    WM_NCLBUTTONDOWN = 0xA1,
    WM_NCPAINT = 0x0085,
    WM_PAINT = 0x000F,
    WM_ERASEBKGND = 0x0014,
    WM_NCHITTEST = 0x84,
    WM_NCCALCSIZE = 0x0083,
    WM_GETMINMAXINFO = 0x0024,
    /// <summary>
    /// The WM_DISPLAYCHANGE message is sent to all windows when the display resolution has changed.
    /// </summary>
    WM_DISPLAYCHANGE = 0x007E,
    /// <summary>
    /// Sent when the effective DPI for a window has changed.
    /// </summary>
    WM_DPICHANGED = 0x02E0,
    /// <summary>
    /// Sent to a window when the window is about to be hidden or shown.
    /// </summary>
    WM_SHOWWINDOW = 0x0018,
    /// <summary>
    /// Sent to a window being destroyed after WM_DESTROY.
    /// </summary>
    WM_NCDESTROY = 0x0082,
    /// <summary>
    /// Sets the large or small icon for a window.
    /// </summary>
    WM_SETICON = 0x0080,
}
