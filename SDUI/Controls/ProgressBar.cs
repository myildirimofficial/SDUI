using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SDUI.Controls;

public class ProgressBar : Control
{
    private long _value = 0;
    public long Value
    {
        get => _value;
        set
        {
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
            _maximum = value;
            if (value > 0)
                _maximum = value;
            else
                _maximum = 1;

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

    private bool _showAsPercent = false;
    public bool ShowAsPercent
    {
        get => _showAsPercent;
        set
        {
            _showAsPercent = value;
            Invalidate();
        }
    }

    private int _percentIndices = 2;
    public int PercentIndices
    {
        get { return _percentIndices; }
        set
        {
            _percentIndices = value;
            Invalidate();
        }
    }

    private float _maxPercentShowValue = 100;
    public float MaxPercentShowValue
    {
        get => _maxPercentShowValue;
        set
        {
            _maxPercentShowValue = value;
            Invalidate();
        }
    }

    private bool _showValue = false;
    public bool ShowValue
    {
        get { return _showValue; }
        set
        {
            _showValue = value;
            Invalidate();
        }
    }

    private int _radius = 4;
    public int Radius
    {
        get { return _radius; }
        set
        {
            _radius = value <= 0 ? 1 : value;
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

    private HatchStyle _hatchType = HatchStyle.Percent10;
    public HatchStyle HatchType
    {
        get => _hatchType;
        set
        {
            _hatchType = HatchStyle.Percent10;
            //_hatchType = value;
            Invalidate();
        }
    }

    public ProgressBar()
    {
        SetStyle(
            ControlStyles.SupportsTransparentBackColor
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Opaque
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.EnableNotifyMessage,
            true
        );

        DoubleBuffered = true;
        UpdateStyles();
        BackColor = Color.Transparent;
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND (0x14) to reduce flicker
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        this.Invalidate();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        // base.OnParentBackColorChanged already triggers repaint
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0014) // WM_ERASEBKGND
        {
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        ButtonRenderer.DrawParentBackground(graphics, e.ClipRectangle, this);

        var intValue = ((_value / (float)_maximum) * Width);
        var percent = ((100.0f * Value) / Maximum);

        using var linearGradientBrush = new LinearGradientBrush(
            new RectangleF(0, 0, Width, Height),
            _gradient[0],
            _gradient[1],
            90
        );
        using var hatchBrush = new HatchBrush(
            HatchType,
            Color.FromArgb(50, _gradient[0]),
            Color.FromArgb(50, _gradient[1])
        );

        var rect = ClientRectangle.ToRectangleF();

        using (var path = rect.Radius(_radius))
            graphics.FillPath(new SolidBrush(ColorScheme.BorderColor), path);

        if (intValue != 0)
        {
            var rectValue = new RectangleF(rect.X, rect.Y, intValue, rect.Height - 1);
            using var path = rectValue.Radius(_radius);
            graphics.FillPath(linearGradientBrush, path);
            graphics.FillPath(hatchBrush, path);
        }

        graphics.DrawPath(
            new Pen(Color.FromArgb(10, Parent.BackColor.Determine())),
            new Rectangle(0, 0, Width - 1, Height - 1).Radius(_radius)
        );

        if (ShowValue)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.SystemDefault;
            var textShadowColor = ColorScheme.ForeColor.Determine();
            var textColor = ColorScheme.ForeColor;

            if (_showAsPercent)
            {
                if (percent == 100)
                    percent = _maxPercentShowValue;

                Text = percent.ToString($"0.{"0".PadRight(_percentIndices, '0')}") + "%";
            }
            else
                Text = $"{_value} / {_maximum}";

            if (percent > 50)
            {
                textColor = Color.White;
                textShadowColor = Color.Black;
            }

            // draw shadow
            var shadowBrush = new SolidBrush(textShadowColor);
            e.Graphics.DrawString(
                Text,
                Font,
                shadowBrush,
                new Rectangle(1, 1, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }
            );

            shadowBrush.Dispose();
            // draw text
            var textBrush = new SolidBrush(textColor);
            e.Graphics.DrawString(
                Text,
                Font,
                textBrush,
                new Rectangle(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }
            );
        }
    }
}
