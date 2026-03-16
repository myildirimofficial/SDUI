using SDUI.Animation;
using SDUI.Styling;
using SkiaSharp;
using System;
using System.ComponentModel;

namespace SDUI.Controls;

public abstract partial class ElementBase
{
    private readonly AnimationManager _visualStyleAnimation = new(true);
    private ElementVisualStyleSnapshot _styleAnimationFrom;
    private ElementVisualStyleSnapshot _styleAnimationTo;
    private ElementVisualStyleSnapshot _styleBaseSnapshot;
    private ElementVisualStyleSnapshot _styleEffectiveSnapshot;
    private bool _hasVisualStyleBaseOverride;
    private bool _isPressed;
    private bool _isPointerOver;
    private bool _visualStylesEnabled;
    private bool _visualStylesInitialized;

    [Browsable(false)]
    public bool IsPressed => _isPressed;

    [Browsable(false)]
    public bool IsPointerOver => _isPointerOver;

    [Browsable(false)]
    public ElementVisualStateContext VisualState => CreateVisualStateContext();

    [Browsable(false)]
    public ElementVisualStyleCollection VisualStyles { get; }

    [Browsable(false)]
    public ElementVisualTransition VisualTransition { get; } = new();

    [Browsable(false)]
    public bool VisualStylesEnabled => _visualStylesEnabled;

    public ElementBase ConfigureVisualStyles(Action<ElementVisualStyleBuilder> configure, bool clearExisting = false)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ElementVisualStyleBuilder(this);
        if (clearExisting)
            builder.ClearRules();

