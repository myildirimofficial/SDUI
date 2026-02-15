using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDUI.Helpers;
using static SDUI.NativeMethods;

namespace SDUI.Controls;

public class UIWindowBase : Form
{
    protected bool enableFullDraggable;
    private int dwmMargin = 1;
    private bool right = false;
    private Point location;
    private Color _lastBackColor = Color.Empty;

    // Theme animation fields
    private Timer _themeTimer;
    private Color _themeFrom;
    private Color _themeTo;
    private int _themeStep;
    private int _themeTotalSteps;
    private const int ThemeDurationMs = 350;
    private const int ThemeIntervalMs = 15;

    public int DwmMargin
    {
        get => dwmMargin;
        set => dwmMargin = value;
    }

    /// <summary>
    /// Get DPI
    /// </summary>
    public float DPI => DeviceDpi / 96.0f;

    private bool _aeroEnabled
    {
        get
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return enabled == 1;
            }

            return false;
        }
    }

    public UIWindowBase()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(0, 0, 0, 0);
        ResizeRedraw = true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (msg.Msg == 256 | msg.Msg == 260)
            if (keyData == Keys.Escape)
                Close();

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        right = e.Button == MouseButtons.Right;
        if (right)
        {
            location = e.Location;
            Invalidate();
        }

        if (!enableFullDraggable)
            return;

        if (e.Button == MouseButtons.Left)
            DragForm(Handle);
    }

    protected void DragForm(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
    }

    protected void PostDragForm(IntPtr handle)
    {
        ReleaseCapture();
        PostMessage(handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (!_aeroEnabled)
                cp.ClassStyle |= CS_DROPSHADOW;

            cp.ClassStyle |= CS_DBLCLKS;

            var style = (uint)cp.Style;

            if (DesignMode)
                style &= ~(uint)SetWindowLongFlags.WS_CAPTION;

            style &= ~(uint)SetWindowLongFlags.WS_SYSMENU;
            style &= ~(uint)SetWindowLongFlags.WS_THICKFRAME;
            style &= ~(uint)SetWindowLongFlags.WS_MINIMIZE;
            style &= ~(uint)SetWindowLongFlags.WS_MINIMIZEBOX;
            style &= ~(uint)SetWindowLongFlags.WS_MAXIMIZE;
            style &= ~(uint)SetWindowLongFlags.WS_MAXIMIZEBOX;
            style |= (uint)SetWindowLongFlags.WS_TILED;
            cp.Style = (int)style;

            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (DesignMode)
            return;

        if (StartPosition == FormStartPosition.CenterScreen)
            CenterToScreen();

        if (BackColor != ColorScheme.BackColor)
            BackColor = ColorScheme.BackColor;
    }

    private const int htLeft = 10;
    private const int htRight = 11;
    private const int htTop = 12;
    private const int htTopLeft = 13;
    private const int htTopRight = 14;
    private const int htBottom = 15;
    private const int htBottomLeft = 16;
    private const int htBottomRight = 17;

    protected override void WndProc(ref Message m)
    {
        if (DesignMode)
        {
            base.WndProc(ref m);
            return;
        }

        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                m.Result = (IntPtr)1;
                return;

            case WM_NCHITTEST:
            {
                if (WindowState != FormWindowState.Maximized)
                {
                    int gripDist = 10;
                    var pt = PointToClient(Cursor.Position);
                    Size clientSize = ClientSize;

                    if (pt.X >= clientSize.Width - gripDist && pt.Y >= clientSize.Height - gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)(IsMirrored ? htBottomLeft : htBottomRight); return; }
                    if (pt.X <= gripDist && pt.Y >= clientSize.Height - gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)(IsMirrored ? htBottomRight : htBottomLeft); return; }
                    if (pt.X <= gripDist && pt.Y <= gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)(IsMirrored ? htTopRight : htTopLeft); return; }
                    if (pt.X >= clientSize.Width - gripDist && pt.Y <= gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)(IsMirrored ? htTopLeft : htTopRight); return; }
                    if (pt.Y <= 2 && clientSize.Height >= 2)
                    { m.Result = (IntPtr)htTop; return; }
                    if (pt.Y >= clientSize.Height - gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)htBottom; return; }
                    if (pt.X <= gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)htLeft; return; }
                    if (pt.X >= clientSize.Width - gripDist && clientSize.Height >= gripDist)
                    { m.Result = (IntPtr)htRight; return; }
                }

                if ((int)m.Result == HTCLIENT)
                    m.Result = (IntPtr)HTCAPTION;

                break;
            }
            case WM_NCCALCSIZE:

                var handle = Handle;
                var lpwp = (WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));
                if (lpwp.HWND == IntPtr.Zero)
                    return;

                if ((lpwp.flags & (SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOREDRAW)) != 0)
                    return;

                int bits = GetWindowLong(handle, WindowLongIndexFlags.GWL_STYLE).ToInt32();
                if ((bits & TCS_MULTILINE) != 0)
                {
                    InvalidateRect(handle, new Rect(), true);
                    return;
                }

                var rect = new Rect();
                SetRect(rect, 0, 0, lpwp.cx, lpwp.cy);
                SendMessage(handle, WM_NCCALCSIZE, 0, ref rect);
                int newWidth = rect.Right - rect.Left;
                int newHeight = rect.Bottom - rect.Top;
                GetClientRect(handle, ref rect);
                int oldWidth = rect.Right - rect.Left;
                int oldHeight = rect.Bottom - rect.Top;
                if (newWidth == oldWidth && newHeight == oldHeight)
                    return;

                var inset = new Rect();
                SendMessage(handle, TCM_ADJUSTRECT, 0, ref inset);
                int marginX = -inset.Right, marginY = -inset.Bottom;
                if (newWidth != oldWidth)
                {
                    int left = newWidth < oldWidth ? newWidth : oldWidth;
                    SetRect(rect, left - marginX, 0, newWidth, newHeight);
                    InvalidateRect(handle, rect, true);
                }
                if (newHeight != oldHeight)
                {
                    int bottom = newHeight < oldHeight ? newHeight : oldHeight;
                    if (newWidth < oldWidth) oldWidth -= marginX;
                    SetRect(rect, 0, bottom - marginY, oldWidth, newHeight);
                    InvalidateRect(handle, rect, true);
                }
                return;
        }

        base.WndProc(ref m);
    }

    public void ChangeControlsTheme(Control control)
    {
        if (control == null || control.IsDisposed || !control.IsHandleCreated)
            return;

        if (!control.Visible)
            return;

        var isDark = ColorScheme.BackColor.IsDark();

        if (control is ListView listView)
        {
            if (WindowsHelper.IsModern)
            {
                try { listView.ApplyTheme(); }
                catch { }
            }
        }
        else if (control is RichTextBox || control is ListBox)
        {
            try
            {
                control.BackColor = ColorScheme.BackColor;
                control.ForeColor = ColorScheme.ForeColor;
            }
            catch { }
        }

        try
        {
            WindowsHelper.UseImmersiveDarkMode(control.Handle, isDark);
        }
        catch { }


        foreach (Control subControl in control.Controls)
            ChangeControlsTheme(subControl);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (DesignMode)
            return;

        WindowsHelper.ApplyRoundCorner(this.Handle);

        if (_aeroEnabled)
        {
            var v = 2;
            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ref v, 4);
            var margins = new MARGINS()
            {
                Bottom = dwmMargin,
                Left = dwmMargin,
                Right = dwmMargin,
                Top = dwmMargin,
            };
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);
        }

        SetWindowPos(
            Handle, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_FRAMECHANGED
                | SetWindowPosFlags.SWP_NOSIZE
                | SetWindowPosFlags.SWP_NOMOVE
                | SetWindowPosFlags.SWP_NOZORDER
                | SetWindowPosFlags.SWP_NOOWNERZORDER
                | SetWindowPosFlags.SWP_NOACTIVATE
        );
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);

        if (DesignMode || !IsHandleCreated)
            return;

        if (_lastBackColor == BackColor)
            return;

        _lastBackColor = BackColor;

        // Skip full theme application during animation intermediate frames
        if (_themeTimer != null && _themeTimer.Enabled && BackColor != _themeTo)
        {
            Invalidate();
            return;
        }

        try
        {
            foreach (Control control in Controls)
                ChangeControlsTheme(control);

            if (!WindowsHelper.IsModern)
                return;

            WindowsHelper.UseImmersiveDarkMode(Handle, ColorScheme.BackColor.IsDark());

            /*if (ColorScheme.BackColor.IsDark())
            {
                var flag = DWMSBT_TABBEDWINDOW;
                DwmSetWindowAttribute(
                    Handle,
                    DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref flag,
                    Marshal.SizeOf<int>()
                );
            }*/
        }
        catch { }
    }

    /// <summary>
    /// Apply the current theme to this window and all child controls (instant).
    /// </summary>
    public void ApplyTheme()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        StopThemeAnimation();
        _lastBackColor = Color.Empty;
        BackColor = ColorScheme.BackColor;
    }

    /// <summary>
    /// Apply the current theme with a smooth color transition animation.
    /// </summary>
    public void ApplyThemeAnimated()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        var target = ColorScheme.BackColor;
        if (BackColor == target)
            return;

        _themeFrom = BackColor;
        _themeTo = target;
        _themeStep = 0;
        _themeTotalSteps = Math.Max(1, ThemeDurationMs / ThemeIntervalMs);

        // Apply native dark/light mode immediately so scrollbars switch early
        if (WindowsHelper.IsModern)
        {
            var isDark = _themeTo.IsDark();
            WindowsHelper.UseImmersiveDarkMode(Handle, isDark);

            foreach (Control control in Controls)
            {
                try
                {
                    if (control.IsHandleCreated)
                        WindowsHelper.UseImmersiveDarkMode(control.Handle, isDark);
                }
                catch { }
            }
        }

        if (_themeTimer == null)
        {
            _themeTimer = new Timer { Interval = ThemeIntervalMs };
            _themeTimer.Tick += OnThemeTimerTick;
        }

        _themeTimer.Start();
    }

    private void OnThemeTimerTick(object sender, EventArgs e)
    {
        _themeStep++;

        if (_themeStep >= _themeTotalSteps)
        {
            StopThemeAnimation();

            // Final step: set exact target and apply full theme
            _lastBackColor = Color.Empty;
            BackColor = _themeTo;
            return;
        }

        float t = (float)_themeStep / _themeTotalSteps;
        // Ease-out cubic: 1 - (1 - t)^3
        t = 1f - (1f - t) * (1f - t) * (1f - t);

        var r = (int)(_themeFrom.R + (_themeTo.R - _themeFrom.R) * t);
        var g = (int)(_themeFrom.G + (_themeTo.G - _themeFrom.G) * t);
        var b = (int)(_themeFrom.B + (_themeTo.B - _themeFrom.B) * t);

        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);

        _lastBackColor = Color.Empty;
        BackColor = Color.FromArgb(r, g, b);
    }

    private void StopThemeAnimation()
    {
        if (_themeTimer != null)
            _themeTimer.Stop();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeTimer?.Stop();
            _themeTimer?.Dispose();
            _themeTimer = null;
        }

        base.Dispose(disposing);
    }
}
