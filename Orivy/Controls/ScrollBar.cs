using Orivy.Animation;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Timers;

namespace Orivy.Controls;

/// <summary>
/// A modern, animated scrollbar control with rubber-band overflow, auto-hide,
/// and smooth scroll animation. Combines exponential-decay rubber-band (stable,
/// no overshoot) with delta-guarded animation callbacks and proper event cleanup.
/// </summary>
public class ScrollBar : ElementBase
{
    // -------------------------------------------------------------------------
    // Timers & animation managers
    // -------------------------------------------------------------------------
    private readonly Timer _hideTimer;
    private readonly Timer _inputSettleTimer;
    private readonly Timer _rubberBandTimer;
    private readonly AnimationManager _scrollAnim;
    private readonly AnimationManager _visibilityAnim;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    private double _animatedValue;
    private bool _autoHide = true;
    private SKPoint _dragStartPoint;
    private float _dragStartValue;
    private int _hideDelay = 1200;
    private bool _isInputStretching;
    private bool _isRubberBandAnimating;
    private bool _hostHovered;
    private bool _isDragging;
    private bool _isHovered;
    private bool _isThumbHovered;
    private bool _isThumbPressed;
    private float _largeChange = 10;
    private float _maximum = 100;
    private float _minimum;
    private Orientation _orientation = Orientation.Vertical;
    private double _scrollAnimIncrement = 0.32;
    private AnimationType _scrollAnimType = AnimationType.CubicEaseOut;
    private float _scrollAnimationStartValue;
    private float _smallChange = 1;
    private float _targetValue;
    private int _thickness = 2;
    private SKRect _thumbRect;
    private SKRect _trackRect;
    private float _visualOverflowValue;
    private float _value;

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------
    private const double InputSettleDelay = 72;
    private const double RubberBandInterval = 16;

    /// <summary>
    /// Exponential decay factor applied each tick (~60 fps).
    /// 0.82 gives a smooth ~200 ms settle without any overshoot.
    /// Prefer this over a spring simulation for UI scrollbars:
    ///   - zero overshoot / oscillation risk
    ///   - single multiply per tick — trivially cheap
    ///   - perceptually indistinguishable from a critically-damped spring
    /// </summary>
    private const float RubberBandDecay = 0.82f;

    /// <summary>Pixels below which overflow is snapped to zero.</summary>
    private const float RubberBandStopThreshold = 0.5f;

    private double _visibilityAnimIncrement = 0.07;
    private AnimationType _visibilityAnimType = AnimationType.EaseOut;

    // -------------------------------------------------------------------------
    // Cached paint objects (reused every frame, never recreated)
    // -------------------------------------------------------------------------
    private readonly SKPaint _trackPaint = new() { IsAntialias = true };
    private readonly SKPaint _thumbPaint = new() { IsAntialias = true };

    /// <summary>
    /// The scrollbar is a transparent overlay; the base renderer must not fill
    /// any background. All visual elements (thumb, optional track) are drawn
    /// inside <see cref="OnPaint"/> with visibility-scaled alpha so the
    /// auto-hide fade animates the complete control to transparent.
    /// </summary>
    public override SKColor BackColor
    {
        get => SKColors.Transparent;
        set { }
    }

    // =========================================================================
    // Constructor
    // =========================================================================
    public ScrollBar()
    {
        Cursor = Cursors.Default;
        Radius = new(6);
        ApplyOrientationSize();

        // --- Visibility animation ---
        _visibilityAnim = new AnimationManager(true)
        {
            Increment = _visibilityAnimIncrement,
            AnimationType = _visibilityAnimType,
            InterruptAnimation = true
        };
        _visibilityAnim.OnAnimationProgress += _ => Invalidate();
        _visibilityAnim.OnAnimationFinished += _ => Invalidate();

        // --- Scroll animation ---
        _scrollAnim = new AnimationManager(true)
        {
            Increment = _scrollAnimIncrement,
            AnimationType = _scrollAnimType,
            InterruptAnimation = true
        };
        _scrollAnim.OnAnimationProgress += _ =>
        {
            double newValue = _scrollAnimationStartValue
                + (_targetValue - _scrollAnimationStartValue) * _scrollAnim.GetProgress();

            // Guard: skip repaint when the visual change is sub-pixel
            if (Math.Abs(_animatedValue - newValue) <= 0.001) return;

            _animatedValue = newValue;
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
        };
        _scrollAnim.OnAnimationFinished += _ =>
        {
            _animatedValue = _targetValue;
            UpdateThumbRect();
            NotifyDisplayValueChanged();
            Invalidate();
        };

        // --- Timers ---
        _hideTimer = new Timer { Interval = _hideDelay, AutoReset = false };
        _hideTimer.Elapsed += HideTimer_Tick;

        _inputSettleTimer = new Timer { Interval = InputSettleDelay, AutoReset = false };
        _inputSettleTimer.Elapsed += InputSettleTimer_Tick;

        _rubberBandTimer = new Timer { Interval = RubberBandInterval, AutoReset = true };
        _rubberBandTimer.Elapsed += RubberBandTimer_Tick;

        // --- Initial state ---
        _visibilityAnim.SetProgress(_autoHide ? 0 : 1);
        _animatedValue = _value;
        _scrollAnimationStartValue = _value;
        _targetValue = _value;

        UpdateThumbRect();
    }

