using SDUI.Animation;
using SDUI.Helpers;
using SkiaSharp;
using System;

namespace SDUI.Controls;

public class GroupBox : UIElementBase
{
    private int _radius = 10;
    private int _shadowDepth = 4;
    private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;
    private bool _collapsible = false;
    private bool _collapsed = false;
    private CollapseDirection _collapseDirection = CollapseDirection.Vertical;
    private AnimationManager? _collapseAnimation;
    private AnimationManager? _arrowAnimation;
    private int _expandedSize;
    private int _collapsedSize = 35;
    private bool _isHeaderHovered = false;
    private bool _sizeInitialized = false;

    private int RadiusScaled => (int)(_radius * ScaleFactor);
    private float ShadowDepthScaled => _shadowDepth * ScaleFactor;

    public GroupBox()
    {
        BackColor = SKColors.Transparent;
        Padding = new Thickness(3, 8, 3, 3);
        InitializeCollapseAnimation();
    }

    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            if (_textAlign == value) return;
            _textAlign = value;
            Invalidate();
        }
    }

    public int ShadowDepth
    {
        get => _shadowDepth;
        set
        {
            if (_shadowDepth == value) return;
            _shadowDepth = value;
            Invalidate();
        }
    }

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Invalidate();
        }
    }

    [System.ComponentModel.Category("Behavior")]
    [System.ComponentModel.DefaultValue(false)]
    public bool Collapsible
    {
        get => _collapsible;
        set
        {
            if (_collapsible == value) return;
            _collapsible = value;
            Invalidate();
        }
    }

    [System.ComponentModel.Category("Behavior")]
    [System.ComponentModel.DefaultValue(false)]
    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed == value) return;
            SetCollapsed(value, animate: false);
        }
    }

    [System.ComponentModel.Category("Behavior")]
    [System.ComponentModel.DefaultValue(CollapseDirection.Vertical)]
    public CollapseDirection CollapseDirection
    {
        get => _collapseDirection;
        set
        {
            if (_collapseDirection == value) return;
            
            if (_sizeInitialized && !_collapsed)
            {
                var oldDirection = _collapseDirection;
                _collapseDirection = value;
                
                if (oldDirection == CollapseDirection.Vertical)
                    _expandedSize = Width;
                else
                    _expandedSize = Height;
            }
            else
            {
                _collapseDirection = value;
            }
            
            Invalidate();
        }
    }

    public event EventHandler? CollapsedChanged;

    private void InitializeCollapseAnimation()
    {
        _collapseAnimation = new AnimationManager
        {
            AnimationType = AnimationType.EaseOut,
            Increment = 0.1,
        };
        _collapseAnimation.OnAnimationProgress += OnCollapseAnimationUpdate;

        _arrowAnimation = new AnimationManager
        {
            AnimationType = AnimationType.EaseOut,
            Increment = 0.15,
        };
        _arrowAnimation.OnAnimationProgress += _ => Invalidate();
    }

    public void ToggleCollapsed()
    {
        if (!_collapsible) return;
        SetCollapsed(!_collapsed, animate: true);
    }

    private void SetCollapsed(bool collapse, bool animate)
    {
        EnsureSizeInitialized();
        
        _collapsed = collapse;

        if (animate && _collapseAnimation != null && _arrowAnimation != null)
        {
            _collapseAnimation.StartNewAnimation(_collapsed ? AnimationDirection.Out : AnimationDirection.In);
            _arrowAnimation.StartNewAnimation(_collapsed ? AnimationDirection.Out : AnimationDirection.In);
        }
        else
        {
            ApplyCollapsedState();
        }

        CollapsedChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCollapseAnimationUpdate(object? sender)
    {
        if (_collapseAnimation == null) return;

        var progress = _collapseAnimation.GetProgress();
        int currentSize;

        if (_collapsed)
        {
            currentSize = (int)(_expandedSize - (_expandedSize - _collapsedSize) * progress);
        }
        else
        {
            currentSize = (int)(_collapsedSize + (_expandedSize - _collapsedSize) * progress);
        }

        if (_collapseDirection == CollapseDirection.Vertical)
        {
            Height = currentSize;
        }
        else
        {
            Width = currentSize;
        }

        Invalidate();
    }

    private void ApplyCollapsedState()
    {
        if (_collapseDirection == CollapseDirection.Vertical)
        {
            if (_collapsed)
                Height = _collapsedSize;
            else
                Height = _expandedSize;
        }
        else
        {
            if (_collapsed)
                Width = _collapsedSize;
            else
                Width = _expandedSize;
        }
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        
        if (!_collapsed)
        {
            if (_collapseDirection == CollapseDirection.Vertical)
                _expandedSize = Height;
            else
                _expandedSize = Width;
                
            _sizeInitialized = true;
        }
    }
    
    private void EnsureSizeInitialized()
    {
        if (!_sizeInitialized)
        {
            if (_collapseDirection == CollapseDirection.Vertical)
                _expandedSize = Height;
            else
                _expandedSize = Width;
                
            _sizeInitialized = true;
        }
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        if (!_collapsible)
        {
            base.OnMouseDown(e);
            return;
        }

        var titleHeight = Font.Size + 7;
        if (e.Y < titleHeight)
        {
            var arrowHitBox = SKRect.Create(0, 0, 25, titleHeight);
            if (arrowHitBox.Contains(e.Location))
            {
                ToggleCollapsed();
                return;
            }
        }
        
        base.OnMouseDown(e);
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_collapsible) return;

        var titleHeight = Font.Size + 7;
        var arrowHitBox = SKRect.Create(0, 0, 25, titleHeight);
        
        bool wasHovered = _isHeaderHovered;
        _isHeaderHovered = arrowHitBox.Contains(e.Location);

        if (wasHovered != _isHeaderHovered)
        {
            Cursor = _isHeaderHovered ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHeaderHovered = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);
        
        EnsureSizeInitialized();

        // Debug �er�evesi
        if (ColorScheme.DrawDebugBorders)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Red,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRect(0, 0, Width - 1, Height - 1, paint);
        }

        var rect = new SkiaSharp.SKRect(0, 0, Width, Height);
        var shadowRect = rect;

        // Balk lleri (padding uygulanm genilik)
        var titleHeight = Font.Size + 7f * ScaleFactor;
        float titleX = Padding.Left;
        var titleWidth = Math.Max(0, rect.Width - Padding.Horizontal);
        var titleRect = new SkiaSharp.SKRect(titleX, 0, titleX + titleWidth, titleHeight);

        // Glge izimi
        using (var shadowMaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ShadowDepthScaled / 2f))
        using (var paint = new SKPaint
               {
                   Color = SKColors.Black.WithAlpha(20),
                   IsAntialias = true,
                   MaskFilter = shadowMaskFilter
               })
        {
            canvas.DrawRoundRect(shadowRect, RadiusScaled, RadiusScaled, paint);
        }

        // Arka plan �izimi
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.BackColor2,
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
        {
            canvas.DrawRoundRect(rect, RadiusScaled, RadiusScaled, paint);
        }

        // Ba�l�k alan� �izimi (padding uygulanm�� s�n�rlar i�inde)
        canvas.Save();
        canvas.ClipRect(titleRect);

        // Ba�l�k �izgisi
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.BorderColor,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = 1f * ScaleFactor
               })
        {
            canvas.DrawLine(titleRect.Left, titleRect.Height - 1, titleRect.Right, titleRect.Height - 1, paint);
        }

        // Ba�l�k arka plan (hafif)
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.BackColor2.WithAlpha(15),
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
        {
            canvas.DrawRoundRect(rect, RadiusScaled, RadiusScaled, paint);
        }

        canvas.Restore();

        // Ba�l�k metni �izimi
        if (!string.IsNullOrEmpty(Text))
        {
            using var font = new SKFont
            {
                Size = Font.Size.Topx(this),
                Typeface = FontManager.GetSKTypeface(Font),
                Edging = SKFontEdging.SubpixelAntialias
            };

            using var textPaint = new SKPaint
            {
                Color = ColorScheme.ForeColor,
                IsAntialias = true
            };

            var textWidth = font.MeasureText(Text);
            var textY = titleRect.Height / 2f - (font.Metrics.Ascent + font.Metrics.Descent) / 2f;
            float textX;

            var textMargin = _collapsible ? 25f : 5f;

            switch (TextAlign)
            {
                case ContentAlignment.MiddleLeft:
                    textX = titleRect.Left + textMargin;
                    break;
                case ContentAlignment.MiddleRight:
                    textX = titleRect.Right - textWidth - 5f;
                    break;
                case ContentAlignment.MiddleCenter:
                default:
                    textX = titleRect.Left + (titleWidth - textWidth) / 2f;
                    break;
            }

            TextRenderingHelper.DrawText(canvas, Text, textX, textY, SKTextAlign.Left, font, textPaint);
        }

        if (_collapsible)
        {
            DrawCollapseArrow(canvas, titleRect.Height);
        }

        // �er�eve �izimi
        using (var paint = new SKPaint
               {
                   Color = ColorScheme.BorderColor,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = 1f * ScaleFactor
               })
        {
            canvas.DrawRoundRect(rect, RadiusScaled, RadiusScaled, paint);
        }
    }

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        var preferredSize = base.GetPreferredSize(proposedSize);
        preferredSize.Width += _shadowDepth;
        preferredSize.Height += _shadowDepth;

        return preferredSize;
    }

    public override SKRect DisplayRectangle
    {
        get
        {
            var titleHeight = Font.Size + 7;
            var clientRect = SKRect.Create(
                Padding.Left,
                Padding.Top + titleHeight,
                Width - Padding.Horizontal - _shadowDepth / 2,
                Height - Padding.Vertical - titleHeight - _shadowDepth / 2
            );
            return clientRect;
        }
    }

    protected override void OnLayout(UILayoutEventArgs e)
    {
        base.OnLayout(e);
        Invalidate();
    }

    private void DrawCollapseArrow(SKCanvas canvas, float titleHeight)
    {
        var arrowSize = 8f;
        var arrowX = 10f;
        var arrowY = titleHeight / 2f;

        var rotationProgress = _arrowAnimation?.GetProgress() ?? (_collapsed ? 0.0 : 1.0);
        
        float rotation;
        if (_collapseDirection == CollapseDirection.Vertical)
        {
            rotation = (float)(rotationProgress * 90);
        }
        else
        {
            rotation = (float)(rotationProgress * 90) + 90f;
        }

        using var arrowPaint = new SKPaint
        {
            Color = ColorScheme.ForeColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        canvas.Save();
        canvas.Translate(arrowX, arrowY);
        canvas.RotateRadians(rotation * (float)Math.PI / 180f);

        using var path = new SKPath();
        path.MoveTo(-arrowSize / 2, -arrowSize / 3);
        path.LineTo(0, arrowSize / 3);
        path.LineTo(arrowSize / 2, -arrowSize / 3);

        canvas.DrawPath(path, arrowPaint);
        canvas.Restore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _collapseAnimation?.Dispose();
            _arrowAnimation?.Dispose();
        }

        base.Dispose(disposing);
    }
}
