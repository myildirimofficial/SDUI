using System;
using System.Drawing;
using System.Windows.Forms;
using static SDUI.NativeMethods;

namespace SDUI.Controls;

public class TreeView : System.Windows.Forms.TreeView
{
    private bool _isUpdating = false;

    public TreeView()
        : base()
    {
        SetStyle(
                 ControlStyles.SupportsTransparentBackColor
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.EnableNotifyMessage
                | ControlStyles.ResizeRedraw,
            true
        );

        DoubleBuffered = true;
        FullRowSelect = true;
        UpdateStyles();
    }

    /// <summary>
    /// Suspends painting to improve performance during bulk operations
    /// </summary>
    public new void BeginUpdate()
    {
        if (_isUpdating || !IsHandleCreated || IsDisposed)
            return;

        try
        {
            _isUpdating = true;
            SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            base.BeginUpdate();
        }
        catch (Exception ex)
        {
            _isUpdating = false;
            System.Diagnostics.Debug.WriteLine($"BeginUpdate failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Resumes painting after bulk operations
    /// </summary>
    public new void EndUpdate()
    {
        if (!_isUpdating || !IsHandleCreated || IsDisposed)
            return;

        try
        {
            base.EndUpdate();
            SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            _isUpdating = false;
            
            // Use RedrawWindow for proper refresh after WM_SETREDRAW
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, 
                RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW | RDW_FRAME);
        }
        catch (Exception ex)
        {
            _isUpdating = false;
            System.Diagnostics.Debug.WriteLine($"EndUpdate failed: {ex.Message}");
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyTheme();
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND (0x14) to reduce flicker
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }

    internal void ApplyTheme()
    {
        if (!IsHandleCreated || DesignMode)
            return;

        var isDark = ColorScheme.BackColor.IsDark();

        try
        {
            int useImmersiveDarkMode = isDark ? 1 : 0;

            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref useImmersiveDarkMode, sizeof(int));
            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                ref useImmersiveDarkMode, sizeof(int));

            if (isDark)
            {
                SetWindowTheme(Handle, "DarkMode_Explorer", null);
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.FromArgb(241, 241, 241);

                IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, "DarkMode_ItemsView", null);
                }
            }
            else
            {
                SetWindowTheme(Handle, "Explorer", null);
                BackColor = SystemColors.Window;
                ForeColor = SystemColors.WindowText;

                IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, "ItemsView", null);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme application failed: {ex.Message}");

            BackColor = ColorScheme.BackColor;
            ForeColor = ColorScheme.ForeColor;
        }
        finally
        {
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                // Prevent flicker - return 1 to indicate handled
                m.Result = (IntPtr)1;
                return;

            case WM_NOTIFY:
                if (!IsHandleCreated || IsDisposed)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }

                var pnmhdr = (NMHDR)m.GetLParam(typeof(NMHDR));

                if (pnmhdr.code == NM_CUSTOMDRAW)
                {
                    var nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));

                    switch (nmcd.dwDrawStage)
                    {
                        case (int)CDDS.CDDS_PREPAINT:
                            m.Result = new IntPtr((int)CDRF.CDRF_NOTIFYITEMDRAW);
                            return;
                        
                        case (int)CDDS.CDDS_ITEMPREPAINT:
                            SetTextColor(nmcd.hdc, ColorTranslator.ToWin32(ColorScheme.ForeColor));
                            m.Result = new IntPtr((int)CDRF.CDRF_DODEFAULT);
                            return;
                        
                        default:
                            m.Result = new IntPtr((int)CDRF.CDRF_DODEFAULT);
                            return;
                    }
                }
                break;
        }

        base.WndProc(ref m);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // WS_CLIPCHILDREN prevents child control flicker
            cp.Style |= 0x02000000; // WS_CLIPCHILDREN
            return cp;
        }
    }

    private const int WM_SETREDRAW = 0x000B;
}
