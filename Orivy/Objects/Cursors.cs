using System;
using System.Runtime.InteropServices;

namespace Orivy;

/// <summary>
/// Provides commonly used system cursors as Orivy `Cursor` instances.
/// On Windows these are loaded via `LoadCursor`, on other platforms they are represented as named cursors (handle = IntPtr.Zero).
/// </summary>
public static class Cursors
{
    // Windows IDC_* constants (MAKEINTRESOURCE values)
    private static readonly IntPtr IDC_ARROW = (IntPtr)32512;
    private static readonly IntPtr IDC_IBEAM = (IntPtr)32513;
    private static readonly IntPtr IDC_WAIT = (IntPtr)32514;
    private static readonly IntPtr IDC_CROSS = (IntPtr)32515;
    private static readonly IntPtr IDC_UPARROW = (IntPtr)32516;
    private static readonly IntPtr IDC_SIZENWSE = (IntPtr)32642;
    private static readonly IntPtr IDC_SIZENESW = (IntPtr)32643;
    private static readonly IntPtr IDC_SIZEWE = (IntPtr)32644;
    private static readonly IntPtr IDC_SIZENS = (IntPtr)32645;
    private static readonly IntPtr IDC_SIZEALL = (IntPtr)32646;
    private static readonly IntPtr IDC_NO = (IntPtr)32648;
    private static readonly IntPtr IDC_HAND = (IntPtr)32649;

    public static readonly Cursor Default = CreateSystemCursor(IDC_ARROW, "Default");
    public static readonly Cursor Arrow = Default;
    public static readonly Cursor IBeam = CreateSystemCursor(IDC_IBEAM, "IBeam");
    public static readonly Cursor Hand = CreateSystemCursor(IDC_HAND, "Hand");
    public static readonly Cursor Help = CreateSystemCursor(IDC_ARROW, "Help");
    public static readonly Cursor SizeAll = CreateSystemCursor(IDC_SIZEALL, "SizeAll");
    public static readonly Cursor SizeNESW = CreateSystemCursor(IDC_SIZENESW, "SizeNESW");
    public static readonly Cursor SizeNS = CreateSystemCursor(IDC_SIZENS, "SizeNS");
    public static readonly Cursor SizeNWSE = CreateSystemCursor(IDC_SIZENWSE, "SizeNWSE");
    public static readonly Cursor SizeWE = CreateSystemCursor(IDC_SIZEWE, "SizeWE");
    public static readonly Cursor Wait = CreateSystemCursor(IDC_WAIT, "Wait");
    public static readonly Cursor No = CreateSystemCursor(IDC_NO, "No");
    public static readonly Cursor UpArrow = CreateSystemCursor(IDC_UPARROW, "UpArrow");

    private static Cursor CreateSystemCursor(IntPtr id, string name)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var h = LoadCursor(IntPtr.Zero, id);
                if (h != IntPtr.Zero)
                    return Cursor.CreateSystem(h, name);
            }
            catch
            {
                // fall through to fallback
            }
        }

        // fallback: no handle available on this platform; name is preserved
        return Cursor.CreateSystem(IntPtr.Zero, name);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
}
