using System.Drawing;
using System.Windows.Forms;

namespace SDUI.Controls;

public class Separator : UserControl
{
    public Separator()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint,
            true
        );

        UpdateStyles();

        this.Size = new Size(120, 6);
    }

    private bool _isVertical = false;
    public bool IsVertical
    {
        get => _isVertical;
        set
        {
            _isVertical = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var pen = new Pen(ColorScheme.BorderColor))
        {
            if (_isVertical)
            {
                var x = Width / 2;
                e.Graphics.DrawLine(pen, x, 0, x, Height);
            }
            else
            {
                var y = Height / 2;
                e.Graphics.DrawLine(pen, 0, y, Width, y);
            }
        }
    }
}
