using System;
using System.Drawing;
using System.Windows.Forms;

namespace SDUI.Controls;

public class TabControl : System.Windows.Forms.TabControl
{
    private Padding _borderRadius = new System.Windows.Forms.Padding(4);
    public Padding Radius
    {
        get => _borderRadius;
        set
        {
            if (_borderRadius == value)
                return;

            _borderRadius = value;
            Invalidate();
        }
    }

    public TabControl()
    {
        SetStyle(
            ControlStyles.SupportsTransparentBackColor
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.EnableNotifyMessage,
            true
        );

        DoubleBuffered = true;
        UpdateStyles();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        // base.OnParentBackColorChanged already triggers repaint
    }

    protected override void CreateHandle()
    {
        base.CreateHandle();

        if (SizeMode != TabSizeMode.Fixed)
            ItemSize = new Size(80, 24);

        Alignment = TabAlignment.Top;
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
        if (TabCount <= 0 || SelectedTab == null)
            return;

        var graphics = e.Graphics;

        GroupBoxRenderer.DrawParentBackground(graphics, e.ClipRectangle, this);

        graphics.SetHighQuality();
        using var borderBrush = new Pen(Color.FromArgb(70, 0, 0, 0));
        using var backBrush = new SolidBrush(
            ColorScheme.BackColor.IsDark() ? Color.FromArgb(10, 255, 255, 255) : Color.FromArgb(50, 0, 0, 0)
        );

        // Draw container rectangle
        var r = new RectangleF(0, ItemSize.Height, Width - 1, Height - ItemSize.Height - 1);
        using (
            var path = r.Radius(
                SelectedIndex == 0 ? 2 : _borderRadius.Left,
                _borderRadius.Top,
                _borderRadius.Right,
                _borderRadius.Bottom
            )
        )
        {
            //graphics.FillPath(backBrush, path);
            graphics.DrawPath(borderBrush, path);
        }

        for (int i = 0; i <= TabCount - 1; i++)
        {
            var tabBounds = GetTabRect(i);
            var textRect = tabBounds;
            textRect.Offset(0, -2);

            if (i == SelectedIndex)
            {
                var rect = new RectangleF(tabBounds.X - 1, tabBounds.Y - 1, tabBounds.Width + 2, tabBounds.Height - 2);
                using var path = rect.Radius(4, 4, 0, 0);
                graphics.FillPath(backBrush, path);
            }

            TabPages[i].DrawString(graphics, ColorScheme.ForeColor, textRect);

            if (TabPages[i].BackColor != ColorScheme.BackColor)
                TabPages[i].BackColor = ColorScheme.BackColor;
        }

        graphics.SetDefaultQuality();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parms = base.CreateParams;
            // WS_EX_COMPOSITED for smoother rendering (reduces flicker)
            parms.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return parms;
        }
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND (0x14) to reduce flicker
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }
}
