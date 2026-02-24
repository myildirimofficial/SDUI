using System;
using System.ComponentModel;
using System.Timers;
using System.Windows.Forms;
using SDUI.Animation;
using SkiaSharp;

namespace SDUI.Controls;

public class ScrollBar : ElementBase
{
    private readonly Timer _hideTimer;
    private readonly AnimationManager _scrollAnim; // scroll animasyonu
    private readonly AnimationManager _visibilityAnim;
    private double _animatedValue; // animasyonlu değer
    private bool _autoHide = true;
    private SKPoint _dragStartPoint;
    private float _dragStartValue;
    private int _hideDelay = 1200; // ms

    private bool _hostHovered; // container hover durumu
    private bool _isDragging;
    private bool _isHovered; // track hover
    private bool _isThumbHovered;
    private bool _isThumbPressed;
    private float _largeChange = 10;
    private float _maximum = 100;
    private float _minimum;
    private Orientation _orientation = Orientation.Vertical;

    // Corner radius for rounded corners (int pixels)
    private int _cornerRadius = 6; // default larger radius
    private double _scrollAnimIncrement = 0.45; // kaydırma için hızlı varsayılan
    private AnimationType _scrollAnimType = AnimationType.EaseOut;
    private float _smallChange = 1;
    private float _targetValue; // animasyon hedefi
    private int _thickness = 2; // ince varsayılan
    private SkiaSharp.SKRect _thumbRect;
    private SkiaSharp.SKRect _trackRect;
    private bool _useThumbShadow = true;
    private float _value;

    // Yeni: animasyon ayarları
    private double _visibilityAnimIncrement = 0.20; // daha hızlı varsayılan
    private AnimationType _visibilityAnimType = AnimationType.EaseInOut;

    public ScrollBar()
    {
        BackColor = SKColors.Transparent; // ColorScheme ile uyumlu
        Cursor = Cursors.Default;
        ApplyOrientationSize();

        _visibilityAnim = new AnimationManager(true)
        {
            Increment = _visibilityAnimIncrement,
            AnimationType = _visibilityAnimType,
            InterruptAnimation = true
        };
        _visibilityAnim.OnAnimationProgress += s => Invalidate();

        _scrollAnim = new AnimationManager(true)
        {
            Increment = _scrollAnimIncrement,
            AnimationType = _scrollAnimType,
            InterruptAnimation = true
        };
        _scrollAnim.OnAnimationProgress += s =>
        {
            _animatedValue = _value + (_targetValue - _value) * _scrollAnim.GetProgress();
            UpdateThumbRect();
            Invalidate();
        };

        _hideTimer = new Timer { Interval = _hideDelay };
        _hideTimer.Elapsed += HideTimer_Tick;

        _visibilityAnim.SetProgress(_autoHide ? 0 : 1);
        _animatedValue = _value;
        _targetValue = _value;
    }

