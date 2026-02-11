using System;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SDUI.Native.Windows;

public partial class Methods
{
    public delegate IntPtr dWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public delegate IntPtr SUBCLASSPROC(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData
    );


    [Flags]
    public enum CDDS
    {
        CDDS_PREPAINT = 0x1,
        CDDS_POSTPAINT = 0x2,
        CDDS_PREERASE = 0x3,
        CDDS_POSTERASE = 0x4,
        CDDS_ITEM = 0x10000,
        CDDS_ITEMPREPAINT = CDDS_ITEM | CDDS_PREPAINT,
        CDDS_ITEMPOSTPAINT = CDDS_ITEM | CDDS_POSTPAINT,
        CDDS_ITEMPREERASE = CDDS_ITEM | CDDS_PREERASE,
        CDDS_ITEMPOSTERASE = CDDS_ITEM | CDDS_POSTERASE,
        CDDS_SUBITEMField = 0x20000
    }


    [Flags]
    public enum CDRF
    {
        CDRF_DODEFAULT = 0x0,
        CDRF_NEWFONT = 0x2,
        CDRF_SKIPDEFAULT = 0x4,
        CDRF_DOERASE = 0x8,
        CDRF_SKIPPOSTPAINT = 0x100,
        CDRF_NOTIFYPOSTPAINT = 0x10,
        CDRF_NOTIFYITEMDRAW = 0x20,
        CDRF_NOTIFYSUBITEMDRAW = 0x20,
        CDRF_NOTIFYPOSTERASE = 0x40
    }


    public const int HT_CAPTION = 0x2;
    public const int CS_DROPSHADOW = 0x00020000;
    public const int CS_HREDRAW = 0x0002;
    public const int CS_VREDRAW = 0x0001;
    public const int WS_MINIMIZEBOX = 0x20000;
    public const int WS_SIZEBOX = 0x00040000;
    public const int WS_SYSMENU = 0x00080000;
    public const int CS_DBLCLKS = 0x8;
    public const int SC_MOVE = 0xF010;
    public const int LVCDI_ITEM = 0x0;
    public const int LVCDI_GROUP = 0x1;
    public const int LVCDI_ITEMSLIST = 0x2;
    public const int LVM_FIRST = 0x1000;

    public const int LVGF_NONE = 0x0;
    public const int LVGF_HEADER = 0x1;
    public const int LVGF_FOOTER = 0x2;
    public const int LVGF_STATE = 0x4;
    public const int LVGF_ALIGN = 0x8;
    public const int LVGF_GROUPID = 0x10;
    public const int LVGF_SUBTITLE = 0x100; // pszSubtitle is valid
    public const int LVGF_TASK = 0x200; // pszTask is valid
    public const int LVGF_DESCRIPTIONTOP = 0x400; // pszDescriptionTop is valid
    public const int LVGF_DESCRIPTIONBOTTOM = 0x800; // pszDescriptionBottom is valid
    public const int LVGF_TITLEIMAGE = 0x1000; // iTitleImage is valid
    public const int LVGF_EXTENDEDIMAGE = 0x2000; // iExtendedImage is valid
    public const int LVGF_ITEMS = 0x4000; // iFirstItem and cItems are valid
    public const int LVGF_SUBSET = 0x8000; // pszSubsetTitle is valid

    public const int
        LVGF_SUBSETITEMS = 0x10000; // readonly, cItems holds count of items in visible subset, iFirstItem is valid

    public const int LVGS_NORMAL = 0x0;
    public const int LVGS_COLLAPSED = 0x1;
    public const int LVGS_HIDDEN = 0x2;
    public const int LVGS_NOHEADER = 0x4;
    public const int LVGS_COLLAPSIBLE = 0x8;
    public const int LVGS_FOCUSED = 0x10;
    public const int LVGS_SELECTED = 0x20;
    public const int LVGS_SUBSETED = 0x40;
    public const int LVGS_SUBSETLINKFOCUSED = 0x80;
    public const int LVGA_HEADER_LEFT = 0x1;
    public const int LVGA_HEADER_CENTER = 0x2;
    public const int LVGA_HEADER_RIGHT = 0x4; // Don't forget to validate exclusivity
    public const int LVGA_FOOTER_LEFT = 0x8;
    public const int LVGA_FOOTER_CENTER = 0x10;
    public const int LVGA_FOOTER_RIGHT = 0x20; // Don't forget to validate exclusivity
    public const int LVGGR_GROUP = 0; // Entire expanded group
    public const int LVGGR_HEADER = 1; // Header only (collapsed group)
    public const int LVGGR_LABEL = 2; // Label only
    public const int LVGGR_SUBSETLINK = 3; // subset link only
    public const int LVS_EX_DOUBLEBUFFER = 0x10000;
    public const int LVM_SETEXTENDEDLISTVIEWSTYLE = 4150;

