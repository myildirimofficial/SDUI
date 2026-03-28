# Element Visual Systems

## Overview

The new ElementBase visual layer adds two opt-in systems:

1. Visual styles for state-driven appearance changes and animated transitions.
2. Motion effects for ambient Skia-rendered shapes such as circles and rectangles.

Both systems are retained-mode and integrate directly into ElementBase. Nothing is enabled implicitly. A control stays on the old behavior path until it explicitly configures visual styles or motion effects.

## Visual Styles

Visual styles are snapshot-based. ElementBase keeps a base snapshot from its normal properties and resolves state rules on top of that snapshot.

Supported animatable properties:

- Width
- Height
- BackColor
- ForeColor
- Border
- BorderColor
- Radius
- Shadows
- Opacity

Available built-in states:

- PointerOver
- Pressed
- Focused
- Disabled
- Hidden

Example:

```csharp
var card = new Element
{
    Size = new SKSize(280, 88),
    Padding = new Thickness(16),
    Radius = new Radius(16),
    Border = new Thickness(1),
    BorderColor = ColorScheme.Outline,
    BackColor = ColorScheme.Surface,
    ForeColor = ColorScheme.ForeColor,
    Text = "Interactive card"
};

card.ConfigureVisualStyles(styles =>
{
    styles
        .DefaultTransition(TimeSpan.FromMilliseconds(180), AnimationType.CubicEaseOut)
        .Base(baseStyle => baseStyle
            .Background(ColorScheme.Surface)
            .Foreground(ColorScheme.ForeColor)
            .Border(1)
            .BorderColor(ColorScheme.Outline)
            .Radius(16)
            .Shadow(new BoxShadow(0f, 2f, 8f, 0, SKColors.Black.WithAlpha(16))))
        .OnHover(rule => rule
            .Background(ColorScheme.SurfaceVariant)
            .BorderColor(ColorScheme.Primary)
            .Shadow(new BoxShadow(0f, 10f, 20f, 0, SKColors.Black.WithAlpha(28))))
        .OnPressed(rule => rule
            .Opacity(0.94f)
            .Background(ColorScheme.SurfaceVariant.Brightness(-0.04f)))
        .OnFocused(rule => rule
            .Border(2)
            .BorderColor(ColorScheme.Primary));
});
```

### Predicate Rules

Custom conditions are supported through predicates:

```csharp
card.ConfigureVisualStyles(styles =>
{
    styles.When(
        (element, state) => Equals(element.Tag, "danger") && state.IsPointerOver,
        rule => rule
            .Background(new SKColor(160, 38, 38))
            .Foreground(SKColors.White)
            .BorderColor(new SKColor(239, 68, 68)));
});
```

### Opt-In Behavior

Visual styles are enabled only when at least one of these happens:

- `ConfigureVisualStyles(...)` adds rules.
- `ApplyVisualStyleBase(...)` overrides the base snapshot.

This avoids accidental animation of normal layout writes such as dock, anchor, or parent-driven size updates.

## Motion Effects

Motion effects render lightweight Skia primitives before `OnPaint(...)`. They are useful for ambient UI depth, hero cards, or optional accent motion.

Supported movement modes:

- Drift
- Orbit
- Bezier path

Supported shape kinds:

- Circle
- Rectangle

Example:

```csharp
hero.ConfigureMotionEffects(scene =>
{
    scene
        .Circle(circle => circle
            .Anchor(0.2f, 0.3f)
            .Size(72f, 72f)
            .Orbit(18f, 12f)
            .Duration(4.2d)
            .Opacity(0.14f, 0.34f)
            .Scale(0.9f, 1.12f)
            .SpeedOnHover(1.5f)
            .Color(new SKColor(56, 189, 248, 120)))
        .Rectangle(rect => rect
            .Anchor(0.72f, 0.68f)
            .Size(112f, 22f)
            .CornerRadius(11f)
            .Bezier(new SKPoint(-28f, 8f), new SKPoint(18f, -24f), new SKPoint(52f, 20f), new SKPoint(-10f, 4f))
            .Duration(5.1d)
            .Opacity(0.08f, 0.18f)
            .SpeedOnFocused(1.4f)
            .Color(SKColors.White.WithAlpha(90)));
});
```

Motion is also opt-in. A control without effects does not run the motion timer.

## New Button Control

`Orivy.Controls.Button` inherits from ElementBase and ships with a default visual-style profile.

Characteristics:

- Uses the new visual style builder internally.
- Supports hover, pressed, focused, and disabled states.
- Keeps keyboard activation through Enter and Space.
- Exposes optional `AccentMotionEnabled` for subtle motion overlays.

Example:

```csharp
var saveButton = new Button
{
    Text = "Save",
    AccentMotionEnabled = true
};

saveButton.Click += (_, _) => SaveDocument();
```

## Notes

- Normal property writes still update the visual-style base snapshot immediately.
- State changes animate only when visual styles are explicitly enabled.
- AutoScroll containers must compute layout before scroll range measurement. The scroll fix for this system moved `UpdateScrollBars()` after `DefaultLayout.Instance.Layout(...)` inside ElementBase.