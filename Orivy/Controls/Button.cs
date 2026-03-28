using Orivy.Animation;
using Orivy.Helpers;
using SkiaSharp;
using System;
using System.ComponentModel;

namespace Orivy.Controls;

public class Button : ElementBase
{
    private bool _accentMotionEnabled;
    private bool _keyboardPressArmed;

    public Button()
    {
        AutoEllipsis = true;
        CanSelect = true;
        Cursor = Cursors.Hand;
        MinimumSize = new SKSize(45, 24);
        Padding = new Thickness(8);
        Radius = new Radius(12);
        Size = new SKSize(45, 24);
        TabStop = true;
        TextAlign = ContentAlignment.MiddleCenter;

        ConfigureVisualStyles(styles =>
        {
            styles
                .DefaultTransition(TimeSpan.FromMilliseconds(140), AnimationType.CubicEaseOut)
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Primary)
                    .Foreground(SKColors.White)
                    .Border(1)
                    .BorderColor(ColorScheme.Primary.Brightness(-0.18f))
                    .Radius(12)
                    .Shadow(new BoxShadow(0f, 6f, 14f, 0, ColorScheme.ShadowColor.WithAlpha(26))))
                .OnHover(rule => rule
                    .Background(ColorScheme.Primary.Brightness(0.06f))
                    .BorderColor(ColorScheme.Primary.Brightness(-0.08f))
                    .Shadow(new BoxShadow(0f, 10f, 20f, 0, ColorScheme.Primary.WithAlpha(34))))
                .OnPressed(rule => rule
                    .Background(ColorScheme.Primary.Brightness(-0.08f))
                    .BorderColor(ColorScheme.Primary.Brightness(-0.24f))
                    .Opacity(0.94f)
                    .Shadow(new BoxShadow(0f, 3f, 10f, 0, ColorScheme.Primary.WithAlpha(22))))
                .OnFocused(rule => rule
                    .Border(2)
                    .BorderColor(ColorScheme.Primary.Brightness(0.18f)))
                .OnDisabled(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .Foreground(ColorScheme.ForeColor.WithAlpha(170))
                    .BorderColor(ColorScheme.Outline.WithAlpha(140))
                    .Opacity(0.8f)
                    .Shadow(BoxShadow.None));
        });
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool AccentMotionEnabled
    {
        get => _accentMotionEnabled;
        set
        {
            if (_accentMotionEnabled == value)
                return;

            _accentMotionEnabled = value;
            UpdateAccentMotion();
        }
    }

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        using var font = Font;
        var measurementConstraints = proposedSize;
        if (measurementConstraints.Width <= 0)
            measurementConstraints.Width = short.MaxValue;
        if (measurementConstraints.Height <= 0)
            measurementConstraints.Height = short.MaxValue;

        var textSize = TextRenderer.MeasureText(
            ProcessedText,
            font,
            measurementConstraints,
            new TextRenderOptions
            {
                MaxWidth = measurementConstraints.Width,
                Trimming = AutoEllipsis ? TextTrimming.CharacterEllipsis : TextTrimming.None,
                UseMnemonic = UseMnemonic,
                Wrap = TextWrap.None
            });

        var desiredWidth = textSize.Width + Padding.Left + Padding.Right + Border.Left + Border.Right;
        var desiredHeight = textSize.Height + Padding.Top + Padding.Bottom + Border.Top + Border.Bottom;

        if (MinimumSize.Width > 0)
            desiredWidth = Math.Max(desiredWidth, MinimumSize.Width);
        if (MinimumSize.Height > 0)
            desiredHeight = Math.Max(desiredHeight, MinimumSize.Height);

        if (MaximumSize.Width > 0)
            desiredWidth = Math.Min(desiredWidth, MaximumSize.Width);
        if (MaximumSize.Height > 0)
            desiredHeight = Math.Min(desiredHeight, MaximumSize.Height);

        return new SKSize((float)Math.Ceiling(desiredWidth), (float)Math.Ceiling(desiredHeight));
    }

    internal override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || !Enabled)
            return;

        if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Space)
            return;

        _keyboardPressArmed = true;
        e.Handled = true;
    }

    internal override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (!_keyboardPressArmed)
            return;

        if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Space)
            return;

        _keyboardPressArmed = false;
        e.Handled = true;

        if (Enabled && Visible)
            PerformClick();
    }

    internal override void OnLostFocus(EventArgs e)
    {
        _keyboardPressArmed = false;
        base.OnLostFocus(e);
    }

    internal virtual void UpdateAccentMotion()
    {
        if (!_accentMotionEnabled)
        {
            ClearMotionEffects();
            return;
        }

        ConfigureMotionEffects(scene =>
        {
            scene
                .Circle(circle => circle
                    .Anchor(0.82f, 0.34f)
                    .Size(16f, 16f)
                    .Orbit(6f, 5f)
                    .Duration(2.8d)
                    .Opacity(0.05f, 0.12f)
                    .Scale(0.92f, 1.08f)
                    .SpeedOnHover(1.8f)
                    .SpeedOnPressed(2.4f)
                    .Color(SKColors.White.WithAlpha(80)))
                .Rectangle(rect => rect
                    .Anchor(0.2f, 0.7f)
                    .Size(26f, 6f)
                    .CornerRadius(3f)
                    .Bezier(new SKPoint(-6f, 0f), new SKPoint(4f, -3f), new SKPoint(12f, 4f), new SKPoint(-3f, 1f))
                    .Duration(3.1d)
                    .Opacity(0.03f, 0.08f)
                    .Scale(0.96f, 1.04f)
                    .SpeedOnHover(1.5f)
                    .Color(SKColors.White.WithAlpha(72)));
        });
    }
}