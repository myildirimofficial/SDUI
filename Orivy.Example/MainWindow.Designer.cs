using SDUI;
using SDUI.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
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
            Dock = SDUI.DockStyle.Fill,
            Radius = new(4, 4, 0, 0),
            Border = new(2),
            Shadows = new[] {
                new BoxShadow(0, 1, 3, 0, SKColors.Black.WithAlpha(30)),           // soft outer
                new BoxShadow(0, 4, 12, new Radius(2), SKColors.Black.WithAlpha(15)), // wide spread
                new BoxShadow(0, 1, 2, 0, SKColors.Black.WithAlpha(40), inset: true)     // subtle inset
            }
        };

        this.buttonOpenGL = new()
        {
            Name = "buttonOpenGL",
            Text = "OpenGL",
            BackColor = SKColors.Red,
            Dock = SDUI.DockStyle.Bottom,
            Size = new(100, 32),
            Radius = new(4),
            Border = new(1),
        };

        buttonOpenGL.Click += ButtonOpenGL_Click;

        this.buttonSoftware = new()
        {
            Name = "buttonSoftware",
            Text = "Software",
            BackColor = SKColors.Green,
            Size = new(100, 32),
            Dock = SDUI.DockStyle.Bottom,
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
            Dock = SDUI.DockStyle.Bottom,
            Radius = new(4),
            Border = new(1)
        };

        buttonDirectX.Click += ButtonDirectX_Click;

        this.buttonDarkMode = new()
        {
            Name = "buttonDarkMode",
            Text = "Toggle Mode",
            BackColor = SKColors.Blue,
            Dock = SDUI.DockStyle.Bottom,
            Size = new(100, 32),
            Radius = new(4),
            Border = new(0, 1, 0, 1),
        };

        buttonDarkMode.Click += ButtonDarkMode_Click;

        windowPageControl = new()
        {
            Name = "windowPageControl",
            Dock = SDUI.DockStyle.Fill,
        };

        windowPageControl.Controls.Add(panel);
        windowPageControl.Controls.Add(panel);
        windowPageControl.Controls.Add(panel);

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
        this.WindowPageControl = windowPageControl;
        this.FormStartPosition = SDUI.FormStartPosition.CenterScreen;
        this.Controls.Add(this.panel);
        this.panel.Controls.Add(this.buttonOpenGL);
        this.panel.Controls.Add(this.buttonSoftware);
        this.panel.Controls.Add(this.buttonDirectX);
        this.panel.Controls.Add(this.buttonDarkMode);
        this.ResumeLayout(false);
    }

    private Element panel;
    private Element buttonOpenGL;
    private Element buttonSoftware;
    private Element buttonDirectX;
    private Element buttonDarkMode;
    private WindowPageControl windowPageControl;
}