    // =========================================================================
    // Public properties
    // =========================================================================

    [DefaultValue(2)]
    [Description("Scrollbar thickness in pixels.")]
    public int Thickness
    {
        get => _thickness;
        set
        {
            value = Math.Clamp(value, 2, 32);
            if (_thickness == value) return;
            _thickness = value;
            ApplyOrientationSize();
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(true)]
    [Description("Automatically fade the scrollbar out after HideDelay ms of inactivity.")]
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide == value) return;
            _autoHide = value;
            if (!_autoHide)
            {
                // Disabling auto-hide: stop any pending hide and snap fully visible
                _hideTimer.Stop();
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
    [Description("Milliseconds of inactivity before the scrollbar fades (AutoHide=true).")]
    public int HideDelay
    {
        get => _hideDelay;
        set
        {
            _hideDelay = Math.Clamp(value, 250, 10_000);
            _hideTimer.Interval = _hideDelay;
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

    [Category("Animation"), DefaultValue(0.20)]
    [Description("Speed of the fade-in / fade-out animation (0.01–1.0).")]
    public double VisibilityAnimationIncrement
    {
        get => _visibilityAnimIncrement;
        set
        {
            _visibilityAnimIncrement = Math.Clamp(value, 0.01, 1.0);
            _visibilityAnim.Increment = _visibilityAnimIncrement;
        }
    }

    [Category("Animation"), DefaultValue(typeof(AnimationType), "EaseInOut")]
    public AnimationType VisibilityAnimationType
    {
        get => _visibilityAnimType;
        set { _visibilityAnimType = value; _visibilityAnim.AnimationType = value; }
    }

    [Category("Animation"), DefaultValue(0.32)]
    [Description("Speed of the programmatic scroll animation (0.01–1.0).")]
    public double ScrollAnimationIncrement
    {
        get => _scrollAnimIncrement;
        set
        {
            _scrollAnimIncrement = Math.Clamp(value, 0.01, 1.0);
            _scrollAnim.Increment = _scrollAnimIncrement;
        }
    }

    [Category("Animation"), DefaultValue(typeof(AnimationType), "CubicEaseOut")]
    public AnimationType ScrollAnimationType
    {
        get => _scrollAnimType;
        set { _scrollAnimType = value; _scrollAnim.AnimationType = value; }
    }

    public bool IsVertical => _orientation == Orientation.Vertical;

    [DefaultValue(0f)]
    public float Value
    {
        get => _value;
        set => SetValueCore(value, animate: !_isDragging);
    }

    [DefaultValue(0f)]
    public float Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum == value) return;
            _minimum = value;
            if (_value < value) Value = value;
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(100f)]
    public float Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum == value) return;
            _maximum = value;
            if (_value > value) Value = value;
            UpdateThumbRect();
            Invalidate();
        }
    }

    [DefaultValue(10f)]
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

    [DefaultValue(1f)]
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
    public SKColor TrackColor { get; set; } = SKColors.Transparent;

    [DefaultValue(typeof(SKColor), "Transparent")]
    public SKColor ThumbColor { get; set; } = SKColors.Transparent;

    // =========================================================================
    // Internal API (consumed by the host ScrollPanel / ScrollViewer)
    // =========================================================================

    internal float DisplayValue => (float)Math.Round(_animatedValue + _visualOverflowValue);

    internal event EventHandler? DisplayValueChanged;
    public event EventHandler? ValueChanged;
    public event EventHandler? Scroll;

    // =========================================================================
    // Dispose
    // =========================================================================

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) { base.Dispose(disposing); return; }

        if (disposing)
        {
            // Unsubscribe events before stopping — prevents a final Elapsed
            // firing after Dispose if the OS thread-pool beat us to it.
            _hideTimer.Elapsed -= HideTimer_Tick;
            _inputSettleTimer.Elapsed -= InputSettleTimer_Tick;
            _rubberBandTimer.Elapsed -= RubberBandTimer_Tick;

            _hideTimer.Stop(); _hideTimer.Dispose();
            _inputSettleTimer.Stop(); _inputSettleTimer.Dispose();
            _rubberBandTimer.Stop(); _rubberBandTimer.Dispose();

            _visibilityAnim.Dispose();
            _scrollAnim.Dispose();

            // Cached paints
            _trackPaint.Dispose();
            _thumbPaint.Dispose();
        }

        base.Dispose(disposing);
    }

    // =========================================================================
    // Geometry helpers
    // =========================================================================

    private void ApplyOrientationSize()
    {
        Size = IsVertical
            ? new SKSize(_thickness, Math.Max(Height, 100))
            : new SKSize(Math.Max(Width, 100), _thickness);
    }

    private void UpdateThumbRect()
    {
        _trackRect = new SKRect(0, 0, Width, Height);

        if (_maximum <= _minimum)
        {
            _thumbRect = SKRect.Empty;
            return;
        }

        float trackLength = MathF.Max(1f, IsVertical ? Height : Width);
        float range = MathF.Max(0.0001f, _maximum - _minimum);

        // Thumb length proportional to the visible page
        float thumbLength = MathF.Max(20f, _largeChange / (_maximum - _minimum + _largeChange) * trackLength);
        thumbLength = MathF.Min(trackLength, thumbLength);

        float trackTravel = MathF.Max(0f, trackLength - thumbLength);
        float currentValue = DisplayValue;
        float bounded = Math.Clamp(currentValue, _minimum, _maximum);
        float normalized = Math.Clamp((bounded - _minimum) / range, 0f, 1f);
        float thumbPos = normalized * trackTravel;

        // Rubber-band: compress thumb when pulled past the limits
        float overflow = currentValue - bounded;
        if (MathF.Abs(overflow) > 0.001f)
        {
            float valuePerPixel = range / MathF.Max(1f, trackTravel);
            float overflowPixels = valuePerPixel <= 0f
                ? 0f
                : MathF.Min(trackLength * 0.22f, MathF.Abs(overflow) / valuePerPixel * 0.22f);

            float minThumbLength = MathF.Max(16f, thumbLength * 0.62f);
            thumbLength = MathF.Max(minThumbLength, thumbLength - overflowPixels);
            thumbPos = overflow < 0f ? 0f : trackLength - thumbLength;
        }

        _thumbRect = IsVertical
            ? new SKRect(0, thumbPos, Width, thumbPos + thumbLength)
            : new SKRect(thumbPos, 0, thumbPos + thumbLength, Height);
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateThumbRect();
    }

    // =========================================================================
    // Painting
    // =========================================================================

    public override void OnPaint(SKCanvas canvas)
    {
        float visibility = _autoHide ? (float)_visibilityAnim.GetProgress() : 1f;
        if (visibility <= 0f || _maximum <= _minimum) return;

        SKColor trackBase = TrackColor == SKColors.Transparent ? ColorScheme.BorderColor : TrackColor;
        byte trackAlpha = _autoHide ? (byte)(38 * visibility) : (byte)255;
        _trackPaint.Color = trackBase.WithAlpha(trackAlpha);

        using var trackRR = new SKRoundRect(_trackRect, Radius.All);
        canvas.DrawRoundRect(trackRR, _trackPaint);

        if (_thumbRect.IsEmpty) return;

        // --- Thumb colour (state-driven) ---
        SKColor schemeBase = ThumbColor == SKColors.Transparent ? ColorScheme.BorderColor : ThumbColor;
        if (schemeBase == SKColors.Transparent) schemeBase = ColorScheme.ForeColor;

        SKColor stateColor = _isThumbPressed
            ? schemeBase.BlendWith(ColorScheme.ForeColor, 0.35f)
            : (/*_isThumbHovered || */_isHovered || _hostHovered)
                ? schemeBase.BlendWith(ColorScheme.ForeColor, 0.25f)
                : schemeBase.BlendWith(ColorScheme.Surface, 0.15f);

        byte thumbAlpha = (byte)(220 * Math.Clamp(visibility, 0f, 1f));
        SKColor thumbColor = stateColor.WithAlpha(thumbAlpha);

        using var thumbRR = new SKRoundRect(_thumbRect, Radius.All);

        _thumbPaint.Color = thumbColor;
        canvas.DrawRoundRect(thumbRR, _thumbPaint);
    }

    // =========================================================================
    // Auto-hide logic
    // =========================================================================

    /// <summary>
    /// Makes the scrollbar visible and (re)starts the hide countdown.
    /// Skips re-starting the animation when it is already fully visible
    /// and heading In — prevents a visible flicker on rapid input.
    /// </summary>
    private void ShowWithAutoHide()
    {
        if (!_autoHide) return;

        if (_visibilityAnim.Direction != AnimationDirection.In
            || _visibilityAnim.GetProgress() < 1.0)
        {
            _visibilityAnim.StartNewAnimation(AnimationDirection.In);
        }

        _hideTimer.Stop();
        _hideTimer.Interval = _hideDelay;
        _hideTimer.Start();
    }

    private void HideNow()
    {
        if (!_autoHide) return;
        if (_isHovered || _isDragging || _isThumbHovered || _isInputStretching) return;

        _hideTimer.Stop();
        _visibilityAnim.StartNewAnimation(AnimationDirection.Out);
    }

    private void HideTimer_Tick(object? sender, ElapsedEventArgs e) => HideNow();

    // =========================================================================
    // Rubber-band (overflow) helpers
    // =========================================================================

    /// <summary>
    /// Maps a raw overflow value to a physically-capped visual offset using
    /// logarithmic resistance — the further you pull, the harder it gets.
    /// </summary>
    private float ComputeVisualOverflow(float overflow)
    {
        if (MathF.Abs(overflow) <= 0.001f) return 0f;

        float viewport = MathF.Max(1f, IsVertical ? Height : Width);
        float maxOverflow = MathF.Max(18f, viewport * 0.12f);
        float resistance = MathF.Max(1f, viewport * 0.24f);
        float normalized = MathF.Abs(overflow) / resistance;
        float magnitude = maxOverflow * (1f - MathF.Exp(-normalized * 0.75f));
        return MathF.CopySign(magnitude, overflow);
    }

    private float MaxVisualOverflow()
    {
        float viewport = MathF.Max(1f, IsVertical ? Height : Width);
        return MathF.Max(18f, viewport * 0.12f);
    }

    /// <summary>
    /// Incremental stretch delta used when the wheel keeps scrolling past
    /// the boundary — progressively harder the further we are from the edge.
    /// </summary>
    private float WheelStretchDelta(float delta)
    {
        float max = MaxVisualOverflow();
        float currentRatio = MathF.Min(1f, MathF.Abs(_visualOverflowValue) / max);
        float resistance = MathF.Max(0.08f, 1f - currentRatio * 0.92f);
        float stretch = MathF.Max(0.35f, MathF.Abs(delta) * 0.18f * resistance);
        return MathF.CopySign(stretch, delta);
    }

    private void SetVisualOverflow(float overflow)
    {
        float clamped = Math.Clamp(overflow, -MaxVisualOverflow(), MaxVisualOverflow());
        if (MathF.Abs(_visualOverflowValue - clamped) <= 0.001f) return;
        _visualOverflowValue = clamped;
        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
    }

    private void ClearVisualOverflow()
    {
        _isInputStretching = false;
        _inputSettleTimer.Stop();
        StopRubberBand();
        if (MathF.Abs(_visualOverflowValue) <= 0.001f) return;
        _visualOverflowValue = 0f;
        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
    }

    private void StopRubberBand()
    {
        _isRubberBandAnimating = false;
        _rubberBandTimer.Stop();
    }

    private void StartRubberBandReturn()
    {
        if (_isInputStretching) return;

        if (MathF.Abs(_visualOverflowValue) <= 0.001f)
        {
            ClearVisualOverflow();
            return;
        }

        _isRubberBandAnimating = true;
        _rubberBandTimer.Stop();
        _rubberBandTimer.Start();
    }

    private void RestartInputSettleTimer()
    {
        _isInputStretching = true;
        _inputSettleTimer.Stop();
        _inputSettleTimer.Interval = InputSettleDelay;
        _inputSettleTimer.Start();
    }

    private void InputSettleTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        _isInputStretching = false;
        if (!_isDragging) StartRubberBandReturn();
    }

    /// <summary>
    /// Each tick: multiply overflow by the decay factor.
    /// Exponential decay is equivalent to a critically-damped spring at the
    /// frequencies a scrollbar operates at, but cannot oscillate — ideal here.
    /// </summary>
    private void RubberBandTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        if (!_isRubberBandAnimating || _isInputStretching || _isDragging) return;

        _visualOverflowValue *= RubberBandDecay;
        //_visualOverflowValue = MathF.Round(_visualOverflowValue);

        if (MathF.Abs(_visualOverflowValue) <= RubberBandStopThreshold)
        {
            StopRubberBand();
            _visualOverflowValue = 0f;
        }

        UpdateThumbRect();
        NotifyDisplayValueChanged();
        Invalidate();
    }

    // =========================================================================
    // Value management
    // =========================================================================

    private void NotifyDisplayValueChanged() => DisplayValueChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Core setter. Returns true when the logical value actually changed.
    /// When <paramref name="animate"/> is false the scroll animation is
    /// explicitly stopped so no stale animation overwrites the new position.
    /// </summary>
    private bool SetValueCore(float value, bool animate)
    {
        value = Math.Clamp(value, _minimum, _maximum);

        if (MathF.Abs(_value - value) <= 0.001f)
        {
            // Value unchanged, but animated position may still lag (e.g. mid-animation)
            if (!animate && MathF.Abs((float)_animatedValue - value) > 0.001f)
            {
                _scrollAnim.Stop();
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

        float startValue = (float)_animatedValue;
        _value = value;

        if (_isDragging || !animate)
        {
            _scrollAnim.Stop();
            _scrollAnimationStartValue = value;
            _animatedValue = value;
            _targetValue = value;
            UpdateThumbRect();
            NotifyDisplayValueChanged();
        }
        else
        {
            _scrollAnimationStartValue = startValue;
            _targetValue = value;
            _scrollAnim.StartNewAnimation(AnimationDirection.In);
        }

        OnValueChanged(EventArgs.Empty);
        Invalidate();
        return true;
    }

    /// <summary>
    /// Returns the best starting point for an accumulated delta:
    /// continues from the display value when we are already overflowing,
    /// otherwise starts from the logical value to avoid jump artefacts.
    /// </summary>
    private float AccumulatedInputBase(float delta)
    {
        float display = DisplayValue;
        if (display < _minimum && delta < 0f) return display + delta;
        if (display > _maximum && delta > 0f) return display + delta;
        if (MathF.Abs(_visualOverflowValue) > 0.001f) return display + delta;
        return _value + delta;
    }

    // =========================================================================
    // Internal API called by the host (ScrollPanel / touch gesture recogniser)
    // =========================================================================

    /// <summary>Applies a wheel-style delta with rubber-band overflow.</summary>
    internal void ApplyWheelDelta(float delta)
    {
        if (MathF.Abs(delta) <= 0.001f) return;

        StopRubberBand();

        float raw = MathF.Abs(_visualOverflowValue) > 0.001f ? DisplayValue + delta : _value + delta;
        float bounded = Math.Clamp(raw, _minimum, _maximum);
        SetValueCore(bounded, animate: false);

        float overflow = raw - bounded;
        if (MathF.Abs(overflow) <= 0.001f)
        {
            ClearVisualOverflow();
            ShowWithAutoHide();
            return;
        }

        float targetOverflow = ComputeVisualOverflow(overflow);
        bool sameDir = MathF.Abs(_visualOverflowValue) > 0.001f
                     && MathF.Sign(_visualOverflowValue) == MathF.Sign(delta);

        float next = sameDir
            ? _visualOverflowValue + WheelStretchDelta(delta)
            : targetOverflow;

        SetVisualOverflow(next);
        RestartInputSettleTimer();
        ShowWithAutoHide();
    }

    /// <summary>Applies an absolute value with optional rubber-band stretch.</summary>
    internal void ApplyInputValue(float value, bool keepStretchActive = false)
    {
        float bounded = Math.Clamp(value, _minimum, _maximum);
        SetValueCore(bounded, animate: !keepStretchActive && !_isDragging);

        float overflow = value - bounded;
        if (MathF.Abs(overflow) <= 0.001f)
        {
            ClearVisualOverflow();
            ShowWithAutoHide();
            return;
        }

        StopRubberBand();
        SetVisualOverflow(ComputeVisualOverflow(overflow));
        ShowWithAutoHide();

        if (keepStretchActive)
        {
            RestartInputSettleTimer();
            return;
        }

        if (!_isDragging) StartRubberBandReturn();
    }

    internal void ApplyInputDelta(float delta, bool keepStretchActive = false)
        => ApplyInputValue(AccumulatedInputBase(delta), keepStretchActive);

    internal void ReleaseVisualOverflow() => StartRubberBandReturn();

    internal void SetHostHover(bool hovered)
    {
        if (_hostHovered == hovered) return;
        _hostHovered = hovered;

        if (_autoHide)
        {
            if (hovered)
            {
                // Mouse entered the host: pause the hide countdown so the scrollbar
                // stays visible while the user can interact with the content.
                _hideTimer.Stop();
            }
            else
            {
                // Mouse left the host: start the hide countdown if not actively used.
                if (!_isHovered && !_isDragging && !_isThumbHovered)
                {
                    _hideTimer.Stop();
                    _hideTimer.Interval = _hideDelay;
                    _hideTimer.Start();
                }
            }
        }

        Invalidate();
    }

    // =========================================================================
    // Mouse events
    // =========================================================================

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        _inputSettleTimer.Stop();
        _isInputStretching = false;

        if (_thumbRect.Contains(e.Location))
        {
            _isDragging = true;
            _isThumbPressed = true;
            _dragStartPoint = e.Location;
            _dragStartValue = _value;
            ((IElement)this).GetParentWindow()?.SetMouseCapture(this);
        }
        else
        {
            // Click on track: jump by one page
            if (IsVertical)
            {
                if (e.Y < _thumbRect.Top) Value -= _largeChange;
                else if (e.Y > _thumbRect.Bottom) Value += _largeChange;
            }
            else
            {
                if (e.X < _thumbRect.Left) Value -= _largeChange;
                else if (e.X > _thumbRect.Right) Value += _largeChange;
            }
        }

        ShowWithAutoHide();
        Invalidate();
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool oldThumbHovered = _isThumbHovered;
        _isThumbHovered = _thumbRect.Contains(e.Location);
        _isHovered = _trackRect.Contains(e.Location);

        if (oldThumbHovered != _isThumbHovered) Invalidate();

        if (_isDragging)
        {
            float delta = IsVertical ? e.Y - _dragStartPoint.Y : e.X - _dragStartPoint.X;
            float trackTravel = IsVertical ? Height - _thumbRect.Height : Width - _thumbRect.Width;

            if (trackTravel > 0f)
            {
                float valuePerPixel = (_maximum - _minimum) / trackTravel;
                ApplyInputValue(_dragStartValue + delta * valuePerPixel);
                OnScroll(EventArgs.Empty);
            }
        }

        ShowWithAutoHide();
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        _isDragging = false;
        _isThumbPressed = false;

        ReleaseVisualOverflow();
        ((IElement)this).GetParentWindow()?.ReleaseMouseCapture(this);

        Invalidate();
        if (_autoHide) { _hideTimer.Stop(); _hideTimer.Start(); }
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        float scrollLines = SystemInformation.MouseWheelScrollLines;
        float delta = e.Delta / 120f * scrollLines * _smallChange;
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
        _isThumbHovered = false;
        Invalidate();

        if (_autoHide) { _hideTimer.Stop(); _hideTimer.Start(); }
    }

    // =========================================================================
    // Overridable event raisers
    // =========================================================================

    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
        ShowWithAutoHide();
    }

    protected virtual void OnScroll(EventArgs e) => Scroll?.Invoke(this, e);

    // =========================================================================
    // Layout
    // =========================================================================

    public override SKSize GetPreferredSize(SKSize proposedSize)
        => IsVertical ? new SKSize(_thickness, 100) : new SKSize(100, _thickness);
}