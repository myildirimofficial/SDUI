using SDUI.Animation;
using SDUI.Helpers;
using SkiaSharp;
using System;
using System.ComponentModel;

namespace SDUI.Controls;

public class Button : UIElementBase
{
    private readonly AnimationManager animationManager;
    private readonly AnimationManager hoverAnimationManager;
    private readonly AnimationManager pressAnimationManager;

    // Render-path caches to avoid per-frame allocations
    private readonly Dictionary<string, SKPaint> _paintCache = new();
    private readonly Dictionary<string, SKFont> _fontCache = new();

    private SKImage _cachedImage;

    private int _elevation = 1;

    private int _mouseState;
    private bool _needsRedraw = true;

    private int _radius = 6;
    private SKPoint? _rippleCenter;

    private float _shadowDepth = 4f;
    private SkiaSharp.SKSize _textSize;
    
    // DPI Scaled properties
    private int RadiusScaled => (int)(_radius * ScaleFactor);
    private float ShadowDepthScaled => _shadowDepth * ScaleFactor;

    public Button()
    {
        TabStop = true;

        // Ripple animasyonu - her tıklamada yeni başlar
        animationManager = new AnimationManager(true)
        {
            Increment = 0.05,
            AnimationType = AnimationType.EaseOut,
            InterruptAnimation = true
        };

        // Hover animasyonu - smooth geçişler
        hoverAnimationManager = new AnimationManager(true)
        {
            Increment = 0.10,
            AnimationType = AnimationType.EaseInOut,
            InterruptAnimation = true
        };

        hoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        animationManager.OnAnimationProgress += sender => Invalidate();

        pressAnimationManager = new AnimationManager(true)
        {
            Increment = 0.18,
            AnimationType = AnimationType.EaseInOut,
            InterruptAnimation = true
        };
        pressAnimationManager.OnAnimationProgress += sender => Invalidate();
    }

    [Category("Appearance")] public SKImage Image { get; set; }

    [Category("Appearance")] public SkiaSharp.SKColor Color { get; set; } = SKColors.Transparent;

    [Category("Behavior")] public bool IsDefault { get; set; }

    [Category("Behavior")] public bool IsCancel { get; set; }

    [Category("Appearance")]
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

    [Category("Appearance")]
    public int Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;

