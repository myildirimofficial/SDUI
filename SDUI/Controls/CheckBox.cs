using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using SDUI.Animation;

namespace SDUI.Controls
{
    public class CheckBox : System.Windows.Forms.CheckBox
    {
        private const int CHECKBOX_SIZE = 16;

        private const int CHECKBOX_SIZE_HALF = CHECKBOX_SIZE / 2;

        private static readonly Point[] CHECKMARK_LINE = { new(1, 6), new(5, 10), new(12, 3) };

        private readonly Animation.AnimationEngine animationManager;

        private readonly Animation.AnimationEngine rippleAnimationManager;

        private int boxOffset;

        private RectangleF boxRectangle;

        private bool ripple;

        [Browsable(false)]
        public int Depth { get; set; }

        [Browsable(false)]
        public Point MouseLocation { get; set; }

        private int _mouseState { get; set; }

        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                base.AutoSize = value;
                if (value)
                {
                    Size = new Size(10, 10);
                }
            }
        }

        [Category("Behavior")]
        public bool Ripple
        {
            get { return ripple; }
            set
            {
                if (ripple == value)
                    return;

                ripple = value;
                AutoSize = AutoSize; //Make AutoSize directly set the bounds.

                if (value)
                {
                    Margin = new Padding(0);
                }

                Invalidate();
            }
        }

        public CheckBox()
        {
            //SetExtendedState(ExtendedStates.UserPreferredSizeCache, true);

            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.ResizeRedraw,
                true
            );

            SetStyle(ControlStyles.FixedHeight | ControlStyles.Selectable, false);
            
            DoubleBuffered = true;

            animationManager = new Animation.AnimationEngine
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.10,
            };
            rippleAnimationManager = new Animation.AnimationEngine(false)
            {
                AnimationType = AnimationType.Linear,
                Increment = 0.10,
                SecondaryIncrement = 0.07,
            };
            animationManager.OnAnimationProgress += sender => Invalidate();
            rippleAnimationManager.OnAnimationProgress += sender => Invalidate();

            CheckedChanged += (sender, args) =>
            {
                animationManager.StartNewAnimation(Checked ? AnimationDirection.In : AnimationDirection.Out);
            };

            Ripple = true;
            MouseLocation = new Point(-1, -1);
        }

        private Bitmap DrawCheckMarkBitmap()
        {
            var checkMark = new Bitmap(CHECKBOX_SIZE, CHECKBOX_SIZE);

            using (var g = Graphics.FromImage(checkMark))
            {
                // clear everything, transparent
                g.Clear(Color.Transparent);
                // draw the checkmark lines
                using var pen = new Pen(Enabled ? Color.White : Color.DarkGray, 2);
                g.DrawLines(pen, CHECKMARK_LINE);
            }

            return checkMark;
        }

        private bool IsMouseInCheckArea()
        {
            return boxRectangle.Contains(MouseLocation);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = boxOffset + CHECKBOX_SIZE + 2 + TextRenderer.MeasureText(Text, Font).Width;
            return Ripple ? new Size(w, 30) : new Size(w, 20);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (DesignMode)
                return;

            _mouseState = 0;
            MouseEnter += (sender, args) =>
            {
                _mouseState = 1;
            };
            MouseLeave += (sender, args) =>
            {
                MouseLocation = new Point(-1, -1);
                _mouseState = 0;
            };
            MouseDown += (sender, args) =>
            {
                _mouseState = 2;

                if (Ripple && args.Button == MouseButtons.Left && IsMouseInCheckArea())
                {
                    rippleAnimationManager.SecondaryIncrement = 0;
                    rippleAnimationManager.StartNewAnimation(AnimationDirection.InOutIn, new object[] { Checked });
                }
            };
            MouseUp += (sender, args) =>
            {
                _mouseState = 1;
                rippleAnimationManager.SecondaryIncrement = 0.08;
            };
            MouseMove += (sender, args) =>
            {
                MouseLocation = args.Location;
                Cursor = IsMouseInCheckArea() ? Cursors.Hand : Cursors.Default;
            };
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

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.SystemDefault;

            CheckBoxRenderer.DrawParentBackground(pevent.Graphics, ClientRectangle, this);

            var CHECKBOX_CENTER = boxOffset + CHECKBOX_SIZE_HALF - 1;

            double animationProgress = animationManager.GetProgress();

            var disabledOffColor = ColorScheme.BorderColor;

            int colorAlpha = Enabled ? (int)(animationProgress * 255.0) : disabledOffColor.A;
            int backgroundAlpha = Enabled
                ? (int)(ColorScheme.BorderColor.A * (1.0 - animationProgress))
                : disabledOffColor.A;

            using var brush = new SolidBrush(
                Color.FromArgb(colorAlpha, Enabled ? ColorScheme.AccentColor : disabledOffColor)
            );
            using var pen = new Pen(brush.Color);

            // draw ripple animation
            if (Ripple && rippleAnimationManager.IsAnimating())
            {
                for (int i = 0; i < rippleAnimationManager.GetAnimationCount(); i++)
                {
                    var animationValue = rippleAnimationManager.GetProgress(i);
                    var animationSource = new Point(CHECKBOX_CENTER, CHECKBOX_CENTER);
                    using var rippleBrush = new SolidBrush(
                        Color.FromArgb(
                            (int)((animationValue * 40)),
                            ((bool)rippleAnimationManager.GetData(i)[0]) ? Color.Black : brush.Color
                        )
                    );
                    var rippleHeight = (Height % 2 == 0) ? Height - 3 : Height - 2;
                    var rippleSize =
                        (rippleAnimationManager.GetDirection(i) == AnimationDirection.InOutIn)
                            ? (int)(rippleHeight * (0.8d + (0.2d * animationValue)))
                            : rippleHeight;

                    using var path = DrawingExtensions.CreateRoundPath(
                        animationSource.X - rippleSize / 2,
                        animationSource.Y - rippleSize / 2,
                        rippleSize,
                        rippleSize,
                        rippleSize / 2
                    );
                    graphics.FillPath(rippleBrush, path);
                }
            }

            var checkMarkLineFill = new Rectangle(boxOffset, boxOffset, (int)(14.0 * animationProgress), 14);
            using (var checkmarkPath = DrawingExtensions.CreateRoundPath(boxOffset, boxOffset, 14, 14, 2))
            {
                using var brush2 = new SolidBrush(
                    ColorScheme.BackColor.BlendWith(
                        Enabled ? ColorScheme.BorderColor : disabledOffColor,
                        backgroundAlpha
                    )
                );
                using var pen2 = new Pen(brush2.Color);

                graphics.FillPath(ColorScheme.BorderColor.Brush(), checkmarkPath);
                graphics.DrawPath(ColorScheme.BorderColor.Pen(), checkmarkPath);
                //graphics.DrawShadow(boxRectangle, 2, 1);

                graphics.FillPath(brush, checkmarkPath);
                graphics.DrawPath(pen, checkmarkPath);

                graphics.DrawImageUnscaledAndClipped(DrawCheckMarkBitmap(), checkMarkLineFill);
            }

            // draw checkbox text
            var textColor = Enabled ? ColorScheme.ForeColor : Color.Gray;

            this.DrawString(
                graphics,
                TextAlign,
                textColor,
                new RectangleF(new Point(boxOffset + CHECKBOX_SIZE, 0), ClientRectangle.Size)
            );

            if (ColorScheme.DrawDebugBorders)
            {
                using var redPen = new Pen(Color.Red, 1);
                redPen.Alignment = PenAlignment.Outset;
                graphics.DrawRectangle(redPen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            boxOffset = Height / 2 - 7;
            boxRectangle = new Rectangle(boxOffset, boxOffset, CHECKBOX_SIZE - 1, CHECKBOX_SIZE - 1);
        }
    }
}
