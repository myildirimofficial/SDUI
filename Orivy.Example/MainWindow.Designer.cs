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
    private Element buttonOpenGL;
    private Element buttonSoftware;
    private Element buttonDirectX;
    private Element buttonDarkMode;
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