    public const uint TME_HOVER = 0x00000001;
    public const uint TME_LEAVE = 0x00000002;
    public const uint HOVER_DEFAULT = 0xFFFFFFFF;

    public const int NM_FIRST = 0;
    public const int NM_CLICK = NM_FIRST - 2;
    public const int NM_CUSTOMDRAW = NM_FIRST - 12;
    public const int HDM_FIRST = 0x1200;
    public const int HDM_GETITEM = HDM_FIRST + 11;
    public const int HDM_SETITEM = HDM_FIRST + 12;
    public const int TCM_FIRST = 4864;
    public const int TCM_ADJUSTRECT = TCM_FIRST + 40;
    public const int TCS_MULTILINE = 0x0200;

    /// <summary>
    ///     An uncompressed format.
    /// </summary>
    private const int BI_RGB = 0;

    /// <summary>
    ///     The BITMAPINFO structure contains an array of literal RGB values.
    /// </summary>
    public const int DIB_RGB_COLORS = 0;

    /// <summary>
    ///     Copies the source rectangle directly to the destination rectangle.
    /// </summary>
    public const int SRCCOPY = 0x00CC0020;

    [DllImport("kernel32", EntryPoint = "RtlMoveMemory", SetLastError = false)]
    public static extern void MoveMemory(IntPtr Destination, IntPtr Source, IntPtr Length);

    [DllImport("gdi32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    [ResourceExposure(ResourceScope.None)]
    public static extern int CombineRgn(IntPtr hRgn, IntPtr hRgn1, IntPtr hRgn2, int nCombineMode);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    public static extern bool FillRgn(IntPtr hdc, IntPtr hrgn, IntPtr hbr);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateSolidBrush(uint crColor);


    [DllImport("user32.dll")]
    public static extern IntPtr GetDCEx(IntPtr hwnd, IntPtr hrgnclip, uint fdwOptions);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetRect(Rect rect, int w, int h, int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT
    {
        public uint cbSize;
        public uint dwFlags;
        public IntPtr hwndTrack;
        public uint dwHoverTime;
    }

    [DllImport(user32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref HDITEM lParam);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref COLORREF lParam);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Rect lParam);

    [DllImport(user32)]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);

    [DllImport(dwmapi)]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InvalidateRect(IntPtr hWnd, Rect rect, bool bErase);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, ref Rect rect);

    [DllImport(user32, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);

    [DllImport(user32, SetLastError = true)]
    public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired,
        uint fuLoad);

    [DllImport(user32, SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
        SetWindowPosFlags uFlags);

    [DllImport(user32, EntryPoint = "SetWindowLong", SetLastError = true)]
    public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport(user32, EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    

    [DllImport(user32)]
    public static extern IntPtr GetDC(IntPtr hwnd);

    /// <summary>
    ///     The SaveDC function saves the current state of the specified device context (DC) by copying data describing
    ///     selected objects and graphic modes
    /// </summary>
    /// <param name="hdc"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern int SaveDC(IntPtr hdc);

    [DllImport(user32)]
    public static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport(user32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    public static extern bool SetWindowSubclass(
        IntPtr hWnd,
        IntPtr pfnSubclass,
        UIntPtr uIdSubclass,
        UIntPtr dwRefData
    );

    [DllImport("comctl32.dll", ExactSpelling = true)]
    public static extern bool RemoveWindowSubclass(
        IntPtr hWnd,
        IntPtr pfnSubclass,
        UIntPtr uIdSubclass
    );

    [DllImport("comctl32.dll", ExactSpelling = true)]
    public static extern IntPtr DefSubclassProc(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam
    );

    /// <summary>
    ///     The CreateDIBSection function creates a DIB that applications can write to directly.
    /// </summary>
    /// <param name="hdc"></param>
    /// <param name="pbmi"></param>
    /// <param name="iUsage"></param>
    /// <param name="ppvBits"></param>
    /// <param name="hSection"></param>
    /// <param name="dwOffset"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, IntPtr ppvBits,
        IntPtr hSection, uint dwOffset);

    /// <summary>
    ///     This function transfers pixels from a specified source rectangle to a specified destination rectangle, altering the
    ///     pixels according to the selected raster operation (ROP) code.
    /// </summary>
    /// <param name="hdc"></param>
    /// <param name="nXDest"></param>
    /// <param name="nYDest"></param>
    /// <param name="nWidth"></param>
    /// <param name="nHeight"></param>
    /// <param name="hdcSrc"></param>
    /// <param name="nXSrc"></param>
    /// <param name="nYSrc"></param>
    /// <param name="dwRop"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc,
        int nXSrc, int nYSrc, uint dwRop);

    /// <summary>
    ///     This function selects an object into a specified device context. The new object replaces the previous object of the
    ///     same type.
    /// </summary>
    /// <param name="hDC"></param>
    /// <param name="hObject"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    /// <summary>
    ///     The DeleteObject function deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system
    ///     resources associated with the object. After the object is deleted, the specified handle is no longer valid.
    /// </summary>
    /// <param name="hObject"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    ///     This function creates a memory device context (DC) compatible with the specified device.
    /// </summary>
    /// <param name="hDC"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    /// <summary>
    ///     The DeleteDC function deletes the specified device context (DC).
    /// </summary>
    /// <param name="hdc"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    internal static extern bool DeleteDC(IntPtr hdc);

    private static bool EnumWindow(IntPtr handle, IntPtr pointer)
    {
        var gch = GCHandle.FromIntPtr(pointer);
        var list = gch.Target as List<IntPtr>;
        if (list == null)
            throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
        list.Add(handle);
        return true;
    }

    public static void DisableVisualStylesForFirstChild(IntPtr parent)
    {
        var children = new List<IntPtr>();
        var listHandle = GCHandle.Alloc(children);
        try
        {
            var childProc = new EnumWindowProc(EnumWindow);
            EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            if (children.Count > 0)
                SetWindowTheme(children[0], "", "");
        }
        finally
        {
            if (listHandle.IsAllocated)
                listHandle.Free();
        }
    }

    

    [DllImport(user32)]
    public static extern IntPtr GetWindowLong(IntPtr hWnd, WindowLongIndexFlags nIndex);

    [DllImport(user32)]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, WindowLongIndexFlags nIndex, SetWindowLongFlags newProc);

