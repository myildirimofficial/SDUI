using SDUI;
using SDUI.Animation;
using SDUI.Binding;
using SDUI.Controls;
using SDUI.Validations;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orivy.Example;

internal partial class MainWindow
{
    internal void InitializeComponent()
    {
        this.SuspendLayout();

        //
        // panel
        this.panel = new()
        {
            Text = "Backend Renderer",
            Name = "panel",
            Padding = new(5),
            Dock = SDUI.DockStyle.Fill
        };

        
        this.panel2 = new()
        {
            Text = "Config",
            Name = "panel2",
            Padding = new(5),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0)
        };
        
        this.panel3 = new()
        {
            Text = "Designer",
            Name = "panel3",
            Padding = new(24),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0),
            AutoScroll = true,
            AutoScrollMargin = new(0, 24)
        };

        this.panel4 = new()
        {
            Text = "Visual Styles",
            Name = "panel4",
            Padding = new(24),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0),
            AutoScroll = true,
            AutoScrollMargin = new(0, 24)
        };

        this.panel5 = new()
        {
            Text = "Scroll Lab",
            Name = "panel5",
            Padding = new(24),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0),
            AutoScroll = true,
            AutoScrollMargin = new(0, 24)
        };

        this.panel6 = new()
        {
            Text = "Grid List",
            Name = "panel6",
            Padding = new(20),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0),
            AutoScroll = true,
            AutoScrollMargin = new(0, 24)
        };

        this.buttonOpenGL = new()
        {
            Name = "buttonOpenGL",
            Text = "OpenGL",
            BackColor = SKColors.Red,
            Dock = SDUI.DockStyle.Bottom,
            Size = new(100, 32),
            Radius = new(6),
        };

        buttonOpenGL.Click += ButtonOpenGL_Click;

        this.buttonSoftware = new()
        {
            Name = "buttonSoftware",
            Text = "Software",
            BackColor = SKColors.Green,
            Size = new(100, 32),
            Dock = SDUI.DockStyle.Left,
            Radius = new(4),
            Border = new(1)
        };

        buttonSoftware.Click += ButtonSoftware_Click;

        this.buttonDirectX = new()
        {
            Name = "buttonDirectX",
            Text = "DirectX",
            BackColor = SKColors.Green,
            Size = new(100, 32),
            Dock = SDUI.DockStyle.Right,
            Radius = new(4),
            Border = new(1),
            Shadows = new[] {
                new BoxShadow(0, 1, 3, 0, ColorScheme.ShadowColor),           // soft outer
                new BoxShadow(0, 4, 12, new Radius(2), ColorScheme.ShadowColor), // wide spread
                new BoxShadow(0, 1, 2, 0, ColorScheme.ShadowColor, inset: true)     // subtle inset
            }
        };

        buttonDirectX.Click += ButtonDirectX_Click;

        this.buttonDarkMode = new()
        {
            Name = "buttonDarkMode",
            Text = "Toggle Mode",
            BackColor = SKColors.Blue,
            Dock = SDUI.DockStyle.Bottom,
            Size = new(100, 32),
            Radius = new(6),
        };

        buttonDarkMode.Click += ButtonDarkMode_Click;

        this.visualStyleHeader = new()
        {
            Name = "visualStyleHeader",
            Text = "Visual Style Builder\nOpt-in only: state refresh and transitions start when a control explicitly configures visual styles.",
            Dock = SDUI.DockStyle.Top,
            Height = 84,
            Padding = new(14),
            Margin = new(0, 0, 0, 16),
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            TextAlign = ContentAlignment.MiddleLeft
        };

        this.visualStyleInteractiveCard = new()
        {
            Name = "visualStyleInteractiveCard",
            Text = "Interactive Card\nHover, press or focus this element to see layered transitions and subtle rectangle drift.",
            Dock = SDUI.DockStyle.Top,
            Height = 92,
            Padding = new(16),
            Margin = new(0, 0, 0, 14),
            Radius = new(16),
            Border = new(1),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };

        this.visualStyleMotionHero = new()
        {
            Name = "visualStyleMotionHero",
            Text = "Motion Builder\nFloating circles, orbiting shapes and bezier path motion are rendered through ConfigureMotionEffects(...).",
            Dock = SDUI.DockStyle.Top,
            Height = 196,
            Padding = new(18),
            Margin = new(0, 0, 0, 14),
            Radius = new(20),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
            TextAlign = ContentAlignment.MiddleLeft
        };

        this.visualStyleDangerCard = new()
        {
            Name = "visualStyleDangerCard",
            Text = "Predicate Card\nClick to toggle a custom predicate state.",
            Dock = SDUI.DockStyle.Top,
            Height = 92,
            Padding = new(16),
            Margin = new(0, 0, 0, 14),
            Radius = new(16),
            Border = new(1),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Tag = "normal"
        };

        this.visualStyleDisabledCard = new()
        {
            Name = "visualStyleDisabledCard",
            Text = "Disabled State Card\nThis card is disabled and styled by OnDisabled.",
            Dock = SDUI.DockStyle.Top,
            Height = 92,
            Padding = new(16),
            Margin = new(0, 0, 0, 14),
            Radius = new(16),
            Border = new(1),
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = false
        };

        this.visualStyleFooterAction = new()
        {
            Name = "visualStyleFooterAction",
            Text = "Toggle Disabled Card",
            Dock = SDUI.DockStyle.Top,
            Height = 54,
            Padding = new(12),
            Margin = new(0, 0, 0, 14),
            Radius = new(14),
            Border = new(0),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };

        this.visualStylePrimaryButton = new Button
        {
            Name = "visualStylePrimaryButton",
            Text = "Primary Button - Accent Motion On",
            Dock = SDUI.DockStyle.Top,
            Height = 46,
            Margin = new(0, 0, 0, 12),
            AccentMotionEnabled = true
        };

        this.visualStyleGhostButton = new Button
        {
            Name = "visualStyleGhostButton",
            Text = "Secondary Button - Accent Motion Off",
            Dock = SDUI.DockStyle.Top,
            Height = 46,
            Margin = new(0, 0, 0, 14),
            AccentMotionEnabled = false
        };

        this.visualStyleScrollProbe = new()
        {
            Name = "visualStyleScrollProbe",
            Text = "Scroll Probe\nIf you can reach this block, AutoScroll is now measuring content after dock layout. The two Button controls above also prove the new control works inside the example page.",
            Dock = SDUI.DockStyle.Top,
            Height = 240,
            Padding = new(18),
            Margin = new(0, 0, 0, 14),
            BackColor = ColorScheme.SurfaceContainerHigh,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(120),
            TextAlign = ContentAlignment.MiddleLeft
        };

        this.visualStyleHeader.ConfigureVisualStyles(styles =>
        {
            styles
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.SurfaceVariant)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline)
                    .Radius(14))
                .OnHover(rule => rule
                    .BorderColor(ColorScheme.Primary)
                    .Background(ColorScheme.SurfaceVariant.Brightness(0.04f)));
        });

        this.visualStyleMotionHero.ConfigureMotionEffects(scene =>
        {
            scene
                .Circle(circle => circle
                    .Anchor(0.18f, 0.34f)
                    .Size(84f, 84f)
                    .Orbit(24f, 16f)
                    .Duration(4.4d)
                    .Opacity(0.16f, 0.42f)
                    .Scale(0.92f, 1.12f)
                    .SpeedOnHover(1.6f)
                    .Color(new SKColor(56, 189, 248, 120)))
                .Circle(circle => circle
                    .Anchor(0.82f, 0.28f)
                    .Size(56f, 56f)
                    .Drift(-16f, 22f)
                    .Delay(0.8d)
                    .Duration(5.1d)
                    .Opacity(0.14f, 0.34f)
                    .Scale(0.88f, 1.18f)
                    .SpeedOnHover(1.35f)
                    .Color(new SKColor(192, 132, 252, 110)))
                .Rectangle(rect => rect
                    .Anchor(0.64f, 0.68f)
                    .Size(120f, 24f)
                    .CornerRadius(12f)
                    .Bezier(new SKPoint(-42f, 10f), new SKPoint(28f, -36f), new SKPoint(78f, 26f), new SKPoint(-16f, 6f))
                    .Rotate(10f)
                    .Duration(4.9d)
                    .Opacity(0.10f, 0.24f)
                    .SpeedOnHover(1.8f)
                    .Color(new SKColor(255, 255, 255, 96)))
                .Rectangle(rect => rect
                    .Anchor(0.28f, 0.74f)
                    .Size(72f, 72f)
                    .CornerRadius(22f)
                    .Orbit(18f, 14f)
                    .Rotate(-14f)
                    .Delay(0.45d)
                    .Duration(5.6d)
                    .Opacity(0.08f, 0.18f)
                    .Scale(0.94f, 1.08f)
                    .Color(new SKColor(255, 255, 255, 84)));
        });

        this.visualStyleInteractiveCard.ConfigureVisualStyles(styles =>
        {
            styles
                .DefaultTransition(TimeSpan.FromMilliseconds(180), AnimationType.CubicEaseOut)
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Surface)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline)
                    .Radius(16)
                    .Opacity(1f)
                    .Shadow(new BoxShadow(0f, 2f, 8f, 0, ColorScheme.ShadowColor)))
                .OnHover(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .BorderColor(ColorScheme.Primary)
                    .Shadow(new BoxShadow(0f, 12f, 24f, 0, ColorScheme.ShadowColor)))
                .OnPressed(rule => rule
                    .Opacity(0.93f)
                    .Background(ColorScheme.SurfaceVariant.Brightness(-0.03f))
                    .Shadow(new BoxShadow(0f, 4f, 12f, 0, ColorScheme.ShadowColor)))
                .OnFocused(rule => rule
                    .Border(2)
                    .BorderColor(ColorScheme.Primary));
        });

        this.visualStyleInteractiveCard.ConfigureMotionEffects(scene =>
        {
            scene
                .Rectangle(rect => rect
                    .Anchor(0.88f, 0.5f)
                    .Size(58f, 58f)
                    .CornerRadius(18f)
                    .Orbit(10f, 10f)
                    .Rotate(18f)
                    .Duration(3.8d)
                    .Opacity(0.04f, 0.12f)
                    .Scale(0.94f, 1.05f)
                    .SpeedOnHover(2f)
                    .SpeedOnPressed(2.6f)
                    .SpeedOnFocused(1.45f)
                    .Color(new SKColor(59, 130, 246, 88)))
                .Circle(circle => circle
                    .Anchor(0.74f, 0.38f)
                    .Size(22f, 22f)
                    .Bezier(new SKPoint(-10f, 4f), new SKPoint(8f, -16f), new SKPoint(22f, 12f), new SKPoint(-6f, 18f))
                    .Duration(2.9d)
                    .Opacity(0.06f, 0.16f)
                    .Scale(0.9f, 1.14f)
                    .SpeedOnHover(2.2f)
                    .Color(new SKColor(255, 255, 255, 90)));
        });

        this.visualStyleDangerCard.ConfigureVisualStyles(styles =>
        {
            styles
                .DefaultTransition(TimeSpan.FromMilliseconds(220), AnimationType.CubicEaseOut)
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Surface)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline)
                    .Radius(16))
                .OnHover(rule => rule
                    .BorderColor(ColorScheme.Primary)
                    .Background(ColorScheme.SurfaceVariant))
                .When((element, state) => Equals(element.Tag, "danger"), rule => rule
                    .Background(ColorScheme.Error)
                    .Foreground(ColorScheme.ForeColor)
                    .BorderColor(ColorScheme.Error)
                    .Shadow(new BoxShadow(0f, 14f, 30f, 0, ColorScheme.ShadowColor)))
                .When((element, state) => Equals(element.Tag, "danger") && state.IsPointerOver, rule => rule
                    .Opacity(0.95f));
        });

        this.visualStyleDisabledCard.ConfigureVisualStyles(styles =>
        {
            styles
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Surface)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline)
                    .Radius(16))
                .OnDisabled(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .Foreground(ColorScheme.ForeColor.WithAlpha(170))
                    .BorderColor(ColorScheme.Outline.WithAlpha(140))
                    .Opacity(0.82f));
        });

        this.visualStyleFooterAction.ConfigureVisualStyles(styles =>
        {
            styles
                .DefaultTransition(TimeSpan.FromMilliseconds(160), AnimationType.CubicEaseOut)
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Primary)
                    .Foreground(ColorScheme.ForeColor)
                    .Radius(14)
                    .Shadow(new BoxShadow(0f, 4f, 12f, 0, ColorScheme.ShadowColor)))
                .OnHover(rule => rule
                    .Background(ColorScheme.Primary.Brightness(0.06f))
                    .Shadow(new BoxShadow(0f, 10f, 18f, 0, ColorScheme.ShadowColor)))
                .OnPressed(rule => rule
                    .Opacity(0.9f));
        });

        this.visualStyleGhostButton.ConfigureVisualStyles(styles =>
        {
            styles
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Surface)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline)
                    .Shadow(BoxShadow.None))
                .OnHover(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .BorderColor(ColorScheme.Primary))
                .OnPressed(rule => rule
                    .Background(ColorScheme.SurfaceVariant.Brightness(-0.04f))
                    .Opacity(0.95f));
        });

        this.visualStyleScrollProbe.ConfigureVisualStyles(styles =>
        {
            styles
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.SurfaceContainerHigh)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Primary.WithAlpha(120))
                    .Radius(18))
                .OnHover(rule => rule
                    .BorderColor(ColorScheme.Primary)
                    .Background(ColorScheme.SurfaceVariant));
        });

        var designerControlHeader = new Element
        {
            Name = "designerControlHeader",
            Text = "Designer Controls\nComboBox demos live only on this page now. Use the motion preset, multi-select dropdown and inline color picker together.",
            Dock = SDUI.DockStyle.Top,
            Height = 102,
            Padding = new(18),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.SurfaceVariant,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var designerControlShell = new Element
        {
            Name = "designerControlShell",
            Dock = SDUI.DockStyle.Top,
            Height = 628,
            Padding = new(18),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(20),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
            Text = "Inspector Surface\nSingle-select, multi-select, popup motion and the new color picker live together here.",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var designerControlStatus = new Element
        {
            Name = "designerControlStatus",
            Dock = SDUI.DockStyle.Bottom,
            Height = 72,
            Padding = new(14),
            Margin = new(0, 14, 0, 0),
            BackColor = ColorScheme.SurfaceContainer,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(86),
            Text = "Designer Status\nDropdown motion, multi-select state and color picker values will report here.",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var designerSurfaceCombo = new ComboBox
        {
            Name = "designerSurfaceCombo",
            Dock = SDUI.DockStyle.Top,
            Height = 42,
            Margin = new(0, 0, 0, 12),
            PlaceholderText = "Inspector surface",
            MaxDropDownItems = 6,
            DropDownItemHeight = 34,
            ShowSelectionIndicator = true,
            DropDownOpeningEffect = OpeningEffectType.PopFade
        };
        designerSurfaceCombo.Items.AddRange(new object[]
        {
            new ComboBoxItem("Canvas Inspector", "canvas"),
            new ComboBoxItem("Prototype Flow", "prototype"),
            new ComboBoxItem("Component Tokens", "tokens"),
            new ComboBoxItem("Export Review", "export")
        });
        designerSurfaceCombo.SelectedIndex = 0;

        var designerMotionCombo = new ComboBox
        {
            Name = "designerMotionCombo",
            Dock = SDUI.DockStyle.Top,
            Height = 42,
            Margin = new(0, 0, 0, 12),
            PlaceholderText = "Popup motion preset",
            MaxDropDownItems = 6,
            DropDownItemHeight = 34
        };
        designerMotionCombo.Items.AddRange(new object[]
        {
            new ComboBoxItem("Pop Fade", OpeningEffectType.PopFade),
            new ComboBoxItem("Scale Fade", OpeningEffectType.ScaleFade),
            new ComboBoxItem("Slide Down Fade", OpeningEffectType.SlideDownFade),
            new ComboBoxItem("Slide Up Fade", OpeningEffectType.SlideUpFade),
            new ComboBoxItem("Fade", OpeningEffectType.Fade)
        });
        designerMotionCombo.SelectedValue = OpeningEffectType.PopFade;

        var designerModulesCombo = new ComboBox
        {
            Name = "designerModulesCombo",
            Dock = SDUI.DockStyle.Top,
            Height = 42,
            Margin = new(0, 0, 0, 12),
            PlaceholderText = "Inspector modules",
            MaxDropDownItems = 8,
            DropDownItemHeight = 34,
            MultiSelect = true,
            DropDownOpeningEffect = OpeningEffectType.PopFade
        };
        designerModulesCombo.Items.AddRange(new object[]
        {
            new ComboBoxItem("Layout Grid", "layout"),
            new ComboBoxItem("Layer Stack", "layers"),
            new ComboBoxItem("Token Studio", "tokens"),
            new ComboBoxItem("Motion Curves", "motion"),
            new ComboBoxItem("Accessibility", "a11y"),
            new ComboBoxItem("Export Hooks", "export")
        });
        designerModulesCombo.SetItemSelected(0, true);
        designerModulesCombo.SetItemSelected(2, true);
        designerModulesCombo.SetItemSelected(3, true);

        var designerAccentPicker = new ColorPicker
        {
            Name = "designerAccentPicker",
            Dock = SDUI.DockStyle.Top,
            Height = 346,
            Margin = new(0, 0, 0, 14),
            ReferenceColor = ColorScheme.Primary,
            SelectedColor = ColorScheme.Primary,
            ShowAlphaChannel = true,
            ShowReferenceSwatch = true
        };

        void UpdateDesignerControlStatus()
        {
            var surfaceText = designerSurfaceCombo.SelectedItem is ComboBoxItem surfaceItem ? surfaceItem.Text : designerSurfaceCombo.Text;
            var motionText = designerMotionCombo.SelectedItem is ComboBoxItem motionItem ? motionItem.Text : designerMotionCombo.Text;
            var modulesText = string.IsNullOrWhiteSpace(designerModulesCombo.Text) ? "None" : designerModulesCombo.Text;
            designerControlStatus.Text = $"Designer Status\nSurface: {surfaceText}. Motion: {motionText}. Modules: {modulesText}. Accent: {designerAccentPicker.HexValue}.";
        }

        designerMotionCombo.SelectionChangeCommitted += (_, _) =>
        {
            if (designerMotionCombo.SelectedValue is not OpeningEffectType effect)
                return;

            designerSurfaceCombo.DropDownOpeningEffect = effect;
            designerModulesCombo.DropDownOpeningEffect = effect;
            UpdateDesignerControlStatus();
        };

        designerSurfaceCombo.SelectionChangeCommitted += (_, _) => UpdateDesignerControlStatus();
        designerModulesCombo.SelectionChangeCommitted += (_, _) => UpdateDesignerControlStatus();
        designerAccentPicker.SelectedColorCommitted += (_, _) =>
        {
            var accent = designerAccentPicker.SelectedColor;
            if (accent != SKColors.Transparent)
                ColorScheme.SetPrimarySeedColor(accent);

            UpdateDesignerControlStatus();
        };
        designerAccentPicker.SelectedColorChanged += (_, _) => UpdateDesignerControlStatus();

        UpdateDesignerControlStatus();

        designerControlShell.Controls.Add(designerControlStatus);
        designerControlShell.Controls.Add(designerAccentPicker);
        designerControlShell.Controls.Add(designerModulesCombo);
        designerControlShell.Controls.Add(designerMotionCombo);
        designerControlShell.Controls.Add(designerSurfaceCombo);

        var scrollLabHeader = new Element
        {
            Name = "scrollLabHeader",
            Text = "Scroll Lab\nUse this page to test thumb drag, track click, wheel scroll, nested scroll hosts, and wheel routing while hovering child controls.",
            Dock = SDUI.DockStyle.Top,
            Height = 96,
            Padding = new(16),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.SurfaceVariant,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(16),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabWheelCards = new Element
        {
            Name = "scrollLabWheelCards",
            Text = "Scenario A\nWheel over these cards. If wheel routing is correct, the page should still move even when the pointer is on child content.",
            Dock = SDUI.DockStyle.Top,
            Height = 180,
            Padding = new(16),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.SurfaceContainer,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabActionA = new Button
        {
            Name = "scrollLabActionA",
            Text = "Action Button In Scroll Flow",
            Dock = SDUI.DockStyle.Top,
            Height = 46,
            Margin = new(0, 0, 0, 12),
            AccentMotionEnabled = true
        };

        var scrollLabActionB = new Button
        {
            Name = "scrollLabActionB",
            Text = "Second Button - Hover Then Wheel",
            Dock = SDUI.DockStyle.Top,
            Height = 46,
            Margin = new(0, 0, 0, 16),
            AccentMotionEnabled = false
        };

        var scrollLabNestedShell = new Element
        {
            Name = "scrollLabNestedShell",
            Text = "Scenario B\nNested scroll host. Test outer wheel on shell, then move over inner cards and wheel again.",
            Dock = SDUI.DockStyle.Top,
            Height = 336,
            Padding = new(16),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabNestedHost = new Element
        {
            Name = "scrollLabNestedHost",
            Dock = SDUI.DockStyle.Fill,
            Padding = new(12),
            Margin = new(0),
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(110),
            BackColor = ColorScheme.Surface.WithAlpha(28),
            AutoScroll = true,
            AutoScrollMargin = new(0, 16)
        };

        var scrollLabNestedTopGap = new Element
        {
            Name = "scrollLabNestedTopGap",
            Dock = SDUI.DockStyle.Top,
            Height = 44,
            Margin = new(0, 0, 0, 10),
            BackColor = ColorScheme.Surface.WithAlpha(20),
            Radius = new(10),
            Border = new(0),
            Text = "Nested Host Start",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = ColorScheme.ForeColor
        };

        var scrollLabNestedCard1 = new Element
        {
            Name = "scrollLabNestedCard1",
            Text = "Nested Card 1\nWheel here and verify the inner host moves.",
            Dock = SDUI.DockStyle.Top,
            Height = 104,
            Padding = new(16),
            Margin = new(0, 0, 0, 12),
            BackColor = ColorScheme.Warning.WithAlpha(46),
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Warning.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabNestedCard2 = new Element
        {
            Name = "scrollLabNestedCard2",
            Text = "Nested Card 2\nDrag the inner scrollbar thumb here.",
            Dock = SDUI.DockStyle.Top,
            Height = 104,
            Padding = new(16),
            Margin = new(0, 0, 0, 12),
            BackColor = ColorScheme.Success.WithAlpha(38),
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Success.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabNestedButton = new Button
        {
            Name = "scrollLabNestedButton",
            Text = "Nested Button - Hover Then Wheel",
            Dock = SDUI.DockStyle.Top,
            Height = 44,
            Margin = new(0, 0, 0, 12),
            AccentMotionEnabled = true
        };

        var scrollLabNestedCard3 = new Element
        {
            Name = "scrollLabNestedCard3",
            Text = "Nested Card 3\nTrack click should jump inside the inner host.",
            Dock = SDUI.DockStyle.Top,
            Height = 104,
            Padding = new(16),
            Margin = new(0, 0, 0, 12),
            BackColor = ColorScheme.Primary.WithAlpha(34),
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabNestedCard4 = new Element
        {
            Name = "scrollLabNestedCard4",
            Text = "Nested Card 4\nBottom probe for inner scrolling.",
            Dock = SDUI.DockStyle.Top,
            Height = 132,
            Padding = new(16),
            Margin = new(0, 0, 0, 12),
            BackColor = ColorScheme.SurfaceVariant.WithAlpha(34),
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.SurfaceVariant.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var scrollLabLongTail = new Element
        {
            Name = "scrollLabLongTail",
            Text = "Scenario C\nLong tail content. Use outer wheel, outer thumb drag, and track click while hovering this large block and the two buttons above.",
            Dock = SDUI.DockStyle.Top,
            Height = 320,
            Padding = new(18),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.SurfaceContainerHigh,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Warning.WithAlpha(110),
            TextAlign = ContentAlignment.MiddleLeft
        };

        scrollLabNestedHost.Controls.Add(scrollLabNestedCard4);
        scrollLabNestedHost.Controls.Add(scrollLabNestedCard3);
        scrollLabNestedHost.Controls.Add(scrollLabNestedButton);
        scrollLabNestedHost.Controls.Add(scrollLabNestedCard2);
        scrollLabNestedHost.Controls.Add(scrollLabNestedCard1);
        scrollLabNestedHost.Controls.Add(scrollLabNestedTopGap);
        scrollLabNestedShell.Controls.Add(scrollLabNestedHost);

        visualStyleDangerCard.Click += VisualStyleDangerToggle_Click;
        visualStylePrimaryButton.Click += VisualStylePrimaryButton_Click;
        visualStyleFooterAction.Click += VisualStyleEnableDisabled_Click;

        this.panel3.Controls.Add(designerControlShell);
        this.panel3.Controls.Add(designerControlHeader);

        this.panel4.Controls.Add(this.visualStyleScrollProbe);
        this.panel4.Controls.Add(this.visualStyleFooterAction);
        this.panel4.Controls.Add(this.visualStyleGhostButton);
        this.panel4.Controls.Add(this.visualStylePrimaryButton);
        this.panel4.Controls.Add(this.visualStyleDisabledCard);
        this.panel4.Controls.Add(this.visualStyleDangerCard);
        this.panel4.Controls.Add(this.visualStyleInteractiveCard);
        this.panel4.Controls.Add(this.visualStyleMotionHero);
        this.panel4.Controls.Add(this.visualStyleHeader);

        this.panel5.Controls.Add(scrollLabLongTail);
        this.panel5.Controls.Add(scrollLabNestedShell);
        this.panel5.Controls.Add(scrollLabActionB);
        this.panel5.Controls.Add(scrollLabActionA);
        this.panel5.Controls.Add(scrollLabWheelCards);
        this.panel5.Controls.Add(scrollLabHeader);

        var gridListHeader = new Element
        {
            Name = "gridListHeader",
            Text = "Grid List Surface\nAnimated groups, sticky header, column resize, optional row resize and denser typography are all visible without leaving this page.",
            Dock = SDUI.DockStyle.Top,
            Height = 124,
            Padding = new(24),
            Margin = new(0, 0, 0, 16),
            BackColor = ColorScheme.SurfaceContainerHigh,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(24),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(92),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI Semibold") ?? SKTypeface.Default, 15f)
        };

        var gridListToolbar = new Element
        {
            Name = "gridListToolbar",
            Dock = SDUI.DockStyle.Top,
            Height = 72,
            Padding = new(10),
            Margin = new(0, 0, 0, 16),
            Radius = new(20),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(92),
            BackColor = ColorScheme.Surface
        };

        this.gridListToggleHeaderButton = new Button
        {
            Name = "gridListToggleHeaderButton",
            Text = "Header: On",
            Dock = SDUI.DockStyle.Left,
            Width = 128,
            Margin = new(0, 0, 10, 0),
            AccentMotionEnabled = true
        };

        this.gridListToggleStickyButton = new Button
        {
            Name = "gridListToggleStickyButton",
            Text = "Sticky: On",
            Dock = SDUI.DockStyle.Left,
            Width = 128,
            Margin = new(0, 0, 10, 0),
            AccentMotionEnabled = false
        };

        this.gridListToggleGroupingButton = new Button
        {
            Name = "gridListToggleGroupingButton",
            Text = "Grouping: On",
            Dock = SDUI.DockStyle.Left,
            Width = 144,
            Margin = new(0, 0, 10, 0),
            AccentMotionEnabled = false
        };

        this.gridListToggleGridLinesButton = new Button
        {
            Name = "gridListToggleGridLinesButton",
            Text = "Grid Lines: On",
            Dock = SDUI.DockStyle.Left,
            Width = 152,
            Margin = new(0, 0, 0, 0),
            AccentMotionEnabled = false
        };

        this.gridListToggleRowResizeButton = new Button
        {
            Name = "gridListToggleRowResizeButton",
            Text = "Row Resize: Off",
            Dock = SDUI.DockStyle.Left,
            Width = 164,
            Margin = new(0, 0, 10, 0),
            AccentMotionEnabled = false
        };

        gridListToolbar.Controls.Add(this.gridListToggleGridLinesButton);
        gridListToolbar.Controls.Add(this.gridListToggleRowResizeButton);
        gridListToolbar.Controls.Add(this.gridListToggleGroupingButton);
        gridListToolbar.Controls.Add(this.gridListToggleStickyButton);
        gridListToolbar.Controls.Add(this.gridListToggleHeaderButton);

        var gridListWorkspace = new Element
        {
            Name = "gridListWorkspace",
            Dock = SDUI.DockStyle.Top,
            Height = 860,
            BackColor = SKColors.Transparent,
            Border = new(0),
            Radius = new(0),
            Padding = new(0),
            Margin = new(0, 0, 0, 8)
        };

        var gridListInspectorRail = new Element
        {
            Name = "gridListInspectorRail",
            Dock = SDUI.DockStyle.Right,
            Width = 328,
            Padding = new(0),
            Margin = new(16, 0, 0, 0),
            BackColor = SKColors.Transparent,
            Border = new(0),
            Radius = new(0)
        };

        this.gridListStatus = new Element
        {
            Name = "gridListStatus",
            Text = "Status\nReady",
            Dock = SDUI.DockStyle.Top,
            Height = 110,
            Padding = new(18),
            Margin = new(0, 0, 0, 14),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(20),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(96),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI Semibold") ?? SKTypeface.Default, 11f)
        };

        var gridListPrimaryShell = new Element
        {
            Name = "gridListPrimaryShell",
            Dock = SDUI.DockStyle.Fill,
            Padding = new(16),
            Margin = new(0),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(24),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(88)
        };

        var gridListPrimaryIntro = new Element
        {
            Name = "gridListPrimaryIntro",
            Text = "Operations Board\nScroll inside the grid to verify sticky header. Resize columns from the header edge, then enable row resize from the toolbar to stretch the body rhythm.",
            Dock = SDUI.DockStyle.Top,
            Height = 84,
            Padding = new(16),
            Margin = new(0, 0, 0, 14),
            BackColor = ColorScheme.SurfaceContainer,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(18),
            Border = new(1),
            BorderColor = ColorScheme.Primary.WithAlpha(78),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default, 10.5f)
        };

        this.gridListPrimary = new GridList
        {
            Name = "gridListPrimary",
            Dock = SDUI.DockStyle.Fill,
            Margin = new(0),
            Radius = new(14),
            Border = new(1),
            HeaderVisible = true,
            StickyHeader = true,
            GroupingEnabled = true,
            MultiSelect = true,
            FullRowSelect = true,
            CheckBoxes = false,
            AllowColumnResize = true,
            AllowRowResize = true,
            ShowGridLines = true,
            HeaderHeight = 42,
            RowHeight = 38,
            GroupHeaderHeight = 32,
            CellPadding = 11,
        };

        gridListPrimaryShell.Controls.Add(this.gridListPrimary);
        gridListPrimaryShell.Controls.Add(gridListPrimaryIntro);

        var gridListCompactShell = new Element
        {
            Name = "gridListCompactShell",
            Dock = SDUI.DockStyle.Top,
            Height = 286,
            Padding = new(16),
            Margin = new(0, 0, 0, 14),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(22),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(88)
        };

        var gridListCompactHeader = new Element
        {
            Name = "gridListCompactHeader",
            Text = "Compact Feed\nHeaderless mode for icon-first rows and faster scanning.",
            Dock = SDUI.DockStyle.Top,
            Height = 72,
            Padding = new(16),
            Margin = new(0, 0, 0, 12),
            BackColor = ColorScheme.SurfaceContainer,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(16),
            Border = new(1),
            BorderColor = ColorScheme.Success.WithAlpha(86),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default, 10.5f)
        };

        this.gridListCompact = new GridList
        {
            Name = "gridListCompact",
            Dock = SDUI.DockStyle.Fill,
            Radius = new(14),
            Border = new(1),
            HeaderVisible = false,
            StickyHeader = false,
            GroupingEnabled = false,
            MultiSelect = true,
            FullRowSelect = true,
            ShowGridLines = false,
            AllowRowResize = true,
            RowHeight = 36,
            CellPadding = 11,
        };

        gridListCompactShell.Controls.Add(this.gridListCompact);
        gridListCompactShell.Controls.Add(gridListCompactHeader);

        var gridListFooter = new Element
        {
            Name = "gridListFooter",
            Text = "Guide\n1. Scroll inside the primary grid to verify sticky header.\n2. Collapse a group and watch the rows animate.\n3. Enable row resize only when you want variable density.",
            Dock = SDUI.DockStyle.Fill,
            Padding = new(18),
            Margin = new(0),
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(22),
            Border = new(1),
            BorderColor = ColorScheme.Outline.WithAlpha(88),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new SKFont(SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default, 10.5f)
        };

        gridListInspectorRail.Controls.Add(gridListFooter);
        gridListInspectorRail.Controls.Add(gridListCompactShell);
        gridListInspectorRail.Controls.Add(this.gridListStatus);

        gridListWorkspace.Controls.Add(gridListPrimaryShell);
        gridListWorkspace.Controls.Add(gridListInspectorRail);

        this.panel6.Controls.Add(gridListWorkspace);
        this.panel6.Controls.Add(gridListToolbar);
        this.panel6.Controls.Add(gridListHeader);

        this.gridListToggleHeaderButton.Click += GridListToggleHeaderButton_Click;
        this.gridListToggleStickyButton.Click += GridListToggleStickyButton_Click;
        this.gridListToggleGroupingButton.Click += GridListToggleGroupingButton_Click;
        this.gridListToggleGridLinesButton.Click += GridListToggleGridLinesButton_Click;
        this.gridListToggleRowResizeButton.Click += GridListToggleRowResizeButton_Click;

        windowPageControl = new()
        {
            Name = "windowPageControl",
            Dock = SDUI.DockStyle.Fill,
            TransitionEffect = WindowPageTransitionEffect.ScaleFade,
            TransitionAnimationType = AnimationType.QuarticEaseOut,
            TransitionDurationMs = 350,
            LockInputDuringTransition = true,
        };

        // build example menu strip demonstrating top‑level menus and submenus
        this.menuStrip = new MenuStrip
        {
            Name = "menuStrip",
            Dock = DockStyle.Top,
            ShowSubmenuArrow = false,
        };

        
        // use extension helpers for concise syntax
        var fileMenu = this.menuStrip.AddMenuItem("File");
        fileMenu.AddMenuItem("Open", (s, e) => { /* nop */ }, Keys.Control | Keys.O);
        fileMenu.AddSeparator();
        fileMenu.AddMenuItem("Exit", (s, e) => this.Close(), Keys.Control | Keys.X);

        var helpMenu = this.menuStrip.AddMenuItem("Help");
        helpMenu.AddMenuItem("About", (s, e) =>
        {
            Debug.WriteLine("Orivy Example\nA simple demo of the Orivy UI framework.\n\nhttps://github.com/mahmutyildirim/orivy");
        });

        var transitionsMenu = this.menuStrip.AddMenuItem("Transitions");
        InitializeTransitionMenu(transitionsMenu);

        // --- ExtendMenu: drop-down that appears when the extend button (⋯) in
        // the title bar is clicked. ExtendBox must be true to show the button.
        this.extendMenu = new ContextMenuStrip();
        
        this.extendMenu.AddMenuItem("Settings", (s, e) => Debug.WriteLine("Settings clicked"), Keys.Control | Keys.O);
        this.extendMenu.AddMenuItem("Check for Updates", (s, e) => Debug.WriteLine("Update check"));
        this.extendMenu.AddSeparator();
        var themeItem = this.extendMenu.AddMenuItem("Dark Mode", null, Keys.Control | Keys.L);
        themeItem.CheckOnClick = true;
        themeItem.Checked = ColorScheme.IsDarkMode;
        themeItem.CheckedChanged += (s, e) => ColorScheme.IsDarkMode = !ColorScheme.IsDarkMode;
        var extendTransitionsMenu = this.extendMenu.AddMenuItem("Page Transition");
        InitializeTransitionMenu(extendTransitionsMenu);

        // assign a real icon so the title bar shows one; the menu glyph option
        // below can be toggled to switch behaviour
        this.Icon = System.Drawing.SystemIcons.Application;
        // Uncomment to replace the icon with a tiny menu glyph:
        // this.ShowMenuInsteadOfIcon = true;

        // wire up ExtendBox + ExtendMenu
        this.ExtendBox = true;
        this.extendMenu.UseAccordionSubmenus = true;
        this.ExtendMenu = this.extendMenu;
        this.ShowMenuInsteadOfIcon = true;
        this.FormMenu = this.extendMenu;

        this.panel.Controls.Add(this.buttonOpenGL);
        this.panel.Controls.Add(this.buttonSoftware);
        this.panel.Controls.Add(this.buttonDirectX);
        this.panel.Controls.Add(this.buttonDarkMode);
        
        windowPageControl.Controls.Add(panel);
        windowPageControl.Controls.Add(panel2);
        windowPageControl.Controls.Add(panel3);
        windowPageControl.Controls.Add(panel4);
        windowPageControl.Controls.Add(panel5);
        windowPageControl.Controls.Add(panel6);

        InitializeBindingDemoPage();
        InitializeGridListDemo();

        extendMenu.ShowShortcutKeys = true;
        menuStrip.ShowShortcutKeys = true;

        // 
        // MainWindow
        // 
        this.Name = "MainWindow";
        this.Text = "Orivy Example";
        this.Width = 1100;
        this.Height = 650;
        this.DwmMargin = 1000;
        this.Padding = new(10);
        this.WindowThemeType = WindowThemeType.Tabbed;
        this.ContextMenuStrip = this.extendMenu;
        this.WindowPageControl = windowPageControl;
        this.FormStartPosition = SDUI.FormStartPosition.CenterScreen;
        this.RenderBackend = SDUI.Rendering.RenderBackend.Software;
        this.Controls.Add(this.windowPageControl);
        this.Controls.Add(this.menuStrip);
        this.menuStrip.BringToFront();
        this.ResumeLayout(false);
    }

    private void InitializeBindingDemoPage()
    {
        _bindingPanel = new Container
        {
            Name = "bindingPanel",
            Text = "Binding Lab",
            Padding = new Thickness(24),
            Dock = DockStyle.Fill,
            Radius = new Radius(0),
            Border = new Thickness(0),
            AutoScroll = true,
            AutoScrollMargin = new SKSize(0, 24)
        }.SetDataContext(_bindingDemoViewModel);

        var header = CreateBindingCard(
            "bindingHeader",
            "Binding Lab\nSDUI keeps state flow local to the control tree. Bindings start from the consumer control, and user actions stay on the existing event system instead of forcing command objects everywhere.",
            110,
            new Thickness(0, 0, 0, 16),
            ColorScheme.SurfaceContainerHigh);

        var snippet = CreateBindingCard(
            "bindingSnippet",
            "panel.SetDataContext(viewModel)\nhero.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.HeroText).OneWay()\npresetCombo.Link(view => view.SelectedItem).FromData((BindingDemoViewModel vm) => vm.SelectedPreset).TwoWay()\ndeployButton.When<BindingDemoViewModel>(\"Click\", (vm, _) => vm.MarkDeployed(vm.DeploymentPayload))",
            132,
            new Thickness(0, 0, 0, 14),
            ColorScheme.SurfaceVariant);

        var hero = CreateBindingCard("bindingHero", string.Empty, 108, new Thickness(0, 0, 0, 14), ColorScheme.Primary.WithAlpha(230));
        hero.ForeColor = SKColors.White;
        hero.Border = new Thickness(0);
        hero.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.HeroText).OneWay();
        hero.Link(view => view.BackColor).FromData((BindingDemoViewModel vm) => vm.AccentColor).OneWay();

        var summary = CreateBindingCard("bindingSummary", string.Empty, 92, new Thickness(0, 0, 0, 14), ColorScheme.SurfaceContainer);
        summary.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.SummaryText).OneWay();

        var alertCard = CreateBindingCard("bindingAlertCard", string.Empty, 78, new Thickness(0, 0, 0, 14), new SKColor(185, 28, 28));
        alertCard.ForeColor = SKColors.White;
        alertCard.Border = new Thickness(0);
        alertCard.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.AlertText).OneWay();
        alertCard.Link(view => view.Visible).FromData((BindingDemoViewModel vm) => vm.AlertVisible).OneWay();

        var statusCard = CreateBindingCard("bindingStatus", string.Empty, 94, new Thickness(0, 0, 0, 16), ColorScheme.SurfaceContainerHigh);
        statusCard.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.StatusText).OneWay();

        var selectionRow = new Container
        {
            Name = "bindingSelectionRow",
            Dock = DockStyle.Top,
            Height = 52,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var teamCombo = new ComboBox
        {
            Name = "bindingTeamCombo",
            Dock = DockStyle.Right,
            Width = 240,
            Margin = new Thickness(12, 0, 0, 0)
        };
        teamCombo.Link(view => view.Items).FromData((BindingDemoViewModel vm) => vm.Teams).OneWay();
        teamCombo.Link(view => view.SelectedItem).FromData((BindingDemoViewModel vm) => vm.SelectedTeam).TwoWay();

        var presetCombo = new ComboBox
        {
            Name = "bindingPresetCombo",
            Dock = DockStyle.Fill
        };
        presetCombo.Link(view => view.Items).FromData((BindingDemoViewModel vm) => vm.Presets).OneWay();
        presetCombo.Link(view => view.SelectedItem).FromData((BindingDemoViewModel vm) => vm.SelectedPreset).TwoWay();
        selectionRow.Controls.Add(teamCombo);
        selectionRow.Controls.Add(presetCombo);

        var actionRow = new Container
        {
            Name = "bindingActionRow",
            Height = 42,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var deployButton = new Button
        {
            Name = "bindingDeployButton",
            Dock = DockStyle.Right,
            Width = 190,
            Margin = new Thickness(12, 0, 0, 0)
        };
        deployButton.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.DeployActionText).OneWay();
        deployButton.Link(view => view.Enabled).FromData((BindingDemoViewModel vm) => vm.CanDeploy).OneWay();
        deployButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.MarkDeployed(vm.DeploymentPayload));

        var addTeamButton = new Button
        {
            Name = "bindingAddTeamButton",
            Dock = DockStyle.Right,
            Width = 150,
            Margin = new Thickness(12, 0, 0, 0),
            Text = "Add Team"
        };
        addTeamButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.AddTeam());

        var toggleAlertButton = new Button
        {
            Name = "bindingToggleAlertButton",
            Dock = DockStyle.Right,
            Width = 170,
            Margin = new Thickness(12, 0, 0, 0),
            Text = "Toggle Signal"
        };
        toggleAlertButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.ToggleAlert());

        var cyclePresetButton = new Button
        {
            Name = "bindingCyclePresetButton",
            Dock = DockStyle.Fill,
            Text = "Cycle Preset"
        };
        cyclePresetButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.CyclePreset());

        var taskShell = new Container
        {
            Name = "bindingTaskShell",
            Dock = DockStyle.Top,
            Height = 266,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var taskDetail = CreateBindingCard("bindingTaskDetail", string.Empty, 266, new Thickness(14, 0, 0, 0), ColorScheme.SurfaceContainerHigh);
        taskDetail.Dock = DockStyle.Right;
        taskDetail.Width = 300;
        taskDetail.Height = 266;
        taskDetail.Margin = new Thickness(14, 0, 0, 0);
        taskDetail.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.TaskDetailText).OneWay();

        var taskGrid = new GridList
        {
            Name = "bindingTaskGrid",
            Dock = DockStyle.Fill,
            Radius = new Radius(16),
            Border = new Thickness(1),
            BorderColor = ColorScheme.Outline.WithAlpha(120),
            BackColor = ColorScheme.SurfaceContainer,
            HeaderVisible = true,
            StickyHeader = true,
            ShowGridLines = true,
            GroupingEnabled = false,
            MultiSelect = false
        };
        taskGrid.Columns.Add(new GridListColumn { Name = "name", HeaderText = "Task", Width = 180f, MinWidth = 120f, SizeMode = GridListColumnSizeMode.Auto });
        taskGrid.Columns.Add(new GridListColumn { Name = "state", HeaderText = "State", Width = 92f, MinWidth = 72f, CellTextAlign = ContentAlignment.MiddleCenter, SizeMode = GridListColumnSizeMode.Auto });
        taskGrid.Columns.Add(new GridListColumn { Name = "lane", HeaderText = "Lane", Width = 110f, MinWidth = 84f, SizeMode = GridListColumnSizeMode.Auto });
        taskGrid.Columns.Add(new GridListColumn { Name = "summary", HeaderText = "Summary", Width = 260f, MinWidth = 180f, SizeMode = GridListColumnSizeMode.Fill, FillWeight = 1.5f, Sortable = false });
        taskGrid.Link(view => view.Items).FromData((BindingDemoViewModel vm) => vm.Tasks, item => CreateBindingTaskItem((BindingTaskRow)item));
        taskGrid.Link(view => view.SelectedIndex).FromData((BindingDemoViewModel vm) => vm.SelectedTaskIndex).TwoWay();

        taskShell.Controls.Add(taskDetail);
        taskShell.Controls.Add(taskGrid);

        var taskActionRow = new Container
        {
            Name = "bindingTaskActionRow",
            Dock = DockStyle.Top,
            Height = 42,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var advanceTaskButton = new Button
        {
            Name = "bindingAdvanceTaskButton",
            Dock = DockStyle.Right,
            Width = 180,
            Margin = new Thickness(12, 0, 0, 0),
            Text = "Advance Task"
        };
        advanceTaskButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.AdvanceSelectedTask());

        var addTaskButton = new Button
        {
            Name = "bindingAddTaskButton",
            Dock = DockStyle.Fill,
            Text = "Add Task"
        };
        addTaskButton.When<BindingDemoViewModel>("Click", (vm, _) => vm.AddTask());

        taskActionRow.Controls.Add(advanceTaskButton);
        taskActionRow.Controls.Add(addTaskButton);

        var taskFooter = CreateBindingCard("bindingTaskFooter", string.Empty, 84, new Thickness(0, 0, 0, 14), ColorScheme.SurfaceVariant);
        taskFooter.Link(view => view.Text).FromData((BindingDemoViewModel vm) => vm.TaskFooterText).OneWay();

        var colorShell = new Container
        {
            Name = "bindingColorShell",
            Dock = DockStyle.Top,
            Height = 228,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var colorDetail = CreateBindingCard(
            "bindingColorDetail",
            "Color Sync\nThe ColorPicker is bound two-way. Move any channel and the hero surface above updates immediately because both are driven by the same view-model color property.",
            228,
            new Thickness(14, 0, 0, 0),
            ColorScheme.SurfaceContainerHigh);
        colorDetail.Dock = DockStyle.Right;
        colorDetail.Width = 300;
        colorDetail.Height = 228;

        var colorPicker = new ColorPicker
        {
            Name = "bindingColorPicker",
            Dock = DockStyle.Fill,
            Margin = new Thickness(0),
            Radius = new Radius(16),
            Border = new Thickness(1),
            BorderColor = ColorScheme.Outline.WithAlpha(110),
            BackColor = ColorScheme.SurfaceContainer
        };
        colorPicker.Link(view => view.SelectedColor).FromData((BindingDemoViewModel vm) => vm.PickerAccentColor).TwoWay();

        colorShell.Controls.Add(colorDetail);
        colorShell.Controls.Add(colorPicker);

        var validationShell = new Container
        {
            Name = "bindingValidationShell",
            Dock = DockStyle.Top,
            Height = 306,
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        var validationGuide = CreateBindingCard(
            "bindingValidationGuide",
            "Validation Flow\nThese controls use the existing ValidationRule system. Error cards are bound directly to control ValidationText and HasValidationError, so no extra glue object is required.",
            306,
            new Thickness(14, 0, 0, 0),
            ColorScheme.SurfaceContainerHigh);
        validationGuide.Dock = DockStyle.Right;
        validationGuide.Width = 320;
        validationGuide.Height = 306;

        var validationStack = new Container
        {
            Name = "bindingValidationStack",
            Dock = DockStyle.Fill,
            Padding = new Thickness(0),
            BackColor = SKColors.Transparent,
            Border = new Thickness(0)
        };

        bindingValidationStatusCard = CreateBindingCard(
            "bindingValidationStatus",
            "Validation Submit\nChoose a team and a preset to satisfy the rules.",
            76,
            new Thickness(0, 0, 0, 12),
            ColorScheme.SurfaceVariant);

        bindingValidationRegionError = CreateBindingCard(
            "bindingValidationRegionError",
            string.Empty,
            52,
            new Thickness(0, 0, 0, 10),
            new SKColor(185, 28, 28));
        bindingValidationRegionError.ForeColor = SKColors.White;
        bindingValidationRegionError.Border = new Thickness(0);

        bindingValidationPresetError = CreateBindingCard(
            "bindingValidationPresetError",
            string.Empty,
            60,
            new Thickness(0, 0, 0, 10),
            new SKColor(127, 29, 29));
        bindingValidationPresetError.ForeColor = SKColors.White;
        bindingValidationPresetError.Border = new Thickness(0);

        bindingValidationSubmitButton = new Button
        {
            Name = "bindingValidationSubmitButton",
            Text = "Validate Workflow",
            Dock = DockStyle.Top,
            Height = 42,
            Margin = new Thickness(0, 0, 0, 10)
        };
        bindingValidationSubmitButton.Click += BindingValidationSubmitButton_Click;

        bindingValidationPresetCombo = new ComboBox
        {
            Name = "bindingValidationPresetCombo",
            Dock = DockStyle.Top,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 10)
        };
        bindingValidationPresetCombo.Link(view => view.Items).FromData((BindingDemoViewModel vm) => vm.Presets).OneWay();
        bindingValidationPresetCombo.Link(view => view.SelectedItem).FromData((BindingDemoViewModel vm) => vm.SelectedPreset).TwoWay();

        bindingValidationTeamCombo = new ComboBox
        {
            Name = "bindingValidationTeamCombo",
            Dock = DockStyle.Top,
            Height = 40,
            Margin = new Thickness(0, 0, 0, 10)
        };
        bindingValidationTeamCombo.Link(view => view.Items).FromData((BindingDemoViewModel vm) => vm.Teams).OneWay();
        bindingValidationTeamCombo.Link(view => view.SelectedItem).FromData((BindingDemoViewModel vm) => vm.SelectedTeam).TwoWay();

        bindingValidationTeamCombo.AddValidationRule(new RequiredFieldValidationRule
        {
            ErrorMessage = "Choose a team before deployment validation can pass."
        });
        bindingValidationPresetCombo.AddValidationRule(new RequiredFieldValidationRule
        {
            ErrorMessage = "Choose a preset before deployment validation can pass."
        });
        bindingValidationPresetCombo.AddValidationRule(new CustomValidationRule(
            _ =>
            {
                var teamName = bindingValidationTeamCombo.SelectedItem as string ?? string.Empty;
                var presetName = bindingValidationPresetCombo.SelectedItem as string ?? string.Empty;

                if (presetName.IndexOf("Incident", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    teamName.IndexOf("Ops", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return (false, "Incident presets are reserved for Ops teams in this example workflow.");
                }

                return (true, string.Empty);
            }));

        bindingValidationTeamCombo.SelectedItemChanged += (_, _) =>
        {
            bindingValidationTeamCombo.ValidateNow();
            bindingValidationPresetCombo.ValidateNow();
        };
        bindingValidationPresetCombo.SelectedItemChanged += (_, _) => bindingValidationPresetCombo.ValidateNow();

        bindingValidationRegionError.Link(view => view.Text).From(bindingValidationTeamCombo, (ComboBox combo) => combo.ValidationText).OneWay();
        bindingValidationRegionError.Link(view => view.Visible).From(bindingValidationTeamCombo, (ComboBox combo) => combo.HasValidationError).OneWay();
        bindingValidationPresetError.Link(view => view.Text).From(bindingValidationPresetCombo, (ComboBox combo) => combo.ValidationText).OneWay();
        bindingValidationPresetError.Link(view => view.Visible).From(bindingValidationPresetCombo, (ComboBox combo) => combo.HasValidationError).OneWay();

        validationStack.Controls.Add(bindingValidationStatusCard);
        validationStack.Controls.Add(bindingValidationRegionError);
        validationStack.Controls.Add(bindingValidationPresetError);
        validationStack.Controls.Add(bindingValidationSubmitButton);
        validationStack.Controls.Add(bindingValidationPresetCombo);
        validationStack.Controls.Add(bindingValidationTeamCombo);

        validationShell.Controls.Add(validationGuide);
        validationShell.Controls.Add(validationStack);

        actionRow.Controls.Add(deployButton);
        actionRow.Controls.Add(addTeamButton);
        actionRow.Controls.Add(toggleAlertButton);
        actionRow.Controls.Add(cyclePresetButton);

        _bindingPanel.Controls.Add(statusCard);
        _bindingPanel.Controls.Add(validationShell);
        _bindingPanel.Controls.Add(colorShell);
        _bindingPanel.Controls.Add(taskFooter);
        _bindingPanel.Controls.Add(taskActionRow);
        _bindingPanel.Controls.Add(taskShell);
        _bindingPanel.Controls.Add(actionRow);
        _bindingPanel.Controls.Add(selectionRow);
        _bindingPanel.Controls.Add(alertCard);
        _bindingPanel.Controls.Add(summary);
        _bindingPanel.Controls.Add(hero);
        _bindingPanel.Controls.Add(snippet);
        _bindingPanel.Controls.Add(header);

        windowPageControl.Controls.Add(_bindingPanel);
        _bindingPanel.BringToFront();
        windowPageControl.PerformLayout();
        windowPageControl.Invalidate();
    }

    private static Element CreateBindingCard(string name, string text, float height, Thickness margin, SKColor background)
    {
        return new Element
        {
            Name = name,
            Text = text,
            Dock = DockStyle.Top,
            Height = (int)Math.Round(height),
            Margin = margin,
            Padding = new Thickness(16),
            Radius = new Radius(16),
            Border = new Thickness(1),
            BorderColor = ColorScheme.Outline.WithAlpha(140),
            BackColor = background,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static GridListItem CreateBindingTaskItem(BindingTaskRow row)
    {
        var item = new GridListItem
        {
            Tag = row
        };

        item.Cells.Add(new GridListCell { Text = row.Name });
        item.Cells.Add(new GridListCell { Text = row.State });
        item.Cells.Add(new GridListCell { Text = row.Lane });
        item.Cells.Add(new GridListCell { Text = row.Summary });
        return item;
    }

    private Container panel;
    private Container panel2;
    private Container panel3;
    private Container panel4;
    private Container panel5;
    private Container panel6;
    private Element buttonOpenGL;
    private Element buttonSoftware;
    private Element buttonDirectX;
    private Element buttonDarkMode;
    private Element visualStyleHeader;
    private Element visualStyleMotionHero;
    private Element visualStyleInteractiveCard;
    private Element visualStyleDangerCard;
    private Element visualStyleDisabledCard;
    private Element visualStyleFooterAction;
    private Button visualStylePrimaryButton;
    private Button visualStyleGhostButton;
    private Element visualStyleScrollProbe;
    private Element gridListStatus;
    private GridList gridListPrimary;
    private GridList gridListCompact;
    private Button gridListToggleHeaderButton;
    private Button gridListToggleStickyButton;
    private Button gridListToggleGroupingButton;
    private Button gridListToggleGridLinesButton;
    private Button gridListToggleRowResizeButton;

    private MenuStrip menuStrip;
    private ContextMenuStrip extendMenu;
    private WindowPageControl windowPageControl;
    private ComboBox bindingValidationTeamCombo;
    private ComboBox bindingValidationPresetCombo;
    private Element bindingValidationRegionError;
    private Element bindingValidationPresetError;
    private Element bindingValidationStatusCard;
    private Button bindingValidationSubmitButton;

}
