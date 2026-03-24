using SDUI.Animation;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Globalization;

namespace SDUI.Controls;

public sealed partial class ColorPicker : ElementBase
{
    // ── Layout constants ────────────────────────────────────────────────────
    private const int DefaultWidth      = 320;
    private const int DefaultHeight     = 372;
    private const int DefaultMinWidth   = 248;
    private const int DefaultMinHeight  = 280;

    private const float PaddingBase     = 16f;
    private const float GapBase         = 12f;
    private const float HueWidthBase    = 18f;
    private const float AlphaHeightBase = 16f;
    private const float FooterHeightBase = 76f;
    private const float SwatchSizeBase  = 38f;
    private const float CircleRadiusBase = 7f;
    private const float StripRadiusBase  = 9f;
    private const float SvRadiusBase     = 12f;

    // ── Interaction state ───────────────────────────────────────────────────
    private enum HitTarget { None, SaturationValue, Hue, Alpha, Reference }

    private HitTarget _activeTarget;
    private HitTarget _hoverTarget;
    private bool      _isDragging;

    // ── HSV + alpha ─────────────────────────────────────────────────────────
    private float    _hue        = 210f;
    private float    _saturation = 0.86f;
    private float    _value      = 0.95f;
    private float    _alphaValue = 1f;
    private SKColor  _selectedColor   = new(33, 150, 243);
    private SKColor  _referenceColor  = new(33, 150, 243);
    private bool     _showAlphaChannel   = true;
    private bool     _showReferenceSwatch = true;

    // ── Shader cache ─────────────────────────────────────────────────────────
    private SKShader? _alphaShader;
    private SKShader? _hueShader;
    private SKShader? _svSaturationShader;
    private SKShader? _svValueShader;
    private SKRect    _cachedAlphaRect         = SKRect.Empty;
    private SKRect    _cachedHueRect           = SKRect.Empty;
    private SKRect    _cachedSvRect            = SKRect.Empty;
    private SKColor   _cachedAlphaShaderColor  = SKColors.Empty;

    // ── Paint / font resources ───────────────────────────────────────────────
    private SKPaint? _borderPaint;
    private SKPaint? _captionPaint;
    private SKPaint? _checkerDarkPaint;
    private SKPaint? _checkerLightPaint;
    private SKPaint? _fillPaint;
    private SKPaint? _markerFillPaint;
    private SKPaint? _markerStrokePaint;
    private SKPaint? _textPaint;
    private SKFont?  _captionFont;
    private SKFont?  _detailFont;
    private SKFont?  _fontSource;
    private int      _fontDpi;

    // ── Layout snapshot (recomputed only when needed) ────────────────────────
    private readonly record struct PickerLayout(
        SKRect SaturationValueRect,
        SKRect HueRect,
        SKRect AlphaRect,
        SKRect CurrentSwatchRect,
        SKRect ReferenceSwatchRect,
        SKRect DetailsRect,
        float  FooterTop,
        float  FooterHeight);

    // ════════════════════════════════════════════════════════════════════════
    //  Construction
    // ════════════════════════════════════════════════════════════════════════

