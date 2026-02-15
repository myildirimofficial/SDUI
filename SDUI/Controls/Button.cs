using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SDUI.Animation;

namespace SDUI.Controls;

public class Button : System.Windows.Forms.Button
{
    /// <summary>
    /// Button raised color
    /// </summary>
    public Color Color { get; set; } = Color.Transparent;

    /// <summary>
    /// Mouse state
    /// </summary>
    private int _mouseState = 0;

    private SizeF textSize;
    public override string Text
    {
        get { return base.Text; }
        set
        {
            if (base.Text == value)
                return;

            base.Text = value;
            textSize = TextRenderer.MeasureText(value, Font);
            if (AutoSize)
                Size = GetPreferredSize();
            Invalidate();
        }
    }

    public override Size MinimumSize
    {
        get => base.MinimumSize;
        set
        {
            if (value.Height < 23)
                value.Height = 23;

            base.MinimumSize = value;
        }
    }

    private float _shadowDepth = 4f;
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

    private int _radius = 6;
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

    private readonly Animation.AnimationEngine animationManager;
    private readonly Animation.AnimationEngine hoverAnimationManager;

    // Rendering cache
    private GraphicsPath _cachedPath;
    private Rectangle _cachedBounds;
    private float _cachedDpi;

    public Button()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.ResizeRedraw,
            true
        );
        
        DoubleBuffered = true;

        animationManager = new Animation.AnimationEngine(false)
        {
            Increment = 0.03,
            AnimationType = AnimationType.EaseOut,
        };

        hoverAnimationManager = new Animation.AnimationEngine
        {
            Increment = 0.07,
            AnimationType = AnimationType.Linear,
        };

        hoverAnimationManager.OnAnimationFinished += (sender) => { };
        hoverAnimationManager.OnAnimationProgress += sender => Invalidate();

        animationManager.OnAnimationProgress += sender => Invalidate();
        UpdateStyles();
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

    private GraphicsPath GetCachedPath(RectangleF rect)
    {
        var currentBounds = Rectangle.Round(rect);
        var currentDpi = DeviceDpi;

        if (_cachedPath != null &&
            _cachedBounds == currentBounds &&
            Math.Abs(_cachedDpi - currentDpi) < 0.01f)
        {
            return _cachedPath;
        }

        DisposeGraphicsCache();

        _cachedPath = rect.Radius(_radius);
        _cachedBounds = currentBounds;
        _cachedDpi = currentDpi;

        return _cachedPath;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _mouseState = 2;
        animationManager.StartNewAnimation(AnimationDirection.In, e.Location);
        // Invalidate handled by animationManager.OnAnimationProgress
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _mouseState = 1;
        // Invalidate handled by animation or next paint
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _mouseState = 1;
        hoverAnimationManager.StartNewAnimation(AnimationDirection.In);
        // Invalidate handled by hoverAnimationManager.OnAnimationProgress
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        _mouseState = 0;
        hoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        // Invalidate handled by hoverAnimationManager.OnAnimationProgress
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        ButtonRenderer.DrawParentBackground(graphics, e.ClipRectangle, this);

        var rectf = ClientRectangle.ToRectangleF();

        if (ColorScheme.DrawDebugBorders)
        {
            using (var redPen = new Pen(Color.Red, 1) { Alignment = PenAlignment.Outset })
                graphics.DrawRectangle(redPen, 0, 0, rectf.Width - 1, rectf.Height - 1);
        }

        var inflate = _shadowDepth / 4f;
        rectf.Inflate(-inflate, -inflate);

        var color = Color.Empty;
        if (Color != Color.Transparent)
            color = Enabled ? Color : Color.Alpha(200);
        else
            color = ColorScheme.ForeColor.Alpha(20);

        var path = GetCachedPath(rectf);

        // Draw border for transparent buttons
        if (Color == Color.Transparent)
        {
            using var outerPen = new Pen(ColorScheme.BorderColor);
            graphics.DrawPath(outerPen, path);
        }

        // Fill button background
        using (var brush = new SolidBrush(color))
            graphics.FillPath(brush, path);

        // Draw hover animation overlay
        var hoverProgress = hoverAnimationManager.GetProgress();
        if (hoverProgress > 0.01f)
        {
            var animationColor = Color != Color.Transparent
                ? Color.FromArgb((int)(hoverProgress * 65), color.Determine())
                : Color.FromArgb((int)(hoverProgress * color.A), color);

            using var b = new SolidBrush(animationColor);
            graphics.FillPath(b, path);
        }

        // Draw shadow
        if (_shadowDepth > 0)
            graphics.DrawShadow(rectf, _shadowDepth, _radius);

        // Draw ripple effect
        if (animationManager.IsAnimating())
        {
            using var ripplePath = (GraphicsPath)path.Clone();

            for (int i = 0; i < animationManager.GetAnimationCount(); i++)
            {
                var animationValue = animationManager.GetProgress(i);
                var animationSource = animationManager.GetSource(i);

                var rippleAlpha = (int)(101 - (animationValue * 100));
                if (rippleAlpha > 0)
                {
                    using var rippleBrush = new SolidBrush(ColorScheme.BackColor.Alpha(rippleAlpha));

                    var rippleSize = (float)(animationValue * Width * 2.0);
                    var rippleRect = new RectangleF(
                        animationSource.X - rippleSize / 2,
                        animationSource.Y - rippleSize / 2,
                        rippleSize,
                        rippleSize
                    );

                    ripplePath.Reset();
                    ripplePath.AddPath(path, false);
                    ripplePath.AddEllipse(rippleRect);

                    graphics.FillPath(rippleBrush, ripplePath);
                }
            }
        }

        // Draw text and image
        var foreColor = Color == Color.Transparent ? ColorScheme.ForeColor : ForeColor;
        if (!Enabled)
            foreColor = Color.Gray;

        var textRect = rectf.ToRectangle();

        if (Image != null)
        {
            var dpiScale = DeviceDpi / 96f;
            var imageSize = (int)(24 * dpiScale);
            var padding = (int)(8 * dpiScale);
            var spacing = (int)(4 * dpiScale);

            Rectangle imageRect = new Rectangle(padding, (Height - imageSize) / 2, imageSize, imageSize);

            if (string.IsNullOrEmpty(Text))
                imageRect.X = (Width - imageSize) / 2;

            graphics.DrawImage(Image, imageRect);

            textRect.Width -= padding + imageSize + spacing + padding;
            textRect.X += padding + imageSize + spacing;
        }

        this.DrawString(graphics, TextAlign, foreColor, textRect, AutoEllipsis, UseMnemonic);
    }

    private Size GetPreferredSize()
    {
        return GetPreferredSize(Size.Empty);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        // Provides extra space for proper padding for content
        int extra = 16;

        if (Image != null)
            // 24 is for icon size
            // 4 is for the space between icon & text
            extra += 24 + 4;

        return new Size((int)Math.Ceiling(textSize.Width) + extra, 23);
    }
}
