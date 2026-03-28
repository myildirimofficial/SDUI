using Orivy.Animation;
using Orivy.Extensions;
using Orivy.Helpers;
using Orivy.Native.Windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using static Orivy.Native.Windows.Methods;

namespace Orivy.Controls;


public partial class Window : WindowBase
{
    private const float TAB_DRAG_THRESHOLD = 4f;

    public enum TabDesingMode
    {
        Rectangle,
        Rounded,
        Chromed
    }

    private const int TAB_HEADER_PADDING = 9;
    private const int TAB_INDICATOR_HEIGHT = 3;

    private const float HOVER_ANIMATION_SPEED = 0.1f;
    private const double TAB_INDICATOR_ANIMATION_SPEED = 0.10; // hız artırıldı, gecikme azaltıldı

    // Hot-path caches (avoid per-frame LINQ allocations)
    private readonly Dictionary<string, SKPaint> _paintCache = new();
    private readonly Dictionary<string, SKFont> _fontCache = new();
    // scratch buffer used by UpdateTabRects
    private readonly List<float> _tabWidthBuffer = new();
    // reusable temporary path for rounded rectangles
    private readonly SKPath _tempPath = new SKPath();


    /// <summary>
    /// Close tab hover animation manager
    /// </summary>
    private readonly AnimationManager closeBoxHoverAnimationManager;

    /// <summary>
    /// Whether to display the control buttons of the form
    /// </summary>
    private readonly bool controlBox = true;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly AnimationManager extendBoxHoverAnimationManager;

    /// <summary>
    /// tab area animation manager
    /// </summary>
    private readonly AnimationManager formMenuHoverAnimationManager;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly AnimationManager maxBoxHoverAnimationManager;

    /// <summary>
    /// Min Box hover animation manager
    /// </summary>
    private readonly AnimationManager minBoxHoverAnimationManager;

    /// <summary>
    ///     new Tab hover animation manager
    /// </summary>
    private readonly AnimationManager newTabHoverAnimationManager;

    /// <summary>
    ///     Tab Area hover animation manager
    /// </summary>
    private readonly AnimationManager pageAreaAnimationManager;

    /// <summary>
    ///     tab area animation manager
    /// </summary>
    private readonly AnimationManager tabCloseHoverAnimationManager;

    // Collection of hover animation managers to simplify bulk operations
    private readonly List<AnimationManager> _hoverAnimationManagers = new();

    /// <summary>
    ///     The rectangle of extend box
    /// </summary>
    private SkiaSharp.SKRect _closeTabBoxRect;

    /// <summary>
    ///     The control box left value
    /// </summary>
    private float _controlBoxLeft;

    /// <summary>
    ///     The rectangle of control box
    /// </summary>
    private SkiaSharp.SKRect _controlBoxRect;

    private bool _drawTabIcons;

    /// <summary>
    ///     Whether to show the title bar of the form
    /// </summary>
    private bool _drawTitleBorder = true;

    private bool _extendBox;

    /// <summary>
    ///     The rectangle of extend box
    /// </summary>
    private SkiaSharp.SKRect _extendBoxRect;

    /// <summary>
    ///     The rectangle of extend box
    /// </summary>
    private SkiaSharp.SKRect _formMenuRect;

    /// <summary>
    ///     If the mouse down <c>true</c>; otherwise <c>false</c>
    /// </summary>
    private bool _formMoveMouseDown;

    /// <summary>
    /// Gets whether the window is currently being moved by user drag.
    /// </summary>
    internal bool IsOnMoving => _formMoveMouseDown;

    /// <summary>
    ///     Gradient header colors
    /// </summary>
    private SKColor[] _gradient = [SKColors.Transparent, SKColors.Transparent];

    private HatchStyle _hatch = HatchStyle.Percent80;

    private float _iconWidth = 44;

    private bool _inCloseBox, _inMaxBox, _inMinBox, _inExtendBox, _inTabCloseBox, _inNewTabBox, _inFormMenuBox;

    /// <summary>
    ///     The starting location when form drag begins
    /// </summary>
    private SKPoint _dragStartLocation;

    /// <summary>
    ///     Whether to show the maximize button of the form
    /// </summary>
    private bool _maximizeBox = true;

    /// <summary>
    ///     The rectangle of maximize box
    /// </summary>
    private SkiaSharp.SKRect _maximizeBoxRect;


    /// <summary>
    ///     Whether to show the minimize button of the form
    /// </summary>
    private bool _minimizeBox = true;

    /// <summary>
    ///     The rectangle of minimize box
    /// </summary>
    private SkiaSharp.SKRect _minimizeBoxRect;

    /// <summary>
    ///     The position of the mouse when the left mouse button is pressed
    /// </summary>
    private SKPoint _mouseOffset;

    /// <summary>
    ///     The rectangle of extend box
    /// </summary>
    private SkiaSharp.SKRect _newTabBoxRect;

    private bool _newTabButton;
    private bool _popupMouseInteractionActive;
    private bool _suppressNextPopupClick;

    private long _stickyBorderTime = 5000000;

    private float _symbolSize = 24;

    private bool _tabCloseButton;
    private int _pendingTabSelectionIndex = -1;
    private SKPoint _pendingTabMouseDownScreen;

    /// <summary>
    ///     Tab desing mode
    /// </summary>
    private TabDesingMode _tabDesingMode = TabDesingMode.Rectangle;

    /// <summary>
    ///     The title height
    /// </summary>
    private float _titleHeight = 35;

    private WindowPageControl _windowPageControl;
    private SKPoint animationSource;

    /// <summary>
    ///     Whether to trigger the stay event on the edge of the display
    /// </summary>
    private bool IsStayAtTopBorder;

    private List<SkiaSharp.SKRect> pageRect;

    private int previousSelectedPageIndex;

    /// <summary>
    ///     Whether to show the title bar of the form
    /// </summary>
    private bool showMenuInsteadOfIcon;

    /// <summary>
    ///     Whether to show the title bar of the form
    /// </summary>
    private bool showTitle = true;

    /// <summary>
    ///     The title color
    /// </summary>
    private SKColor titleColor;

    /// <summary>
    ///     The time at which the display edge dwell event was triggered
    /// </summary>
    private long TopBorderStayTicks;

    /// <summary>
    ///     The contructor
    /// </summary>
    public Window()
    {
        AutoScaleMode = AutoScaleMode.None;
        enableFullDraggable = false;
        ColorScheme.ThemeChanged += OnThemeChanged;

        // allocate pageRect once to avoid repeated allocations
        pageRect = [];

        // create individual hover managers then register for bulk operations
        pageAreaAnimationManager = CreateHoverAnimation(TAB_INDICATOR_ANIMATION_SPEED);
        minBoxHoverAnimationManager = CreateHoverAnimation();
        maxBoxHoverAnimationManager = CreateHoverAnimation();
        closeBoxHoverAnimationManager = CreateHoverAnimation();
        extendBoxHoverAnimationManager = CreateHoverAnimation();
        tabCloseHoverAnimationManager = CreateHoverAnimation();
        newTabHoverAnimationManager = CreateHoverAnimation();
        formMenuHoverAnimationManager = CreateHoverAnimation();

        _hoverAnimationManagers.AddRange(
        [
            pageAreaAnimationManager,
            minBoxHoverAnimationManager,
            maxBoxHoverAnimationManager,
            closeBoxHoverAnimationManager,
            extendBoxHoverAnimationManager,
            tabCloseHoverAnimationManager,
            newTabHoverAnimationManager,
            formMenuHoverAnimationManager
        ]);

        //WindowsHelper.ApplyRoundCorner(this.Handle);
    }

