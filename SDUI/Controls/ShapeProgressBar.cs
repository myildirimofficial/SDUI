using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls;

public class ShapeProgressBar : Control
{
    private float _weight = 8;
    public float Weight
    {
        get => _weight;
        set
        {
            if (value < 0)
                value = 1;

            _weight = value;
            Invalidate();
        }
    }

    private long _value;
    public long Value
    {
        get => _value;
        set
        {
            if (value > _maximum)
                value = _maximum;

            _value = value;
            Invalidate();
        }
    }

    private long _maximum = 100;
    public long Maximum
    {
        get => _maximum;
        set
        {
            if (value < 1)
                value = 1;

            _maximum = value;
            Invalidate();
        }
    }

    private Color[] _gradient = new Color[2];
    public Color[] Gradient
    {
        get => _gradient;
        set
        {
            _gradient = value;
            Invalidate();
        }
    }

    private bool _drawHatch = false;
    public bool DrawHatch
    {
        get { return _drawHatch; }
        set
        {
            _drawHatch = value;
            Invalidate();
        }
    }

    private HatchStyle _hatchType = HatchStyle.Min;
    public HatchStyle HatchType
    {
        get { return _hatchType; }
        set
        {
            _hatchType = value;
            Invalidate();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
    }

    public ShapeProgressBar()
    {
        Size = new Size(100, 100);
        Font = new Font("Segoe UI", 15);
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.SupportsTransparentBackColor,
            true
        );
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var calc = (int)Math.Round((double)((360.0 / _maximum) * _value));

        var renderWidth = ClientRectangle.Width - _weight - 1;
        var renderHeight = ClientRectangle.Height - _weight - 1;

        using (
            var brush = new LinearGradientBrush(
                ClientRectangle,
                _gradient[0],
                _gradient[1],
                LinearGradientMode.ForwardDiagonal
            )
        )
        {
            using (var pen = new Pen(brush, _weight))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawArc(pen, _weight / 2, _weight / 2, renderWidth, renderHeight, -90, calc);
            }
        }

        if (_drawHatch)
        {
            using (
                var hatchBrush = new HatchBrush(
                    HatchType,
                    Color.FromArgb(50, _gradient[0]),
                    Color.FromArgb(50, _gradient[1])
                )
            )
            {
                using (var pen = new Pen(hatchBrush, 14f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    graphics.DrawArc(pen, _weight / 2, _weight / 2, renderWidth, renderHeight, -90, calc);
                }
            }
        }

        using (
            var brush = new LinearGradientBrush(
                ClientRectangle,
                ColorScheme.BackColor,
                ColorScheme.BackColor2,
                LinearGradientMode.Vertical
            )
        )
            graphics.FillEllipse(brush, _weight / 2, _weight / 2, renderWidth, renderHeight);

        var percent = (100 / _maximum) * _value;
        var percentString = percent.ToString();
        var stringSize = graphics.MeasureString(percentString, Font);

        using (var textBrush = new SolidBrush(ColorScheme.ForeColor))
            graphics.DrawString(
                percentString,
                Font,
                textBrush,
                Width / 2 - stringSize.Width / 2,
                Height / 2 - stringSize.Height / 2
            );
    }
}
