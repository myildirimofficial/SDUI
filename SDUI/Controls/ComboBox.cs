using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls;

public class ComboBox : System.Windows.Forms.ComboBox
{
    private int _radius = 5;
    public int Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;

            _radius = value;
            Invalidate();
        }
    }

    private float _shadowDepth = 4f;
    public float ShadowDepth
    {
        get => _shadowDepth;
        set
        {
            if (_shadowDepth == value)
                return;

            _shadowDepth = value;
            Invalidate();
        }
    }

    public new int ItemHeight
    {
        get => base.ItemHeight;
        set
        {
            if(value < 22)
                value = 22;

            if (!IsHandleCreated)
                return;

            if (base.ItemHeight == value)
                return;
     
            base.ItemHeight = value;
            Invalidate();
        }
    }

    public ComboBox()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.Selectable
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.EnableNotifyMessage,
            true
        );

        DoubleBuffered = true;
        DrawMode = DrawMode.OwnerDrawVariable;
        DropDownStyle = ComboBoxStyle.DropDownList;
        ItemHeight = 22;
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var index = e.Index;
        if (index < 0 || index >= Items.Count)
            return;

        var foreColor = ColorScheme.ForeColor;

        if (
            (e.State & DrawItemState.Selected) == DrawItemState.Selected
            || (e.State & DrawItemState.Focus) == DrawItemState.Focus
            || (e.State & DrawItemState.NoFocusRect) != DrawItemState.NoFocusRect
        )
        {
            foreColor = Color.White;
            using var brush = new SolidBrush(Color.Blue);
            e.Graphics.FillRectangle(brush, e.Bounds);
        }
        else
            e.Graphics.FillRectangle(ColorScheme.BackColor, e.Bounds);

        var stringFormat = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter,
        };

        using var textBrush = new SolidBrush(foreColor);
        e.Graphics.DrawString(Items[index].ToString(), e.Font, textBrush, e.Bounds, stringFormat);
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        // base.OnParentBackColorChanged already triggers repaint
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        SuspendLayout();
        Update();
        ResumeLayout();
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND (0x14) to reduce flicker
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
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

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // WS_EX_COMPOSITED for smoother rendering (reduces flicker)
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        ButtonRenderer.DrawParentBackground(graphics, ClientRectangle, this);

        var rectf = ClientRectangle.ToRectangleF();

        if (ColorScheme.DrawDebugBorders)
        {
            using var redPen = new Pen(Color.Red, 1);
            redPen.Alignment = PenAlignment.Outset;
            graphics.DrawRectangle(redPen, 0, 0, rectf.Width - 1, rectf.Height - 1);
        }

        var inflate = _shadowDepth / 4f;
        //rectf.Inflate(-inflate, -inflate);

        var textRectangle = new Rectangle(3 * (DeviceDpi / 96), 0, Width - (18 * (DeviceDpi / 96)), Height);

        var backColor = ColorScheme.BackColor.Alpha(100);
        var borderColor = ColorScheme.BorderColor;

        using var path = rectf.Radius(_radius);

        using var backBrush = new SolidBrush(backColor);
        e.Graphics.FillPath(backBrush, path);

        var _extendBoxRect = new RectangleF(
            rectf.Width - (24f * (DeviceDpi / 96)),
            0,
            (16 * (DeviceDpi / 96)),
            rectf.Height - (4 * (DeviceDpi / 96)) + _shadowDepth
        );

        using var symbolPen = new Pen(ColorScheme.ForeColor);
        graphics.DrawLine(
            symbolPen,
            _extendBoxRect.Left + _extendBoxRect.Width / 2 - (5 * (DeviceDpi / 96)) - 1,
            _extendBoxRect.Top + _extendBoxRect.Height / 2 - (2 * (DeviceDpi / 96)),
            _extendBoxRect.Left + _extendBoxRect.Width / 2 - (1 * (DeviceDpi / 96)),
            _extendBoxRect.Top + _extendBoxRect.Height / 2 + (3 * (DeviceDpi / 96))
        );

        graphics.DrawLine(
            symbolPen,
            _extendBoxRect.Left + _extendBoxRect.Width / 2 + (5 * (DeviceDpi / 96)) - 1,
            _extendBoxRect.Top + _extendBoxRect.Height / 2 - (2 * (DeviceDpi / 96)),
            _extendBoxRect.Left + _extendBoxRect.Width / 2 - (1 * (DeviceDpi / 96)),
            _extendBoxRect.Top + _extendBoxRect.Height / 2 + (3 * (DeviceDpi / 96))
        );

        graphics.DrawShadow(rectf, _shadowDepth, _radius);
        e.Graphics.DrawPath(ColorScheme.BorderColor, path);

        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.TextBoxControl;
        TextRenderer.DrawText(graphics, Text, Font, textRectangle, ColorScheme.ForeColor, flags);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var preferredSize = base.GetPreferredSize(proposedSize);
        preferredSize.Width += (int)_shadowDepth;
        preferredSize.Height += (int)_shadowDepth;

        return preferredSize;
    }
}