        configure(builder);
        RefreshVisualStyles();
        return this;
    }

    public void ClearVisualStyles()
    {
        VisualStyles.Clear();
        _hasVisualStyleBaseOverride = false;
        UpdateVisualStylesEnabledState();
        if (!_visualStylesEnabled)
            return;

        RefreshVisualStyles(forceImmediate: true);
    }

    private void InitializeVisualStyleSystem()
    {
        _styleBaseSnapshot = CaptureCurrentSnapshot();
        _styleEffectiveSnapshot = _styleBaseSnapshot;
        _styleAnimationFrom = _styleBaseSnapshot;
        _styleAnimationTo = _styleBaseSnapshot;

        _visualStyleAnimation.AnimationType = VisualTransition.AnimationType;
        _visualStyleAnimation.Increment = VisualTransition.GetIncrement();
        _visualStyleAnimation.SecondaryIncrement = _visualStyleAnimation.Increment;
        _visualStyleAnimation.InterruptAnimation = true;
        _visualStyleAnimation.OnAnimationProgress += HandleVisualStyleAnimationProgress;
        _visualStyleAnimation.OnAnimationFinished += HandleVisualStyleAnimationFinished;

        _visualStylesInitialized = true;
    }

    internal void OnVisualStyleDefinitionsChanged()
    {
        UpdateVisualStylesEnabledState();
        if (!_visualStylesEnabled)
            return;

        RefreshVisualStyles();
    }

    public void ReevaluateVisualStyles()
    {
        if (!_visualStylesEnabled)
            return;

        RefreshVisualStyles();
    }

    public void ApplyVisualStyleBase(ElementVisualStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        var snapshot = _styleBaseSnapshot;
        style.ApplyTo(ref snapshot);
        _styleBaseSnapshot = snapshot;
        _hasVisualStyleBaseOverride = true;
        UpdateVisualStylesEnabledState();

        _size = snapshot.Size;
        _backColor = snapshot.BackColor;
        _foreColor = snapshot.ForeColor;
        _border = snapshot.Border;
        _borderColor = snapshot.BorderColor;
        _radius = snapshot.Radius;
        _shadows = ElementVisualStyleInterpolator.CloneShadows(snapshot.Shadows);
        _opacity = snapshot.Opacity;

        RefreshVisualStyles(forceImmediate: true);
    }

    private void RefreshVisualStyles(bool forceImmediate = false)
    {
        if (!_visualStylesInitialized || !_visualStylesEnabled)
            return;

        var targetTransition = VisualTransition;
        var targetSnapshot = ResolveVisualStyleSnapshot(ref targetTransition);
        if (_styleAnimationTo.Equals(targetSnapshot) && !_visualStyleAnimation.IsAnimating())
            return;

        _styleAnimationTo = targetSnapshot;

        if (forceImmediate || !targetTransition.Enabled || targetTransition.Duration <= TimeSpan.Zero)
        {
            _visualStyleAnimation.SetProgress(1);
            ApplyEffectiveVisualStyle(targetSnapshot);
            return;
        }

        _styleAnimationFrom = _styleEffectiveSnapshot;
        if (_styleAnimationFrom.Equals(targetSnapshot))
            return;

        _visualStyleAnimation.AnimationType = targetTransition.AnimationType;
        _visualStyleAnimation.Increment = targetTransition.GetIncrement();
        _visualStyleAnimation.SecondaryIncrement = _visualStyleAnimation.Increment;
        _visualStyleAnimation.SetProgress(0);
        _visualStyleAnimation.StartNewAnimation(AnimationDirection.In);
    }

    private ElementVisualStyleSnapshot ResolveVisualStyleSnapshot(ref ElementVisualTransition transition)
    {
        var snapshot = _styleBaseSnapshot;
        var state = CreateVisualStateContext();

        for (var i = 0; i < VisualStyles.Count; i++)
        {
            var rule = VisualStyles[i];
            if (!rule.Matches(this, state))
                continue;

            rule.Style.ApplyTo(ref snapshot);
            if (rule.Transition != null)
                transition = rule.Transition;
        }

        return snapshot;
    }

    private ElementVisualStateContext CreateVisualStateContext()
    {
        var states = ElementVisualStates.None;

        if (_isPointerOver)
            states |= ElementVisualStates.PointerOver;

        if (_isPressed)
            states |= ElementVisualStates.Pressed;

        if (Focused)
            states |= ElementVisualStates.Focused;

        if (!Enabled)
            states |= ElementVisualStates.Disabled;

        if (!Visible)
            states |= ElementVisualStates.Hidden;

        return new ElementVisualStateContext(this, states);
    }

    private void HandleVisualStyleAnimationProgress(object _)
    {
        var snapshot = ElementVisualStyleInterpolator.Interpolate(
            _styleAnimationFrom,
            _styleAnimationTo,
            (float)_visualStyleAnimation.GetProgress());
        ApplyEffectiveVisualStyle(snapshot);
    }

    private void HandleVisualStyleAnimationFinished(object _)
    {
        ApplyEffectiveVisualStyle(_styleAnimationTo);
    }

    private void ApplyEffectiveVisualStyle(in ElementVisualStyleSnapshot snapshot)
    {
        var previousSize = _size;
        var previousBackColor = _backColor;
        var previousForeColor = _foreColor;
        var previousBorder = _border;
        var previousBorderColor = _borderColor;
        var previousRadius = _radius;
        var previousShadows = _shadows;
        var previousOpacity = _opacity;

        _size = snapshot.Size;
        _backColor = snapshot.BackColor;
        _foreColor = snapshot.ForeColor;
        _border = snapshot.Border;
        _borderColor = snapshot.BorderColor;
        _radius = snapshot.Radius;
        _shadows = ElementVisualStyleInterpolator.CloneShadows(snapshot.Shadows);
        _opacity = snapshot.Opacity;
        _styleEffectiveSnapshot = new ElementVisualStyleSnapshot(
            _size,
            _backColor,
            _foreColor,
            _border,
            _borderColor,
            _radius,
            _shadows,
            _opacity);

        var sizeChanged = previousSize != _size;
        var visualsChanged = previousBackColor != _backColor ||
                             previousForeColor != _foreColor ||
                             previousBorder != _border ||
                             previousBorderColor != _borderColor ||
                             previousRadius != _radius ||
                             Math.Abs(previousOpacity - _opacity) > 0.0001f ||
                             !ElementVisualStyleInterpolator.AreShadowsEqual(previousShadows, _shadows);

        if (sizeChanged)
            HandleEffectiveSizeChanged();
        else if (visualsChanged)
            Invalidate();
    }

    private void HandleEffectiveSizeChanged()
    {
        if (!IsPerformingLayout && Controls.Count > 0)
            PerformLayout();

        Invalidate();
    }

    private ElementVisualStyleSnapshot CaptureCurrentSnapshot()
    {
        return new ElementVisualStyleSnapshot(
            _size,
            _backColor,
            _foreColor,
            _border,
            _borderColor,
            _radius,
            ElementVisualStyleInterpolator.CloneShadows(_shadows),
            _opacity);
    }

    private void SetStyleBaseSize(SKSize size)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithSize(size);
    }

    private void SetStyleBaseBackColor(SKColor color)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithBackColor(color);
    }

    private void SetStyleBaseForeColor(SKColor color)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithForeColor(color);
    }

    private void SetStyleBaseBorder(Thickness border)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithBorder(border);
    }

    private void SetStyleBaseBorderColor(SKColor color)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithBorderColor(color);
    }

    private void SetStyleBaseRadius(Radius radius)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithRadius(radius);
    }

    private void SetStyleBaseShadows(BoxShadow[] shadows)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithShadows(ElementVisualStyleInterpolator.CloneShadows(shadows));
    }

    private void SetStyleBaseOpacity(float opacity)
    {
        _styleBaseSnapshot = _styleBaseSnapshot.WithOpacity(opacity);
    }

    private void UpdatePointerOverState(bool isPointerOver)
    {
        if (!_visualStylesEnabled)
            return;

        if (_isPointerOver == isPointerOver)
            return;

        _isPointerOver = isPointerOver;
        RefreshVisualStyles();
    }

    private void UpdatePressedState(bool isPressed)
    {
        if (!_visualStylesEnabled)
            return;

        if (_isPressed == isPressed)
            return;

        _isPressed = isPressed;
        RefreshVisualStyles();
    }

    private void RefreshVisualStylesForStateChange()
    {
        if (!_visualStylesEnabled)
            return;

        RefreshVisualStyles();
    }

    private void UpdateVisualStylesEnabledState()
    {
        var wasEnabled = _visualStylesEnabled;
        _visualStylesEnabled = _hasVisualStyleBaseOverride || VisualStyles.Count > 0;

        if (wasEnabled && !_visualStylesEnabled)
        {
            _visualStyleAnimation.SetProgress(1);
            _styleAnimationFrom = _styleBaseSnapshot;
            _styleAnimationTo = _styleBaseSnapshot;
            ApplyEffectiveVisualStyle(_styleBaseSnapshot);
        }
    }

    private void DisposeVisualStyleSystem()
    {
        _visualStyleAnimation.OnAnimationProgress -= HandleVisualStyleAnimationProgress;
        _visualStyleAnimation.OnAnimationFinished -= HandleVisualStyleAnimationFinished;
        _visualStyleAnimation.Dispose();
    }
}