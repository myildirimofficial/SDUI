using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SDUI.Helpers;

namespace SDUI.Controls;

public class Panel : System.Windows.Forms.Panel
{
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

    private Padding _border;
    public Padding Border
    {
        get => _border;
        set
        {
            if (_border == value)
                return;

            _border = value;
            Invalidate();
        }
    }

    private Color _borderColor = Color.Transparent;
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor == value)
                return;

            _borderColor = value;
            Invalidate();
        }
    }

    private float _shadowDepth = 4;
    public float ShadowDepth
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

    // Rendering cache for performance
    private GraphicsPath _cachedPath;
    private Rectangle _cachedBounds;
    private float _cachedDpi;

    public Panel()
    {
        SetStyle(
            ControlStyles.SupportsTransparentBackColor
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw,
            true
        );

        BackColor = Color.Transparent;
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

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                // Prevent flicker by not erasing background
                m.Result = (IntPtr)1;
                return;

            case 0x000F: // WM_PAINT
                // Let base handle paint but with our optimization
                break;
        }

        base.WndProc(ref m);
    }

    private GraphicsPath GetCachedPath()
    {
        var currentBounds = ClientRectangle;
        var currentDpi = DeviceDpi;

        // Check if cache is valid
        if (_cachedPath != null && 
            _cachedBounds == currentBounds && 
            Math.Abs(_cachedDpi - currentDpi) < 0.01f)
        {
            return _cachedPath;
        }

        // Dispose old cache
        DisposeGraphicsCache();

        // Create new cached path
        if (_radius > 0)
        {
            var rect = currentBounds.ToRectangleF();
            _cachedPath = rect.Radius(_radius);
            _cachedBounds = currentBounds;
            _cachedDpi = currentDpi;
        }

        return _cachedPath;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;

        // Only use AntiAlias when needed (rounded corners)
        if (_radius > 0)
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw parent background only if transparent
        if (BackColor == Color.Transparent || BackColor.A < 255)
            GroupBoxRenderer.DrawParentBackground(graphics, e.ClipRectangle, this);

        if (ColorScheme.DrawDebugBorders)
        {
            using var redPen = new Pen(Color.Red, 1) { Alignment = PenAlignment.Inset };
            graphics.DrawRectangle(redPen, 0, 0, Width - 1, Height - 1);
        }

        var rect = ClientRectangle.ToRectangleF();
        var color = BackColor == Color.Transparent ? ColorScheme.BackColor2 : BackColor;
        var borderColor = _borderColor == Color.Transparent ? ColorScheme.BorderColor : _borderColor;

        if (_radius > 0)
        {
            var path = GetCachedPath();
            if (path != null)
            {
                // Fill background
                using (var brush = new SolidBrush(color))
                    graphics.FillPath(brush, path);

                // Draw shadow if needed
                if (_shadowDepth > 0)
                {
                    ShadowUtils.DrawShadow(
                        graphics,
                        ColorScheme.ShadowColor,
                        rect.ToRectangle(),
                        (int)(_shadowDepth + 1) + 40,
                        DockStyle.Right
                    );
                }

                // Draw border
                if (_border.All > 0)
                {
                    using var pen = new Pen(borderColor, _border.All);
                    graphics.DrawPath(pen, path);
                }
            }
        }
        else
        {
            // Fast path for non-rounded rectangles
            using (var brush = new SolidBrush(color))
                graphics.FillRectangle(brush, rect);

            if (_shadowDepth > 0)
                graphics.DrawShadow(rect, _shadowDepth, 1);

            // Draw border
            if (_border.Left > 0 || _border.Top > 0 || _border.Right > 0 || _border.Bottom > 0)
            {
                ControlPaint.DrawBorder(
                    graphics,
                    ClientRectangle,
                    borderColor,
                    _border.Left,
                    ButtonBorderStyle.Solid,
                    borderColor,
                    _border.Top,
                    ButtonBorderStyle.Solid,
                    borderColor,
                    _border.Right,
                    ButtonBorderStyle.Solid,
                    borderColor,
                    _border.Bottom,
                    ButtonBorderStyle.Solid
                );
            }
        }
    }
}
