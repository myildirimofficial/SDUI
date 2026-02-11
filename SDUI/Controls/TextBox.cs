using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls;

public class TextBox : Control
{
    public class InternalTextBox : System.Windows.Forms.TextBox
    {
        public InternalTextBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
    }

    private int _radius = 2;
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
    private InternalTextBox _textBox;

    private bool _passmask = false;
    public bool UseSystemPasswordChar
    {
        get { return _passmask; }
        set
        {
            if (_passmask == value)
                return;

            _textBox.UseSystemPasswordChar = value;
            _passmask = value;
            // No Invalidate needed - internal textbox handles display
        }
    }

    private bool _passFocusShow = false;
    public bool PassFocusShow
    {
        get { return _passFocusShow; }
        set
        {
            if (_passFocusShow == value)
                return;

            _passFocusShow = value;
            // No Invalidate needed - doesn't affect visual state until focus changes
        }
    }

    protected override void OnEnter(System.EventArgs e)
    {
        if (UseSystemPasswordChar && PassFocusShow)
            _textBox.UseSystemPasswordChar = false;
    }

    protected override void OnLeave(System.EventArgs e)
    {
        if (UseSystemPasswordChar && PassFocusShow)
            _textBox.UseSystemPasswordChar = UseSystemPasswordChar;
    }

    private int _maxchars = 32767;
    public int MaxLength
    {
        get { return _maxchars; }
        set
        {
            if (_maxchars == value)
                return;

            _maxchars = value;
            _textBox.MaxLength = value;
            // No Invalidate needed - internal textbox handles this
        }
    }

    private HorizontalAlignment _align;
    public HorizontalAlignment TextAlignment
    {
        get { return _align; }
        set
        {
            if (_align == value)
                return;

            _align = value;
            // No Invalidate needed - internal textbox handles text alignment
        }
    }

    private bool _multiline = false;
    public bool MultiLine
    {
        get { return _multiline; }
        set
        {
            if (_multiline == value)
                return;

            _multiline = value;
            // No Invalidate needed - internal textbox handles multiline
        }
    }

    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    protected override void OnBackColorChanged(System.EventArgs e)
    {
        base.OnBackColorChanged(e);
        _textBox.BackColor = Color.FromArgb(BackColor.R, BackColor.G, BackColor.B);
        // base.OnBackColorChanged already triggers repaint
    }

    protected override void OnForeColorChanged(System.EventArgs e)
    {
        base.OnForeColorChanged(e);
        _textBox.ForeColor = ForeColor;
        // base.OnForeColorChanged already triggers repaint
    }

    protected override void OnFontChanged(System.EventArgs e)
    {
        base.OnFontChanged(e);
        _textBox.Font = Font;
    }

    protected override void OnGotFocus(System.EventArgs e)
    {
        base.OnGotFocus(e);
        _textBox.Focus();
    }

    public TextBox()
    {
        _textBox = new InternalTextBox();
        _textBox.Multiline = false;
        _textBox.Text = string.Empty;
        _textBox.TextAlign = HorizontalAlignment.Center;
        _textBox.BorderStyle = BorderStyle.None;
        _textBox.BackColor = Color.Transparent;
        _textBox.Location = new Point(3, 2);
        _textBox.Font = Font;
        _textBox.Size = new Size(Width - 10, Height - 11);
        _textBox.UseSystemPasswordChar = UseSystemPasswordChar;
        _textBox.TextChanged += _textbox_TextChanged;
        _textBox.PreviewKeyDown += _textbox_PreviewKeyDown;
        Controls.Add(_textBox);

        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(135, 35);
        DoubleBuffered = true;
    }

    private void _textbox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        OnPreviewKeyDown(e);
    }

    private void _textbox_TextChanged(object sender, System.EventArgs e)
    {
        Text = _textBox.Text;
        OnTextChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ButtonRenderer.DrawParentBackground(e.Graphics, ClientRectangle, this);

        var color = Color.CornflowerBlue;

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        var determinedColor = ColorScheme.BackColor.Determine();
        var backColor = ColorScheme.BackColor.Brightness(-.1f);

        Height = _textBox.Height + 5;
        var textbox = _textBox;
        textbox.Width = Width - 10;
        textbox.TextAlign = TextAlignment;
        textbox.UseSystemPasswordChar = UseSystemPasswordChar;
        textbox.ForeColor = ColorScheme.ForeColor;
        textbox.BackColor = backColor;

        using var backBrush = new SolidBrush(backColor);
        graphics.FillPath(backBrush, new Rectangle(0, 0, Width - 1, Height - 1).Radius(_radius));

        var colorBegin = determinedColor.Brightness(.1f).Alpha(90);
        var colorEnd = determinedColor.Brightness(-.1f).Alpha(60);

        using var innerBorderBrush = new LinearGradientBrush(
            new Rectangle(1, 1, Width - 2, Height - 2),
            colorBegin,
            colorEnd,
            90
        );
        using var innerBorderPen = new Pen(innerBorderBrush);

        graphics.DrawPath(innerBorderPen, new Rectangle(1, 1, Width - _radius, Height - _radius).Radius(_radius));
        graphics.DrawLine(ColorScheme.BorderColor, new Point(1, 1), new Point(Width - 3, 1));
    }
}
