using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls
{
    public class TestTrackBar : Control
    {
        #region  Enums

        public enum ValueDivisor
        {
            By1 = 1,
            By10 = 10,
            By100 = 100,
            By1000 = 1000,
        }

        #endregion
        #region  Variables

        private GraphicsPath PipeBorder;
        private GraphicsPath FillValue;
        private Rectangle TrackBarHandleRect;
        private bool Cap;
        private int ValueDrawer;

        private Size ThumbSize = new Size(15, 15);
        private Rectangle TrackThumb;

        private int _Minimum = 0;
        private int _Maximum = 10;
        private int _Value = 0;

        private bool _DrawValueString = false;
        private bool _JumpToMouse = false;
        private ValueDivisor DividedValue = ValueDivisor.By1;

        #endregion
        #region  Properties

        public int Minimum
        {
            get { return _Minimum; }
            set
            {
                if (value >= _Maximum)
                {
                    value = _Maximum - 10;
                }
                if (_Value < value)
                {
                    _Value = value;
                }

                _Minimum = value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _Maximum; }
            set
            {
                if (value <= _Minimum)
                {
                    value = _Minimum + 10;
                }
                if (_Value > value)
                {
                    _Value = value;
                }

                _Maximum = value;
                Invalidate();
            }
        }

        public delegate void ValueChangedEventHandler();
        private ValueChangedEventHandler ValueChangedEvent;

        public event ValueChangedEventHandler ValueChanged
        {
            add { ValueChangedEvent = (ValueChangedEventHandler)System.Delegate.Combine(ValueChangedEvent, value); }
            remove { ValueChangedEvent = (ValueChangedEventHandler)System.Delegate.Remove(ValueChangedEvent, value); }
        }

        public int Value
        {
            get { return _Value; }
            set
            {
                if (_Value != value)
                {
                    if (value < _Minimum)
                    {
                        _Value = _Minimum;
                    }
                    else
                    {
                        if (value > _Maximum)
                        {
                            _Value = _Maximum;
                        }
                        else
                        {
                            _Value = value;
                        }
                    }
                    Invalidate();
                    if (ValueChangedEvent != null)
                        ValueChangedEvent();
                }
            }
        }

        public ValueDivisor ValueDivison
        {
            get { return DividedValue; }
            set
            {
                DividedValue = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public float ValueToSet
        {
            get { return _Value / (int)DividedValue; }
            set { Value = (int)(value * (int)DividedValue); }
        }

        public bool JumpToMouse
        {
            get { return _JumpToMouse; }
            set
            {
                _JumpToMouse = value;
                Invalidate();
            }
        }

        public bool DrawValueString
        {
            get { return _DrawValueString; }
            set
            {
                _DrawValueString = value;
                if (_DrawValueString == true)
                {
                    Height = 35;
                }
                else
                {
                    Height = 22;
                }
                Invalidate();
            }
        }

        #endregion
        #region  EventArgs

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            checked
            {
                bool flag = this.Cap && e.X > -1 && e.X < this.Width + 1;
                if (flag)
                {
                    this.Value =
                        this._Minimum
                        + (int)Math.Round((double)(this._Maximum - this._Minimum) * ((double)e.X / (double)this.Width));
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            bool flag = e.Button == MouseButtons.Left;
            checked
            {
                if (flag)
                {
                    this.ValueDrawer = (int)
                        Math.Round(
                            ((double)(this._Value - this._Minimum) / (double)(this._Maximum - this._Minimum))
                                * (double)(this.Width - 11)
                        );
                    this.TrackBarHandleRect = new Rectangle(this.ValueDrawer, 0, 25, 25);
                    this.Cap = this.TrackBarHandleRect.Contains(e.Location);
                    this.Focus();
                    flag = this._JumpToMouse;
                    if (flag)
                    {
                        this.Value =
                            this._Minimum
                            + (int)
                                Math.Round(
                                    (double)(this._Maximum - this._Minimum) * ((double)e.X / (double)this.Width)
                                );
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Cap = false;
        }

        #endregion

        public TestTrackBar()
        {
            SetStyle(
                ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.DoubleBuffer,
                true
            );

            Size = new Size(80, 22);
            MinimumSize = new Size(47, 22);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_DrawValueString == true)
            {
                Height = 35;
            }
            else
            {
                Height = 22;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ButtonRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);

            Graphics gfx = e.Graphics;

            //gfx.Clear(Parent.BackColor);
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            TrackThumb = new Rectangle(8, 10, Width - 16, 2);
            PipeBorder = DrawingExtensions.CreateRoundPath(1, 8, Width - 3, 5, 2);

            try
            {
                this.ValueDrawer = (int)
                    Math.Round(
                        ((double)(this._Value - this._Minimum) / (double)(this._Maximum - this._Minimum))
                            * (double)(this.Width - 11)
                    );
            }
            catch (Exception) { }

            TrackBarHandleRect = new Rectangle(ValueDrawer, 0, 10, 20);

            gfx.SetClip(PipeBorder); // Set the clipping region of this Graphics to the specified GraphicsPath
            gfx.FillPath(new SolidBrush(SDUI.ColorScheme.BackColor), PipeBorder);
            FillValue = DrawingExtensions.CreateRoundPath(
                1,
                8,
                TrackBarHandleRect.X + TrackBarHandleRect.Width - 4,
                5,
                2
            );

            gfx.ResetClip(); // Reset the clip region of this Graphics to an infinite region

            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.DrawPath(new Pen(SDUI.ColorScheme.BorderColor), PipeBorder); // Draw pipe border
            gfx.FillPath(Color.Blue.Brush(), FillValue);

            gfx.FillEllipse(
                new SolidBrush(SDUI.ColorScheme.BackColor),
                this.TrackThumb.X
                    + (int)
                        Math.Round(
                            unchecked((double)this.TrackThumb.Width * ((double)this.Value / (double)this.Maximum))
                        )
                    - (int)Math.Round((double)this.ThumbSize.Width / 2.0),
                this.TrackThumb.Y
                    + (int)Math.Round((double)this.TrackThumb.Height / 2.0)
                    - (int)Math.Round((double)this.ThumbSize.Height / 2.0),
                this.ThumbSize.Width,
                this.ThumbSize.Height
            );
            gfx.DrawEllipse(
                new Pen(Color.FromArgb(180, 180, 180)),
                this.TrackThumb.X
                    + (int)
                        Math.Round(
                            unchecked((double)this.TrackThumb.Width * ((double)this.Value / (double)this.Maximum))
                        )
                    - (int)Math.Round((double)this.ThumbSize.Width / 2.0),
                this.TrackThumb.Y
                    + (int)Math.Round((double)this.TrackThumb.Height / 2.0)
                    - (int)Math.Round((double)this.ThumbSize.Height / 2.0),
                this.ThumbSize.Width,
                this.ThumbSize.Height
            );

            if (_DrawValueString == true)
            {
                gfx.DrawString(System.Convert.ToString(ValueToSet), Font, Brushes.DimGray, 1, 20);
            }
        }
    }
}