    [DefaultValue(4)]
    [Description("Scrollbar kalınlığı (dikeyde genişlik, yatayda yükseklik)")]
    public int Thickness
    {
        get => _thickness;
        set
        {
            value = Math.Max(2, Math.Min(32, value));
            if (_thickness == value) return;
            _thickness = value;
            ApplyOrientationSize();
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(6)]
    [Description("Köşe yuvarlaklık yarıçapı")]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            value = Math.Max(0, Math.Min(64, value));
            if (_cornerRadius == value) return;
            _cornerRadius = value;
            Invalidate();
        }
    }

    [DefaultValue(true)]
    [Description("Otomatik gizleme")]
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide == value) return;
            _autoHide = value;
            if (!_autoHide)
            {
                _visibilityAnim.SetProgress(1);
                Invalidate();
            }
            else
            {
                ShowWithAutoHide();
            }
        }
    }

    [DefaultValue(1200)]
    [Description("Gizleme gecikmesi (ms)")]
    public int HideDelay
    {
        get => _hideDelay;
        set
        {
            _hideDelay = Math.Max(250, Math.Min(10000, value));
            _hideTimer.Interval = _hideDelay;
        }
    }

    [DefaultValue(true)]
    [Description("Thumb gölge efekti")]
    public bool UseThumbShadow
    {
        get => _useThumbShadow;
        set
        {
            _useThumbShadow = value;
            Invalidate();
        }
    }

    [DefaultValue(Orientation.Vertical)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value) return;
            _orientation = value;
            ApplyOrientationSize();
            UpdateThumbRect();
            Invalidate();
        }
    }

    // Yeni: animasyon ayarlarını dışarı aç
    [Category("Animation")]
    [DefaultValue(0.20)]
    [Description("Görünürlük animasyonu hız (Increment). Daha yüksek değer daha hızlıdır.")]
    public double VisibilityAnimationIncrement
    {
        get => _visibilityAnimIncrement;
        set
        {
            _visibilityAnimIncrement = Math.Clamp(value, 0.01, 1.0);
            _visibilityAnim.Increment = _visibilityAnimIncrement;
        }
    }

    [Category("Animation")]
    [DefaultValue(typeof(AnimationType), "EaseInOut")]
    [Description("Görünürlük animasyonu tipi (Easing)")]
    public AnimationType VisibilityAnimationType
    {
        get => _visibilityAnimType;
        set
        {
            _visibilityAnimType = value;
            _visibilityAnim.AnimationType = _visibilityAnimType;
        }
    }

    [Category("Animation")]
    [DefaultValue(0.45)]
    [Description("Scroll animasyonu hız (Increment). Daha yüksek değer daha hızlıdır.")]
    public double ScrollAnimationIncrement
    {
        get => _scrollAnimIncrement;
        set
        {
            _scrollAnimIncrement = Math.Clamp(value, 0.01, 1.0);
            _scrollAnim.Increment = _scrollAnimIncrement;
        }
    }

    [Category("Animation")]
    [DefaultValue(typeof(AnimationType), "EaseOut")]
    [Description("Scroll animasyonu tipi (Easing)")]
    public AnimationType ScrollAnimationType
    {
        get => _scrollAnimType;
        set
        {
            _scrollAnimType = value;
            _scrollAnim.AnimationType = _scrollAnimType;
        }
    }

    public bool IsVertical => Orientation == Orientation.Vertical;

    [DefaultValue(0)]
    public float Value
    {
        get => _value;
        set
        {
            value = Math.Max(Minimum, Math.Min(Maximum, value));
            if (_value == value) return;
            _value = value;

            if (_isDragging)
            {
                _animatedValue = value;
                _targetValue = value;
                UpdateThumbRect();
            }
            else
            {
                _targetValue = value;
                _scrollAnim.StartNewAnimation(AnimationDirection.In);
            }

            OnValueChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    [DefaultValue(0)]
    public float Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum == value) return;
            _minimum = value;
            if (Value < value) Value = value;
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(100)]
    public float Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum == value) return;
            _maximum = value;
            if (Value > value) Value = value;
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(10)]
    public float LargeChange
    {
        get => _largeChange;
        set
        {
            if (_largeChange == value) return;
            _largeChange = value;
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(1)]
    public float SmallChange
    {
        get => _smallChange;
        set
        {
            if (_smallChange == value) return;
            _smallChange = value;
        }
    }

    [DefaultValue(typeof(SKColor), "Transparent")]
    [Description("Track rengi override; Transparent ise ColorScheme kullanılır")]
    public SKColor TrackColor { get; set; } = SKColors.Transparent;

    [DefaultValue(typeof(SKColor), "Transparent")]
    [Description("Thumb rengi override; Transparent ise ColorScheme kullanılır")]
    public SKColor ThumbColor { get; set; } = SKColors.Transparent;

    public event EventHandler ValueChanged;
    public event EventHandler Scroll;

    private void ApplyOrientationSize()
    {
        Size = IsVertical ? new SKSize(_thickness, Math.Max(Height, 100)) : new SKSize(Math.Max(Width, 100), _thickness);
    }

    private void HideTimer_Tick(object sender, EventArgs e)
    {
        HideNow();
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            base.Dispose(disposing);
            return;
        }

        if (disposing)
        {
            _hideTimer.Stop();
            _hideTimer.Elapsed -= HideTimer_Tick;
            _hideTimer.Dispose();

            _visibilityAnim.Dispose();
            _scrollAnim.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateThumbRect()
    {
        if (Maximum <= Minimum)
        {
            _thumbRect = SkiaSharp.SKRect.Empty;
            return;
        }

        var trackLength = IsVertical ? Height : Width;
        var thumbLength = Math.Max(20, (int)((float)LargeChange / (Maximum - Minimum + LargeChange) * trackLength));
        int thumbPos;

        var currentValue = _animatedValue;

        if (IsVertical)
        {
            thumbPos = (int)((currentValue - Minimum) / (Maximum - Minimum) * (Height - thumbLength));
            _thumbRect = new SkiaSharp.SKRect(0, thumbPos, Width, thumbLength);
            _trackRect = new SkiaSharp.SKRect(0, 0, Width, Height);
        }
        else
        {
            thumbPos = (int)((currentValue - Minimum) / (Maximum - Minimum) * (Width - thumbLength));
            _thumbRect = new SkiaSharp.SKRect(thumbPos, 0, thumbLength, Height);
            _trackRect = new SkiaSharp.SKRect(0, 0, Width, Height);
        }
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);

        var visibility = _autoHide ? (float)_visibilityAnim.GetProgress() : 1f;
        if (visibility <= 0f)
            return; // tamamen gizli

        var baseTrackColor = TrackColor == SKColors.Transparent ? ColorScheme.BackColor2 : TrackColor;
        var blendedTrack = baseTrackColor.BlendWith(ColorScheme.ForeColor, 0.18f);
        var trackAlpha = (byte)(50 * visibility);
        var trackSk = blendedTrack.WithAlpha(trackAlpha);

        using (var trackPaint = new SKPaint
               {
                   Color = trackSk,
                   IsAntialias = true
               })
        {
            var radius = Math.Max(0, _cornerRadius * ScaleFactor);
            canvas.DrawRoundRect(new SKRoundRect(new SkiaSharp.SKRect(0, 0, Width, Height), radius), trackPaint);
        }

        if (_thumbRect.IsEmpty) return;

        var schemeBase = ThumbColor == SKColors.Transparent ? ColorScheme.BorderColor : ThumbColor;
        if (schemeBase == SKColors.Transparent)
            schemeBase = ColorScheme.ForeColor;

        SKColor stateColor;
        if (_isThumbPressed)
            stateColor = schemeBase.BlendWith(ColorScheme.ForeColor, 0.35f);
        else if (_isThumbHovered || _isHovered || _hostHovered)
            stateColor = schemeBase.BlendWith(ColorScheme.ForeColor, 0.25f);
        else
            stateColor = schemeBase.BlendWith(ColorScheme.BackColor, 0.15f);

        var thumbColor = stateColor.WithAlpha((byte)(220 * Math.Clamp(visibility, 0f, 1f)));

        if (_useThumbShadow && visibility > 0f)
        {
            using var shadowFilter =
                SKImageFilter.CreateDropShadow(0, 0, 2, 2, SKColors.Black.WithAlpha((byte)(70 * visibility)));
            using var shadowPaint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha((byte)(30 * visibility)),
                ImageFilter = shadowFilter,
                IsAntialias = true
            };

            var rad = Math.Max(0, _cornerRadius * ScaleFactor);
            canvas.DrawRoundRect(new SKRoundRect(_thumbRect, rad), shadowPaint);
        }

        using (var thumbPaint = new SKPaint
               {
                   Color = thumbColor,
                   IsAntialias = true
               })
        {
            var rad = Math.Max(0, _cornerRadius * ScaleFactor);
            canvas.DrawRoundRect(new SKRoundRect(_thumbRect, rad), thumbPaint);
        }

        // Debug
        if (ColorScheme.DrawDebugBorders)
        {
            using var dbg = new SKPaint
                { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
            canvas.DrawRect(0, 0, Width - 1, Height - 1, dbg);
        }
    }

    private void ShowWithAutoHide()
    {
        if (!_autoHide) return;
        _visibilityAnim.StartNewAnimation(AnimationDirection.In);
        _hideTimer.Stop();
        _hideTimer.Interval = _hideDelay;
        _hideTimer.Start();
    }

    private void HideNow()
    {
        if (!_autoHide) return;
        if (_isHovered || _isDragging || _isThumbHovered) return;
        _hideTimer.Stop();
        _visibilityAnim.StartNewAnimation(AnimationDirection.Out);
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            if (_thumbRect.Contains(e.Location))
            {
                _isDragging = true;
                _isThumbPressed = true;
                _dragStartPoint = e.Location;
                _dragStartValue = Value;

                // Capture mouse at window level so we continue receiving moves/up even when cursor leaves scrollbar bounds
                var parentWindow = (this as ElementBase).GetParentWindow();
                if (parentWindow != null)
                    parentWindow.SetMouseCapture(this);
            }
            else
            {
                if (IsVertical)
                {
                    if (e.Y < _thumbRect.Location.Y)
                        Value -= LargeChange;
                    else if (e.Y > _thumbRect.Bottom)
                        Value += LargeChange;
                }
                else
                {
                    if (e.X < _thumbRect.Location.X)
                        Value -= LargeChange;
                    else if (e.X > _thumbRect.Right)
                        Value += LargeChange;
                }
            }

            ShowWithAutoHide();
            Invalidate();
        }
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var oldThumbHovered = _isThumbHovered;
        _isThumbHovered = _thumbRect.Contains(e.Location);
        _isHovered = SKRect.Create(SKPoint.Empty, Size).Contains(e.Location);
        if (oldThumbHovered != _isThumbHovered)
            Invalidate();

        if (_isDragging)
        {
            var delta = IsVertical ? e.Y - _dragStartPoint.Y : e.X - _dragStartPoint.X;
            var trackLength = IsVertical ? Height - _thumbRect.Height : Width - _thumbRect.Width;
            if (trackLength <= 0) return;
            var valuePerPixel = (float)(Maximum - Minimum) / trackLength;
            var newValue = _dragStartValue + (int)(delta * valuePerPixel);
            Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
            OnScroll(EventArgs.Empty);
        }

        ShowWithAutoHide();
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            _isThumbPressed = false;

            // Release capture if we had captured it
            var parentWindow = (this as IElement).GetParentWindow();
            if (parentWindow != null)
                parentWindow.ReleaseMouseCapture(this);

            Invalidate();
            if (_autoHide)
            {
                _hideTimer.Stop();
                _hideTimer.Start();
            }
        }
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        var scrollLines = SystemInformation.MouseWheelScrollLines;
        var delta = e.Delta / 120 * scrollLines * SmallChange;
        Value -= delta;
        OnScroll(EventArgs.Empty);
        ShowWithAutoHide();
    }

    internal override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        ShowWithAutoHide();
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        if (_autoHide)
        {
            _hideTimer.Stop();
            _hideTimer.Start();
        }
    }

    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
        ShowWithAutoHide();
    }

    protected virtual void OnScroll(EventArgs e)
    {
        Scroll?.Invoke(this, e);
    }

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        return IsVertical ? new SKSize(_thickness, 100) : new SKSize(100, _thickness);
    }

    internal void SetHostHover(bool hovered)
    {
        _hostHovered = hovered;
        if (_autoHide)
        {
            if (hovered)
            {
                _visibilityAnim.StartNewAnimation(AnimationDirection.In);
                _hideTimer.Stop();
            }
            else
            {
                _hideTimer.Stop();
                _hideTimer.Interval = _hideDelay;
                _hideTimer.Start();
            }
        }

        Invalidate();
    }
}