    public struct MARGINS // struct for box shadow
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    public struct SubclassInfo
    {
        public COLORREF headerTextColor;
    }

    private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

    [Flags]
    internal enum DrawingOptions
    {
        PRF_CHECKVISIBLE = 0x01,
        PRF_NONCLIENT = 0x02,
        PRF_CLIENT = 0x04,
        PRF_ERASEBKGND = 0x08,
        PRF_CHILDREN = 0x10,
        PRF_OWNED = 0x20
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct COLORREF
    {
        public byte R;
        public byte G;
        public byte B;
    }



    

    

    [StructLayout(LayoutKind.Sequential)]
    public struct NMHDR
    {
        public IntPtr hwndFrom;
        public IntPtr idFrom;
        public int code;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int X
        {
            get => Left;
            set
            {
                Right -= Left - value;
                Left = value;
            }
        }

        public int Y
        {
            get => Top;
            set
            {
                Bottom -= Top - value;
                Top = value;
            }
        }

        public int Height
        {
            get => Bottom - Top;
            set => Bottom = value + Top;
        }

        public int Width
        {
            get => Right - Left;
            set => Right = value + Left;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HDITEM
    {
        public Mask mask;
        public int cxy;
        [MarshalAs(UnmanagedType.LPTStr)] public string pszText;
        public IntPtr hbm;
        public int cchTextMax;
        public Format fmt;

        public IntPtr lParam;

        // _WIN32_IE >= 0x0300 
        public int iImage;

        public int iOrder;

        // _WIN32_IE >= 0x0500
        public uint type;

        public IntPtr pvFilter;

        // _WIN32_WINNT >= 0x0600
        public uint state;

        [Flags]
        public enum Mask
        {
            Format = 0x4 // HDI_FORMAT
        }

        [Flags]
        public enum Format
        {
            SortDown = 0x200, // HDF_SORTDOWN
            SortUp = 0x400 // HDF_SORTUP
        }
    }

    /// <summary>
    ///     This structure contains information about the dimensions and color format of a device-independent bitmap (DIB).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public readonly int biSizeImage;
        public readonly int biXPelsPerMeter;
        public readonly int biYPelsPerMeter;
        public readonly int biClrUsed;
        public readonly int biClrImportant;
    }

    /// <summary>
    ///     This structure describes a color consisting of relative intensities of red, green, and blue.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RGBQUAD
    {
        public readonly byte rgbBlue;
        public readonly byte rgbGreen;
        public readonly byte rgbRed;
        public readonly byte rgbReserved;
    }

    /// <summary>
    ///     This structure defines the dimensions and color information of a Windows-based device-independent bitmap (DIB).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public readonly RGBQUAD bmiColors;
    }
}