using SDUI.Animation;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Timers;

namespace SDUI.Controls;

public class ScrollBar : ElementBase
{
    private readonly Timer _hideTimer;
    private readonly Timer _inputSettleTimer;
    private readonly AnimationManager _scrollAnim;
    private readonly Timer _rubberBandAnim;
    private readonly AnimationManager _visibilityAnim;
    private double _animatedValue;
    private bool _autoHide = true;
    private SKPoint _dragStartPoint;
    private float _dragStartValue;
    private int _hideDelay = 1200; // ms
    private bool _isInputStretching;
    private bool _isRubberBandAnimating;

    private bool _hostHovered;
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
    private double _scrollAnimIncrement = 0.45;
    private AnimationType _scrollAnimType = AnimationType.EaseOut;
    private float _rubberBandAnimationStartValue;
    private float _scrollAnimationStartValue;
    private float _smallChange = 1;
    private float _springVelocity;
    private float _targetValue;
    private int _thickness = 2;
    private SkiaSharp.SKRect _thumbRect;
    private SkiaSharp.SKRect _trackRect;
    private bool _useThumbShadow = true;
    private float _visualOverflowValue;
    private float _value;
    private const double InputSettleDelay = 72;
    private const double SpringTickInterval = 16;
    private const float SpringStiffness = 260f;
    private const float SpringDamping = 24f;
    private const float SpringStopDistance = 0.2f;
    private const float SpringStopVelocity = 4f;

    private double _visibilityAnimIncrement = 0.20;
    private AnimationType _visibilityAnimType = AnimationType.EaseInOut;

    public ScrollBar()
    {
        BackColor = SKColors.Transparent;
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
            _animatedValue = _scrollAnimationStartValue + (_targetValue - _scrollAnimationStartValue) * _scrollAnim.GetProgress();
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
        };
        _scrollAnim.OnAnimationFinished += s =>
        {
            _animatedValue = _targetValue;
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
        };

        _hideTimer = new Timer { Interval = _hideDelay };
        _hideTimer.Elapsed += HideTimer_Tick;

        _inputSettleTimer = new Timer { Interval = InputSettleDelay, AutoReset = false };
        _inputSettleTimer.Elapsed += InputSettleTimer_Tick;

        _rubberBandAnim = new Timer { Interval = SpringTickInterval, AutoReset = true };
        _rubberBandAnim.Elapsed += SpringTimer_Tick;

