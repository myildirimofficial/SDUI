using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SDUI.Helpers;

namespace SDUI.Controls;

public class GroupBox : System.Windows.Forms.GroupBox
{
    private int _shadowDepth = 4;
    public int ShadowDepth
    {
        get => _shadowDepth;
        set
        {
            if (_shadowDepth == value)
                return;

            _shadowDepth = value;
            Invalidate();
        }
    }

    private int _radius = 10;
    public int Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;

            _radius = value;
            DisposeGraphicsCache();
            Invalidate();
        }
    }

    // Rendering cache
    private GraphicsPath _cachedPath;
    private Rectangle _cachedBounds;
    private float _cachedDpi;

    public GroupBox()
    {
        SetStyle(
            ControlStyles.SupportsTransparentBackColor
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint
                | ControlStyles.EnableNotifyMessage,
            true
        );

        UpdateStyles();
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(3, 8, 3, 3);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // WS_EX_COMPOSITED - enables double-buffered painting for smooth child control rendering
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND to prevent child control flickering
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeGraphicsCache();
        }
        base.Dispose(disposing);
    }

    private void DisposeGraphicsCache()
    {
        _cachedPath?.Dispose();
        _cachedPath = null;
        _cachedBounds = Rectangle.Empty;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        DisposeGraphicsCache();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        DisposeGraphicsCache();
        Invalidate();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (Parent == null)
        {
            DisposeGraphicsCache();
        }
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        // base.OnParentBackColorChanged already triggers repaint
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        // Only invalidate the added control's area
        if (e.Control != null)
            Invalidate(e.Control.Bounds);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                m.Result = (IntPtr)1;
                return;
        }

        base.WndProc(ref m);
    }

    private GraphicsPath GetCachedPath()
    {
        var currentBounds = ClientRectangle;
        var currentDpi = DeviceDpi;

        if (_cachedPath != null && 
            _cachedBounds == currentBounds && 
            Math.Abs(_cachedDpi - currentDpi) < 0.01f)
        {
            return _cachedPath;
        }

        DisposeGraphicsCache();

        var rect = currentBounds.ToRectangleF();
        var inflate = _shadowDepth / 4f;
        rect.Inflate(-inflate, -inflate);

        _cachedPath = rect.Radius(_radius);
        _cachedBounds = currentBounds;
        _cachedDpi = currentDpi;

        return _cachedPath;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw parent background only in the invalidated region
        GroupBoxRenderer.DrawParentBackground(graphics, e.ClipRectangle, this);

        if (ColorScheme.DrawDebugBorders)
        {
            using var redPen = new Pen(Color.Red, 1) { Alignment = PenAlignment.Inset };
            graphics.DrawRectangle(redPen, 0, 0, Width - 1, Height - 1);
        }

        var rect = ClientRectangle.ToRectangleF();
        var inflate = _shadowDepth / 4f;
        rect.Inflate(-inflate, -inflate);

        var path = GetCachedPath();
        if (path == null)
            return;

        // Fill background
        using (var brush = new SolidBrush(ColorScheme.BackColor2))
            graphics.FillPath(brush, path);

        // Draw header area
        var headerRect = new RectangleF(0, 0, rect.Width, Font.Height + 7);
        
        using (var backColorBrush = new SolidBrush(ColorScheme.BackColor2.Alpha(15)))
        {
            var clip = graphics.ClipBounds;
            graphics.SetClip(headerRect);
            
            graphics.DrawLine(ColorScheme.BorderColor, 0, headerRect.Height - 1, headerRect.Width, headerRect.Height - 1);
            graphics.FillPath(backColorBrush, path);

            this.DrawString(graphics, ColorScheme.ForeColor, headerRect);
            
            graphics.SetClip(clip);
        }

        // Draw shadow and border
        if (_shadowDepth > 0)
            graphics.DrawShadow(rect, _shadowDepth, _radius);

        graphics.DrawPath(ColorScheme.BorderColor, path);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var preferredSize = base.GetPreferredSize(proposedSize);
        preferredSize.Width += _shadowDepth;
        preferredSize.Height += _shadowDepth;

        return preferredSize;
    }
}