    private float _titleHeightDPI => _titleHeight * ScaleFactor;
    private float _maximizedTitleInsetDPI => GetMaximizedTitleInsetDpi();
    private float _maximizedHorizontalInsetDPI => GetMaximizedHorizontalInsetDpi();
    private float _titleBarLeftInsetDPI => _maximizedHorizontalInsetDPI;
    private float _titleBarRightInsetDPI => _maximizedHorizontalInsetDPI;
    private float _titleBarTopDPI => _maximizedTitleInsetDPI;
    private float _titleBarBottomDPI => _titleBarTopDPI + _titleHeightDPI;
    private float _titleBarCenterYDPI => _titleBarTopDPI + (_titleHeightDPI / 2f);

    private float GetMaximizedTitleInsetDpi()
    {
        if (!OperatingSystem.IsWindows() || !ShowTitle || WindowState != FormWindowState.Maximized)
            return 0f;

        return GetSystemMetrics(SM_CYSIZEFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);
    }

    private float GetMaximizedHorizontalInsetDpi()
    {
        if (!OperatingSystem.IsWindows() || WindowState != FormWindowState.Maximized)
            return 0f;

        return GetSystemMetrics(SM_CXSIZEFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);
    }
    private float _iconWidthDPI => _iconWidth * ScaleFactor;
    private float _symbolSizeDPI => _symbolSize * ScaleFactor;

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
    public ContextMenuStrip ExtendMenu
    {
        get => _extendMenu;
        set
        {
            _extendMenu = value;
            if (_extendMenu != null)
            {
                _extendMenu.OpeningEffect = OpeningEffectType.SlideDownFade;
            }
        }
    }

    private ContextMenuStrip _extendMenu;

    [DefaultValue(null)] public ContextMenuStrip FormMenu { get; set; }

    /// <summary>
    ///     Gets or sets whether to show the title bar of the form
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
    ///     Gets or sets whether a small menu glyph is displayed in the left side
    ///     of the title bar instead of the window icon. When this property is
    ///     false and no icon is shown (either <see cref="ShowIcon"/> is false
    ///     or <see cref="Icon"/> is null), the left padding used for tabs and
    ///     title text collapses back to zero, eliminating the empty gap.
    /// </summary>
    public bool ShowMenuInsteadOfIcon
    {
        get => showMenuInsteadOfIcon;
        set
        {
            if (showMenuInsteadOfIcon == value)
                return;

            showMenuInsteadOfIcon = value;
            CalcSystemBoxPos();
            InvalidateMeasureRecursive();
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>
    ///     Gets or sets whether to show the title bar of the form
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

    /// <summary>
    ///     Whether to show the maximize button of the form
    /// </summary>
    public bool MaximizeBox
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
    ///     Whether to show the minimize button of the form
    /// </summary>
    public bool MinimizeBox
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
    ///     Gets or sets the title height
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

    public SKColor[] Gradient
    {
        get => _gradient;
        set
        {
            _gradient = value;
            Invalidate();
        }
    }

    /// <summary>
    ///     Gets or sets the title color
    /// </summary>
    [Description("Title color")]
    [DefaultValue(typeof(SKColor), "224, 224, 224")]
    public SKColor TitleColor
    {
        get => titleColor;
        set
        {
            titleColor = value;
            Invalidate();
        }
    }

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
    ///     Draw hatch brush on form
    /// </summary>
    public bool FullDrawHatch { get; set; }

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

    [Description("Set or get the maximum time to stay at the edge of the display(ms)")]
    [DefaultValue(500)]
    public long StickyBorderTime
    {
        get => _stickyBorderTime / 10000;
        set => _stickyBorderTime = value * 10000;
    }

    public SKSize CanvasSize =>
        _cacheBitmap == null ? SKSize.Empty : new SKSize(_cacheBitmap.Width, _cacheBitmap.Height);


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
            _windowPageControl.ControlAdded += delegate { Invalidate(); };
            _windowPageControl.ControlRemoved += delegate { Invalidate(); };
        }
    }

    public SKRectI MaximizedBounds { get; private set; }


    /// <summary>
    ///     If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnFormMenuClick;

    /// <summary>
    ///     If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnExtendBoxClick;

    /// <summary>
    ///     If extend box clicked invoke the event
    /// </summary>
    public event EventHandler<int> OnCloseTabBoxClick;

    /// <summary>
    ///     If extend box clicked invoke the event
    /// </summary>
    public event EventHandler OnNewTabBoxClick;

    protected override bool ShouldScaleSizeOnDpiChange(float newDpi, float oldDpi)
    {
        // During native WM_DPICHANGED flow, Windows already applied the suggested bounds.
        // Scaling Size again here causes double-scaling and jumpy window movement.
        return !_isHandlingDpiChange;
    }

    internal override void OnDpiChanged(float newDpi, float oldDpi)
    {
        try
        {
            base.OnDpiChanged(newDpi, oldDpi);

            if (newDpi == oldDpi)
                return;

            BeginImmediateUpdateSuppression();

            // CRITICAL: Aggressive layout recalculation to handle all control repositioning
            // Step 1: Invalidate all measurements
            InvalidateMeasureRecursive();

            // Step 2: base.OnDpiChanged already propagates to children.
            // Perform full layout pass to reposition all children.
            PerformLayout();

            // Step 3: Update window chrome (title bar buttons, tabs)
            CalcSystemBoxPos();
            if (_windowPageControl != null && _windowPageControl.Count > 0)
                UpdateTabRects();

            // Step 4: Final invalidation to ensure complete redraw
            NeedsFullChildRedraw = true;
            Invalidate();
        }
        finally
        {
            EndImmediateUpdateSuppression();
        }
    }

    private void InvalidateMeasureRecursive()
    {
        // Recursively invalidate all children
        foreach (ElementBase child in Controls) InvalidateMeasureRecursiveInternal(child);
    }

    private static void InvalidateMeasureRecursiveInternal(ElementBase element)
    {
        element.InvalidateMeasure();
        foreach (ElementBase child in element.Controls) InvalidateMeasureRecursiveInternal(child);
    }

    // Helper to toggle hover animations consistently
    private static void SetHoverState(AnimationManager manager, bool enter)
    {
        manager.StartNewAnimation(enter ? AnimationDirection.In : AnimationDirection.Out);
    }

