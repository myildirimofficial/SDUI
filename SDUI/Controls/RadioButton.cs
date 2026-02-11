using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using SDUI.Animation;

namespace SDUI.Controls;

public class Radio : RadioButton
{
    private const int RADIOBUTTON_INNER_CIRCLE_SIZE = RADIOBUTTON_SIZE - (2 * RADIOBUTTON_OUTER_CIRCLE_WIDTH);

    private const int RADIOBUTTON_OUTER_CIRCLE_WIDTH = 1;

    // size constants
    private const int RADIOBUTTON_SIZE = 15;

    private const int RADIOBUTTON_SIZE_HALF = RADIOBUTTON_SIZE / 2;

    // animation managers
    private readonly Animation.AnimationEngine animationManager;

    private readonly Animation.AnimationEngine rippleAnimationManager;

    private int boxOffset;
    private int _mouseState;

    // size related variables which should be recalculated onsizechanged
    private Rectangle radioButtonBounds;

    private bool ripple;

    [Browsable(false)]
    private Point _mouseLocation { get; set; }

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

    public Radio()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint,
            true
        );

        animationManager = new Animation.AnimationEngine { AnimationType = AnimationType.EaseInOut, Increment = 0.06 };
        rippleAnimationManager = new Animation.AnimationEngine(false)
        {
            AnimationType = AnimationType.Linear,
            Increment = 0.10,
            SecondaryIncrement = 0.08,
        };
        animationManager.OnAnimationProgress += sender => Invalidate();
        rippleAnimationManager.OnAnimationProgress += sender => Invalidate();

        CheckedChanged += (sender, args) =>
            animationManager.StartNewAnimation(Checked ? AnimationDirection.In : AnimationDirection.Out);

        SizeChanged += OnSizeChanged;

        Ripple = true;
        _mouseLocation = new Point(-1, -1);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = boxOffset + 20 + (int)CreateGraphics().MeasureString(Text, Font).Width;
        return Ripple ? new Size(width, 30) : new Size(width, 20);
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
            _mouseLocation = new Point(-1, -1);
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
            _mouseLocation = args.Location;
            Cursor = IsMouseInCheckArea() ? Cursors.Hand : Cursors.Default;
        };
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.SystemDefault;

        RadioButtonRenderer.DrawParentBackground(pevent.Graphics, ClientRectangle, this);

        var RADIOBUTTON_CENTER = boxOffset + RADIOBUTTON_SIZE_HALF;

        var animationProgress = animationManager.GetProgress();

        var disabledOffColor = Color.LightGray;

        var colorAlpha = Enabled ? (int)(animationProgress * 255.0) : disabledOffColor.A;
        var backgroundAlpha = Enabled
            ? (int)(ColorScheme.BorderColor.A * (1.0 - animationProgress))
            : disabledOffColor.A;
        var animationSize = (float)(animationProgress * 8f);
        var animationSizeHalf = animationSize / 2;
        animationSize = (float)(animationProgress * 9f);

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
                var animationSource = new Point(RADIOBUTTON_CENTER, RADIOBUTTON_CENTER);

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

        using var ellipseBrush = new SolidBrush(ColorScheme.BorderColor);

        graphics.FillEllipse(ellipseBrush, boxOffset, boxOffset, RADIOBUTTON_SIZE, RADIOBUTTON_SIZE);

        using (
            var path = DrawingExtensions.CreateRoundPath(boxOffset, boxOffset, RADIOBUTTON_SIZE, RADIOBUTTON_SIZE, 7)
        )
        {
            // draw radiobutton circle
            var uncheckedColor = ColorScheme.BackColor.BlendWith(
                Enabled ? ColorScheme.BorderColor : disabledOffColor,
                backgroundAlpha
            );

            using var brush2 = new SolidBrush(uncheckedColor);
            //graphics.FillPath(brush2, path);

            graphics.FillEllipse(
                brush2,
                boxOffset,
                boxOffset,
                RADIOBUTTON_INNER_CIRCLE_SIZE,
                RADIOBUTTON_INNER_CIRCLE_SIZE
            );

            if (Enabled)
                graphics.FillEllipse(brush, boxOffset, boxOffset, RADIOBUTTON_SIZE, RADIOBUTTON_SIZE);

            //
            //    graphics.FillPath(brush, path);
        }

        if (Checked)
        {
            using (
                var path = DrawingExtensions.CreateRoundPath(
                    RADIOBUTTON_CENTER - animationSizeHalf,
                    RADIOBUTTON_CENTER - animationSizeHalf,
                    animationSize,
                    animationSize,
                    7
                )
            )
                graphics.FillPath(brush, path);
        }

        var textColor = Enabled ? ColorScheme.ForeColor : Color.Gray;

        this.DrawString(
            graphics,
            TextAlign,
            textColor,
            new RectangleF(new Point(boxOffset + RADIOBUTTON_SIZE, 0), ClientRectangle.Size)
        );
    }

    private bool IsMouseInCheckArea()
    {
        return radioButtonBounds.Contains(_mouseLocation);
    }

    private void OnSizeChanged(object sender, EventArgs eventArgs)
    {
        boxOffset = Height / 2 - (int)Math.Ceiling(RADIOBUTTON_SIZE / 2d);
        radioButtonBounds = new Rectangle(boxOffset, boxOffset, RADIOBUTTON_SIZE, RADIOBUTTON_SIZE);
    }
}