        _visibilityAnim.SetProgress(_autoHide ? 0 : 1);
        _animatedValue = _value;
        _scrollAnimationStartValue = _value;
        _targetValue = _value;
    }

    [DefaultValue(4)]
    [Description("Scrollbar thickness (width vertically, height horizontally)")]
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
    [Description("The corner radius")]
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
    [Description("Auto hide")]
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
    [Description("Auto hidden (ms)")]
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
    [Description("Thumb shadow effect")]
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

    // New: expose animation settings
    [Category("Animation")]
    [DefaultValue(0.20)]
    [Description("Visibility animation speed (Increment). Higher values are faster.")]
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
    [Description("Visibility animation easing type")]
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
    [Description("Scroll animation speed (Increment). Higher values are faster.")]
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
    [Description("Scroll animation easing type")]
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
        set => SetValueCore(value, animate: !_isDragging);
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
    [Description("Track color override; if Transparent, ColorScheme is used")]
    public SKColor TrackColor { get; set; } = SKColors.Transparent;

    [DefaultValue(typeof(SKColor), "Transparent")]
    [Description("Thumb color override; if Transparent, ColorScheme is used")]
    public SKColor ThumbColor { get; set; } = SKColors.Transparent;

    internal float DisplayValue => GetDisplayValue();

    internal event EventHandler DisplayValueChanged;

    public event EventHandler ValueChanged;
    public event EventHandler Scroll;

    private float GetDisplayValue()
    {
        return (float)(_animatedValue + _visualOverflowValue);
    }

    private void NotifyDisplayValueChanged()
    {
        DisplayValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyOrientationSize()
    {
        Size = IsVertical ? new SKSize(_thickness, Math.Max(Height, 100)) : new SKSize(Math.Max(Width, 100), _thickness);
    }

    private void HideTimer_Tick(object sender, EventArgs e)
    {
        HideNow();
    }

    private void InputSettleTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        _isInputStretching = false;

        if (_isDragging)
            return;

        StartVisualOverflowReturn();
    }

    private void SpringTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        if (!_isRubberBandAnimating)
            return;

        if (_isInputStretching || _isDragging)
            return;

        var deltaTime = (float)(SpringTickInterval / 1000d);
        var acceleration = (-SpringStiffness * _visualOverflowValue) - (SpringDamping * _springVelocity);
        _springVelocity += acceleration * deltaTime;
        _visualOverflowValue += _springVelocity * deltaTime;

        if (MathF.Abs(_visualOverflowValue) <= SpringStopDistance && MathF.Abs(_springVelocity) <= SpringStopVelocity)
        {
            StopVisualOverflowReturn();
            _visualOverflowValue = 0f;
        }

        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
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

            _inputSettleTimer.Stop();
            _inputSettleTimer.Elapsed -= InputSettleTimer_Tick;
            _inputSettleTimer.Dispose();

            _rubberBandAnim.Stop();
            _rubberBandAnim.Elapsed -= SpringTimer_Tick;
            _rubberBandAnim.Dispose();

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
            _trackRect = new SkiaSharp.SKRect(0, 0, Width, Height);
            return;
        }

        var trackLength = MathF.Max(1f, IsVertical ? Height : Width);
        var thumbLength = MathF.Max(20f, (float)Math.Round(LargeChange / (Maximum - Minimum + LargeChange) * trackLength));
        thumbLength = MathF.Min(trackLength, thumbLength);

        var currentValue = GetDisplayValue();
        var range = MathF.Max(0.0001f, Maximum - Minimum);
        var trackTravel = MathF.Max(0f, trackLength - thumbLength);
        var boundedValue = Math.Clamp(currentValue, Minimum, Maximum);
        var normalized = Math.Clamp((boundedValue - Minimum) / range, 0f, 1f);
        var thumbPos = (float)Math.Round(normalized * trackTravel);
        var overflow = currentValue - boundedValue;

        if (Math.Abs(overflow) > 0.001f)
        {
            var valuePerPixel = range / MathF.Max(1f, trackTravel);
            var overflowPixels = valuePerPixel <= 0f
                ? 0f
                : MathF.Min(trackLength * 0.45f, MathF.Abs(overflow) / valuePerPixel * 0.45f);
            var minThumbLength = MathF.Min(thumbLength, MathF.Max(12f, thumbLength * 0.45f));
            thumbLength = MathF.Max(minThumbLength, thumbLength - overflowPixels);
            thumbPos = overflow < 0f ? 0f : trackLength - thumbLength;
        }

        if (IsVertical)
        {
            _thumbRect = new SkiaSharp.SKRect(0, thumbPos, Width, thumbPos + thumbLength);
            _trackRect = new SkiaSharp.SKRect(0, 0, Width, Height);
        }
        else
        {
            _thumbRect = new SkiaSharp.SKRect(thumbPos, 0, thumbPos + thumbLength, Height);
            _trackRect = new SkiaSharp.SKRect(0, 0, Width, Height);
        }
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateThumbRect();
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);

        var visibility = _autoHide ? (float)_visibilityAnim.GetProgress() : 1f;
        if (visibility <= 0f)
            return; // tamamen gizli

        var baseTrackColor = TrackColor == SKColors.Transparent ? ColorScheme.Surface : TrackColor;
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
            stateColor = schemeBase.BlendWith(ColorScheme.Surface, 0.15f);

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

    private void ClearVisualOverflow()
    {
        _isInputStretching = false;
        _inputSettleTimer.Stop();
        StopVisualOverflowReturn();
        _visualOverflowValue = 0f;
    }

    private void StopVisualOverflowReturn()
    {
        _isRubberBandAnimating = false;
        _rubberBandAnim.Stop();
        _springVelocity = 0f;
    }

    private void RestartInputSettleTimer()
    {
        _isInputStretching = true;
        _inputSettleTimer.Stop();
        _inputSettleTimer.Interval = InputSettleDelay;
        _inputSettleTimer.Start();
    }

    private void StartVisualOverflowReturn()
    {
        if (_isInputStretching)
            return;

        if (Math.Abs(_visualOverflowValue) <= 0.001f)
        {
            ClearVisualOverflow();
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
            return;
        }

        _rubberBandAnimationStartValue = _visualOverflowValue;
        _isRubberBandAnimating = true;
        _springVelocity = -_rubberBandAnimationStartValue * 1.8f;
        _rubberBandAnim.Stop();
        _rubberBandAnim.Start();
    }

    private float GetVisualOverflowValue(float overflow)
    {
        if (Math.Abs(overflow) <= 0.001f)
            return 0f;

        var viewportLength = MathF.Max(1f, IsVertical ? Height : Width);
        var maxOverflow = MathF.Max(56f, viewportLength * 0.42f);
        var resistance = MathF.Max(1f, viewportLength * 0.08f);
        var normalized = MathF.Abs(overflow) / resistance;
        var magnitude = maxOverflow * (normalized / (normalized + 0.22f));
        return MathF.CopySign(magnitude, overflow);
    }

    private float GetMaximumVisualOverflow()
    {
        var viewportLength = MathF.Max(1f, IsVertical ? Height : Width);
        return MathF.Max(56f, viewportLength * 0.42f);
    }

    private float GetWheelStretchDelta(float delta)
    {
        var maxOverflow = GetMaximumVisualOverflow();
        var currentRatio = MathF.Min(1f, MathF.Abs(_visualOverflowValue) / maxOverflow);
        var resistance = MathF.Max(0.18f, 1f - currentRatio * 0.82f);
        var stretch = MathF.Max(1.5f, MathF.Abs(delta) * 0.75f * resistance);
        return MathF.CopySign(stretch, delta);
    }

    private void SetVisualOverflow(float overflow)
    {
        var maxOverflow = GetMaximumVisualOverflow();
        _visualOverflowValue = Math.Clamp(overflow, -maxOverflow, maxOverflow);
        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
    }

    internal void ApplyWheelDelta(float delta)
    {
        if (Math.Abs(delta) <= 0.001f)
            return;

        StopVisualOverflowReturn();

        var rawValue = Math.Abs(_visualOverflowValue) > 0.001f
            ? GetDisplayValue() + delta
            : Value + delta;
        var boundedValue = Math.Clamp(rawValue, Minimum, Maximum);
        SetValueCore(boundedValue, animate: false);

        var overflow = rawValue - boundedValue;
        if (Math.Abs(overflow) <= 0.001f)
        {
            ClearVisualOverflow();
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
            ShowWithAutoHide();
            return;
        }

        var targetOverflow = GetVisualOverflowValue(overflow);
        var sameDirection = Math.Abs(_visualOverflowValue) > 0.001f && MathF.Sign(_visualOverflowValue) == MathF.Sign(delta);
        var nextOverflow = sameDirection
            ? _visualOverflowValue + GetWheelStretchDelta(delta)
            : targetOverflow;

        SetVisualOverflow(nextOverflow);
        RestartInputSettleTimer();
        ShowWithAutoHide();
    }

    private float GetAccumulatedInputValue(float delta)
    {
        var displayValue = GetDisplayValue();
        if (displayValue < Minimum && delta < 0f)
            return displayValue + delta;

        if (displayValue > Maximum && delta > 0f)
            return displayValue + delta;

        if (Math.Abs(_visualOverflowValue) > 0.001f)
            return displayValue + delta;

        return Value + delta;
    }

    private bool SetValueCore(float value, bool animate)
    {
        value = Math.Clamp(value, Minimum, Maximum);

        if (_value == value)
        {
            if (!animate && Math.Abs((float)_animatedValue - value) > 0.001f)
            {
                _scrollAnimationStartValue = value;
                _animatedValue = value;
                _targetValue = value;
                UpdateThumbRect();
                NotifyDisplayValueChanged();
                Invalidate();
            }

            return false;
        }

        ClearVisualOverflow();
        var startValue = _animatedValue;
        _value = value;

        if (_isDragging || !animate)
        {
            _scrollAnimationStartValue = value;
            _animatedValue = value;
            _targetValue = value;
            UpdateThumbRect();
            NotifyDisplayValueChanged();
        }
        else
        {
            _scrollAnimationStartValue = (float)startValue;
            _targetValue = value;
            _scrollAnim.StartNewAnimation(AnimationDirection.In);
        }

        OnValueChanged(EventArgs.Empty);
        Invalidate();
        return true;
    }

    internal void ApplyInputValue(float value, bool keepStretchActive = false)
    {
        var boundedValue = Math.Clamp(value, Minimum, Maximum);
        SetValueCore(boundedValue, animate: !keepStretchActive && !_isDragging);

        var overflow = value - boundedValue;
        if (Math.Abs(overflow) <= 0.001f)
        {
            ClearVisualOverflow();
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
            ShowWithAutoHide();
            return;
        }

        StopVisualOverflowReturn();
        _visualOverflowValue = GetVisualOverflowValue(overflow);
        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
        ShowWithAutoHide();

        if (keepStretchActive)
        {
            RestartInputSettleTimer();
            return;
        }

        if (!_isDragging)
            StartVisualOverflowReturn();
    }

    internal void ApplyInputDelta(float delta, bool keepStretchActive = false)
    {
        ApplyInputValue(GetAccumulatedInputValue(delta), keepStretchActive);
    }

    internal void ReleaseVisualOverflow()
    {
        StartVisualOverflowReturn();
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _inputSettleTimer.Stop();
            _isInputStretching = false;

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
            var newValue = _dragStartValue + delta * valuePerPixel;
            ApplyInputValue(newValue);
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
            ReleaseVisualOverflow();

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
        ApplyWheelDelta(-delta);
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