            _radius = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public int Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value)
                return;

            _elevation = Math.Clamp(value, 0, 5);
            Invalidate();
        }
    }

    [Category("Behavior")] public DialogResult DialogResult { get; set; }

    public void NotifyDefault(bool value)
    {
        //throw new NotImplementedException();
    }

    private void InvalidateCache()
    {
        _needsRedraw = true;
        _cachedImage?.Dispose();
        _cachedImage = null;
    }

    internal override void OnTextChanged(EventArgs e)
    {
        InvalidateCache();
        base.OnTextChanged(e);
        
        // Use ProcessedText for measurement (escape sequences already handled)
        using (var paint = new SKPaint())
        {
            paint.TextSize = Font.Size.Topx(this);
            paint.Typeface = FontManager.GetSKTypeface(Font);
            var metrics = paint.FontMetrics;
            _textSize = new SkiaSharp.SKSize(paint.MeasureText(ProcessedText), metrics.Descent - metrics.Ascent);
        }

        if (AutoSize)
            base.Size = GetPreferredSize(SkiaSharp.SKSize.Empty);
    }

    public override void OnClick(EventArgs e)
    {
        if (DialogResult != DialogResult.None && FindForm() is UIWindowBase form) form.DialogResult = DialogResult;
        base.OnClick(e);
    }

    internal override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
        {
            PerformClick();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    internal override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    internal override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        RestartAnimation(pressAnimationManager, AnimationDirection.Out);
        Invalidate();
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _mouseState = 2;
        _rippleCenter = e.Location;
        RestartAnimation(animationManager, AnimationDirection.In, e.Location);
        RestartAnimation(pressAnimationManager, AnimationDirection.In);
        Invalidate();
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _mouseState = 1;
        RestartAnimation(pressAnimationManager, AnimationDirection.Out);
        Invalidate();
    }

    internal override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _mouseState = 1;
        RestartAnimation(hoverAnimationManager, AnimationDirection.In);
        Invalidate();
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _mouseState = 0;
        RestartAnimation(hoverAnimationManager, AnimationDirection.Out);
        RestartAnimation(pressAnimationManager, AnimationDirection.Out);
        Invalidate();
    }

    public override void OnPaint(SKCanvas canvas)
    {

        var hoverProgress = (float)hoverAnimationManager.GetProgress();
        var pressProgress = (float)pressAnimationManager.GetProgress();
        var rippleProgress = (float)animationManager.GetProgress();

        // Use the full bounds for the button body
        // Shadow will be drawn outside thanks to UIElementBase clipping changes
        var bodyRect = new SkiaSharp.SKRect(0, 0, Width, Height);

        canvas.Save();

        // Calculate dynamic elevation based on state
        var currentElevation = _elevation;
        if (Enabled)
        {
            if (pressProgress > 0) currentElevation += 1;
            else if (hoverProgress > 0) currentElevation += 1;
        }

        currentElevation = Math.Min(currentElevation, 5);

        // Draw elevation shadow
        if (Enabled && currentElevation > 0) 
            ElevationHelper.DrawElevation(canvas, bodyRect, RadiusScaled, currentElevation);

        ElevationHelper.DrawGlassEffect(canvas, bodyRect, RadiusScaled, 0.05f);
        
        DrawButton(canvas, bodyRect, hoverProgress, pressProgress);

        // Draw ripple effect
        if (rippleProgress > 0 && rippleProgress < 1 && _rippleCenter.HasValue)
        {
            canvas.Save();
            using (var path = new SKPath())
            {
                path.AddRoundRect(bodyRect, RadiusScaled, RadiusScaled);
                canvas.ClipPath(path, SKClipOperation.Intersect, true);

                ElevationHelper.DrawRipple(
                    canvas,
                    new SKPoint(_rippleCenter.Value.X, _rippleCenter.Value.Y),
                    Math.Max(bodyRect.Width, bodyRect.Height) * rippleProgress,
                    rippleProgress,
                    ColorScheme.Primary.WithAlpha(100));
            }

            canvas.Restore();
        }

        canvas.Restore();
    }

    private void DrawButton(SKCanvas canvas, SkiaSharp.SKRect bodyRect, float hoverProgress, float pressProgress)
    {
        // Determine base color - use Primary for filled buttons
        var fillColor = Color != SKColors.Transparent
            ? Color
            : ColorScheme.Primary;

        if (!Enabled)
            // Material Design 3 Disabled State: OnSurface 12%
            fillColor = ColorScheme.OnSurface.WithAlpha(30);

        // Draw base fill
        var fillPaint = GetOrCreatePaint("fill", () => new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        });
        fillPaint.Color = fillColor;
        canvas.DrawRoundRect(bodyRect, RadiusScaled, RadiusScaled, fillPaint);

        // Draw state layer (hover/press)
        if (Enabled)
        {
            var stateLayerColor = SKColors.Transparent;
            if (pressProgress > 0)
                stateLayerColor = ColorScheme.StateLayerPressed;
            else if (hoverProgress > 0)
                stateLayerColor = ColorScheme.StateLayerHover;

            if (stateLayerColor.Alpha > 0)
            {
                using var statePaint = new SKPaint
                {
                    Color = stateLayerColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRoundRect(bodyRect, _radius, _radius, statePaint);
            }
        }

        // Draw focus indicator
        if (Focused)
        {
            using var focusPaint = new SKPaint
            {
                Color = ColorScheme.OnPrimary.WithAlpha(90),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f
            };
            var focusRect = bodyRect;
            focusRect.Inflate(2, 2);
            canvas.DrawRoundRect(focusRect, _radius + 1, _radius + 1, focusPaint);
        }

        // Icon drawing
        var contentStartX = bodyRect.Left + 16f;
        if (Image != null)
        {
            var imageSize = 20;
            var imageRect = new SkiaSharp.SKRect(
                (int)contentStartX,
                (int)(bodyRect.Top + (bodyRect.Height - imageSize) / 2f),
                imageSize,
                imageSize);

            if (string.IsNullOrEmpty(Text))
                imageRect.Offset(bodyRect.Left + (bodyRect.Width - imageSize) / 2f, 0);

            canvas.DrawImage(Image, imageRect);

            contentStartX += imageSize + 8f;
        }

        // Text drawing with OnPrimary color
        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = Color != SKColors.Transparent
                ? ColorScheme.OnPrimary
                : ColorScheme.OnPrimary;

            if (!Enabled)
                textColor = ColorScheme.OnSurface.WithAlpha(80);

            var textPaint = GetOrCreatePaint("text", () => new SKPaint { IsAntialias = true });
            textPaint.Color = textColor;

            var font = GetOrCreateFont("text", () => new SKFont
            {
                Size = Font.Size.Topx(this),
                Typeface = FontManager.GetSKTypeface(Font) ?? SKTypeface.Default,
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full
            });
            if (Math.Abs(font.Size - Font.Size.Topx(this)) > 0.01f)
                font.Size = Font.Size.Topx(this);

            // Use ProcessedText for rendering (escape sequences already processed at property-set time)
            var displayText = ProcessedText;
            var textWidth = font.MeasureText(displayText);
            
            // Use bodyRect for text positioning (already accounts for elevation padding)
            var textX = Image == null
                ? bodyRect.Left + (bodyRect.Width - textWidth) / 2f
                : contentStartX;
            
            // Calculate text Y baseline position for vertical centering
            // In SkiaSharp, DrawText Y coordinate is the baseline, not the top or center
            // Ascent is negative, Descent is positive
            var textY = bodyRect.MidY - (font.Metrics.Ascent + font.Metrics.Descent) / 2f;

            // DrawText with font parameter - correct SkiaSharp 2.88+ API
            canvas.DrawText(displayText, textX, textY, SKTextAlign.Left, font, textPaint);
        }
    }

    private void DrawAnimations(SKCanvas canvas, SkiaSharp.SKRect bodyRect, float hoverProgress, float pressProgress)
    {
        var accentSk = ColorScheme.AccentColor;

        // Hover parıltısı
        if (hoverProgress > 0f)
        {
            var hoverPaint = GetOrCreatePaint("hover", () => new SKPaint { IsAntialias = true });
            hoverPaint.Color = accentSk.WithAlpha((byte)Math.Clamp(hoverProgress * 90f, 0f, 120f));
            canvas.DrawRoundRect(bodyRect, _radius, _radius, hoverPaint);
        }

        // Ripple efekti
        if (animationManager.IsAnimating())
            for (var i = 0; i < animationManager.GetAnimationCount(); i++)
            {
                var animationValue = animationManager.GetProgress(i);
                var animationSource = animationManager.GetSource(i);

                var alpha = (byte)Math.Clamp((1.0 - animationValue) * (140 + pressProgress * 90f), 0, 255);
                var ripplePaint = GetOrCreatePaint("ripple", () => new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                    Style = SKPaintStyle.Fill
                });
                ripplePaint.Color = accentSk.WithAlpha(alpha);

                var rippleSize = (float)(animationValue * Math.Max(Width, Height) * 2.2f);
                var rippleRect = new SkiaSharp.SKRect(
                    animationSource.X - rippleSize / 2f,
                    animationSource.Y - rippleSize / 2f,
                    animationSource.X + rippleSize / 2f,
                    animationSource.Y + rippleSize / 2f
                );

                canvas.Save();
                canvas.ClipRoundRect(new SKRoundRect(bodyRect, _radius, _radius), SKClipOperation.Intersect, true);
                canvas.DrawOval(rippleRect, ripplePaint);
                canvas.Restore();
            }
    }

    private static void RestartAnimation(AnimationManager engine, AnimationDirection direction, SKPoint? source = null)
    {
        if (engine == null)
            return;

        engine.SetDirection(direction);

        var startProgress = direction switch
        {
            AnimationDirection.Out or AnimationDirection.InOutOut or AnimationDirection.InOutRepeatingOut => 1.0,
            _ => 0.0
        };

        engine.SetProgress(startProgress);

        if (source.HasValue)
            engine.StartNewAnimation(direction, source.Value);
        else
            engine.StartNewAnimation(direction);
    }

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        var extra = 16;

        if (Image != null)
            extra += 24 + 4;

        using (var font = new SKFont())
        {
            font.Size = Font.Size * 1.5f;
            font.Typeface = FontManager.GetSKTypeface(Font);
            
            // Use ProcessedText for measurement
            _textSize = new SkiaSharp.SKSize(font.MeasureText(ProcessedText), font.Metrics.Descent - font.Metrics.Ascent);
        }

        return new SKSize((int)Math.Ceiling(_textSize.Width) + extra, 32);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cachedImage?.Dispose();
            _cachedImage = null;
        }

        base.Dispose(disposing);
    }

    internal override void OnEnabledChanged(EventArgs e)
    {
        InvalidateCache();
        base.OnEnabledChanged(e);
    }

    internal override void OnBackColorChanged(EventArgs e)
    {
        InvalidateCache();
        base.OnBackColorChanged(e);
    }

    internal override void OnForeColorChanged(EventArgs e)
    {
        InvalidateCache();
        base.OnForeColorChanged(e);
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        InvalidateCache();
        base.OnSizeChanged(e);
    }

    private SKPaint GetOrCreatePaint(string key, Func<SKPaint> factory)
    {
        if (_paintCache.TryGetValue(key, out var paint))
            return paint;
        paint = factory();
        _paintCache[key] = paint;
        return paint;
    }

    private SKFont GetOrCreateFont(string key, Func<SKFont> factory)
    {
        if (_fontCache.TryGetValue(key, out var font))
            return font;
        font = factory();
        _fontCache[key] = font;
        return font;
    }
}