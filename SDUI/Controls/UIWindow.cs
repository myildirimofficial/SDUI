using SDUI.Animation;
using SDUI.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SDUI.Controls;

public class UIWindow : UIWindowBase
{
    public enum TabDesingMode
    {
        Rectangle,
        Rounded,
        Chromed,
    }

    /// <summary>
    /// If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnFormMenuClick;

    /// <summary>
    /// If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnExtendBoxClick;

    /// <summary>
    /// If extend box clicked invoke the event
    /// </summary>
    public event EventHandler<int> OnCloseTabBoxClick;

    /// <summary>
    /// If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnNewTabBoxClick;

    /// <summary>
    /// Is form active <c>true</c>; otherwise <c>false</c>
    /// </summary>
    private bool _isActive;

    /// <summary>
    /// If the mouse down <c>true</c>; otherwise <c>false</c>
    /// </summary>
    private bool _formMoveMouseDown;

    /// <summary>
    /// Determines whether the user is dragging the form
    /// </summary>
    private bool _isDragging;

    /// <summary>
    /// The starting point of the drag
    /// </summary>
    private Point _dragStartPoint;

    /// <summary>
    /// The position of the form when the left mouse button is pressed
    /// </summary>
    private Point _location;

    /// <summary>
    /// The position of the mouse when the left mouse button is pressed
    /// </summary>
    private Point _mouseOffset;

    /// <summary>
    /// The rectangle of control box
    /// </summary>
    private RectangleF _controlBoxRect;

    /// <summary>
    /// The rectangle of maximize box
    /// </summary>
    private RectangleF _maximizeBoxRect;

    /// <summary>
    /// The rectangle of minimize box
    /// </summary>
    private RectangleF _minimizeBoxRect;

    /// <summary>
    /// The rectangle of extend box
    /// </summary>
    private RectangleF _extendBoxRect;

    /// <summary>
    /// The rectangle of extend box
    /// </summary>
    private RectangleF _closeTabBoxRect;

    /// <summary>
    /// The rectangle of extend box
    /// </summary>
    private RectangleF _newTabBoxRect;

    /// <summary>
    /// The rectangle of extend box
    /// </summary>
    private RectangleF _formMenuRect;

    /// <summary>
    /// The control box left value
    /// </summary>
    private float _controlBoxLeft;

    /// <summary>
    /// The size of the window before it is maximized
    /// </summary>
    private Size _sizeOfBeforeMaximized;

    /// <summary>
    /// The position of the window before it is maximized
    /// </summary>
    private Point _locationOfBeforeMaximized;

    private float _titleHeightDPI => _titleHeight * DPI;
    private float _iconWidthDPI => _iconWidth * DPI;
    private float _symbolSizeDPI => _symbolSize * DPI;

    private float _iconWidth = 42;

