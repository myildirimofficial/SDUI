using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls;

public class FormControlBox : Control
{
    int _mouseState = 0;
    int _mousePos;
    Rectangle _closeButtonRect = new(3, 3, 16, 16);
    Rectangle _minimizeButtonRect = new(3, 23, 16, 16);
    Rectangle _maximizeButtonRect = new(3, 43, 16, 16);

    private bool _isVertical = true;
    public bool IsVertical
    {
        get => _isVertical;
        set
        {
            _isVertical = value;
            if (value)
            {
                _closeButtonRect = new(3, 3, 16, 16);
                _minimizeButtonRect = new(3, 23, 16, 16);
                _maximizeButtonRect = new(3, 43, 16, 16);
                Size = new(23, 65);
            }
            else
            {
                _closeButtonRect = new(3, 3, 16, 16);
                _minimizeButtonRect = new(23, 3, 16, 16);
                _maximizeButtonRect = new(43, 3, 16, 16);
                Size = new(65, 23);
            }

            Invalidate();
        }
    }

    protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
    {
        base.OnMouseDown(e);

        _mouseState = 2;
        Invalidate();
    }

    protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
    {
        base.OnMouseUp(e);
        var form = FindForm();
        if (form == null)
            return;

        if (_mousePos > 3 && _mousePos < 20)
        {
            form.Close();
        }
        else if (_mousePos > 23 && _mousePos < 40)
        {
            form.WindowState = FormWindowState.Minimized;
        }
        else if (_mousePos > 43 && _mousePos < 60)
        {
            if (_maximize == true)
            {
                if (form.WindowState == FormWindowState.Maximized)
                {
                    form.WindowState = FormWindowState.Minimized;
                    form.WindowState = FormWindowState.Normal;
                }
                else
                {
                    form.WindowState = FormWindowState.Minimized;
                    form.WindowState = FormWindowState.Maximized;
                }
            }
        }
        _mouseState = 1;
        Invalidate();
    }

    protected override void OnMouseEnter(System.EventArgs e)
    {
        base.OnMouseEnter(e);
        _mouseState = 1;
        Invalidate();
    }

    protected override void OnMouseLeave(System.EventArgs e)
    {
        base.OnMouseLeave(e);
        _mouseState = 0;
        Invalidate();
    }

    protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isVertical)
            _mousePos = e.Location.Y;
        else
            _mousePos = e.Location.X;
        Invalidate();
    }

    #region  Properties

    bool _maximize = true;
    public bool EnableMaximize
    {
        get { return _maximize; }
        set
        {
            _maximize = value;
            Invalidate();
        }
    }

    #endregion

    public FormControlBox()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.ResizeRedraw
                | ControlStyles.DoubleBuffer,
            true
        );
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Font = new Font("Webdings", 9);
        Anchor = AnchorStyles.Top | AnchorStyles.Left;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        GroupBoxRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);

        var graphics = e.Graphics;

        base.OnPaint(e);

        using var borderPen = new Pen(ColorScheme.BorderColor);

        using var brushClose = new SolidBrush(Color.FromArgb(253, 97, 90));

        using var brushMin = new SolidBrush(Color.FromArgb(254, 189, 47));

        using var brushMax = new SolidBrush(Color.YellowGreen);

        switch (_mouseState)
        {
            case 1:
                if (_mousePos > 3 && _mousePos < 20)
                {
                    brushClose.Color = brushClose.Color.Alpha(150);
                }
                else if (_mousePos > 23 && _mousePos < 40)
                {
                    brushMin.Color = brushMin.Color.Alpha(150);
                }
                else if (_maximize && _mousePos > 43 && _mousePos < 60)
                {
                    brushMax.Color = brushMax.Color.Alpha(150);
                }
                break;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        graphics.FillEllipse(brushClose, _closeButtonRect);
        graphics.DrawEllipse(new Pen(brushClose.Color.Alpha(200)), _closeButtonRect);

        graphics.FillEllipse(brushMin, _minimizeButtonRect);
        graphics.DrawEllipse(new Pen(brushMin.Color.Alpha(200)), _minimizeButtonRect);

        if (_maximize == true)
        {
            graphics.FillEllipse(brushMax, _maximizeButtonRect);
            graphics.DrawEllipse(new Pen(brushMax.Color.Alpha(200)), _maximizeButtonRect);

            if (_isVertical)
                graphics.DrawString("@", Font, new SolidBrush(ColorScheme.ForeColor), new RectangleF(4, 42, 0, 0));
            else
                graphics.DrawString("@", Font, new SolidBrush(ColorScheme.ForeColor), new RectangleF(43, 2, 0, 0));
        }

        if (_isVertical)
        {
            graphics.DrawString("r", Font, new SolidBrush(Color.FromArgb(110, 1, 1)), new RectangleF(4f, 2, 0, 0));
            graphics.DrawString("0", Font, new SolidBrush(Color.FromArgb(153, 87, 0)), new RectangleF(4f, 19f, 0, 0));
        }
        else
        {
            graphics.DrawString("r", Font, new SolidBrush(Color.FromArgb(110, 1, 1)), new RectangleF(4, 2, 0, 0));
            graphics.DrawString("0", Font, new SolidBrush(Color.FromArgb(153, 87, 0)), new RectangleF(24, 0, 0, 0));
        }
    }
}
