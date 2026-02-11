using SkiaSharp;
using System;
using static SDUI.Native.Windows.Methods;

namespace SDUI;

/// <summary>
/// Lightweight wrapper around a platform cursor handle. We avoid WinForms types so SDUI can be used
/// outside of System.Windows.Forms contexts. System cursors are not disposed by SDUI (we don't own them).
/// </summary>
public sealed class Cursor : IDisposable
{
    public IntPtr Handle { get; }
    public string Name { get; }
    public bool IsSystem { get; }
    public SKPoint Position { get; set; }

    internal Cursor(IntPtr handle, string name, bool isSystem = true)
    {
        Handle = handle;
        Name = name ?? "Cursor";
        IsSystem = isSystem;
    }

    /// <summary>
    /// Create a cursor for a custom handle (SDUI does not own the handle by default).
    /// </summary>
    public static Cursor FromHandle(IntPtr handle, string name = "Handle") => new Cursor(handle, name, isSystem: false);

    internal static Cursor CreateSystem(IntPtr handle, string name) => new Cursor(handle, name, isSystem: true);

    public void Dispose()
    {
        // We do not dispose system cursors. If in future we add ownership semantics for custom cursors, do it here.
    }

    /// <summary>
    /// Gets or sets the cursor clipping rectangle in screen coordinates.
    /// Setting to null or SKRectI.Empty removes clipping restrictions.
    /// </summary>
    public static SKRectI? Clip
    {
        get
        {
            if (GetClipCursor(out var rect))
            {
                return SKRectI.Create(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return null;
        }
        set
        {
            if (value == null || value.Value.IsEmpty)
            {
                ClipCursor(IntPtr.Zero);
            }
            else
            {
                var clipRect = value.Value;
                var rect = new Rect
                {
                    Left = clipRect.Left,
                    Top = clipRect.Top,
                    Right = clipRect.Right,
                    Bottom = clipRect.Bottom
                };
                ClipCursor(ref rect);
            }
        }
    }

    public override string ToString() => Name;
}