    // End all hover animations (used on mouse leave)
    private void EndAllHoverAnimations()
    {
        for (var i = 0; i < _hoverAnimationManagers.Count; i++)
        {
            var mgr = _hoverAnimationManagers[i];
            mgr?.StartNewAnimation(AnimationDirection.Out);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnsureInitialLayoutAndDpiSync();
        // Ensure caption hit test state is correct from the start
        CalcSystemBoxPos();
    }

    private void EnsureInitialLayoutAndDpiSync()
    {
        // ensure measurements are fresh now that handle (and correct client size) exist
        InvalidateMeasureRecursive();
        PerformLayout();
        Invalidate();
        CalcSystemBoxPos();

        // Initial DPI sync: Ensure controls match the window's actual DPI
        try
        {
            var dpi = Screen.GetDpiForWindowHandle(Handle);
            foreach (var control in Controls.OfType<ElementBase>())
            {
                var oldDpi = control.ScaleFactor * 96f;
                if (Math.Abs(oldDpi - dpi) > 0.001f)
                {
                    control.OnDpiChanged(dpi, oldDpi);
                }
            }

            // layout again after DPI adjustments (child OnDpiChanged may alter desired sizes)
            InvalidateMeasureRecursive();
            PerformLayout();
            CalcSystemBoxPos();
            if (_windowPageControl != null && _windowPageControl.Count > 0)
                UpdateTabRects();
            Invalidate();
        }
        catch
        {
            // keep best-effort behavior: if DPI helpers fail, leave layout as-is
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        QueueNativeThemeApply();
        NeedsFullChildRedraw = true;
        InvalidateRenderTree();
        Invalidate();
    }

    internal override void OnControlAdded(ElementEventArgs e)
    {
        base.OnControlAdded(e);

        if (ShowTitle && !AllowAddControlOnTitle && e.Element.Location.Y < TitleHeight)
        {
            var newLoc = e.Element.Location;
            newLoc.Y = Padding.Top;
            e.Element.Location = newLoc;
        }
    }

    protected override bool IsCaptionHit(SKPoint clientPt)
    {
        // if title not shown, definitely not caption
        if (!ShowTitle)
            return false;

        // clientPt.Y >= titleHeightDpi means below the title region, including the
        // hidden maximized frame inset that we reserve visually.
        if (clientPt.Y >= _titleBarBottomDPI)
            return false;

        if (IsPointOverTabHeader(clientPt))
            return false;

        // ignore control button areas
        if (_controlBoxRect.Contains(clientPt))
            return false;

        if (_maximizeBoxRect.Contains(clientPt))
            return false;

        if (_minimizeBoxRect.Contains(clientPt))
            return false;

        if (_extendBoxRect.Contains(clientPt))
            return false;

        if (_tabCloseButton && _closeTabBoxRect.Contains(clientPt))
            return false;

        if (_newTabButton && _newTabBoxRect.Contains(clientPt))
            return false;

        // if the menu glyph is visible we must exclude its bounds from the
        // caption area, otherwise the user should be able to drag from there.
        if (showMenuInsteadOfIcon && _formMenuRect.Contains(clientPt))
        {
            return false;
        }

        return true;
    }

    private void CalcSystemBoxPos()
    {
        _controlBoxLeft = Width;
        var rightEdge = Width - _titleBarRightInsetDPI;

        if (controlBox)
        {
            _controlBoxRect = SKRect.Create(rightEdge - _iconWidthDPI, _titleBarTopDPI, _iconWidthDPI, _titleHeightDPI);
            _controlBoxLeft = _controlBoxRect.Left - 2;

            if (MaximizeBox)
            {
                _maximizeBoxRect = SKRect.Create(_controlBoxRect.Left - _iconWidthDPI, _controlBoxRect.Top,
                    _iconWidthDPI, _titleHeightDPI);
                _controlBoxLeft = _maximizeBoxRect.Left - 2;
            }
            else
            {
                _maximizeBoxRect = SKRect.Create(Width + 1, Height + 1, 1, 1);
            }

            if (MinimizeBox)
            {
                _minimizeBoxRect =
                    SKRect.Create(
                        MaximizeBox
                            ? _maximizeBoxRect.Left - _iconWidthDPI - 2
                            : _controlBoxRect.Left - _iconWidthDPI - 2, _controlBoxRect.Top, _iconWidthDPI,
                        _titleHeightDPI);
                _controlBoxLeft = _minimizeBoxRect.Left - 2;
            }
            else
            {
                _minimizeBoxRect = SKRect.Create(Width + 1, Height + 1, 1, 1);
            }

            if (ExtendBox)
            {
                if (MinimizeBox)
                    _extendBoxRect = SKRect.Create(_minimizeBoxRect.Left - _iconWidthDPI - 2, _controlBoxRect.Top,
                        _iconWidthDPI, _titleHeightDPI);
                else
                    _extendBoxRect = SKRect.Create(_controlBoxRect.Left - _iconWidthDPI - 2, _controlBoxRect.Top,
                        _iconWidthDPI, _titleHeightDPI);
            }
        }
        else
        {
            _extendBoxRect = _maximizeBoxRect =
            _minimizeBoxRect = _controlBoxRect = SKRect.Create(Width + 1, Height + 1, 1, 1);
        }

        var titleIconSize = 24 * ScaleFactor;
        _formMenuRect = SKRect.Create(_titleBarLeftInsetDPI + 10, _titleBarCenterYDPI - titleIconSize / 2, titleIconSize, titleIconSize);

        Padding = new Thickness(
            (int)MathF.Ceiling(_titleBarLeftInsetDPI),
            (int)(showTitle ? MathF.Ceiling(_titleBarBottomDPI) : 0),
            (int)MathF.Ceiling(_titleBarRightInsetDPI),
            Padding.Bottom);
    }

    private bool TryGetTabIndexAtPoint(SKPoint point, out int tabIndex)
    {
        tabIndex = -1;

        if (_windowPageControl == null || _windowPageControl.Count == 0)
            return false;

        UpdateTabRects();

        for (var i = 0; i < pageRect.Count; i++)
        {
            if (pageRect[i].Contains(point))
            {
                tabIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool IsPointOverTabHeader(SKPoint point)
    {
        return TryGetTabIndexAtPoint(point, out _);
    }

    protected override bool ShouldIncludeHitTestElement(ElementBase element, bool requireEnabled)
    {
        if (!base.ShouldIncludeHitTestElement(element, requireEnabled))
            return false;

        // Floating popups must remain hit-testable even when they overlap the custom title area.
        if (element is ContextMenuStrip contextMenu && contextMenu.Visible)
            return true;

        return !ShowTitle || AllowAddControlOnTitle || element.Location.Y >= _titleBarBottomDPI;
    }

    private ContextMenuStrip? FindTopmostOpenPopup(SKPoint location)
    {
        ContextMenuStrip? popup = null;
        var bestZOrder = int.MinValue;

        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is not ContextMenuStrip contextMenu || !contextMenu.Visible || !contextMenu.IsOpen)
                continue;

            if (!GetWindowRelativeBounds(contextMenu).Contains(location))
                continue;

            if (popup == null || contextMenu.ZOrder > bestZOrder)
            {
                popup = contextMenu;
                bestZOrder = contextMenu.ZOrder;
            }
        }

        return popup;
    }

    private bool TryRouteMouseEventToOpenPopup(MouseEventArgs e, Action<ContextMenuStrip, MouseEventArgs> route)
    {
        var popup = FindTopmostOpenPopup(e.Location);
        if (popup == null)
            return false;

        var bounds = GetWindowRelativeBounds(popup);
        var localEvent = new MouseEventArgs(
            e.Button,
            e.Clicks,
            (int)(e.X - bounds.Left),
            (int)(e.Y - bounds.Top),
            e.Delta,
            e.IsHorizontalWheel);

        route(popup, localEvent);
        return true;
    }

    private bool TryRouteMouseWheelToOpenPopup(MouseEventArgs e)
    {
        var popup = FindTopmostOpenPopup(e.Location);
        if (popup == null)
            return false;

        var popupBounds = popup.Bounds;
        var localEvent = new MouseEventArgs(
            e.Button,
            e.Clicks,
            (int)(e.X - popupBounds.Left),
            (int)(e.Y - popupBounds.Top),
            e.Delta,
            e.IsHorizontalWheel);

        popup.OnMouseWheel(localEvent);
        return true;
    }

    protected internal override void OnMouseClick(MouseEventArgs e)
    {
        if (_suppressNextPopupClick)
        {
            _suppressNextPopupClick = false;
            return;
        }

        if (TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseClick(localEvent)))
            return;

        base.OnMouseClick(e);

        if (!ShowTitle)
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
            // Force repaint to prevent stale background captures
            Update();
            if (ExtendMenu != null)
            {
                var menuSize = ExtendMenu.MeasurePreferredSize();
                // Open menu centered horizontally under the extend box
                var centerX = _extendBoxRect.Left + (_extendBoxRect.Width - menuSize.Width) / 2f;
                ExtendMenu.Show(PointToScreen(new SKPoint(
                    Convert.ToInt32(centerX),
                    Convert.ToInt32(_extendBoxRect.Bottom)
                )));
            }
            else
                OnExtendBoxClick?.Invoke(this, EventArgs.Empty);
        }

        if (_inFormMenuBox)
        {
            _inFormMenuBox = false;
            // Force repaint to prevent stale background captures
            Update();
            if (FormMenu != null)
                FormMenu.Show(PointToScreen(new SKPoint(Convert.ToInt32(_formMenuRect.Left),
                    Convert.ToInt32(_formMenuRect.Bottom))));
            else
                OnFormMenuClick?.Invoke(this, EventArgs.Empty);
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

        if (_formMoveMouseDown && !CursorScreenPosition.Equals(_mouseOffset))
            return;

        if (_tabCloseButton && e.Button == MouseButtons.Middle && TryGetTabIndexAtPoint(e.Location, out var middleClickTabIndex))
            OnCloseTabBoxClick?.Invoke(null, middleClickTabIndex);
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        // Make sure this Form receives keyboard input.
        if (CanFocus)
            Focus();

        if (TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseDown(localEvent)))
        {
            _popupMouseInteractionActive = true;
            return;
        }

        // Title bar drag has absolute priority over child controls.
        // Check this BEFORE hit-testing children so that a misplaced child
        // (e.g. during the first layout pass) cannot steal the drag.
        var inTitleArea = ShowTitle && e.Y < Padding.Top;
        var inTabHeader = TryGetTabIndexAtPoint(e.Location, out var clickedTabIndex);
        var inControlBox = _inCloseBox || _inMaxBox || _inMinBox || _inExtendBox
                           || _inTabCloseBox || _inNewTabBox || _inFormMenuBox;

        if (e.Button == MouseButtons.Left && inTabHeader && !inControlBox)
        {
            _pendingTabSelectionIndex = clickedTabIndex;
            _pendingTabMouseDownScreen = CursorScreenPosition;
            SetCapture(Handle);

            return;
        }

        if (inTitleArea && !inControlBox && !inTabHeader)
        {
            if (enableFullDraggable && e.Button == MouseButtons.Left)
                DragForm(Handle);

            if (e.Button == MouseButtons.Left && Movable)
            {
                _formMoveMouseDown = true;
                _dragStartLocation = Location;
                _mouseOffset = CursorScreenPosition;
                SetCapture(Handle);
            }
            return;
        }

        base.OnMouseDown(e);

        var element = FindHitTestElement(e.Location, requireEnabled: true);
        if (element != null)
        {
            BringToFront(element);
        }

        // NOTE: Window context menus should open on MouseUp (standard behavior).
        // Showing on MouseDown can lead to double menus when the mouse moves slightly
        // and an element handles right-click on MouseUp.
    }

    internal override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseDoubleClick(localEvent)))
            return;

        // Title bar maximize gesture has priority — check before child hit-testing.
        var inTitleAreaDbl = ShowTitle && MaximizeBox && e.Y < Padding.Top;
        if (inTitleAreaDbl)
        {
            var inTabHeaderDbl = IsPointOverTabHeader(e.Location);
            var inControlBoxDbl = _controlBoxRect.Contains(e.Location)
                                  || _maximizeBoxRect.Contains(e.Location)
                                  || _minimizeBoxRect.Contains(e.Location)
                                  || _extendBoxRect.Contains(e.Location)
                                  || (_tabCloseButton && _closeTabBoxRect.Contains(e.Location))
                                  || (_newTabButton && _newTabBoxRect.Contains(e.Location))
                                  || (showMenuInsteadOfIcon && _formMenuRect.Contains(e.Location));

            if (!inControlBoxDbl && !inTabHeaderDbl)
            {
                ShowMaximize();
                return;
            }
        }

        base.OnMouseDoubleClick(e);

        if (FindHitTestElement(e.Location, requireEnabled: true) is { } element)
        {
            BringToFront(element);
        }
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        if (_popupMouseInteractionActive)
        {
            _popupMouseInteractionActive = false;
            _suppressNextPopupClick = true;

            TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseUp(localEvent));
            return;
        }

        if (!_formMoveMouseDown && _mouseCapturedElement == null &&
            TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseUp(localEvent)))
            return;

        var pendingTabSelectionIndex = _pendingTabSelectionIndex;
        var shouldSelectPendingTab = e.Button == MouseButtons.Left && pendingTabSelectionIndex >= 0 && !_formMoveMouseDown;

        // If an element captured the mouse, forward the mouse up to it and release capture if left button
        if (_mouseCapturedElement != null)
        {
            var captured = _mouseCapturedElement;
            var bounds = GetWindowRelativeBounds(captured);
            var localEvent = new MouseEventArgs(e.Button, e.Clicks, (int)(e.X - bounds.Left), (int)(e.Y - bounds.Top), e.Delta, e.IsHorizontalWheel);
            captured.OnMouseUp(localEvent);
            if (e.Button == MouseButtons.Left) ReleaseMouseCapture(captured);
        }

        base.OnMouseUp(e);

        if (!IsDisposed && _formMoveMouseDown)
        {
            var screenPos = CursorScreenPosition;
            var screen = Screen.FromPoint(screenPos);
            if (screenPos.Y == screen.WorkingArea.Top && MaximizeBox) ShowMaximize(true);

            var location = Location;
            if (location.X < screen.WorkingArea.Left)
                location.X = screen.WorkingArea.Left;

            if (location.Y > screen.WorkingArea.Bottom - TitleHeight)
                location.Y = Convert.ToInt32(screen.WorkingArea.Bottom - _titleHeightDPI);

            Location = location;
        }

        IsStayAtTopBorder = false;
        Cursor.Clip = null;
        // Always release capture on mouse-up regardless of which code path acquired it
        // (title-bar drag, tab-header drag detection, or child element capture). If capture
        // is not released here, WM_NCHITTEST is never sent and resize stops working until
        // the window is maximised/restored.
        ReleaseCapture();
        _formMoveMouseDown = false;

        if (shouldSelectPendingTab && _windowPageControl != null && pendingTabSelectionIndex < _windowPageControl.Count)
        {
            _windowPageControl.SelectedIndex = pendingTabSelectionIndex;
        }

        _pendingTabSelectionIndex = -1;

        animationSource = e.Location;

        var hitElement = FindHitTestElement(e.Location, requireEnabled: true);
        var elementClicked = hitElement != null;

        if (e.Button == MouseButtons.Right && ContextMenuStrip != null)
        {
            // If nothing was hit, show the window menu.
            // If an element was hit but no element/parent has a menu, fall back to the window menu.
            // Exception: TextBox can show a native menu fallback; don't show window menu on top.
            var shouldShowWindowMenu = !elementClicked;

            if (!shouldShowWindowMenu && hitElement != null)
            {
                var isTextBox = hitElement is TextBox;
                if (!isTextBox && !HasContextMenuInChain(hitElement))
                    shouldShowWindowMenu = true;
            }

            if (shouldShowWindowMenu)
            {
                var point = PointToScreen(e.Location);
                ContextMenuStrip.Show(point);
            }
        }
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        // Window drag always takes priority over element capture.
        // Without this ordering, a child control that accidentally captured the mouse
        // (e.g. due to a wrong initial layout position) would block all window movement.
        var screenCursor = CursorScreenPosition;

        if (!_formMoveMouseDown && _mouseCapturedElement == null && _pendingTabSelectionIndex < 0 &&
            TryRouteMouseEventToOpenPopup(e, static (popup, localEvent) => popup.OnMouseMove(localEvent)))
            return;

        if (!_formMoveMouseDown && _pendingTabSelectionIndex >= 0)
        {
            var deltaX = screenCursor.X - _pendingTabMouseDownScreen.X;
            var deltaY = screenCursor.Y - _pendingTabMouseDownScreen.Y;
            var dragDistanceSquared = (deltaX * deltaX) + (deltaY * deltaY);
            var dragThresholdSquared = TAB_DRAG_THRESHOLD * TAB_DRAG_THRESHOLD;

            if (Movable && dragDistanceSquared >= dragThresholdSquared)
            {
                _formMoveMouseDown = true;
                _dragStartLocation = Location;
                _mouseOffset = _pendingTabMouseDownScreen;
            }
            else
            {
                base.OnMouseMove(e);
                return;
            }
        }

        if (!_formMoveMouseDown && _mouseCapturedElement != null)
        {
            // Forward all mouse move events to the captured element so dragging
            // continues even when the cursor leaves its bounds.
            var captured = _mouseCapturedElement;
            var bounds = GetWindowRelativeBounds(captured);
            var localEvent = new MouseEventArgs(e.Button, e.Clicks, (int)(e.X - bounds.Left), (int)(e.Y - bounds.Top), e.Delta, e.IsHorizontalWheel);
            captured.OnMouseMove(localEvent);
            return;
        }
        if (_formMoveMouseDown && !screenCursor.Equals(_mouseOffset))
        {
            if (WindowState == FormWindowState.Maximized)
            {
                var maximizedWidth = Width;
                var locationX = Location.X;
                ShowMaximize();

                var offsetXRatio = 1 - (float)Width / maximizedWidth;
                _mouseOffset.X -= (int)((_mouseOffset.X - locationX) * offsetXRatio);
            }

            var offsetX = _mouseOffset.X - screenCursor.X;
            var offsetY = _mouseOffset.Y - screenCursor.Y;
            var screen = Screen.FromPoint(screenCursor);
            var _workingArea = screen.WorkingArea;

            if (screenCursor.Y - _workingArea.Top == 0)
            {
                if (!IsStayAtTopBorder)
                {
                    Cursor.Clip = _workingArea;
                    TopBorderStayTicks = DateTime.Now.Ticks;
                    IsStayAtTopBorder = true;
                }
                else if (DateTime.Now.Ticks - TopBorderStayTicks > _stickyBorderTime)
                {
                    Cursor.Clip = null;
                }
            }

            var newX = (int)(_dragStartLocation.X - offsetX);
            var newY = (int)(_dragStartLocation.Y - offsetY);
            base.Location = new SKPoint(newX, newY);
            SetWindowPos(Handle, IntPtr.Zero, newX, newY, 0, 0,
                SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER |
                SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOCOPYBITS);
        }
        else
        {
            var inCloseBox = _controlBoxRect.Contains(e.Location.X, e.Location.Y);
            var inMaxBox = _maximizeBoxRect.Contains(e.Location.X, e.Location.Y);
            var inMinBox = _minimizeBoxRect.Contains(e.Location.X, e.Location.Y);
            var inExtendBox = _extendBoxRect.Contains(e.Location.X, e.Location.Y);
            var inFormMenuBox = showMenuInsteadOfIcon && _formMenuRect.Contains(e.Location.X, e.Location.Y);
            var inCloseTabBox = _tabCloseButton && _closeTabBoxRect.Contains(e.Location.X, e.Location.Y);
            var inNewTabBox = _newTabButton && _newTabBoxRect.Contains(e.Location.X, e.Location.Y);

            var isChange = false;

            if (inCloseBox != _inCloseBox)
            {
                _inCloseBox = inCloseBox;
                isChange = true;
                SetHoverState(closeBoxHoverAnimationManager, inCloseBox);
            }

            if (inMaxBox != _inMaxBox)
            {
                _inMaxBox = inMaxBox;
                isChange = true;
                SetHoverState(maxBoxHoverAnimationManager, inMaxBox);
            }

            if (inMinBox != _inMinBox)
            {
                _inMinBox = inMinBox;
                isChange = true;
                SetHoverState(minBoxHoverAnimationManager, inMinBox);
            }

            if (inExtendBox != _inExtendBox)
            {
                _inExtendBox = inExtendBox;
                isChange = true;
                SetHoverState(extendBoxHoverAnimationManager, inExtendBox);
            }

            if (inCloseTabBox != _inTabCloseBox)
            {
                _inTabCloseBox = inCloseTabBox;
                isChange = true;
                SetHoverState(tabCloseHoverAnimationManager, inCloseTabBox);
            }

            if (inNewTabBox != _inNewTabBox)
            {
                _inNewTabBox = inNewTabBox;
                isChange = true;
                SetHoverState(newTabHoverAnimationManager, inNewTabBox);
            }

            if (inFormMenuBox != _inFormMenuBox)
            {
                _inFormMenuBox = inFormMenuBox;
                isChange = true;
                SetHoverState(formMenuHoverAnimationManager, inFormMenuBox);
            }

            if (isChange)
                Invalidate();
        }

        // let element base propagate the mouse move and manage hover/cursor
        base.OnMouseMove(e);
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _inExtendBox = _inCloseBox = _inMaxBox = _inMinBox = _inTabCloseBox = _inNewTabBox = _inFormMenuBox = false;

        // End all hover animations in a single loop to avoid repetition
        EndAllHoverAnimations();

        Invalidate();
    }

    internal override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        if (TryRouteMouseWheelToOpenPopup(e))
            return;

        base.OnMouseWheel(e);

        var mousePos = e.Location;

        if (PropagateMouseWheel(Controls, mousePos, e))
            return;
    }

    private void ShowMaximize(bool IsOnMoving = false)
    {
        // Cancel any active drag operation
        _formMoveMouseDown = false;
        ReleaseCapture();

        if (WindowState == FormWindowState.Normal)
        {
            WindowState = FormWindowState.Maximized;
        }
        else if (WindowState == FormWindowState.Maximized)
        {
            // Native restore handles returning to previous size/position
            WindowState = FormWindowState.Normal;
        }

        Invalidate();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        Debug.WriteLine("OnActivated");
        Invalidate();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Debug.WriteLine("OnDeactivate");
        Invalidate();
    }

    protected override void RenderWindowFrame(SKCanvas canvas, SKImageInfo info)
    {
        PaintSurface(canvas, info);
    }

    private void PaintSurface(SKCanvas canvas, SKImageInfo info)
    {
        if (info.Width <= 0 || info.Height <= 0)
            return;

        bool revealNativeBackdrop = UsesNativeBackdropMaterial && DwmMargin != 0;

        if (revealNativeBackdrop)
        {
            // Clear the entire canvas to transparent (premultiplied black).
            // DWM treats pure-black GDI pixels as transparent when DwmExtendFrameIntoClientArea
            // covers the area, so the native backdrop (Mica / Acrylic / Tabbed) shows through
            // everywhere that controls don't paint over it.
            canvas.Clear(SKColors.Transparent);
        }
        else
        {
            canvas.Clear(ColorScheme.Surface);
        }

        if (!ShowTitle)
        {
            return;
        }

        var foreColor = ColorScheme.ForeColor;
        var hoverColor = ColorScheme.BorderColor;

        if (FullDrawHatch)
        {
            using var hatchBrush = new HatchBrush(_hatch, hoverColor.WithAlpha(30), SKColors.Transparent);
            using var hatchPaint = hatchBrush.CreatePaint();

            canvas.DrawRect(0, 0, Width, Height, hatchPaint);
        }

        if (titleColor != SKColor.Empty)
        {
            foreColor = titleColor.Determine();
            hoverColor = foreColor.WithAlpha(20);
            using var paint = new SKPaint { Color = titleColor };
            canvas.DrawRect(0, _titleBarTopDPI, Width, _titleHeightDPI, paint);
        }
        else if (_gradient.Length == 2 &&
                 !(_gradient[0] == SKColors.Transparent && _gradient[1] == SKColors.Transparent))
        {
            // Gradient mode
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(Width, _titleBarBottomDPI),
                new[] { _gradient[0], _gradient[1] },
                null,
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(0, _titleBarTopDPI, Width, _titleHeightDPI, paint);

            foreColor = _gradient[0].Determine();
            hoverColor = foreColor.WithAlpha(20);
        }

        if (controlBox)
        {
            var closeHoverColor = new SKColor(232, 17, 35);
            using SKPaint strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = foreColor,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true
            };

            using var paint = new SKPaint
            {
                IsAntialias = true
            };

            if (_inCloseBox)
            {
                paint.Color = closeHoverColor.WithAlpha((byte)(closeBoxHoverAnimationManager.GetProgress() * 120));
                canvas.DrawRect(_controlBoxRect, paint);
            }

            strokePaint.Color = _inCloseBox ? SKColors.White : foreColor;

            var centerX = _controlBoxRect.Left + _controlBoxRect.Width / 2;
            var centerY = _controlBoxRect.Top + _controlBoxRect.Height / 2;
            var size = 5 * ScaleFactor;

            canvas.DrawLine(
                centerX - size,
                centerY - size,
                centerX + size,
                centerY + size,
                strokePaint);

            canvas.DrawLine(
                centerX - size,
                centerY + size,
                centerX + size,
                centerY - size,
                strokePaint);

            strokePaint.Color = foreColor;
            if (MaximizeBox)
            {
                if (_inMaxBox)
                {
                    paint.Color = hoverColor.WithAlpha((byte)(maxBoxHoverAnimationManager.GetProgress() * 80));
                    canvas.DrawRect(_maximizeBoxRect, paint);
                }

                centerX = _maximizeBoxRect.Left + _maximizeBoxRect.Width / 2;
                centerY = _maximizeBoxRect.Top + _maximizeBoxRect.Height / 2;
                size = (WindowState == FormWindowState.Maximized ? 4 : 5) * ScaleFactor;

                float offset = size * 0.5f;
                float cornerRadius = 2.0f * ScaleFactor;

                var frontRect = new SKRect(
                    centerX - size,
                    centerY - size,
                    centerX + size,
                    centerY + size
                );

                if (WindowState == FormWindowState.Maximized)
                {
                    var backRect = new SKRect(
                        frontRect.Left + offset,
                        frontRect.Top - offset,
                        frontRect.Right + offset,
                        frontRect.Bottom - offset
                    );

                    canvas.Save();

                    SKRect clipRect = frontRect;
                    clipRect.Inflate(ScaleFactor / 2f, ScaleFactor / 2f);
                    SKRoundRect clipRoundRect = new(clipRect, cornerRadius + (ScaleFactor / 2f));

                    canvas.ClipRoundRect(clipRoundRect, SKClipOperation.Difference, true);
                    canvas.DrawRoundRect(backRect, cornerRadius, cornerRadius, strokePaint);
                    canvas.Restore();
                }

                canvas.DrawRect(frontRect, strokePaint);
            }

            if (MinimizeBox)
            {
                if (_inMinBox)
                {
                    paint.Color = hoverColor.WithAlpha((byte)(minBoxHoverAnimationManager.GetProgress() * 80));
                    canvas.DrawRect(_minimizeBoxRect, paint);
                }

                centerX = _minimizeBoxRect.Left + _minimizeBoxRect.Width / 2;
                centerY = _minimizeBoxRect.Top + _minimizeBoxRect.Height / 2;
                size = 5 * ScaleFactor;

                canvas.DrawLine(
                    centerX - size,
                    centerY,
                    centerX + size,
                    centerY,
                    strokePaint);
            }
        }

        if (ExtendBox)
        {
            var color = foreColor;
            if (_inExtendBox)
            {
                var hoverSize = 24 * ScaleFactor;
                using var paint = new SKPaint
                {
                    Color = hoverColor.WithAlpha((byte)(extendBoxHoverAnimationManager.GetProgress() * 60)),
                    IsAntialias = true
                };

                using var path = new SKPath();
                path.AddRoundRect(new SKRect(
                    _extendBoxRect.Left + 20 * ScaleFactor,
                    _titleBarCenterYDPI - hoverSize / 2,
                    _extendBoxRect.Left + 20 * ScaleFactor + hoverSize,
                    _titleBarCenterYDPI + hoverSize / 2
                ), 15, 15);

                canvas.DrawPath(path, paint);
            }

            var size = 16 * ScaleFactor;
            using var extendPaint = new SKPaint
            {
                Color = foreColor,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            var iconRect = new SkiaSharp.SKRect(
                _extendBoxRect.Left + 24 * ScaleFactor,
                _titleBarCenterYDPI - size / 2,
                _extendBoxRect.Left + 24 * ScaleFactor + size,
                _titleBarCenterYDPI + size / 2);

            canvas.DrawLine(
                iconRect.Left + iconRect.Width / 2 - 5 * ScaleFactor - 1,
                iconRect.Top + iconRect.Height / 2 - 2 * ScaleFactor,
                iconRect.Left + iconRect.Width / 2 - 1 * ScaleFactor,
                iconRect.Top + iconRect.Height / 2 + 3 * ScaleFactor,
                extendPaint);

            canvas.DrawLine(
                iconRect.Left + iconRect.Width / 2 + 5 * ScaleFactor - 1,
                iconRect.Top + iconRect.Height / 2 - 2 * ScaleFactor,
                iconRect.Left + iconRect.Width / 2 - 1 * ScaleFactor,
                iconRect.Top + iconRect.Height / 2 + 3 * ScaleFactor,
                extendPaint);
        }

        var faviconSize = 16 * ScaleFactor;

        if (showMenuInsteadOfIcon)
        {
            using var paint = new SKPaint
            {
                Color = hoverColor.WithAlpha((byte)(formMenuHoverAnimationManager.GetProgress() * 60)),
                IsAntialias = true
            };

            using var path = new SKPath();
            path.AddRoundRect(_formMenuRect, 10, 10);
            canvas.DrawPath(path, paint);

            using var menuPaint = new SKPaint
            {
                Color = foreColor,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            canvas.DrawLine(
                _formMenuRect.Left + _formMenuRect.Width / 2 - 5 * ScaleFactor - 1,
                _formMenuRect.Top + _formMenuRect.Height / 2 - 2 * ScaleFactor,
                _formMenuRect.Left + _formMenuRect.Width / 2 - 1 * ScaleFactor,
                _formMenuRect.Top + _formMenuRect.Height / 2 + 3 * ScaleFactor,
                menuPaint);

            canvas.DrawLine(
                _formMenuRect.Left + _formMenuRect.Width / 2 + 5 * ScaleFactor - 1,
                _formMenuRect.Top + _formMenuRect.Height / 2 - 2 * ScaleFactor,
                _formMenuRect.Left + _formMenuRect.Width / 2 - 1 * ScaleFactor,
                _formMenuRect.Top + _formMenuRect.Height / 2 + 3 * ScaleFactor,
                menuPaint);
        }
        else if (ShowIcon && Icon != null)
        {
            using var bitmap = Icon.ToBitmap();
            using var skBitmap = bitmap.ToSKBitmap();
            using var image = SKImage.FromBitmap(skBitmap);
            var iconRect = SKRect.Create(_titleBarLeftInsetDPI + 10, _titleBarCenterYDPI - faviconSize / 2, faviconSize, faviconSize);
            canvas.DrawImage(image, iconRect);
        }

        if (_windowPageControl == null || _windowPageControl.Count == 0)
        {
            var baseFont = Font;
            var font = GetOrCreateFont("title", () => new SKFont(baseFont.Typeface ?? SKTypeface.Default)
            {
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full
            });
            font.Size = baseFont.Size.Topx(this);

            var textPaint = GetOrCreatePaint("titleText", () => new SKPaint { IsAntialias = true });
            textPaint.Color = foreColor;

            var bounds = new SKRect();
            font.MeasureText(Text, out bounds);
            var textX = showMenuInsteadOfIcon
                ? _formMenuRect.Left + _formMenuRect.Width + 8 * ScaleFactor
                : _titleBarLeftInsetDPI + ((ShowIcon && Icon != null) ? faviconSize + 14 * ScaleFactor : 8);
            var textY = _titleBarCenterYDPI + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;

            TextRenderer.DrawText(canvas, Text, textX, textY, SKTextAlign.Left, font, textPaint);
        }

        if (_windowPageControl != null && _windowPageControl.Count > 0)
        {
            var isPageTransitionAnimating = pageAreaAnimationManager.IsAnimating();

            if (!isPageTransitionAnimating || pageRect == null ||
                pageRect.Count != _windowPageControl.Count)
                UpdateTabRects();

            var transitionProgress = isPageTransitionAnimating ? pageAreaAnimationManager.GetProgress() : 1d;

            // fix desing time error
            if (_windowPageControl.SelectedIndex <= -1 || _windowPageControl.SelectedIndex >= _windowPageControl.Count)
                return;

            // Animate page indicator
            if (!isPageTransitionAnimating)
            {
                previousSelectedPageIndex = _windowPageControl.SelectedIndex;
            }
            else if (previousSelectedPageIndex >= pageRect.Count)
            {
                previousSelectedPageIndex = _windowPageControl.SelectedIndex;
            }

            var previousSelectedPageIndexIfHasOne = previousSelectedPageIndex == -1
                ? _windowPageControl.SelectedIndex
                : previousSelectedPageIndex;
            var previousActivePageRect = pageRect[previousSelectedPageIndexIfHasOne];
            var activePageRect = pageRect[_windowPageControl.SelectedIndex];

            var y = activePageRect.Bottom - 2;
            var x = previousActivePageRect.Left + (activePageRect.Left - previousActivePageRect.Left) * (float)transitionProgress;
            var width = previousActivePageRect.Width +
                        (activePageRect.Width - previousActivePageRect.Width) * (float)transitionProgress;

            if (_tabDesingMode == TabDesingMode.Rectangle)
            {
                var tabPaint = GetOrCreatePaint("tabBg", () => new SKPaint { IsAntialias = true });
                tabPaint.Color = ColorScheme.Surface.InterpolateColor(hoverColor, 0.15f);

                canvas.DrawRect(x, _titleBarTopDPI, width, _titleHeightDPI, tabPaint);

                var indicatorPaint = GetOrCreatePaint("tabIndicator", () => new SKPaint { Color = SKColors.DodgerBlue, IsAntialias = true });
                canvas.DrawRect(x, _titleBarBottomDPI - TAB_INDICATOR_HEIGHT, width, TAB_INDICATOR_HEIGHT, indicatorPaint);
            }
            else if (_tabDesingMode == TabDesingMode.Rounded)
            {
                if (titleColor != SKColor.Empty && !titleColor.IsDark())
                    hoverColor = foreColor.WithAlpha(60);

                var tabPaint = GetOrCreatePaint("tabBg", () => new SKPaint { IsAntialias = true });
                tabPaint.Color = ColorScheme.Surface.InterpolateColor(hoverColor, 0.2f);

                var tabRect = new SKRect(x, _titleBarTopDPI + 6, x + width, _titleBarBottomDPI);
                var radius = 9 * ScaleFactor;

                _tempPath.Reset();
                _tempPath.AddRoundRect(tabRect, radius, radius);
                canvas.DrawPath(_tempPath, tabPaint);
            }
            else // Chromed
            {
                if (titleColor != SKColor.Empty && !titleColor.IsDark())
                    hoverColor = foreColor.WithAlpha(60);

                var tabPaint = GetOrCreatePaint("tabBg", () => new SKPaint { IsAntialias = true });
                tabPaint.Color = ColorScheme.Surface.InterpolateColor(hoverColor, 0.2f);

                var tabRect = new SKRect(x, _titleBarTopDPI + 5, x + width, _titleBarBottomDPI - 7);
                var radius = 12;

                _tempPath.Reset();
                _tempPath.AddRoundRect(tabRect, radius, radius);
                canvas.DrawPath(_tempPath, tabPaint);
            }
            // Draw tab headers
            for (var currentTabIndex = 0; currentTabIndex < _windowPageControl.Count; currentTabIndex++)
            {
                var page = _windowPageControl.GetPageAt(currentTabIndex);
                if (page == null)
                    continue;

                var rect = pageRect[currentTabIndex];
                var closeIconSize = 24 * ScaleFactor;

                if (_drawTabIcons)
                {
                    var baseFont = Font;
                    var font = GetOrCreateFont("tabIcon", () => new SKFont(baseFont.Typeface ?? SKTypeface.Default)
                    {
                        Subpixel = true,
                        Edging = SKFontEdging.SubpixelAntialias,
                        Hinting = SKFontHinting.Full
                    });
                    font.Size = 12f.Topx(this);

                    var textPaint = GetOrCreatePaint("tabText", () => new SKPaint { IsAntialias = true });
                    textPaint.Color = foreColor;

                    var startingIconBounds = new SkiaSharp.SKRect();
                    font.MeasureText("", out startingIconBounds);
                    var iconX = rect.Left + TAB_HEADER_PADDING * ScaleFactor;

                    var inlinePaddingX = startingIconBounds.Width + TAB_HEADER_PADDING * ScaleFactor;
                    var adjustedRect = SKRect.Create(
                        rect.Left + inlinePaddingX,
                        rect.Top,
                        rect.Width - inlinePaddingX - closeIconSize,
                        rect.Height);

                    var textY = _titleBarCenterYDPI + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;
                    TextRenderer.DrawText(canvas, "", iconX, textY, SKTextAlign.Center, font, textPaint);

                    var bounds = new SKRect();
                    font.MeasureText(page.Text, out bounds);
                    var textX = adjustedRect.Left + adjustedRect.Width / 2;
                    TextRenderer.DrawText(canvas, page.Text, textX, textY, SKTextAlign.Center, font, textPaint);
                }
                else
                {
                    var baseFont = Font;
                    var font = GetOrCreateFont("tab", () => new SKFont(baseFont.Typeface ?? SKTypeface.Default)
                    {
                        Subpixel = true,
                        Edging = SKFontEdging.SubpixelAntialias,
                        Hinting = SKFontHinting.Full
                    });
                    font.Size = 9f.Topx(this);

                    var textPaint = GetOrCreatePaint("tabText", () => new SKPaint { IsAntialias = true });
                    textPaint.Color = foreColor;

                    var bounds = new SKRect();
                    font.MeasureText(page.Text, out bounds);
                    var textX = rect.Location.X + rect.Width / 2;
                    var textY = _titleBarCenterYDPI + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;
                    TextRenderer.DrawText(canvas, page.Text, textX, textY, SKTextAlign.Center, font, textPaint);
                }
            }

            // Tab close button
            if (_tabCloseButton)
            {
                var size = 20 * ScaleFactor;
                var closeHoverColor = hoverColor;

                using var buttonPaint = new SKPaint
                {
                    Color = closeHoverColor.WithAlpha((byte)(tabCloseHoverAnimationManager.GetProgress() * 60)),
                    IsAntialias = true
                };

                _closeTabBoxRect = SKRect.Create(
                    x + width - TAB_HEADER_PADDING / 2 - size,
                    _titleBarCenterYDPI - size / 2,
                    size,
                    size);
                var buttonRect = _closeTabBoxRect;

                canvas.DrawCircle(buttonRect.MidX, buttonRect.MidY, size / 2, buttonPaint);

                using var linePaint = new SKPaint
                {
                    Color = foreColor,
                    StrokeWidth = 1f * ScaleFactor,
                    StrokeCap = SKStrokeCap.Round
                };

                size = 4f * ScaleFactor;
                canvas.DrawLine(
                    buttonRect.MidX - size,
                    buttonRect.MidY - size,
                    buttonRect.MidX + size,
                    buttonRect.MidY + size,
                    linePaint);

                canvas.DrawLine(
                    buttonRect.MidX - size,
                    buttonRect.MidY + size,
                    buttonRect.MidX + size,
                    buttonRect.MidY - size,
                    linePaint);
            }

            // New tab button
            if (_newTabButton)
            {
                var size = 24 * ScaleFactor;
                var newHoverColor = hoverColor.WithAlpha(20);

                using var buttonPaint = new SKPaint
                {
                    Color = newHoverColor.WithAlpha((byte)(newTabHoverAnimationManager.GetProgress() *
                                                           newHoverColor.Alpha)),
                    IsAntialias = true
                };

                var lastTabRect = pageRect[pageRect.Count - 1];
                _newTabBoxRect = SKRect.Create(
                    lastTabRect.Left + lastTabRect.Width + size / 2,
                    _titleBarCenterYDPI - size / 2,
                    size,
                    size);
                var buttonRect = _newTabBoxRect;

                using var path = new SKPath();
                path.AddRoundRect(buttonRect, 4, 4);
                canvas.DrawPath(path, buttonPaint);

                using var linePaint = new SKPaint
                {
                    Color = foreColor,
                    StrokeWidth = 1.1f * ScaleFactor,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };

                size = 6 * ScaleFactor;
                canvas.DrawLine(
                    buttonRect.MidX - size,
                    buttonRect.MidY,
                    buttonRect.MidX + size,
                    buttonRect.MidY,
                    linePaint);

                canvas.DrawLine(
                    buttonRect.MidX,
                    buttonRect.MidY - size,
                    buttonRect.MidX,
                    buttonRect.MidY + size,
                    linePaint);
            }
        }

        // Title border
        if (_drawTitleBorder)
        {
            var borderPaint = GetOrCreatePaint("titleBorder", () => new SKPaint
            {
                StrokeWidth = 1,
                IsAntialias = true
            });
            borderPaint.Color = titleColor != SKColor.Empty
                ? titleColor.Determine().WithAlpha(30)
                : ColorScheme.BorderColor;

            canvas.DrawLine(Width, _titleBarBottomDPI - 1, 0, _titleBarBottomDPI - 1, borderPaint);
        }
    }

    internal override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        CalcSystemBoxPos();
        NeedsFullChildRedraw = true;

        base.OnSizeChanged(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        CalcSystemBoxPos();

        // Trigger initial layout with current DPI
        InvalidateMeasureRecursive();
        PerformLayout();
        Invalidate();
    }

    private void UpdateTabRects()
    {
        // reuse existing list to avoid allocating every time
        if (pageRect == null)
            pageRect = new List<SKRect>();
        else
            pageRect.Clear();

        if (_windowPageControl == null || _windowPageControl.Count == 0)
            return;

        var leadingInset = _titleBarLeftInsetDPI;
        var trailingInset = _titleBarRightInsetDPI;
        // determine if we actually need the large left inset that was previously hard-coded to 44*scale.
        // reserve extra space only when there is an icon or the "menu" glyph.
        bool leftGroupVisible = showMenuInsteadOfIcon || (ShowIcon && Icon != null);
        var initialOffset = leftGroupVisible ? 44 * ScaleFactor : 0;

        var occupiedWidth = initialOffset + leadingInset + trailingInset;

        if (controlBox)
            occupiedWidth += _controlBoxRect.Width;

        if (MinimizeBox)
            occupiedWidth += _minimizeBoxRect.Width;

        if (MaximizeBox)
            occupiedWidth += _maximizeBoxRect.Width;

        if (ExtendBox)
            occupiedWidth += _extendBoxRect.Width;

        occupiedWidth += 30 * ScaleFactor;

        var availableWidth = Width - occupiedWidth;
        var maxSize = 250f * ScaleFactor;

        var baseFont = Font;
        var font = GetOrCreateFont("tabMeasure", () => new SKFont(baseFont.Typeface ?? SKTypeface.Default)
        {
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias,
            Hinting = SKFontHinting.Full
        });
        font.Size = (_drawTabIcons ? 12f : 9f).Topx(this);

        // reuse buffer to avoid allocation every layout pass
        _tabWidthBuffer.Clear();
        var desiredWidths = _tabWidthBuffer;
        float totalDesiredWidth = 0;

        for (var pageIndex = 0; pageIndex < _windowPageControl.Count; pageIndex++)
        {
            var page = _windowPageControl.GetPageAt(pageIndex);
            if (page == null)
                continue;

            var bounds = new SkiaSharp.SKRect();
            font.MeasureText(page.Text ?? "", out bounds);

            var width = bounds.Width + (20 * ScaleFactor);

            if (_drawTabIcons)
                width += 30 * ScaleFactor;

            if (_tabCloseButton)
                width += 24 * ScaleFactor;

            desiredWidths.Add(width);
            totalDesiredWidth += width;
        }

        float scale = 1.0f;
        float extraPerTab = 0;

        if (totalDesiredWidth > availableWidth && totalDesiredWidth > 0)
        {
            scale = availableWidth / totalDesiredWidth;
        }
        else if (totalDesiredWidth < availableWidth && _windowPageControl.Count > 0)
        {
            var extra = availableWidth - totalDesiredWidth;
            extraPerTab = extra / _windowPageControl.Count;
        }

        var currentX = leadingInset + (leftGroupVisible ? 44 * ScaleFactor : 0);

        for (int i = 0; i < desiredWidths.Count; i++)
        {
            var finalWidth = (desiredWidths[i] * scale) + extraPerTab;

            if (finalWidth > maxSize)
                finalWidth = maxSize;

            pageRect.Add(SKRect.Create(currentX, _titleBarTopDPI, finalWidth, _titleHeightDPI));
            currentX += finalWidth;
        }
    }

    // optimization helpers --------------------------------------------------

    /// <summary>
    /// Lightweight factory for hover‑style animation managers.
    /// </summary>
    private AnimationManager CreateHoverAnimation(double increment = HOVER_ANIMATION_SPEED)
    {
        var m = new AnimationManager
        {
            Increment = increment,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };
        m.OnAnimationProgress += _ =>
        {
            if (IsHandleCreated && Visible && WindowState != FormWindowState.Minimized)
                Invalidate();
        };
        return m;
    }

    /// <summary>
    /// Retrieve or create an <see cref="SKPaint"/> from the per-window cache.
    /// Properties may be modified by the caller before use.
    /// </summary>
    private SKPaint GetOrCreatePaint(string key, Func<SKPaint> factory)
    {
        if (_paintCache.TryGetValue(key, out var paint))
            return paint;
        paint = factory();
        _paintCache[key] = paint;
        return paint;
    }

    /// <summary>
    /// Retrieve or create an <see cref="SKFont"/> from the per-window cache.
    /// Cached fonts are reused across paints; callers may alter size/color after retrieval.
    /// </summary>
    private SKFont GetOrCreateFont(string key, Func<SKFont> factory)
    {
        if (_fontCache.TryGetValue(key, out var font))
            return font;
        font = factory();
        _fontCache[key] = font;
        return font;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ColorScheme.ThemeChanged -= OnThemeChanged;

            for (var i = 0; i < _hoverAnimationManagers.Count; i++)
                _hoverAnimationManagers[i]?.Dispose();
            _hoverAnimationManagers.Clear();

            foreach (var paint in _paintCache.Values)
                paint.Dispose();
            _paintCache.Clear();

            foreach (var font in _fontCache.Values)
                font.Dispose();
            _fontCache.Clear();

            _tempPath.Dispose();
        }

        base.Dispose(disposing);
    }
}
