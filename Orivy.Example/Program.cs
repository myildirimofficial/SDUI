using Orivy;
using Orivy.Controls;
using SkiaSharp;

namespace Orivy.Example;

class Program
{
    public static void Main(string[] args)
    {
        var window = new Window();
        window.Width = 1100;
        window.Height = 650;
        window.Text = "Orivy Example";
        window.DwmMargin = 1000;
        window.WindowThemeType = WindowThemeType.Tabbed;
        window.BackColor = SKColors.Black.WithAlpha(100);

        Application.Run(new MainWindow());
    }
}