    public ColorPicker()
    {
        CanSelect   = true;
        TabStop     = true;
        MinimumSize = new SKSize(DefaultMinWidth, DefaultMinHeight);
        Size        = new SKSize(DefaultWidth, DefaultHeight);
        Padding     = new Thickness(16);
        Radius      = new Radius(20);

        Border      = new Thickness(1);
        BorderColor = ColorScheme.Outline.WithAlpha(118);
        BackColor   = ColorScheme.Surface;
        ForeColor   = ColorScheme.ForeColor;

        ConfigureVisualStyles(styles => styles
            .DefaultTransition(TimeSpan.FromMilliseconds(160), AnimationType.CubicEaseOut)
            .Base(s => s
                .Background(ColorScheme.Surface)
                .Foreground(ColorScheme.ForeColor)
                .Border(1)
                .BorderColor(ColorScheme.Outline.WithAlpha(118))
                .Radius(20)
                .Shadow(new BoxShadow(0f, 6f, 18f, 0, ColorScheme.ShadowColor.WithAlpha(22))))
            .OnHover(s => s
                .Background(ColorScheme.SurfaceContainer)
                .BorderColor(ColorScheme.Primary.WithAlpha(78)))
            .OnFocused(s => s
                .Background(ColorScheme.SurfaceContainerHigh)
                .BorderColor(ColorScheme.Primary.WithAlpha(148)))
            .OnPressed(s => s
                .Background(ColorScheme.SurfaceContainerHigh)
                .BorderColor(ColorScheme.Primary.WithAlpha(164))
                .Opacity(0.98f))
            .OnDisabled(s => s
                .Background(ColorScheme.SurfaceVariant)
                .Foreground(ColorScheme.ForeColor.WithAlpha(168))
                .BorderColor(ColorScheme.Outline.WithAlpha(96))
                .Opacity(0.84f)
                .Shadow(BoxShadow.None)));

        SyncFromColor(_selectedColor);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Public properties
    // ════════════════════════════════════════════════════════════════════════

    [Category("Appearance"), DefaultValue(true)]
    public bool ShowAlphaChannel
    {
        get => _showAlphaChannel;
        set
        {
            if (_showAlphaChannel == value) return;
            _showAlphaChannel = value;
            if (!_showAlphaChannel) _alphaValue = 1f;
            UpdateSelectedColor(raiseChanged: true, raiseCommitted: false);
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Appearance"), DefaultValue(true)]
    public bool ShowReferenceSwatch
    {
        get => _showReferenceSwatch;
        set
        {
            if (_showReferenceSwatch == value) return;
            _showReferenceSwatch = value;
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor ReferenceColor
    {
        get => _referenceColor;
        set
        {
            if (_referenceColor == value) return;
            _referenceColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (_selectedColor == value) return;
            SyncFromColor(value);
            Invalidate();
            SelectedColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Browsable(false)]
    public string HexValue => ShowAlphaChannel
        ? $"#{_selectedColor.Alpha:X2}{_selectedColor.Red:X2}{_selectedColor.Green:X2}{_selectedColor.Blue:X2}"
        : $"#{_selectedColor.Red:X2}{_selectedColor.Green:X2}{_selectedColor.Blue:X2}";

    [Browsable(false)]
    public float Hue
    {
        get => _hue;
        set
        {
            var clamped = NormalizeHue(value);
            if (Math.Abs(_hue - clamped) < 0.001f) return;
            _hue = clamped;
            UpdateSelectedColor(raiseChanged: true, raiseCommitted: false);
        }
    }

    [Browsable(false)]
    public float Saturation
    {
        get => _saturation;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_saturation - clamped) < 0.001f) return;
            _saturation = clamped;
            UpdateSelectedColor(raiseChanged: true, raiseCommitted: false);
        }
    }

    [Browsable(false)]
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_value - clamped) < 0.001f) return;
            _value = clamped;
            UpdateSelectedColor(raiseChanged: true, raiseCommitted: false);
        }
    }

    [Browsable(false)]
    public float AlphaValue
    {
        get => _alphaValue;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_alphaValue - clamped) < 0.001f) return;
            _alphaValue = clamped;
            UpdateSelectedColor(raiseChanged: true, raiseCommitted: false);
        }
    }

    public event EventHandler? SelectedColorChanged;
    public event EventHandler? SelectedColorCommitted;

    // ════════════════════════════════════════════════════════════════════════
    //  Layout
    // ════════════════════════════════════════════════════════════════════════

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        var w = Math.Max(DefaultWidth,  MinimumSize.Width  > 0 ? MinimumSize.Width  : DefaultMinWidth);
        var h = Math.Max(DefaultHeight, MinimumSize.Height > 0 ? MinimumSize.Height : DefaultMinHeight);

        if (MaximumSize.Width  > 0) w = Math.Min(w, MaximumSize.Width);
        if (MaximumSize.Height > 0) h = Math.Min(h, MaximumSize.Height);

        return new SKSize(w, h);
    }

    private PickerLayout GetLayout()
    {
        var sf            = ScaleFactor;
        var padding       = Scale(PaddingBase,      sf, 12f);
        var gap           = Scale(GapBase,           sf, 10f);
        var hueWidth      = Scale(HueWidthBase,      sf, 16f);
        var alphaHeight   = ShowAlphaChannel ? Scale(AlphaHeightBase, sf, 14f) : 0f;
        var footerHeight  = Scale(FooterHeightBase,  sf, 68f);
        var swatchSize    = Scale(SwatchSizeBase,     sf, 34f);

        var availableW    = Math.Max(88f, Width  - padding * 2f - hueWidth - gap);
        var availableH    = Math.Max(88f, Height - padding * 2f - footerHeight
                                          - (ShowAlphaChannel ? alphaHeight + gap : 0f));
        var squareSize    = Math.Max(88f, Math.Min(availableW, availableH));

        var svRect        = SKRect.Create(padding,           padding,           squareSize, squareSize);
        var hueRect       = SKRect.Create(svRect.Right + gap, svRect.Top,       hueWidth,   squareSize);
        var alphaRect     = ShowAlphaChannel
                            ? SKRect.Create(svRect.Left, svRect.Bottom + gap, svRect.Width, alphaHeight)
                            : SKRect.Empty;

        var footerTop     = ShowAlphaChannel ? alphaRect.Bottom + gap : svRect.Bottom + gap;
        var swatchOffsetY = Scale(16f, sf, 14f);
        var swatchGap     = Scale(12f, sf, 10f);

        var currentSwatch   = SKRect.Create(padding, footerTop + swatchOffsetY, swatchSize, swatchSize);
        var referenceSwatch = ShowReferenceSwatch
                              ? SKRect.Create(currentSwatch.Right + swatchGap, currentSwatch.Top, swatchSize, swatchSize)
                              : SKRect.Empty;

        var detailsLeft     = ShowReferenceSwatch
                              ? referenceSwatch.Right + gap
                              : currentSwatch.Right + gap;
        var detailsRect     = SKRect.Create(
                              detailsLeft,
                              footerTop + Scale(8f, sf, 6f),
                              Math.Max(1f, Width - detailsLeft - padding),
                              footerHeight - Scale(12f, sf, 10f));

        return new PickerLayout(
            svRect, hueRect, alphaRect,
            currentSwatch, referenceSwatch, detailsRect,
            footerTop, footerHeight);
    }

    // Scales a base value by ScaleFactor, clamped to a minimum.
    private static float Scale(float baseValue, float scaleFactor, float minimum) =>
        Math.Max(minimum, baseValue * scaleFactor);

    // ════════════════════════════════════════════════════════════════════════
    //  Paint
    // ════════════════════════════════════════════════════════════════════════

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);
        InitializePaints();

        var layout = GetLayout();
        DrawSaturationValueSurface(canvas, layout.SaturationValueRect);
        DrawHueStrip(canvas, layout.HueRect);
        if (ShowAlphaChannel)
            DrawAlphaStrip(canvas, layout.AlphaRect);
        DrawFooter(canvas, layout);
        DrawHandles(canvas, layout);
    }

    private void DrawSaturationValueSurface(SKCanvas canvas, SKRect rect)
    {
        var radius = Scale(SvRadiusBase, ScaleFactor, 10f);
        var hot    = _hoverTarget == HitTarget.SaturationValue || _activeTarget == HitTarget.SaturationValue;
        DrawSurfaceFrame(canvas, rect, radius, hot);

        var save = canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);

        _fillPaint!.Shader = null;
        _fillPaint.Color   = HsvToColor(_hue, 1f, 1f, 255);
        canvas.DrawRect(rect, _fillPaint);

        EnsureSvShaders(rect);
        _fillPaint.Shader = _svSaturationShader;
        canvas.DrawRect(rect, _fillPaint);
        _fillPaint.Shader = _svValueShader;
        canvas.DrawRect(rect, _fillPaint);
        _fillPaint.Shader = null;

        canvas.RestoreToCount(save);
    }

    private void DrawHueStrip(SKCanvas canvas, SKRect rect)
    {
        var radius = Scale(StripRadiusBase, ScaleFactor, 8f);
        DrawSurfaceFrame(canvas, rect, radius, _hoverTarget == HitTarget.Hue || _activeTarget == HitTarget.Hue);

        var save = canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);
        EnsureHueShader(rect);
        _fillPaint!.Shader = _hueShader;
        canvas.DrawRect(rect, _fillPaint);
        _fillPaint.Shader = null;
        canvas.RestoreToCount(save);
    }

    private void DrawAlphaStrip(SKCanvas canvas, SKRect rect)
    {
        if (rect.IsEmpty) return;

        var radius = Scale(StripRadiusBase, ScaleFactor, 8f);
        DrawSurfaceFrame(canvas, rect, radius, _hoverTarget == HitTarget.Alpha || _activeTarget == HitTarget.Alpha);

        var save = canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);
        DrawCheckerboard(canvas, rect, Scale(7f, ScaleFactor, 6f));
        EnsureAlphaShader(rect);
        _fillPaint!.Shader = _alphaShader;
        canvas.DrawRect(rect, _fillPaint);
        _fillPaint.Shader = null;
        canvas.RestoreToCount(save);
    }

    private void DrawFooter(SKCanvas canvas, PickerLayout layout)
    {
        var sf            = ScaleFactor;
        var footerRect    = SKRect.Create(
                            layout.CurrentSwatchRect.Left,
                            layout.FooterTop,
                            Width - layout.CurrentSwatchRect.Left * 2f,
                            layout.FooterHeight);
        var footerRadius  = Scale(14f, sf, 12f);

        _fillPaint!.Color  = ColorScheme.SurfaceContainerHigh;
        _borderPaint!.Color = ColorScheme.Outline.WithAlpha(84);
        canvas.DrawRoundRect(footerRect, footerRadius, footerRadius, _fillPaint);
        canvas.DrawRoundRect(footerRect, footerRadius, footerRadius, _borderPaint);

        DrawSwatch(canvas, layout.CurrentSwatchRect,   _selectedColor,  highlighted: true);
        if (ShowReferenceSwatch)
            DrawSwatch(canvas, layout.ReferenceSwatchRect, _referenceColor, highlighted: _hoverTarget == HitTarget.Reference);

        var captionFont = GetCaptionFont();
        var detailFont  = GetDetailFont();

        var lineH    = Math.Max(16f, detailFont.Metrics.Descent  - detailFont.Metrics.Ascent);
        var captionH = Math.Max(14f, captionFont.Metrics.Descent - captionFont.Metrics.Ascent);
        var line1    = SKRect.Create(layout.DetailsRect.Left, layout.DetailsRect.Top,                           layout.DetailsRect.Width, captionH + 2f);
        var line2    = SKRect.Create(layout.DetailsRect.Left, line1.Bottom + 2f * sf,                           layout.DetailsRect.Width, lineH    + 2f);
        var line3    = SKRect.Create(layout.DetailsRect.Left, line2.Bottom,                                     layout.DetailsRect.Width, lineH    + 2f);

        _captionPaint!.Color = ForeColor.WithAlpha(156);
        _textPaint!.Color    = ForeColor;

        DrawControlText(canvas, "Selected color",      line1, _captionPaint, captionFont, ContentAlignment.MiddleLeft, false, false);
        DrawControlText(canvas, HexValue,              line2, _textPaint,    detailFont,  ContentAlignment.MiddleLeft, false, false);
        DrawControlText(canvas, FormatColorMetadata(), line3, _captionPaint, captionFont, ContentAlignment.MiddleLeft, false, false);
    }

    /// <summary>Draws a color swatch with a checkerboard background and a rounded border.</summary>
    private void DrawSwatch(SKCanvas canvas, SKRect rect, SKColor color, bool highlighted)
    {
        var radius     = Scale(10f, ScaleFactor, 8f);
        var swatchSave = canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius), antialias: true);
        DrawCheckerboard(canvas, rect, Scale(7f, ScaleFactor, 6f));
        _fillPaint!.Color = color;
        canvas.DrawRoundRect(rect, radius, radius, _fillPaint);
        canvas.RestoreToCount(swatchSave);

        _borderPaint!.Color = highlighted
            ? ColorScheme.Primary.WithAlpha(164)
            : ColorScheme.Outline.WithAlpha(112);
        canvas.DrawRoundRect(rect, radius, radius, _borderPaint);
    }

    private void DrawHandles(SKCanvas canvas, PickerLayout layout)
    {
        var sf = ScaleFactor;

        // ── SV circle handle ────────────────────────────────────────────────
        var svX      = layout.SaturationValueRect.Left + _saturation * layout.SaturationValueRect.Width;
        var svY      = layout.SaturationValueRect.Top  + (1f - _value)  * layout.SaturationValueRect.Height;
        var circleR  = Scale(CircleRadiusBase, sf, 6f);

        // Outer shadow ring
        _markerStrokePaint!.Color = SKColors.Black.WithAlpha(120);
        _markerStrokePaint.StrokeWidth = 2f * sf;
        canvas.DrawCircle(svX, svY, circleR + 2.5f * sf, _markerStrokePaint);

        // White fill
        _markerFillPaint!.Color = SKColors.White;
        canvas.DrawCircle(svX, svY, circleR + 1.5f * sf, _markerFillPaint);

        // Color center dot
        _markerFillPaint.Color = HsvToColor(_hue, _saturation, _value, 255);
        canvas.DrawCircle(svX, svY, circleR - 1.5f * sf, _markerFillPaint);

        // Inner crisp border
        _markerStrokePaint.Color = SKColors.Black.WithAlpha(60);
        _markerStrokePaint.StrokeWidth = 1f * sf;
        canvas.DrawCircle(svX, svY, circleR - 1.5f * sf, _markerStrokePaint);

        // ── Hue bar handle ───────────────────────────────────────────────────
        var hueY = layout.HueRect.Top + (1f - _hue / 360f) * layout.HueRect.Height;
        DrawBarHandle(canvas,
            SKRect.Create(
                layout.HueRect.Left - 2f * sf,
                hueY - 3f * sf,
                layout.HueRect.Width + 4f * sf,
                6f * sf),
            sf);

        // ── Alpha bar handle ─────────────────────────────────────────────────
        if (ShowAlphaChannel && !layout.AlphaRect.IsEmpty)
        {
            var alphaX = layout.AlphaRect.Left + _alphaValue * layout.AlphaRect.Width;
            DrawBarHandle(canvas,
                SKRect.Create(
                    alphaX - 3f * sf,
                    layout.AlphaRect.Top - 2f * sf,
                    6f * sf,
                    layout.AlphaRect.Height + 4f * sf),
                sf);
        }
    }

    /// <summary>Draws a rounded-rectangle slider handle (used for hue and alpha strips).</summary>
    private void DrawBarHandle(SKCanvas canvas, SKRect rect, float sf)
    {
        var rx = 14f * sf;

        _markerFillPaint!.Color    = SKColors.White.WithAlpha(230);
        _markerStrokePaint!.Color  = SKColors.Black.WithAlpha(140);
        _markerStrokePaint.StrokeWidth = 1f * sf;

        canvas.DrawRoundRect(rect, rx, rx, _markerFillPaint);
        canvas.DrawRoundRect(rect, rx, rx, _markerStrokePaint);
    }

    private void DrawSurfaceFrame(SKCanvas canvas, SKRect rect, float radius, bool hot)
    {
        _fillPaint!.Color   = ColorScheme.SurfaceContainer;
        _borderPaint!.Color = hot
            ? ColorScheme.Primary.WithAlpha(152)
            : ColorScheme.Outline.WithAlpha(92);
        canvas.DrawRoundRect(rect, radius, radius, _fillPaint);
        canvas.DrawRoundRect(rect, radius, radius, _borderPaint);
    }

    private void DrawCheckerboard(SKCanvas canvas, SKRect rect, float cellSize)
    {
        var save = canvas.Save();
        canvas.ClipRect(rect);

        var row = 0;
        for (var y = rect.Top; y < rect.Bottom; y += cellSize, row++)
        {
            var col = row % 2;
            for (var x = rect.Left; x < rect.Right; x += cellSize, col++)
            {
                var tile = SKRect.Create(x, y,
                    Math.Min(cellSize, rect.Right  - x),
                    Math.Min(cellSize, rect.Bottom - y));
                canvas.DrawRect(tile, col % 2 == 0 ? _checkerLightPaint : _checkerDarkPaint);
            }
        }

        canvas.RestoreToCount(save);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Input handling
    // ════════════════════════════════════════════════════════════════════════

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;

        var layout = GetLayout();
        var hit    = HitTest(e.Location, layout);

        if (hit == HitTarget.Reference && ShowReferenceSwatch)
        {
            ApplyReferenceColor();
            return;
        }

        if (hit == HitTarget.None) return;

        _activeTarget = hit;
        _isDragging   = true;
        GetParentWindow()?.SetMouseCapture(this);
        UpdateFromInteraction(e.Location, layout, commit: false);
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var layout = GetLayout();

        if (_isDragging)
        {
            UpdateFromInteraction(e.Location, layout, commit: false);
            return;
        }

        var hit = HitTest(e.Location, layout);
        if (_hoverTarget == hit) return;

        _hoverTarget = hit;
        Cursor       = _hoverTarget == HitTarget.Reference ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        var wasDragging = _isDragging;
        _isDragging     = false;
        _activeTarget   = HitTarget.None;
        GetParentWindow()?.ReleaseMouseCapture(this);

        if (wasDragging)
            SelectedColorCommitted?.Invoke(this, EventArgs.Empty);
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_isDragging) return;

        if (_hoverTarget != HitTarget.None)
        {
            _hoverTarget = HitTarget.None;
            Cursor       = Cursors.Default;
            Invalidate();
        }
    }

    internal override void OnLostFocus(EventArgs e)
    {
        if (_isDragging)
        {
            _isDragging   = false;
            _activeTarget = HitTarget.None;
            GetParentWindow()?.ReleaseMouseCapture(this);
        }

        base.OnLostFocus(e);
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateShaderCaches();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Hit testing & interaction
    // ════════════════════════════════════════════════════════════════════════

    private HitTarget HitTest(SKPoint point, PickerLayout layout)
    {
        if (layout.SaturationValueRect.Contains(point)) return HitTarget.SaturationValue;
        if (layout.HueRect.Contains(point))             return HitTarget.Hue;
        if (ShowAlphaChannel && layout.AlphaRect.Contains(point))          return HitTarget.Alpha;
        if (ShowReferenceSwatch && layout.ReferenceSwatchRect.Contains(point)) return HitTarget.Reference;
        return HitTarget.None;
    }

    private void UpdateFromInteraction(SKPoint point, PickerLayout layout, bool commit)
    {
        switch (_activeTarget)
        {
            case HitTarget.SaturationValue:
            {
                var x = Math.Clamp(point.X, layout.SaturationValueRect.Left, layout.SaturationValueRect.Right);
                var y = Math.Clamp(point.Y, layout.SaturationValueRect.Top,  layout.SaturationValueRect.Bottom);
                _saturation = layout.SaturationValueRect.Width  <= 0f ? 0f : (x - layout.SaturationValueRect.Left) / layout.SaturationValueRect.Width;
                _value      = layout.SaturationValueRect.Height <= 0f ? 0f : 1f - (y - layout.SaturationValueRect.Top) / layout.SaturationValueRect.Height;
                break;
            }
            case HitTarget.Hue:
            {
                var y          = Math.Clamp(point.Y, layout.HueRect.Top, layout.HueRect.Bottom);
                var normalized = layout.HueRect.Height <= 0f ? 0f : (y - layout.HueRect.Top) / layout.HueRect.Height;
                _hue           = NormalizeHue((1f - normalized) * 360f);
                break;
            }
            case HitTarget.Alpha when ShowAlphaChannel:
            {
                var x   = Math.Clamp(point.X, layout.AlphaRect.Left, layout.AlphaRect.Right);
                _alphaValue = layout.AlphaRect.Width <= 0f ? 1f : (x - layout.AlphaRect.Left) / layout.AlphaRect.Width;
                break;
            }
        }

        UpdateSelectedColor(raiseChanged: true, raiseCommitted: commit);
    }

    private void ApplyReferenceColor()
    {
        SelectedColor = ReferenceColor;
        SelectedColorCommitted?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Resource management
    // ════════════════════════════════════════════════════════════════════════

    private void InitializePaints()
    {
        var strokeWidth = Math.Max(1f, ScaleFactor);

        _fillPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _borderPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth };
        _textPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ForeColor };
        _captionPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ForeColor.WithAlpha(156) };
        _checkerLightPaint ??= new SKPaint { IsAntialias = false, Style = SKPaintStyle.Fill, Color = ColorScheme.Surface };
        _checkerDarkPaint  ??= new SKPaint { IsAntialias = false, Style = SKPaintStyle.Fill, Color = ColorScheme.SurfaceVariant.Brightness(-0.04f) };
        _markerFillPaint   ??= new SKPaint { IsAntialias = true,  Style = SKPaintStyle.Fill,   Color = SKColors.White };
        _markerStrokePaint ??= new SKPaint { IsAntialias = true,  Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1.5f, 1.5f * ScaleFactor), Color = SKColors.Black.WithAlpha(188) };
    }

    private SKFont GetDetailFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        if (_detailFont is not null && ReferenceEquals(_fontSource, Font) && _fontDpi == dpi)
            return _detailFont;

        _detailFont?.Dispose();
        _captionFont?.Dispose();

        _detailFont  = CreateRenderFont(Font);
        _captionFont = CreateRenderFont(Font);
        _captionFont.Size = Math.Max(10f, _captionFont.Size * 0.82f);

        _fontSource = Font;
        _fontDpi    = dpi;
        return _detailFont;
    }

    private SKFont GetCaptionFont()
    {
        _ = GetDetailFont();
        return _captionFont!;
    }

    protected override void InvalidateFontCache()
    {
        base.InvalidateFontCache();
        _detailFont?.Dispose();  _detailFont  = null;
        _captionFont?.Dispose(); _captionFont = null;
        _fontSource = null;
        _fontDpi    = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fillPaint?.Dispose();
            _borderPaint?.Dispose();
            _textPaint?.Dispose();
            _captionPaint?.Dispose();
            _checkerLightPaint?.Dispose();
            _checkerDarkPaint?.Dispose();
            _markerFillPaint?.Dispose();
            _markerStrokePaint?.Dispose();
            _detailFont?.Dispose();
            _captionFont?.Dispose();
            InvalidateShaderCaches();
        }

        base.Dispose(disposing);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Shader cache
    // ════════════════════════════════════════════════════════════════════════

    private void EnsureSvShaders(SKRect rect)
    {
        if (_svSaturationShader is not null && _svValueShader is not null && _cachedSvRect == rect)
            return;

        _svSaturationShader?.Dispose();
        _svValueShader?.Dispose();

        _svSaturationShader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Right, rect.Top),
            [SKColors.White, SKColors.White.WithAlpha(0)], SKShaderTileMode.Clamp);
        _svValueShader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Left, rect.Bottom),
            [SKColors.Black.WithAlpha(0), SKColors.Black], SKShaderTileMode.Clamp);

        _cachedSvRect = rect;
    }

    private void EnsureHueShader(SKRect rect)
    {
        if (_hueShader is not null && _cachedHueRect == rect) return;

        _hueShader?.Dispose();
        _hueShader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Bottom),
            new SKPoint(rect.Left, rect.Top),
            [
                new SKColor(255, 0,   0),
                new SKColor(255, 255, 0),
                new SKColor(0,   255, 0),
                new SKColor(0,   255, 255),
                new SKColor(0,   0,   255),
                new SKColor(255, 0,   255),
                new SKColor(255, 0,   0)
            ],
            [0f, 0.17f, 0.33f, 0.5f, 0.67f, 0.83f, 1f],
            SKShaderTileMode.Clamp);

        _cachedHueRect = rect;
    }

    private void EnsureAlphaShader(SKRect rect)
    {
        var color = HsvToColor(_hue, _saturation, _value, 255);
        if (_alphaShader is not null && _cachedAlphaRect == rect && _cachedAlphaShaderColor == color)
            return;

        _alphaShader?.Dispose();
        _alphaShader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Right, rect.Top),
            [color.WithAlpha(0), color.WithAlpha(255)], SKShaderTileMode.Clamp);

        _cachedAlphaRect        = rect;
        _cachedAlphaShaderColor = color;
    }

    private void InvalidateShaderCaches()
    {
        _svSaturationShader?.Dispose(); _svSaturationShader = null;
        _svValueShader?.Dispose();      _svValueShader      = null;
        _hueShader?.Dispose();          _hueShader          = null;
        _alphaShader?.Dispose();        _alphaShader        = null;

        _cachedSvRect           = SKRect.Empty;
        _cachedHueRect          = SKRect.Empty;
        _cachedAlphaRect        = SKRect.Empty;
        _cachedAlphaShaderColor = SKColors.Empty;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Color state
    // ════════════════════════════════════════════════════════════════════════

    private void SyncFromColor(SKColor color)
    {
        _selectedColor = color;
        RgbToHsv(color, out _hue, out _saturation, out _value);
        _alphaValue = color.Alpha / 255f;
        InvalidateShaderCaches();
    }

    private void UpdateSelectedColor(bool raiseChanged, bool raiseCommitted)
    {
        var alpha = ShowAlphaChannel ? (byte)Math.Round(_alphaValue * 255f) : (byte)255;
        var next  = HsvToColor(_hue, _saturation, _value, alpha);

        if (_selectedColor == next)
        {
            Invalidate();
            return;
        }

        _selectedColor = next;
        InvalidateShaderCaches();
        Invalidate();

        if (raiseChanged)   SelectedColorChanged?.Invoke(this, EventArgs.Empty);
        if (raiseCommitted) SelectedColorCommitted?.Invoke(this, EventArgs.Empty);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private string FormatColorMetadata()
    {
        var rgb = FormattableString.Invariant(
            $"RGB {_selectedColor.Red}, {_selectedColor.Green}, {_selectedColor.Blue}");
        var hsv = FormattableString.Invariant(
            $"HSV {Math.Round(_hue),0}°, {Math.Round(_saturation * 100f),0}%, {Math.Round(_value * 100f),0}%");

        return ShowAlphaChannel
            ? $"{rgb}   A {Math.Round(_alphaValue * 100f).ToString(CultureInfo.InvariantCulture)}%   {hsv}"
            : $"{rgb}   {hsv}";
    }

    private static float NormalizeHue(float hue)
    {
        var h = hue % 360f;
        return h < 0f ? h + 360f : h;
    }

    private static SKColor HsvToColor(float hue, float saturation, float value, byte alpha)
    {
        hue        = NormalizeHue(hue);
        saturation = Math.Clamp(saturation, 0f, 1f);
        value      = Math.Clamp(value,      0f, 1f);

        if (saturation <= 0.0001f)
        {
            var gray = (byte)Math.Round(value * 255f);
            return new SKColor(gray, gray, gray, alpha);
        }

        var sector   = hue / 60f;
        var index    = (int)Math.Floor(sector);
        var fraction = sector - index;
        var p = value * (1f - saturation);
        var q = value * (1f - saturation * fraction);
        var t = value * (1f - saturation * (1f - fraction));

        var (r, g, b) = index switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };

        return new SKColor(
            (byte)Math.Round(r * 255f),
            (byte)Math.Round(g * 255f),
            (byte)Math.Round(b * 255f),
            alpha);
    }

    private static void RgbToHsv(SKColor color,
        out float hue, out float saturation, out float value)
    {
        color.ToHsv(out hue, out saturation, out value);

        var r = color.Red   / 255f;
        var g = color.Green / 255f;
        var b = color.Blue  / 255f;

        var max   = Math.Max(r, Math.Max(g, b));
        var min   = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        value      = max;
        saturation = max <= 0.0001f ? 0f : delta / max;

        if (delta <= 0.0001f) { hue = 0f; return; }

        if      (Math.Abs(max - r) < 0.0001f) hue = 60f * (((g - b) / delta) % 6f);
        else if (Math.Abs(max - g) < 0.0001f) hue = 60f * (((b - r) / delta) + 2f);
        else                                   hue = 60f * (((r - g) / delta) + 4f);

        hue = NormalizeHue(hue);
    }
}