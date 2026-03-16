using SDUI;
using SDUI.Animation;
using SDUI.Controls;
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
            Padding = new(5),
            Dock = SDUI.DockStyle.Fill,
            Radius = new(0),
            Border = new(0)
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
                new BoxShadow(0, 1, 3, 0, SKColors.Black.WithAlpha(30)),           // soft outer
                new BoxShadow(0, 4, 12, new Radius(2), SKColors.Black.WithAlpha(15)), // wide spread
                new BoxShadow(0, 1, 2, 0, SKColors.Black.WithAlpha(40), inset: true)     // subtle inset
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
            BackColor = ColorScheme.SurfaceVariant,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(14),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
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
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(16),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
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
            BackColor = new SKColor(18, 24, 38),
            ForeColor = SKColors.White,
            Radius = new(20),
            Border = new(1),
            BorderColor = new SKColor(74, 93, 124),
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
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(16),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
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
            BackColor = ColorScheme.Surface,
            ForeColor = ColorScheme.ForeColor,
            Radius = new(16),
            Border = new(1),
            BorderColor = ColorScheme.Outline,
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
            BackColor = ColorScheme.Primary,
            ForeColor = SKColors.White,
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
            BackColor = new SKColor(19, 36, 31),
            ForeColor = new SKColor(220, 252, 231),
            Radius = new(18),
            Border = new(1),
            BorderColor = new SKColor(52, 211, 153, 120),
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
                    .Shadow(new BoxShadow(0f, 2f, 8f, 0, SKColors.Black.WithAlpha(16))))
                .OnHover(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .BorderColor(ColorScheme.Primary)
                    .Shadow(new BoxShadow(0f, 12f, 24f, 0, SKColors.Black.WithAlpha(28))))
                .OnPressed(rule => rule
                    .Opacity(0.93f)
                    .Background(ColorScheme.SurfaceVariant.Brightness(-0.03f))
                    .Shadow(new BoxShadow(0f, 4f, 12f, 0, SKColors.Black.WithAlpha(18))))
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
                    .Background(new SKColor(160, 38, 38))
                    .Foreground(SKColors.White)
                    .BorderColor(new SKColor(239, 68, 68))
                    .Shadow(new BoxShadow(0f, 14f, 30f, 0, new SKColor(127, 29, 29, 64))))
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
                    .Foreground(SKColors.White)
                    .Radius(14)
                    .Shadow(new BoxShadow(0f, 4f, 12f, 0, SKColors.Black.WithAlpha(22))))
                .OnHover(rule => rule
                    .Background(ColorScheme.Primary.Brightness(0.06f))
                    .Shadow(new BoxShadow(0f, 10f, 18f, 0, SKColors.Black.WithAlpha(30))))
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
                    .Background(new SKColor(19, 36, 31))
                    .Foreground(new SKColor(220, 252, 231))
                    .Border(1)
                    .BorderColor(new SKColor(52, 211, 153, 120))
                    .Radius(18))
                .OnHover(rule => rule
                    .BorderColor(new SKColor(110, 231, 183))
                    .Background(new SKColor(24, 45, 39)));
        });

        visualStyleDangerCard.Click += VisualStyleDangerToggle_Click;
        visualStylePrimaryButton.Click += VisualStylePrimaryButton_Click;
        visualStyleFooterAction.Click += VisualStyleEnableDisabled_Click;

        this.panel4.Controls.Add(this.visualStyleScrollProbe);
        this.panel4.Controls.Add(this.visualStyleFooterAction);
        this.panel4.Controls.Add(this.visualStyleGhostButton);
        this.panel4.Controls.Add(this.visualStylePrimaryButton);
        this.panel4.Controls.Add(this.visualStyleDisabledCard);
        this.panel4.Controls.Add(this.visualStyleDangerCard);
        this.panel4.Controls.Add(this.visualStyleInteractiveCard);
        this.panel4.Controls.Add(this.visualStyleMotionHero);
        this.panel4.Controls.Add(this.visualStyleHeader);

        windowPageControl = new()
        {
            Name = "windowPageControl",
            Dock = SDUI.DockStyle.Fill,
            TransitionEffect = WindowPageTransitionEffect.SlideHorizontal,
            TransitionAnimationType = AnimationType.CubicEaseOut,
            TransitionIncrement = 0.18,
            TransitionSecondaryIncrement = 0.18,
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

        extendMenu.ShowShortcutKeys = true;
        menuStrip.ShowShortcutKeys = true;

        // 
        // MainWindow
        // 
        this.Name = "MainWindow";
        this.Text = "Orivy Example";
        this.Width = 800;
        this.Height = 450;
        this.DwmMargin = -1;
        this.Padding = new(10);
        this.EnableMica = true;
        this.ContextMenuStrip = this.extendMenu;
        this.WindowPageControl = windowPageControl;
        this.FormStartPosition = SDUI.FormStartPosition.CenterScreen;
        this.RenderBackend = SDUI.Rendering.RenderBackend.Software;
        this.Controls.Add(this.windowPageControl);
        this.Controls.Add(this.menuStrip);
        this.menuStrip.BringToFront();
        this.ResumeLayout(false);
    }

    private Element panel;
    private Element panel2;
    private Element panel3;
    private Element panel4;
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
    private WindowPageControl windowPageControl;
    private MenuStrip menuStrip;
    private ContextMenuStrip extendMenu;
    private MenuItem fileMenuItem;
    private MenuItem openMenuItem;
    private MenuItem helpMenuItem;
    private MenuItem transitionsMenuItem;
    
    private MenuItem settingsMenuItem;
    private MenuItem checkForUpdatesMenuItem;
    private MenuItemSeparator menuItemSeparator;

}
