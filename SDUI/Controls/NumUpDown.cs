using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Policy;
using System.Windows.Forms;
using SDUI;

namespace SDUI.Controls
{
    public class NumUpDown : Control
    {
        private const int LONG_PRESS_TIMER_INTERVAL = 250;

        public event EventHandler ValueChanged;

        private decimal _value;
        private decimal _min;
        private decimal _max;
        private Point _mouseLocation;
        private bool _isUsingKeyboard;
        private Timer _longPressTimer = new();

        private RectangleF _upButtonRect;
        private RectangleF _downButtonRect;

        private const int SIZE = 20;
        private float _dpi => DeviceDpi / 96f;

        public decimal Value
        {
            get { return _value; }
            set
            {
                if (value > _max || value < _min)
                    return;

                if (_value == value)
                    return;

                _value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public decimal Minimum
        {
            get { return _min; }
            set
            {
                if (_min == value || value >= _max)
                    return;

                _min = value;

                if (_value < _min)
                    Value = _min;
                else
                    Invalidate();
            }
        }

        public decimal Maximum
        {
            get { return _max; }
            set
            {
                if (_max == value || value <= _min)
                    return;

                _max = value;

                if (_value > _max)
                    Value = _max;
                else
                    Invalidate();
            }
        }

        public override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = Color.Transparent;
        }

        public NumUpDown()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.DoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.UserPaint,
                true
            );

            UpdateStyles();
            this.DoubleBuffered = true;

            BackColor = Color.Transparent;
            _min = 0;
            _max = 100;
            Size = new Size(80, 25);
            MinimumSize = Size;

            _longPressTimer.Tick += LongPressTimer_Tick;
            _longPressTimer.Interval = LONG_PRESS_TIMER_INTERVAL;

            _upButtonRect = new(Width - SIZE * _dpi, 0, SIZE * _dpi, Height);
            _downButtonRect = new(Width - SIZE * _dpi * 2, 0, SIZE * _dpi, Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mouseLocation = e.Location;
        }

        private void ClickButton()
        {
            if (_mouseLocation.InRect(_upButtonRect))
            {
                if (_value + 1 <= _max)
                    Value++;
            }
            else
            {
                if (_mouseLocation.InRect(_downButtonRect))
                {
                    if (_value - 1 >= _min)
                        Value--;
                }
                _isUsingKeyboard = !_isUsingKeyboard;
            }
            Focus();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            ClickButton();
            _longPressTimer.Start();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _longPressTimer.Stop();
            _longPressTimer.Interval = LONG_PRESS_TIMER_INTERVAL;
        }

        private void LongPressTimer_Tick(object sender, EventArgs e)
        {
            ClickButton();

            if (_longPressTimer.Interval == LONG_PRESS_TIMER_INTERVAL)
                _longPressTimer.Interval = 50;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            try
            {
                if (_isUsingKeyboard == true && _value < _max)
                    Value = long.Parse(_value.ToString() + e.KeyChar.ToString());
            }
            catch (Exception) { }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Back)
            {
                var tempVal = _value.ToString();
                tempVal = tempVal.Remove(Convert.ToInt32(tempVal.Length - 1));
                if (tempVal.Length == 0)
                    tempVal = "0";

                Value = Convert.ToInt32(tempVal);
            }
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
            {
                if (_value + 1 <= _max)
                {
                    Value++;
                }
                Invalidate();
            }
            else
            {
                if (_value - 1 >= _min)
                {
                    Value--;
                }
                Invalidate();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            _upButtonRect = new(Width - SIZE * _dpi, 0, SIZE * _dpi, Height);
            _downButtonRect = new(Width - SIZE * _dpi * 2, 0, SIZE * _dpi, Height);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);

            _upButtonRect = new(Width - SIZE * _dpi, 0, SIZE * _dpi, Height);
            _downButtonRect = new(Width - SIZE * _dpi * 2, 0, SIZE * _dpi, Height);
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
            //base.OnPaint(e);
            var graphics = e.Graphics;
            ButtonRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);

            using var borderPen = new Pen(ColorScheme.BorderColor);
            using var backColorBrush = ColorScheme.BackColor.Alpha(90).Brush();

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var round = ClientRectangle.Radius(8);

            graphics.FillPath(backColorBrush, round);
            graphics.DrawPath(borderPen, round);

            this.DrawString(graphics, "▲", ColorScheme.ForeColor, _upButtonRect);
            this.DrawString(graphics, "▼", ColorScheme.ForeColor, _downButtonRect);

            graphics.DrawLine(borderPen, _upButtonRect.X, 0, _upButtonRect.X, _upButtonRect.Height);
            graphics.DrawLine(borderPen, _downButtonRect.X, 0, _downButtonRect.X, _downButtonRect.Height);

            TextRenderer.DrawText(
                graphics,
                Value.ToString(),
                Font,
                new Rectangle(Padding.Left, 0, Width - 1, Height - 1),
                ColorScheme.ForeColor,
                TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.VerticalCenter | TextFormatFlags.Left
            );
        }
    }
}
