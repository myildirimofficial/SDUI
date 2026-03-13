using SDUI;
using SDUI.Controls;
using System;

namespace Orivy.Example
{
    internal partial class MainWindow : Window
    {
        internal MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonDirectX_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.DirectX11;
        }

        private void ButtonSoftware_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.Software;
        }

        private void ButtonOpenGL_Click(object sender, EventArgs e)
        {
            this.RenderBackend = SDUI.Rendering.RenderBackend.OpenGL;
        }

        private void ButtonDarkMode_Click(object sender, EventArgs e)
        {
            ColorScheme.IsDarkMode = !ColorScheme.IsDarkMode;
        }
    }
}