    [DefaultValue(42)]
    [Description("Gets or sets the header bar icon width")]
    public float IconWidth
    {
        get => _iconWidth;
        set
        {
            _iconWidth = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    [DefaultValue(true)]
    [Description("Gets or sets form can movable")]
    public bool Movable { get; set; } = true;

    [DefaultValue(false)]
    public bool AllowAddControlOnTitle { get; set; }

    private bool _extendBox;

    [DefaultValue(false)]
    public bool ExtendBox
    {
        get => _extendBox;
        set
        {
            _extendBox = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    private bool _drawTabIcons;

    [DefaultValue(false)]
    public bool DrawTabIcons
    {
        get => _drawTabIcons;
        set
        {
            _drawTabIcons = value;
            Invalidate();
        }
    }

    private bool _tabCloseButton;

    [DefaultValue(false)]
    public bool TabCloseButton
    {
        get => _tabCloseButton;
        set
        {
            _tabCloseButton = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    private bool _newTabButton;

    [DefaultValue(false)]
    public bool NewTabButton
    {
        get => _newTabButton;
        set
        {
            _newTabButton = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    private float _symbolSize = 24;

    [DefaultValue(24)]
    public float ExtendSymbolSize
    {
        get => _symbolSize;
        set
        {
            _symbolSize = Math.Max(value, 16);
            _symbolSize = Math.Min(value, 128);
            Invalidate();
        }
    }

    [DefaultValue(null)]
    public ContextMenuStrip ExtendMenu { get; set; }

    [DefaultValue(null)]
    public ContextMenuStrip FormMenu { get; set; }

    /*
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new FormBorderStyle FormBorderStyle
    {
        get
        {
            return base.FormBorderStyle;
        }
        set
        {
            if (!Enum.IsDefined(typeof(FormBorderStyle), value))
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(FormBorderStyle));
            base.FormBorderStyle = FormBorderStyle.Sizable;
        }
    }*/

    /// <summary>
    /// Whether to show the title bar of the form
    /// </summary>
    private bool showTitle = true;

    /// <summary>
    /// Gets or sets whether to show the title bar of the form
    /// </summary>
    public bool ShowTitle
    {
        get => showTitle;
        set
        {
            showTitle = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    /// <summary>
    /// Whether to show the title bar of the form
    /// </summary>
    private bool showMenuInsteadOfIcon = false;

    /// <summary>
    /// Gets or sets whether to show the title bar of the form
    /// </summary>
    public bool ShowMenuInsteadOfIcon
    {
        get => showMenuInsteadOfIcon;
        set
        {
            showMenuInsteadOfIcon = value;
            CalcSystemBoxPos();
            Invalidate();
        }
    }

    /// <summary>
    /// Whether to show the title bar of the form
    /// </summary>
    private bool _drawTitleBorder = true;

    /// <summary>
    /// Gets or sets whether to show the title bar of the form
    /// </summary>
    public bool DrawTitleBorder
    {
        get => _drawTitleBorder;
        set
        {
            _drawTitleBorder = value;
            Invalidate();
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        InvalidateMetrics();
        base.OnDpiChanged(e);
        Invalidate();
    }

    /// <summary>
    /// Whether to display the control buttons of the form
    /// </summary>
    private bool controlBox = true;

    /// <summary>
    /// Gets or sets whether to display the control buttons of the form
    /// </summary>
    /*public new bool ControlBox
    {
        get => controlBox;
        set
        {
            controlBox = value;
            if (!controlBox)
            {
                MinimizeBox = MaximizeBox = false;
            }

            CalcSystemBoxPos();
            Invalidate();
        }
    }*/

    /// <summary>
    /// Whether to show the maximize button of the form
    /// </summary>
    private bool _maximizeBox = true;

    /// <summary>
    /// Whether to show the maximize button of the form
    /// </summary>
    public new bool MaximizeBox
    {
        get => _maximizeBox;
        set
        {
            _maximizeBox = value;

            if (value)
                _minimizeBox = true;

            CalcSystemBoxPos();
            Invalidate();
        }
    }

    /// <summary>
    /// Whether to show the minimize button of the form
    /// </summary>
    private bool _minimizeBox = true;

    /// <summary>
    /// Whether to show the minimize button of the form
    /// </summary>
    public new bool MinimizeBox
    {
        get => _minimizeBox;
        set
        {
            _minimizeBox = value;

            if (!value)
                _maximizeBox = false;

            CalcSystemBoxPos();
            Invalidate();
        }
    }

    /// <summary>
    /// The title height
    /// </summary>
    private float _titleHeight = 32;

    /// <summary>
    /// Gets or sets the title height
    /// </summary>
    public float TitleHeight
    {
        get => _titleHeight;
        set
        {
            _titleHeight = Math.Max(value, 31);
            Invalidate();
            CalcSystemBoxPos();
        }
    }

    /// <summary>
    /// Gradient header colors
    /// </summary>
    private Color[] _gradient = new[] { Color.Transparent, Color.Transparent };
    public Color[] Gradient
    {
        get => _gradient;
        set
        {
            _gradient = value;
            Invalidate();
        }
    }

    /// <summary>
    /// The title color
    /// </summary>
    private Color titleColor;

    /// <summary>
    /// Gets or sets the title color
    /// </summary>
    [Description("Title color"), DefaultValue(typeof(Color), "224, 224, 224")]
    public Color TitleColor
    {
        get => titleColor;
        set
        {
            titleColor = value;
            Invalidate();
        }
    }

    /// <summary>
    /// The title color
    /// </summary>
    private Color borderColor = Color.Transparent;

    /// <summary>
    /// Gets or sets the title color
    /// </summary>
    [Description("Border Color"), DefaultValue(typeof(Color), "Transparent")]
    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;

            if (value != Color.Transparent)
                WindowsHelper.ApplyBorderColor(this.Handle, this.borderColor);

            Invalidate();
        }
    }

    /// <summary>
    /// Tab desing mode
    /// </summary>
    private TabDesingMode _tabDesingMode = TabDesingMode.Rectangle;
    public TabDesingMode TitleTabDesingMode
    {
        get => _tabDesingMode;
        set
        {
            if (_tabDesingMode == value)
                return;

            _tabDesingMode = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Draw hatch brush on form
    /// </summary>
    public bool FullDrawHatch { get; set; }

    private HatchStyle _hatch = HatchStyle.Percent80;
    public HatchStyle Hatch
    {
        get => _hatch;
        set
        {
            if (_hatch == value)
                return;

            _hatch = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Whether to trigger the stay event on the edge of the display
    /// </summary>
    private bool IsStayAtTopBorder;

    /// <summary>
    /// The time at which the display edge dwell event was triggered
    /// </summary>
    private long TopBorderStayTicks;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine minBoxHoverAnimationManager;

    /// <summary>
    /// Tab Area hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine pageAreaAnimationManager;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine maxBoxHoverAnimationManager;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine extendBoxHoverAnimationManager;

    /// <summary>
    /// Close tab hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine closeBoxHoverAnimationManager;

    /// <summary>
    /// new Tab hover animation manager
    /// </summary>
    private readonly Animation.AnimationEngine newTabHoverAnimationManager;

    /// <summary>
    /// tab area animation manager
    /// </summary>
    private readonly Animation.AnimationEngine tabCloseHoverAnimationManager;

    /// <summary>
    /// tab area animation manager
    /// </summary>
    private readonly Animation.AnimationEngine formMenuHoverAnimationManager;

    private int previousSelectedPageIndex;
    private Point animationSource;
    private List<RectangleF> pageRect;
    private const int TAB_HEADER_PADDING = 9;
    private const int TAB_INDICATOR_HEIGHT = 3;

    private long _stickyBorderTime = 5000000;

    [Description("Set or get the maximum time to stay at the edge of the display(ms)")]
    [DefaultValue(500)]
    public long StickyBorderTime
    {
        get => _stickyBorderTime / 10000;
        set => _stickyBorderTime = value * 10000;
    }

    private struct CachedMetrics
    {
        public float TitleHeightDPI;
        public float IconWidthDPI;
        public float SymbolSizeDPI;
        public bool IsMetricsValid;
    }

    private CachedMetrics _cachedMetrics;
    private bool _needsLayoutUpdate;

    // Cached rendering objects to prevent GDI leaks
    private GraphicsPath _cachedTabPath;
    private Rectangle _lastTabBounds;
    private Pen _cachedPen;
    private Brush _cachedBrush;

    private void InvalidateMetrics()
    {
        _cachedMetrics.IsMetricsValid = false;
        _needsLayoutUpdate = true;
        DisposeRenderingCache();
    }

    private void EnsureMetrics()
    {
        if (_cachedMetrics.IsMetricsValid)
            return;

        _cachedMetrics.TitleHeightDPI = _titleHeight * DPI;
        _cachedMetrics.IconWidthDPI = _iconWidth * DPI;
        _cachedMetrics.SymbolSizeDPI = _symbolSize * DPI;
        _cachedMetrics.IsMetricsValid = true;
    }

    private void DisposeRenderingCache()
    {
        _cachedTabPath?.Dispose();
        _cachedTabPath = null;
        _cachedPen?.Dispose();
        _cachedPen = null;
        _cachedBrush?.Dispose();
        _cachedBrush = null;
        _lastTabBounds = Rectangle.Empty;
    }

    /// <summary>
    /// The contructor
    /// </summary>
    public UIWindow()
        : base()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.DoubleBuffer
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.SupportsTransparentBackColor,
            true
        );

        UpdateStyles();

        enableFullDraggable = false;

        pageAreaAnimationManager = new() { AnimationType = AnimationType.EaseOut, Increment = 0.07 };

        minBoxHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };
        maxBoxHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };
        closeBoxHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };

        extendBoxHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };

        tabCloseHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };

        newTabHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };

        formMenuHoverAnimationManager = new() { Increment = 0.15, AnimationType = AnimationType.Linear };

        minBoxHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        maxBoxHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        closeBoxHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        extendBoxHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        tabCloseHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        newTabHoverAnimationManager.OnAnimationProgress += sender => Invalidate();
        pageAreaAnimationManager.OnAnimationProgress += sender => Invalidate();
        formMenuHoverAnimationManager.OnAnimationProgress += sender => Invalidate();

        //WindowsHelper.ApplyRoundCorner(this.Handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeRenderingCache();
        }
        base.Dispose(disposing);
    }

    private bool _inCloseBox,
        _inMaxBox,
        _inMinBox,
        _inExtendBox,
        _inTabCloseBox,
        _inNewTabBox,
        _inFormMenuBox;

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);

        if (ShowTitle && !AllowAddControlOnTitle && e.Control.Top < TitleHeight)
        {
            e.Control.Top = Padding.Top;
        }
    }

    private void CalcSystemBoxPos()
    {
        EnsureMetrics();

        _controlBoxLeft = Width;

        if (controlBox)
        {
            _controlBoxRect = new(Width - _cachedMetrics.IconWidthDPI, 0, _cachedMetrics.IconWidthDPI, _cachedMetrics.TitleHeightDPI);
            _controlBoxLeft = _controlBoxRect.Left - 2;

            if (MaximizeBox)
            {
                _maximizeBoxRect = new(
                    _controlBoxRect.Left - _cachedMetrics.IconWidthDPI,
                    _controlBoxRect.Top,
                    _cachedMetrics.IconWidthDPI,
                    _cachedMetrics.TitleHeightDPI
                );
                _controlBoxLeft = _maximizeBoxRect.Left - 2;
            }
            else
            {
                _maximizeBoxRect = new(Width + 1, Height + 1, 1, 1);
            }

            if (MinimizeBox)
            {
                _minimizeBoxRect = new(
                    MaximizeBox ? _maximizeBoxRect.Left - _cachedMetrics.IconWidthDPI - 2 : _controlBoxRect.Left - _cachedMetrics.IconWidthDPI - 2,
                    _controlBoxRect.Top,
                    _cachedMetrics.IconWidthDPI,
                    _cachedMetrics.TitleHeightDPI
                );
                _controlBoxLeft = _minimizeBoxRect.Left - 2;
            }
            else
            {
                _minimizeBoxRect = new Rectangle(Width + 1, Height + 1, 1, 1);
            }

            if (ExtendBox)
            {
                if (MinimizeBox)
                {
                    _extendBoxRect = new(
                        _minimizeBoxRect.Left - _cachedMetrics.IconWidthDPI - 2,
                        _controlBoxRect.Top,
                        _cachedMetrics.IconWidthDPI,
                        _cachedMetrics.TitleHeightDPI
                    );
                }
                else
                {
                    _extendBoxRect = new(
                        _controlBoxRect.Left - _cachedMetrics.IconWidthDPI - 2,
                        _controlBoxRect.Top,
                        _cachedMetrics.IconWidthDPI,
                        _cachedMetrics.TitleHeightDPI
                    );
                }
            }
        }
        else
        {
            _extendBoxRect =
                _maximizeBoxRect =
                _minimizeBoxRect =
                _controlBoxRect =
                    new Rectangle(Width + 1, Height + 1, 1, 1);
        }

        var titleIconSize = 24 * DPI;
        _formMenuRect = new(10, _cachedMetrics.TitleHeightDPI / 2 - (titleIconSize / 2), titleIconSize, titleIconSize);

        Padding = new Padding(Padding.Left, (int)(showTitle ? _cachedMetrics.TitleHeightDPI : 0), Padding.Right, Padding.Bottom);
        
        _needsLayoutUpdate = true;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (!ShowTitle)
            return;

        if (_isDragging)
            return;

        if (_inCloseBox)
        {
            _inCloseBox = false;
            Close();
        }

        if (_inMinBox)
        {
            _inMinBox = false;
            WindowState = FormWindowState.Minimized;
        }

        if (_inMaxBox)
        {
            _inMaxBox = false;
            ShowMaximize();
        }

        if (_inExtendBox)
        {
            _inExtendBox = false;
            if (ExtendMenu != null)
            {
                ExtendMenu.Show(this, Convert.ToInt32(_extendBoxRect.Left), Convert.ToInt32(_titleHeightDPI - 1));
            }
            else
            {
                OnExtendBoxClick?.Invoke(this, EventArgs.Empty);
            }
        }

        if (_inFormMenuBox)
        {
            _inFormMenuBox = false;
            if (FormMenu != null)
            {
                FormMenu.Show(this, Convert.ToInt32(_formMenuRect.Left), Convert.ToInt32(_titleHeightDPI - 1));
            }
            else
            {
                OnFormMenuClick?.Invoke(this, EventArgs.Empty);
            }
        }

        if (_inTabCloseBox)
        {
            _inTabCloseBox = false;

            OnCloseTabBoxClick?.Invoke(this, _windowPageControl.SelectedIndex);
        }

        if (_inNewTabBox)
        {
            _inNewTabBox = false;

            OnNewTabBoxClick?.Invoke(this, EventArgs.Empty);
        }

        if (pageRect == null)
            UpdateTabRects();

        for (int i = 0; i < pageRect.Count; i++)
        {
            if (pageRect[i].Contains(e.Location))
            {
                _windowPageControl.SelectedIndex = i;

                if (_tabCloseButton && e.Button == MouseButtons.Middle)
                    OnCloseTabBoxClick?.Invoke(null, i);

                break;
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_inCloseBox || _inMaxBox || _inMinBox || _inExtendBox || _inTabCloseBox || _inNewTabBox || _inFormMenuBox)
            return;

        if (!ShowTitle)
            return;

        if (e.Y > Padding.Top)
            return;

        if (e.Button == MouseButtons.Left && Movable)
        {
            _formMoveMouseDown = true;
            _dragStartPoint = e.Location;
            _location = Location;
            _mouseOffset = MousePosition;
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (!MaximizeBox)
            return;

        bool inCloseBox = e.Location.InRect(_controlBoxRect);
        bool inMaxBox = e.Location.InRect(_maximizeBoxRect);
        bool inMinBox = e.Location.InRect(_minimizeBoxRect);
        bool inExtendBox = e.Location.InRect(_extendBoxRect);
        bool inCloseTabBox = _tabCloseButton && e.Location.InRect(_closeTabBoxRect);
        bool inNewTabBox = _newTabButton && e.Location.InRect(_newTabBoxRect);
        bool inFormMenuBox = e.Location.InRect(_formMenuRect);

        if (inCloseBox || inMaxBox || inMinBox || inExtendBox || inCloseTabBox || inNewTabBox || inFormMenuBox)
            return;

        if (!ShowTitle)
            return;

        if (e.Y > Padding.Top)
            return;

        ShowMaximize();

        base.OnMouseDoubleClick(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (!IsDisposed && _formMoveMouseDown)
        {
            //int screenIndex = GetMouseInScreen(PointToScreen(e.Location));
            var screen = Screen.FromPoint(MousePosition);
            if (MousePosition.Y == screen.WorkingArea.Top && MaximizeBox)
            {
                ShowMaximize(true);
            }

            if (Top < screen.WorkingArea.Top)
            {
                Top = screen.WorkingArea.Top;
            }

            if (Top > screen.WorkingArea.Bottom - TitleHeight)
            {
                Top = Convert.ToInt32(screen.WorkingArea.Bottom - _titleHeightDPI);
            }
        }

        IsStayAtTopBorder = false;
        Cursor.Clip = new Rectangle();
        _formMoveMouseDown = false;
        _isDragging = false;

        animationSource = e.Location;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_formMoveMouseDown)
        {
            if (!_isDragging)
            {
                var dragRect = new Rectangle(_dragStartPoint, Size.Empty);
                dragRect.Inflate(SystemInformation.DragSize);
                if (!dragRect.Contains(e.Location))
                {
                    _isDragging = true;
                    PostDragForm(Handle);
                }
            }
        }
        else
        {
            bool inCloseBox = e.Location.InRect(_controlBoxRect);
            bool inMaxBox = e.Location.InRect(_maximizeBoxRect);
            bool inMinBox = e.Location.InRect(_minimizeBoxRect);
            bool inExtendBox = e.Location.InRect(_extendBoxRect);
            bool inCloseTabBox = _tabCloseButton && e.Location.InRect(_closeTabBoxRect);
            bool inNewTabBox = _newTabButton && e.Location.InRect(_newTabBoxRect);
            bool inFormMenuBox = e.Location.InRect(_formMenuRect);
            bool isChange = false;

            if (inCloseBox != _inCloseBox)
            {
                _inCloseBox = inCloseBox;
                isChange = true;

                if (inCloseBox)
                    closeBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    closeBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inMaxBox != _inMaxBox)
            {
                _inMaxBox = inMaxBox;
                isChange = true;
                if (inMaxBox)
                    maxBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    maxBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inMinBox != _inMinBox)
            {
                _inMinBox = inMinBox;
                isChange = true;
                if (inMinBox)
                    minBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    minBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inExtendBox != _inExtendBox)
            {
                _inExtendBox = inExtendBox;
                isChange = true;
                if (inExtendBox)
                    extendBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    extendBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inCloseTabBox != _inTabCloseBox)
            {
                _inTabCloseBox = inCloseTabBox;
                isChange = true;

                if (inCloseTabBox)
                    tabCloseHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    tabCloseHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inNewTabBox != _inNewTabBox)
            {
                _inNewTabBox = inNewTabBox;
                isChange = true;

                if (inNewTabBox)
                    newTabHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    newTabHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (inFormMenuBox != _inFormMenuBox)
            {
                _inFormMenuBox = inFormMenuBox;
                isChange = true;

                if (inFormMenuBox)
                    formMenuHoverAnimationManager.StartNewAnimation(AnimationDirection.In);
                else
                    formMenuHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
            }

            if (isChange)
                Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                m.Result = (IntPtr)1;
                return;
        }

        base.WndProc(ref m);

        if (m.Msg == NativeMethods.WM_EXITSIZEMOVE)
        {
            _formMoveMouseDown = false;
            _isDragging = false;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _inExtendBox = _inCloseBox = _inMaxBox = _inMinBox = _inTabCloseBox = _inNewTabBox = _inFormMenuBox = false;
        closeBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        minBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        maxBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        extendBoxHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        tabCloseHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        newTabHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);
        formMenuHoverAnimationManager.StartNewAnimation(AnimationDirection.Out);

        Invalidate();
    }

    private void ShowMaximize(bool IsOnMoving = false)
    {
        Screen screen = Screen.FromPoint(MousePosition);
        base.MaximumSize = screen.WorkingArea.Size;
        if (screen.Primary)
            MaximizedBounds = screen.WorkingArea;
        else
            MaximizedBounds = new Rectangle(0, 0, 0, 0);

        if (WindowState == FormWindowState.Normal)
        {
            _sizeOfBeforeMaximized = Size;
            _locationOfBeforeMaximized = IsOnMoving ? _location : Location;
            WindowState = FormWindowState.Maximized;
        }
        else if (WindowState == FormWindowState.Maximized)
        {
            if (_sizeOfBeforeMaximized.Width == 0 || _sizeOfBeforeMaximized.Height == 0)
            {
                int w = 800;
                if (MinimumSize.Width > 0)
                    w = MinimumSize.Width;
                int h = 600;
                if (MinimumSize.Height > 0)
                    h = MinimumSize.Height;
                _sizeOfBeforeMaximized = new Size(w, h);
            }

            Size = _sizeOfBeforeMaximized;
            if (_locationOfBeforeMaximized.X == 0 && _locationOfBeforeMaximized.Y == 0)
            {
                _locationOfBeforeMaximized = new Point(
                    screen.Bounds.Left + screen.Bounds.Width / 2 - _sizeOfBeforeMaximized.Width / 2,
                    screen.Bounds.Top + screen.Bounds.Height / 2 - _sizeOfBeforeMaximized.Height / 2
                );
            }

            Location = _locationOfBeforeMaximized;
            WindowState = FormWindowState.Normal;
        }

        Invalidate();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _isActive = true;
        Invalidate();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        _isActive = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0)
            return;

        EnsureMetrics(); // Cache DPI calculations

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.Low;

        var foreColor = ColorScheme.ForeColor;
        var hoverColor = ColorScheme.BorderColor;

        // Background rendering
        if (FullDrawHatch)
        {
            using var hatchBrush = new HatchBrush(_hatch, ColorScheme.BackColor, hoverColor);
            graphics.FillRectangle(hatchBrush, 0, 0, Width, Height);
        }
        else
        {
            using var backBrush = ColorScheme.BackColor.Brush();
            graphics.FillRectangle(backBrush, ClientRectangle);
        }

        if (!ShowTitle)
            return;

        graphics.SetHighQuality();

        // Title bar background
        if (titleColor != Color.Empty)
        {
            foreColor = titleColor.Determine();
            hoverColor = foreColor.Alpha(20);
            using var titleBrush = titleColor.Brush();
            graphics.FillRectangle(titleBrush, 0, 0, Width, _cachedMetrics.TitleHeightDPI);
        }
        else if (_gradient.Length == 2 && !(_gradient[0] == Color.Transparent && _gradient[1] == Color.Transparent))
        {
            using var brush = new LinearGradientBrush(
                new RectangleF(0, 0, Width, _cachedMetrics.TitleHeightDPI),
                _gradient[0],
                _gradient[1],
                45
            );
            graphics.FillRectangle(brush, 0, 0, Width, _cachedMetrics.TitleHeightDPI);

            foreColor = _gradient[0].Determine();
            hoverColor = foreColor.Alpha(20);
        }

        RenderControlBoxes(graphics, foreColor, hoverColor);
        RenderWindowTitleOrTabs(graphics, foreColor, hoverColor);
        RenderTitleBorder(graphics);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    private void RenderControlBoxes(Graphics graphics, Color foreColor, Color hoverColor)
    {
        if (!controlBox)
            return;

        var closeHoverColor = Color.FromArgb(222, 179, 30, 30);

        // Close Box
        if (_inCloseBox)
        {
            using var closeBrush = new SolidBrush(Color.FromArgb(
                (int)(closeBoxHoverAnimationManager.GetProgress() * closeHoverColor.A),
                closeHoverColor.RemoveAlpha()));
            graphics.FillRectangle(closeBrush, _controlBoxRect);
        }

        using (var closePen = new Pen(_inCloseBox ? Color.White : foreColor, 1.2f * DPI))
        {
            const int size = 5;
            graphics.DrawLine(
                closePen,
                _controlBoxRect.Left + _controlBoxRect.Width / 2 - (size * DPI),
                _controlBoxRect.Top + _controlBoxRect.Height / 2 - (size * DPI),
                _controlBoxRect.Left + _controlBoxRect.Width / 2 + (size * DPI),
                _controlBoxRect.Top + _controlBoxRect.Height / 2 + (size * DPI)
            );

            graphics.DrawLine(
                closePen,
                _controlBoxRect.Left + _controlBoxRect.Width / 2 - (size * DPI),
                _controlBoxRect.Top + _controlBoxRect.Height / 2 + (size * DPI),
                _controlBoxRect.Left + _controlBoxRect.Width / 2 + (size * DPI),
                _controlBoxRect.Top + _controlBoxRect.Height / 2 - (size * DPI)
            );
        }

        using var tempPen = new Pen(foreColor, 1.2f * DPI);
        tempPen.StartCap = LineCap.Round;
        tempPen.EndCap = LineCap.Round;

        graphics.SmoothingMode = SmoothingMode.None;

        // Maximize Box
        if (MaximizeBox)
        {
            if (_inMaxBox)
            {
                using var maxBrush = new SolidBrush(Color.FromArgb(
                    (int)(maxBoxHoverAnimationManager.GetProgress() * hoverColor.A),
                    hoverColor.RemoveAlpha()));
                graphics.FillRectangle(maxBrush, _maximizeBoxRect);
            }

            float size = 10.0f * DPI;
            float offset = 2.4f * DPI;

            float x = _maximizeBoxRect.Left + (_maximizeBoxRect.Width - size) / 2.0f;
            float y = _maximizeBoxRect.Top + (_maximizeBoxRect.Height - size) / 2.0f;

            if (WindowState != FormWindowState.Maximized)
            {
                graphics.DrawRectangle(tempPen, x, y, size, size);
            }
            else
            {
                var frontRect = new RectangleF(x - offset / 2, y + offset / 2, size - offset, size - offset);

                float rX = frontRect.X + offset;
                float rY = frontRect.Y - offset;
                float rW = frontRect.Width;
                float rH = frontRect.Height;

                graphics.DrawLine(tempPen, rX, rY, rX + rW, rY);
                graphics.DrawLine(tempPen, rX + rW, rY, rX + rW, rY + rH);
                graphics.DrawLine(tempPen, rX, rY, rX, frontRect.Y);
                graphics.DrawLine(tempPen, frontRect.X + frontRect.Width, rY + rH, rX + rW, rY + rH);

                graphics.DrawRectangle(tempPen, frontRect.X, frontRect.Y, frontRect.Width, frontRect.Height);
            }
        }

        // Minimize Box
        if (MinimizeBox)
        {
            if (_inMinBox)
            {
                using var minBrush = new SolidBrush(Color.FromArgb(
                    (int)(minBoxHoverAnimationManager.GetProgress() * hoverColor.A),
                    hoverColor.RemoveAlpha()));
                graphics.FillRectangle(minBrush, _minimizeBoxRect);
            }

            graphics.DrawLine(
                tempPen,
                _minimizeBoxRect.Left + _minimizeBoxRect.Width / 2 - (7 * DPI),
                _minimizeBoxRect.Top + _minimizeBoxRect.Height / 2,
                _minimizeBoxRect.Left + _minimizeBoxRect.Width / 2 + (6 * DPI),
                _minimizeBoxRect.Top + _minimizeBoxRect.Height / 2
            );
        }

        graphics.SetHighQuality();

        // Extend Box
        if (ExtendBox)
        {
            if (_inExtendBox)
            {
                var hoverSize = 24 * DPI;
                using var brush = new SolidBrush(Color.FromArgb(
                    (int)(extendBoxHoverAnimationManager.GetProgress() * hoverColor.A),
                    hoverColor.RemoveAlpha()));
                using var path = new RectangleF(
                    _extendBoxRect.X + 18 * DPI,
                    (_cachedMetrics.TitleHeightDPI / 2) - (hoverSize / 2),
                    hoverSize,
                    hoverSize
                ).Radius(15);
                graphics.FillPath(brush, path);
            }

            var size = 5f * DPI;
            var centerX = _extendBoxRect.Left - size / 2 + _extendBoxRect.Width / 2;
            var centerY = _extendBoxRect.Top + _extendBoxRect.Height / 2;

            graphics.DrawLine(tempPen, centerX + (size * 2) - size, centerY - size / 2, centerX + (size * 2), centerY + size / 2);
            graphics.DrawLine(tempPen, centerX + (size * 2), centerY + size / 2, centerX + (size * 2) + size, centerY - size / 2);
        }
    }

    private void RenderWindowTitleOrTabs(Graphics graphics, Color foreColor, Color hoverColor)
    {
        // Form Menu/Icon
        var faviconSize = 16 * DPI;
        if (showMenuInsteadOfIcon)
        {
            using var brush = new SolidBrush(Color.FromArgb(
                (int)(formMenuHoverAnimationManager.GetProgress() * hoverColor.A),
                hoverColor.RemoveAlpha()));
            using var path = _formMenuRect.Radius(10);
            graphics.FillPath(brush, path);

            using var pen = foreColor.Pen();
            graphics.DrawLine(pen,
                _formMenuRect.Left + _formMenuRect.Width / 2 - (5 * DPI) - 1,
                _formMenuRect.Top + _formMenuRect.Height / 2 - (2 * DPI),
                _formMenuRect.Left + _formMenuRect.Width / 2 - (1 * DPI),
                _formMenuRect.Top + _formMenuRect.Height / 2 + (3 * DPI));

            graphics.DrawLine(pen,
                _formMenuRect.Left + _formMenuRect.Width / 2 + (5 * DPI) - 1,
                _formMenuRect.Top + _formMenuRect.Height / 2 - (2 * DPI),
                _formMenuRect.Left + _formMenuRect.Width / 2 - (1 * DPI),
                _formMenuRect.Top + _formMenuRect.Height / 2 + (3 * DPI));
        }
        else
        {
            if (ShowIcon && Icon != null)
            {
                using var iconBitmap = Icon.ToBitmap();
                graphics.DrawImage(iconBitmap, 10,
                    (_cachedMetrics.TitleHeightDPI / 2) - (faviconSize / 2),
                    faviconSize, faviconSize);
            }
        }

        // Window Title or Tabs
        if (_windowPageControl == null || _windowPageControl.Count == 0)
        {
            var textPoint = new PointF(
                (showMenuInsteadOfIcon ? _formMenuRect.X + _formMenuRect.Width : faviconSize + 14),
                (_cachedMetrics.TitleHeightDPI / 2 - Font.Height / 2));

            using var textBrush = foreColor.Brush();
            graphics.DrawString(Text, Font, textBrush, textPoint, StringFormat.GenericDefault);
        }
        else
        {
            RenderTabs(graphics, foreColor, hoverColor);
        }
    }

    private void RenderTabs(Graphics graphics, Color foreColor, Color hoverColor)
    {
        if (_needsLayoutUpdate || pageRect == null || pageRect.Count != _windowPageControl.Count)
        {
            UpdateTabRects();
            _needsLayoutUpdate = false;
        }

        var animationProgress = pageAreaAnimationManager.GetProgress();

        // Click feedback ripple
        if (pageAreaAnimationManager.IsAnimating())
        {
            using var rippleBrush = new SolidBrush(Color.FromArgb((int)(51 - (animationProgress * 50)), foreColor));
            var rippleSize = (int)(animationProgress * pageRect[_windowPageControl.SelectedIndex].Width * 1.75);

            graphics.SetClip(pageRect[_windowPageControl.SelectedIndex]);
            graphics.FillEllipse(
                rippleBrush,
                new Rectangle(
                    animationSource.X - rippleSize / 2,
                    animationSource.Y - rippleSize / 2,
                    rippleSize,
                    rippleSize
                )
            );
            graphics.ResetClip();
        }

        // Safety check
        if (_windowPageControl.SelectedIndex < 0 || _windowPageControl.SelectedIndex >= _windowPageControl.Count)
            return;

        // Animate page indicator
        if (previousSelectedPageIndex == pageRect.Count)
            previousSelectedPageIndex = -1;

        var previousSelectedPageIndexIfHasOne =
            previousSelectedPageIndex == -1 ? _windowPageControl.SelectedIndex : previousSelectedPageIndex;
        var previousActivePageRect = pageRect[previousSelectedPageIndexIfHasOne];
        var activePageRect = pageRect[_windowPageControl.SelectedIndex];

        var x = previousActivePageRect.X + (int)((activePageRect.X - previousActivePageRect.X) * animationProgress);
        var width = previousActivePageRect.Width + (int)((activePageRect.Width - previousActivePageRect.Width) * animationProgress);

        // Draw tab indicator based on design mode
        if (_tabDesingMode == TabDesingMode.Rectangle)
        {
            using var hoverBrush = hoverColor.Brush();
            graphics.DrawRectangle(hoverColor, activePageRect.X, 0, width, _cachedMetrics.TitleHeightDPI);
            graphics.FillRectangle(hoverBrush, x, 0, width, _cachedMetrics.TitleHeightDPI);
            
            using var indicatorBrush = Color.DodgerBlue.Brush();
            graphics.FillRectangle(indicatorBrush, x, _cachedMetrics.TitleHeightDPI - TAB_INDICATOR_HEIGHT, width, TAB_INDICATOR_HEIGHT);
        }
        else if (_tabDesingMode == TabDesingMode.Rounded)
        {
            if (titleColor != Color.Empty && !titleColor.IsDark())
                hoverColor = ForeColor.Alpha(60);

            using var hoverBrush = hoverColor.Brush();
            var tabRect = new RectangleF(x, 6, width, _cachedMetrics.TitleHeightDPI);
            var radius = 9 * DPI;
            
            using var path = tabRect.Radius(radius, radius, 0, 0);
            graphics.FillPath(hoverBrush, path);
        }
        else // Chromed
        {
            if (titleColor != Color.Empty && !titleColor.IsDark())
                hoverColor = ForeColor.Alpha(60);

            using var hoverBrush = hoverColor.Brush();
            var tabRect = new RectangleF(x, 5, width, _cachedMetrics.TitleHeightDPI - 7);
            
            using var path = tabRect.ChromePath(12);
            graphics.FillPath(hoverBrush, path);
        }

        // Draw tab headers
        using var foreBrush = foreColor.Brush();
        
        foreach (Control page in _windowPageControl.Controls)
        {
            var currentTabIndex = _windowPageControl.Controls.IndexOf(page);
            var rect = pageRect[currentTabIndex];

            if (_drawTabIcons)
            {
                var iconMeasure = graphics.MeasureString("", Font);
                var iconX = rect.X + (TAB_HEADER_PADDING * DPI);
                var inlinePaddingX = iconMeasure.Width + (TAB_HEADER_PADDING * DPI);
                
                rect.X += inlinePaddingX;
                rect.Width -= inlinePaddingX + (24 * DPI);

                graphics.DrawString("", Font, foreBrush,
                    new RectangleF(iconX, _cachedMetrics.TitleHeightDPI / 2 - iconMeasure.Height / 2,
                        iconMeasure.Width, iconMeasure.Height));

                using var format = new StringFormat()
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap,
                };

                graphics.DrawString(page.Text, Font, foreBrush, rect, format);
            }
            else
            {
                page.DrawString(graphics, foreColor, rect);
            }
        }

        // Tab Close Button
        if (_tabCloseButton)
        {
            var size = 20 * DPI;
            using var brush = new SolidBrush(
                Color.FromArgb(
                    (int)(tabCloseHoverAnimationManager.GetProgress() * hoverColor.A),
                    hoverColor.RemoveAlpha()
                )
            );

            _closeTabBoxRect = new(
                x + width - TAB_HEADER_PADDING / 2 - size,
                _cachedMetrics.TitleHeightDPI / 2 - size / 2,
                size, size
            );
            
            graphics.FillPie(brush, _closeTabBoxRect.X, _closeTabBoxRect.Y,
                _closeTabBoxRect.Width, _closeTabBoxRect.Height, 0, 360);

            using var linePen = new Pen(foreColor) { Width = 1.6f * DPI };
            var lineSize = 4f * DPI;

            graphics.DrawLine(linePen,
                _closeTabBoxRect.Left + _closeTabBoxRect.Width / 2 - lineSize,
                _closeTabBoxRect.Top + _closeTabBoxRect.Height / 2 - lineSize,
                _closeTabBoxRect.Left + _closeTabBoxRect.Width / 2 + lineSize,
                _closeTabBoxRect.Top + _closeTabBoxRect.Height / 2 + lineSize);

            graphics.DrawLine(linePen,
                _closeTabBoxRect.Left + _closeTabBoxRect.Width / 2 - lineSize,
                _closeTabBoxRect.Top + _closeTabBoxRect.Height / 2 + lineSize,
                _closeTabBoxRect.Left + _closeTabBoxRect.Width / 2 + lineSize,
                _closeTabBoxRect.Top + _closeTabBoxRect.Height / 2 - lineSize);
        }

        // New Tab Button
        if (_newTabButton)
        {
            var size = 24 * DPI;
            var newHoverColor = hoverColor.Alpha(30);
            
            using var brush = new SolidBrush(Color.FromArgb(
                (int)(newTabHoverAnimationManager.GetProgress() * newHoverColor.A),
                newHoverColor.RemoveAlpha()));
            
            var lastTabRect = pageRect[pageRect.Count - 1];
            _newTabBoxRect = new(
                lastTabRect.X + lastTabRect.Width + size / 2,
                _cachedMetrics.TitleHeightDPI / 2 - size / 2,
                size, size
            );

            using var path = _newTabBoxRect.Radius(4);
            graphics.FillPath(brush, path);

            using var linePen = new Pen(foreColor.Alpha(220)) { Width = 1.6f * DPI };
            var lineSize = 6 * DPI;

            graphics.DrawLine(linePen,
                _newTabBoxRect.Left + _newTabBoxRect.Width / 2 - lineSize,
                _newTabBoxRect.Top + _newTabBoxRect.Height / 2,
                _newTabBoxRect.Left + _newTabBoxRect.Width / 2 + lineSize,
                _newTabBoxRect.Top + _newTabBoxRect.Height / 2);

            graphics.DrawLine(linePen,
                _newTabBoxRect.Left + _newTabBoxRect.Width / 2,
                _newTabBoxRect.Top + _newTabBoxRect.Height / 2 - lineSize,
                _newTabBoxRect.Left + _newTabBoxRect.Width / 2,
                _newTabBoxRect.Top + _newTabBoxRect.Height / 2 + lineSize);
        }
    }

    private void RenderTitleBorder(Graphics graphics)
    {
        if (!_drawTitleBorder)
            return;

        if (titleColor != Color.Empty)
        {
            using var pen = TitleColor.Determine().Alpha(30).Pen();
            graphics.DrawLine(pen, Width, _cachedMetrics.TitleHeightDPI - 1, 0, _cachedMetrics.TitleHeightDPI - 1);
        }
        else
        {
            using var pen = ColorScheme.BorderColor.Pen();
            graphics.DrawLine(pen, Width, _cachedMetrics.TitleHeightDPI - 1, 0, _cachedMetrics.TitleHeightDPI - 1);
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        InvalidateMetrics();
        base.OnSizeChanged(e);
        CalcSystemBoxPos();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        CalcSystemBoxPos();
    }

    protected void AddMousePressMove(params Control[] cs)
    {
        foreach (Control ctrl in cs)
        {
            if (ctrl != null && !ctrl.IsDisposed)
            {
                ctrl.MouseDown += CtrlMouseDown;
            }
        }
    }

    /// <summary>
    /// Handles the MouseDown event of the c control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
    private void CtrlMouseDown(object sender, MouseEventArgs e)
    {
        if (WindowState == FormWindowState.Maximized)
            return;

        if (sender == this)
        {
            if (e.Y <= _titleHeightDPI && e.X < _controlBoxLeft)
            {
                DragForm(Handle);
            }
        }
        else
        {
            DragForm(Handle);
        }
    }

    private WindowPageControl _windowPageControl;

    public WindowPageControl WindowPageControl
    {
        get => _windowPageControl;
        set
        {
            _windowPageControl = value;
            if (_windowPageControl == null)
                return;

            previousSelectedPageIndex = _windowPageControl.SelectedIndex;

            _windowPageControl.SelectedIndexChanged += (sender, previousIndex) =>
            {
                previousSelectedPageIndex = previousIndex;
                pageAreaAnimationManager.SetProgress(0);
                pageAreaAnimationManager.StartNewAnimation(AnimationDirection.In);
            };
            _windowPageControl.ControlAdded += delegate
            {
                Invalidate();
            };
            _windowPageControl.ControlRemoved += delegate
            {
                Invalidate();
            };
        }
    }

    private void UpdateTabRects()
    {
        if (pageRect == null)
            pageRect = new();
        else
            pageRect.Clear();

        //If there isn't a base tab control, the rects shouldn't be calculated
        //If there aren't tab pages in the base tab control, the list should just be empty which has been set already; exit the void
        if (_windowPageControl == null || _windowPageControl.Count == 0)
            return;

        //Calculate the bounds of each tab header specified in the base tab control

        float tabAreaWidth = 44;

        if (controlBox)
            tabAreaWidth += _controlBoxRect.Width;

        if (MinimizeBox)
            tabAreaWidth += _minimizeBoxRect.Width;

        if (MaximizeBox)
            tabAreaWidth += _maximizeBoxRect.Width;

        if (ExtendBox)
            tabAreaWidth += _extendBoxRect.Width;

        float maxSize = 200f * DPI;

        tabAreaWidth = (Width - tabAreaWidth - 30) / _windowPageControl.Count;
        if (tabAreaWidth > maxSize)
            tabAreaWidth = maxSize;

        pageRect.Add(new(44, 0, tabAreaWidth, _cachedMetrics.TitleHeightDPI));
        for (int i = 1; i < _windowPageControl.Count; i++)
            pageRect.Add(new(pageRect[i - 1].Right, 0, tabAreaWidth, _cachedMetrics.TitleHeightDPI));

        _needsLayoutUpdate = false;
    }
}
