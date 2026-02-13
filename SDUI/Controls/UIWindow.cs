using SDUI.Animation;
using SDUI.Extensions;
using SDUI.Helpers;
using SDUI.Native.Windows;
using SDUI.Rendering;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Timers;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Controls;


public partial class UIWindow : UIWindowBase
{
    public enum TabDesingMode
    {
        Rectangle,
        Rounded,
        Chromed
    }

    private const int TAB_HEADER_PADDING = 9;
    private const int TAB_INDICATOR_HEIGHT = 3;

    private const float HOVER_ANIMATION_SPEED = 0.1f;
    private const float HOVER_ANIMATION_OPACITY = 0.4f;

    // Hot-path caches (avoid per-frame LINQ allocations)
    private readonly List<ElementBase> _frameElements = new();
    private readonly List<ElementBase> _hitTestElements = new();
    private readonly Dictionary<string, SKPaint> _paintCache = new();
    private readonly object _softwareCacheLock = new();
    private readonly List<ZOrderSortItem> _zOrderSortBuffer = new();

    /// <summary>
    ///     Close tab hover animation manager
    /// </summary>
    private readonly AnimationManager closeBoxHoverAnimationManager;

    /// <summary>
    ///     Whether to display the control buttons of the form
    /// </summary>
    private readonly bool controlBox = true;

    /// <summary>
    ///     Min Box hover animation manager
    /// </summary>
    private readonly AnimationManager extendBoxHoverAnimationManager;

    /// <summary>
    ///     tab area animation manager
    /// </summary>
    private readonly AnimationManager formMenuHoverAnimationManager;

    /// <summary>
    ///     Min Box hover animation manager
    /// </summary>
    private readonly AnimationManager maxBoxHoverAnimationManager;

    /// <summary>
    ///     Min Box hover animation manager
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

    private SKBitmap _cacheBitmap;
    private SKSurface _cacheSurface;

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

    private Cursor _currentCursor;

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
    private SKColor[] _gradient = new[] { SKColors.Transparent, SKColors.Transparent };

    private HatchStyle _hatch = HatchStyle.Percent80;

    private float _iconWidth = 42;

    private Timer? _idleMaintenanceTimer;

    private bool _inCloseBox, _inMaxBox, _inMinBox, _inExtendBox, _inTabCloseBox, _inNewTabBox, _inFormMenuBox;

    /// <summary>
    ///     Is form active <c>true</c>; otherwise <c>false</c>
    /// </summary>
    private bool _isActive;

    private ElementBase _lastHoveredElement;

    private int _layoutSuspendCount;

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

    private int _maxZOrder;

    /// <summary>
    ///     Whether to show the minimize button of the form
    /// </summary>
    private bool _minimizeBox = true;

    /// <summary>
    ///     The rectangle of minimize box
    /// </summary>
    private SkiaSharp.SKRect _minimizeBoxRect;

    // Element that has explicitly captured mouse input (via SetMouseCapture)
    private ElementBase? _mouseCapturedElement;

    /// <summary>
    ///     The position of the mouse when the left mouse button is pressed
    /// </summary>
    private SKPoint _mouseOffset;

    private bool _needsFullRedraw = true;

    /// <summary>
    ///     The rectangle of extend box
    /// </summary>
    private SkiaSharp.SKRect _newTabBoxRect;

    private bool _newTabButton;
    private long _perfLastTimestamp;
    private double _perfSmoothedFrameMs;

    private RenderBackend _renderBackend = RenderBackend.Software;
    private IWindowRenderer? _renderer;

    private bool _showPerfOverlay = true;

    /// <summary>
    ///     The size of the window before it is maximized
    /// </summary>
    private SKSize _sizeOfBeforeMaximized;
    private SKPoint _locationOfBeforeMaximized;

    // Prevent Invalidate()->Update() storms in Software backend
    private bool _softwareUpdateQueued;

    private long _stickyBorderTime = 5000000;
    private int _suppressImmediateUpdateCount;


    private float _symbolSize = 24;

    private bool _tabCloseButton;

    /// <summary>
    ///     Tab desing mode
    /// </summary>
    private TabDesingMode _tabDesingMode = TabDesingMode.Rectangle;

    /// <summary>
    ///     The title height
    /// </summary>
    private float _titleHeight = 32;

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
    public UIWindow()
    {
        AutoScaleMode = AutoScaleMode.None;

        // WinForms double-buffering can cause visible flicker when the window is presented by
        // a GPU swapchain (OpenGL/DX). Keep it for software, disable it for GPU backends.
        ApplyRenderStyles();
        enableFullDraggable = false;

        pageAreaAnimationManager = new AnimationManager
        {
            AnimationType = AnimationType.EaseOut,
            Increment = 0.07,
            Singular = true,
            InterruptAnimation = true
        };

        minBoxHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        maxBoxHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        closeBoxHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        extendBoxHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        tabCloseHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        newTabHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

        formMenuHoverAnimationManager = new AnimationManager
        {
            Increment = HOVER_ANIMATION_SPEED,
            AnimationType = AnimationType.EaseInOut,
            Singular = true,
            InterruptAnimation = true
        };

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

    private float _titleHeightDPI => _titleHeight * ScaleFactor;
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

    [DefaultValue(false)] public bool AllowAddControlOnTitle { get; set; }

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
    ///     Gets or sets whether to show the title bar of the form
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

    public new ContextMenuStrip ContextMenuStrip { get; set; }

    public SKSize CanvasSize =>
        _cacheBitmap == null ? SKSize.Empty : new SKSize(_cacheBitmap.Width, _cacheBitmap.Height);

    public ElementBase LastHoveredElement
    {
        get => _lastHoveredElement;
        internal set
        {
            if (_lastHoveredElement != value)
            {
                _lastHoveredElement = value;
                UpdateCursor(value);
            }
        }
    }

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

    /// <summary>
    ///     Maximum retained bytes for the software backbuffer (SKBitmap + SKSurface + GDI Bitmap wrapper).
    ///     This prevents 4K/8K windows from permanently retaining very large pixel buffers.
    ///     Set to 0 (or less) to disable the limit (unlimited).
    /// </summary>
    public static long MaxSoftwareBackBufferBytes { get; set; } = 24L * 1024 * 1024;

    public static bool EnableIdleMaintenance { get; set; } = true;

    /// <summary>
    ///     Delay (ms) after the last repaint request before trimming retained backbuffers and
    ///     asking Skia to purge resource caches.
    /// </summary>
    public static int IdleMaintenanceDelayMs { get; set; } = 1500;

    public static bool PurgeSkiaResourceCacheOnIdle { get; set; } = true;

    [DefaultValue(false)]
    [Description("Shows a small FPS/frame-time overlay for measuring renderer performance.")]
    public bool ShowPerfOverlay
    {
        get => _showPerfOverlay;
        set
        {
            if (_showPerfOverlay == value)
                return;
            _showPerfOverlay = value;
            _perfLastTimestamp = 0;
            _perfSmoothedFrameMs = 0;
            Invalidate();
        }
    }

    [DefaultValue(RenderBackend.Software)]
    [Description(
        "Selects how UIWindow presents frames: Software (GDI), OpenGL, or DirectX11 (DXGI/GDI-compatible swapchain).")]
    public RenderBackend RenderBackend
    {
        get => _renderBackend;
        set
        {
            if (_renderBackend == value)
                return;

            _renderBackend = value;
            ApplyRenderStyles();
            RecreateRenderer();
            Invalidate();
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;

            if (_renderBackend != RenderBackend.Software)
            {
                cp.Style |= (int)(SetWindowLongFlags.WS_CLIPCHILDREN |
                                  SetWindowLongFlags.WS_CLIPSIBLINGS);
                // WS_EX_NOREDIRECTIONBITMAP helps some WGL/SwapBuffers flicker scenarios,
                // but can interfere with DXGI swapchains. Apply only for OpenGL.
                if (_renderBackend == RenderBackend.OpenGL)
                    cp.ExStyle |= (uint)SetWindowLongFlags.WS_EX_NOREDIRECTIONBITMAP;
                cp.ExStyle &= ~(uint)SetWindowLongFlags.WS_EX_COMPOSITED;
            }

            return cp;
        }
    }

    public SKRectI MaximizedBounds { get; private set; }

    public override void Invalidate()
    {
        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        if (_renderBackend == RenderBackend.Software)
        {
            if (_suppressImmediateUpdateCount <= 0 && ShouldForceSoftwareUpdate())
                QueueSoftwareUpdate();
            else
                InvalidateWindow();
        }
        else
        {
            InvalidateWindow();
        }
    }

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

    internal override void OnDpiChanged(float newDpi, float oldDpi)
    {
        try
        {
            base.OnDpiChanged(newDpi, oldDpi);

            if (newDpi == oldDpi)
                return;

            _suppressImmediateUpdateCount++;

            SKRect scaledRect = new SKRect(
                Bounds.Left * newDpi,
                Bounds.Top * newDpi,
                Bounds.Right * newDpi,
                Bounds.Bottom * newDpi
            );

            if (Bounds == scaledRect)
                return;

            Bounds = scaledRect;

            // Invalidate measurements recursively before DPI notification
            InvalidateMeasureRecursive();

            foreach (ElementBase element in Controls)
                element.OnDpiChanged(newDpi, oldDpi);

            // Invalidate layout measurements on DPI change
            PerformLayout();

            _needsFullRedraw = true;
            Invalidate();
        }
        finally
        {
            _suppressImmediateUpdateCount--;
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

    public override void SetMouseCapture(ElementBase element)
    {
        _mouseCapturedElement = element;
        SetCapture(Handle);
    }

    public override void ReleaseMouseCapture(ElementBase element)
    {
        if (_mouseCapturedElement == element)
        {
            _mouseCapturedElement = null;
            ReleaseCapture();
        }
    }

    private bool ShouldForceSoftwareUpdate()
    {
        // Only force synchronous Update() when we need smooth, real-time visuals.
        // Otherwise let WinForms coalesce paints to keep idle CPU low.
        if (_suppressImmediateUpdateCount > 0)
            return false;

        if (_showPerfOverlay)
            return true;

        return pageAreaAnimationManager.Running
               || minBoxHoverAnimationManager.Running
               || maxBoxHoverAnimationManager.Running
               || closeBoxHoverAnimationManager.Running
               || extendBoxHoverAnimationManager.Running
               || tabCloseHoverAnimationManager.Running
               || newTabHoverAnimationManager.Running
               || formMenuHoverAnimationManager.Running;
    }

    private void EnsureIdleMaintenanceTimer()
    {
        if (!EnableIdleMaintenance)
            return;

        if (_idleMaintenanceTimer == null)
        {
            _idleMaintenanceTimer = new Timer();
            _idleMaintenanceTimer.Interval = IdleMaintenanceDelayMs;
            _idleMaintenanceTimer.Elapsed += IdleMaintenanceTimer_Tick;
        }
    }

    private void ArmIdleMaintenance()
    {
        if (!EnableIdleMaintenance)
            return;

        EnsureIdleMaintenanceTimer();
        if (_idleMaintenanceTimer != null)
        {
            _idleMaintenanceTimer.Stop();
            _idleMaintenanceTimer.Start();
        }
    }

    private void IdleMaintenanceTimer_Tick(object? sender, EventArgs e)
    {
        _idleMaintenanceTimer?.Stop();

        // 1. Trim renderer caches (DirectX / OpenGL)
        _renderer?.TrimCaches();

        // 2. Trim software backbuffer if using software rendering
        if (_renderer == null)
        {
            DisposeSoftwareBackBuffer();
            _needsFullRedraw = true;
        }

        // 3. Purge global Skia resource cache if requested
        if (PurgeSkiaResourceCacheOnIdle) SKGraphics.PurgeResourceCache();
    }

    private void QueueSoftwareUpdate()
    {
        if (_softwareUpdateQueued)
            return;

        if (!IsHandleCreated || IsDisposed || Disposing)
            return;

        _softwareUpdateQueued = true;
        try
        {
            BeginInvoke((Action)(() =>
            {
                _softwareUpdateQueued = false;
                if (!IsHandleCreated || IsDisposed || Disposing)
                    return;

                if (_renderBackend == RenderBackend.Software)
                    InvalidateWindow(); // ✅ Use native invalidate to avoid recursion
            }));
        }
        catch
        {
            _softwareUpdateQueued = false;
        }
    }

    private void StableSortByZOrderAscending(List<ElementBase> list)
    {
        _zOrderSortBuffer.Clear();
        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];
            _zOrderSortBuffer.Add(new ZOrderSortItem(element, element.ZOrder, i));
        }

        _zOrderSortBuffer.Sort(static (a, b) =>
        {
            var cmp = a.ZOrder.CompareTo(b.ZOrder);
            return cmp != 0 ? cmp : a.Sequence.CompareTo(b.Sequence);
        });

        for (var i = 0; i < list.Count; i++)
            list[i] = _zOrderSortBuffer[i].Element;
    }

    private void StableSortByZOrderDescending(List<ElementBase> list)
    {
        _zOrderSortBuffer.Clear();
        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];
            _zOrderSortBuffer.Add(new ZOrderSortItem(element, element.ZOrder, i));
        }

        _zOrderSortBuffer.Sort(static (a, b) =>
        {
            var cmp = b.ZOrder.CompareTo(a.ZOrder);
            return cmp != 0 ? cmp : a.Sequence.CompareTo(b.Sequence);
        });

        for (var i = 0; i < list.Count; i++)
            list[i] = _zOrderSortBuffer[i].Element;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        //ApplyRenderStyles();
        RecreateRenderer();

        // Initial DPI sync: Ensure controls match the window's actual DPI 
        // (which might differ from System DPI captured during initialization).
        var dpi = Screen.GetDpiForWindowHandle(Handle);
        foreach (var control in Controls.OfType<ElementBase>())
        {
            var oldDpi = control.ScaleFactor * 96f;
            if (Math.Abs(oldDpi - dpi) > 0.001f)
            {
                control.OnDpiChanged(dpi, oldDpi);
            }
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        try
        {
            _renderer?.Dispose();
        }
        finally
        {
            _renderer = null;
            base.OnHandleDestroyed(e);
        }
    }

    private void RecreateRenderer()
    {
        if (IsDisposed || Disposing)
            return;

        _renderer?.Dispose();
        _renderer = null;

        if (!IsHandleCreated)
            return;

        if (_renderBackend == RenderBackend.Software)
            return;

        try
        {
            _renderer = _renderBackend switch
            {
                RenderBackend.OpenGL => new OpenGlWindowRenderer(),
                RenderBackend.DirectX11 => new DirectX11WindowRenderer(),
                _ => null
            };

            _renderer?.Initialize(Handle);
            _renderer?.Resize((int)ClientSize.Width, (int)ClientSize.Height);
        }
        catch
        {
            _renderer?.Dispose();
            _renderer = null;
            _renderBackend = RenderBackend.Software;
        }
    }

    private static MouseEventArgs CreateChildMouseEvent(MouseEventArgs source, ElementBase element)
    {
        var elementWindowRect = GetWindowRelativeBoundsStatic(element);
        return new MouseEventArgs(
            source.Button,
            source.Clicks,
            source.X - (int)elementWindowRect.Location.X,
            source.Y - (int)elementWindowRect.Location.Y,
            source.Delta);
    }

    private static SkiaSharp.SKRect GetWindowRelativeBoundsStatic(ElementBase element)
    {
        if (element?.Parent == null)
            return SKRect.Create(element?.Location ?? SKPoint.Empty, element?.Size ?? SKSize.Empty);

        if (element.Parent is UIWindowBase window && !window.IsDisposed)
        {
            var screenLoc = element.PointToScreen(SKPoint.Empty);
            var clientLoc = window.PointToClient(screenLoc);
            return SKRect.Create(clientLoc, element.Size);
        }

        if (element.Parent is ElementBase parentElement)
        {
            var screenLoc = element.PointToScreen(SKPoint.Empty);
            // Pencereyi zincirden bul
            UIWindowBase parentWindow = null;
            var current = parentElement;
            while (current != null && parentWindow == null)
            {
                if (current.Parent is UIWindowBase w)
                {
                    parentWindow = w;
                    break;
                }

                current = current.Parent as ElementBase;
            }

            if (parentWindow != null)
            {
                var clientLoc = parentWindow.PointToClient(screenLoc);
                return SKRect.Create(clientLoc, element.Size);
            }
        }

        return SKRect.Create(element.Location, element.Size);
    }

    private void CreateOrUpdateCache(SKImageInfo info)
    {
        if (info.Width <= 0 || info.Height <= 0)
        {
            DisposeSoftwareBackBuffer();
            return;
        }

        if (_cacheBitmap == null || _cacheBitmap.Width != info.Width || _cacheBitmap.Height != info.Height)
        {
            _cacheSurface?.Dispose();
            _cacheBitmap?.Dispose();

            _cacheBitmap = new SKBitmap(info);
            var pixels = _cacheBitmap.GetPixels();
            _cacheSurface = SKSurface.Create(info, pixels, _cacheBitmap.RowBytes);
            _needsFullRedraw = true;
        }
    }

    private static long EstimateBackBufferBytes(SKImageInfo info)
    {
        // Estimate: BGRA8888/RGBA8888 => 4 bytes per pixel
        var bytes = (long)info.Width * info.Height * 4;
        return bytes > 0 ? bytes : 0;
    }

    private static bool ShouldCacheSoftwareBackBuffer(SKImageInfo info)
    {
        var maxBytes = MaxSoftwareBackBufferBytes;
        if (maxBytes <= 0)
            return true;

        var estimated = EstimateBackBufferBytes(info);
        return estimated > 0 && estimated <= maxBytes;
    }

    private void DisposeSoftwareBackBuffer()
    {
        _cacheSurface?.Dispose();
        _cacheSurface = null;

        _cacheBitmap?.Dispose();
        _cacheBitmap = null;
    }

    // REMOVED: RenderSoftwareFrameUncached - using UIWindowBase native rendering

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

    private void CalcSystemBoxPos()
    {
        _controlBoxLeft = Width;

        if (controlBox)
        {
            _controlBoxRect = SKRect.Create(Width - _iconWidthDPI, 0, _iconWidthDPI, _titleHeightDPI);
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
        _formMenuRect = SKRect.Create(10, _titleHeightDPI / 2 - titleIconSize / 2, titleIconSize, titleIconSize);

        Padding = new Thickness(Padding.Left, (int)(showTitle ? _titleHeightDPI : 0), Padding.Right, Padding.Bottom);
    }

    private void BuildHitTestList(bool requireEnabled)
    {
        _hitTestElements.Clear();
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is not ElementBase element)
                continue;
            if (!element.Visible)
                continue;
            if (requireEnabled && !element.Enabled)
                continue;
            _hitTestElements.Add(element);
        }

        // Stable ordering prevents subtle behavior changes when ZOrder ties exist.
        StableSortByZOrderDescending(_hitTestElements);
    }

    protected internal override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        BuildHitTestList(true);
        for (var i = 0; i < _hitTestElements.Count; i++)
        {
            var element = _hitTestElements[i];
            if (!GetWindowRelativeBoundsStatic(element).Contains(e.Location))
                continue;

            var localEvent = CreateChildMouseEvent(e, element);
            element.OnMouseClick(localEvent);
            break;
        }

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

        if (pageRect == null)
            UpdateTabRects();

        if (_formMoveMouseDown && !CursorScreenPosition.Equals(_mouseOffset))
            return;

        for (var i = 0; i < pageRect.Count; i++)
            if (pageRect[i].Contains(e.Location))
            {
                _windowPageControl.SelectedIndex = i;

                if (_tabCloseButton && e.Button == MouseButtons.Middle)
                    OnCloseTabBoxClick?.Invoke(null, i);

                break;
            }
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        // Make sure this Form receives keyboard input.
        if (CanFocus)
            Focus();

        var elementClicked = false;
        // Z-order'a g�re tersten kontrol et (�stteki elementten ba�la)
        BuildHitTestList(true);
        for (var i = 0; i < _hitTestElements.Count; i++)
        {
            var element = _hitTestElements[i];
            if (!GetWindowRelativeBoundsStatic(element).Contains(e.Location))
                continue;

            elementClicked = true;

            // If the element (or its descendants) doesn't set focus, fall back to focusing this element.
            var prevFocus = FocusedElement;

            var localEvent = CreateChildMouseEvent(e, element);
            element.OnMouseDown(localEvent);

            if (FocusedElement == prevFocus)
            {
                static bool IsDescendantOf(ElementBase? maybeChild, ElementBase ancestor)
                {
                    var current = maybeChild;
                    while (current != null)
                    {
                        if (ReferenceEquals(current, ancestor))
                            return true;
                        current = current.Parent as ElementBase;
                    }

                    return false;
                }

                // If focus stayed on an existing descendant (common when clicking inside an already-focused TextBox),
                // don't steal focus back to the container.
                if (prevFocus == null || !IsDescendantOf(prevFocus, element))
                    FocusedElement = element;
            }

            // T�klanan elementi en �ste getir
            BringToFront(element);
            break; // �lk t�klanan elementten sonra di�erlerini kontrol etmeye gerek yok
        }

        if (!elementClicked)
        {
            FocusManager.SetFocus(null);
            FocusedElement = null;
        }

        if (enableFullDraggable && e.Button == MouseButtons.Left)
            //right = e.Button == MouseButtons.Right;
            //location = e.Location;
            DragForm(Handle);

        // NOTE: Window context menus should open on MouseUp (standard behavior).
        // Showing on MouseDown can lead to double menus when the mouse moves slightly
        // and an element handles right-click on MouseUp.

        if (_inCloseBox || _inMaxBox || _inMinBox || _inExtendBox || _inTabCloseBox || _inNewTabBox || _inFormMenuBox)
            return;

        if (!ShowTitle)
            return;

        if (e.Y > Padding.Top)
            return;

        if (e.Button == MouseButtons.Left && Movable)
        {
            _formMoveMouseDown = true;
            _dragStartLocation = Location;
            _mouseOffset = CursorScreenPosition;
            SetCapture(Handle);
        }
    }

    internal override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        var elementClicked = false;
        // Z-order'a g�re tersten kontrol et (�stteki elementten ba�la)
        BuildHitTestList(true);
        for (var i = 0; i < _hitTestElements.Count; i++)
        {
            var element = _hitTestElements[i];
            if (!GetWindowRelativeBoundsStatic(element).Contains(e.Location))
                continue;

            elementClicked = true;

            var localEvent = CreateChildMouseEvent(e, element);
            element.OnMouseDoubleClick(localEvent);
            // T�klanan elementi en �ste getir
            BringToFront(element);
            break; // �lk t�klanan elementten sonra di�erlerini kontrol etmeye gerek yok
        }

        if (!elementClicked)
        {
            FocusManager.SetFocus(null);
            FocusedElement = null;
        }

        if (!MaximizeBox)
            return;

        var inCloseBox = _controlBoxRect.Contains(e.Location);
        var inMaxBox = _maximizeBoxRect.Contains(e.Location);
        var inMinBox = _minimizeBoxRect.Contains(e.Location);
        var inExtendBox = _extendBoxRect.Contains(e.Location);
        var inCloseTabBox = _tabCloseButton && _closeTabBoxRect.Contains(e.Location);
        var inNewTabBox = _newTabButton && _newTabBoxRect.Contains(e.Location);
        var inFormMenuBox = _formMenuRect.Contains(e.Location);

        if (inCloseBox || inMaxBox || inMinBox || inExtendBox || inCloseTabBox || inNewTabBox || inFormMenuBox)
            return;

        if (!ShowTitle)
            return;

        if (e.Y > Padding.Top)
            return;

        ShowMaximize();
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        // If an element captured the mouse, forward the mouse up to it and release capture if left button
        if (_mouseCapturedElement != null)
        {
            var captured = _mouseCapturedElement;
            var bounds = GetWindowRelativeBoundsStatic(captured);
            var localEvent = new MouseEventArgs(e.Button, e.Clicks, (int)(e.X - bounds.Left), (int)(e.Y - bounds.Top), e.Delta);
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
        if (_formMoveMouseDown)
            ReleaseCapture();
        _formMoveMouseDown = false;

        animationSource = e.Location;

        // Z-order'a g�re tersten kontrol et
        var elementClicked = false;
        ElementBase? hitElement = null;
        BuildHitTestList(true);
        for (var i = 0; i < _hitTestElements.Count; i++)
        {
            var element = _hitTestElements[i];
            if (!GetWindowRelativeBoundsStatic(element).Contains(e.Location))
                continue;

            elementClicked = true;
            hitElement = element;
            var localEvent = CreateChildMouseEvent(e, element);
            element.OnMouseUp(localEvent);
            break;
        }

        if (e.Button == MouseButtons.Right && ContextMenuStrip != null)
        {
            static bool HasContextMenuInChain(ElementBase? start)
            {
                var current = start;
                while (current != null)
                {
                    if (current.ContextMenuStrip != null)
                        return true;
                    current = current.Parent as ElementBase;
                }

                return false;
            }

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
        // If an element has captured mouse, forward all mouse move events to it (so dragging continues even when cursor leaves its bounds)
        if (_mouseCapturedElement != null)
        {
            var captured = _mouseCapturedElement;
            var bounds = GetWindowRelativeBoundsStatic(captured);
            var localEvent = new MouseEventArgs(e.Button, e.Clicks, (int)(e.X - bounds.Left), (int)(e.Y - bounds.Top), e.Delta);
            captured.OnMouseMove(localEvent);
            return;
        }

        var screenCursor = CursorScreenPosition;
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
            var inFormMenuBox = _formMenuRect.Contains(e.Location.X, e.Location.Y);
            var inCloseTabBox = _tabCloseButton && _closeTabBoxRect.Contains(e.Location.X, e.Location.Y);
            var inNewTabBox = _newTabButton && _newTabBoxRect.Contains(e.Location.X, e.Location.Y);

            var isChange = false;

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

        ElementBase hoveredElement = null;

        // Z-order'a g�re tersten kontrol et
        foreach (var element in Controls.OfType<ElementBase>().OrderByDescending(el => el.ZOrder)
                     .Where(el => el.Visible && el.Enabled))
            if (GetWindowRelativeBoundsStatic(element).Contains(e.Location))
            {
                hoveredElement = element;
                var localEvent = CreateChildMouseEvent(e, element);
                element.OnMouseMove(localEvent);
                break; // �lk hover edilen elementten sonra di�erlerini kontrol etmeye gerek yok
            }

        // Cursor should reflect the deepest hovered child (e.g., TextBox -> IBeam)
        var cursorElement = hoveredElement;
        while (cursorElement?.LastHoveredElement != null)
            cursorElement = cursorElement.LastHoveredElement;
        UpdateCursor(cursorElement);

        if (hoveredElement != _lastHoveredElement)
        {
            _lastHoveredElement?.OnMouseLeave(EventArgs.Empty);
            hoveredElement?.OnMouseEnter(EventArgs.Empty);
            LastHoveredElement = hoveredElement;
        }

        base.OnMouseMove(e);
    }

    internal override void OnMouseLeave(EventArgs e)
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

        _lastHoveredElement?.OnMouseLeave(e);
        LastHoveredElement = null;

        Invalidate();
    }

    internal override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);

        // Z-order'a g�re tersten kontrol et
        foreach (var element in Controls.OfType<ElementBase>().OrderByDescending(el => el.ZOrder)
                     .Where(el => el.Visible && el.Enabled))
        {
            var mousePos = PointToClient(MousePosition);
            if (GetWindowRelativeBoundsStatic(element).Contains(mousePos))
            {
                element.OnMouseEnter(e);
                break;
            }
        }
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        // Mouse pozisyonunu window client koordinatlar�na �evir
        var mousePos = PointToClient(MousePosition);

        // Recursive olarak do�ru child'� bul ve wheel olay�n� ilet
        if (PropagateMouseWheel(Controls.OfType<ElementBase>(), mousePos, e))
            return; // Event i�lendi
    }

    /// <summary>
    ///     Recursive olarak child elementlere mouse wheel olay�n� iletir
    /// </summary>
    private bool PropagateMouseWheel(IEnumerable<ElementBase> elements, SKPoint windowMousePos, MouseEventArgs e)
    {
        // Z-order'a g�re tersten kontrol et - en �stteki element �nce
        foreach (var element in elements.OrderByDescending(el => el.ZOrder).Where(el => el.Visible && el.Enabled))
        {
            var elementBounds = GetWindowRelativeBoundsStatic(element);
            if (!elementBounds.Contains(windowMousePos))
                continue;

            // �nce bu elementin child'lar�n� kontrol et (daha spesifik -> daha genel)
            if (element.Controls != null && element.Controls.Count > 0)
            {
                var childElements = element.Controls.OfType<ElementBase>();
                if (PropagateMouseWheel(childElements, windowMousePos, e))
                    return true; // Child i�ledi
            }

            // Child i�lemediyse bu elemente g�nder
            var localEvent = new MouseEventArgs(
                e.Button,
                e.Clicks,
                (int)windowMousePos.X - (int)elementBounds.Left,
                (int)windowMousePos.Y - (int)elementBounds.Top,
                e.Delta);

            element.OnMouseWheel(localEvent);
            return true; // Event i�lendi
        }

        return false; // Hi�bir element i�lemedi
    }

    private void ShowMaximize(bool IsOnMoving = false)
    {
        var screen = Screen.FromPoint(MousePosition);
        base.MaximumSize = screen.WorkingArea.Size;
        if (screen.IsPrimary)
            MaximizedBounds = screen.WorkingArea;
        else
            MaximizedBounds = SKRectI.Empty;

        if (WindowState == FormWindowState.Normal)
        {
            _sizeOfBeforeMaximized = Size;
            _locationOfBeforeMaximized = IsOnMoving ? _dragStartLocation : Location;
            WindowState = FormWindowState.Maximized;
        }
        else if (WindowState == FormWindowState.Maximized)
        {
            if (_sizeOfBeforeMaximized.Width == 0 || _sizeOfBeforeMaximized.Height == 0)
            {
                var w = 800;
                if (MinimumSize.Width > 0) w = (int)MinimumSize.Width;
                var h = 600;
                if (MinimumSize.Height > 0) h = (int)MinimumSize.Height;
                _sizeOfBeforeMaximized = new SKSize(w, h);
            }

            Size = _sizeOfBeforeMaximized;
            if (_locationOfBeforeMaximized.X == 0 && _locationOfBeforeMaximized.Y == 0)
                _locationOfBeforeMaximized = new SKPoint(
                    screen.Bounds.Left + screen.Bounds.Width / 2 - _sizeOfBeforeMaximized.Width / 2,
                    screen.Bounds.Top + screen.Bounds.Height / 2 - _sizeOfBeforeMaximized.Height / 2);

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

    // REMOVED: NotifyInvalidate - method doesn't exist in UIWindowBase

    // REMOVED: WndProc - using UIWindowBase native WndProc instead

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        var clientArea = ClientRectangle;
        var clientPadding = Padding;

        var adjustedArea = SKRect.Create(
            clientArea.Left + clientPadding.Left,
            clientArea.Top + clientPadding.Top,
            clientArea.Width - clientPadding.Horizontal,
            clientArea.Height - clientPadding.Vertical);

        var remainingArea = adjustedArea;

        // WinForms dock order: Reverse z-order (last added first) in a single pass
        // This matches WinForms DefaultLayout behavior where docking is z-order dependent
        // and processed in reverse (children.Count - 1 down to 0)
        for (var i = Controls.Count - 1; i >= 0; i--)
            if (Controls[i] is ElementBase control && control.Visible)
                PerformDefaultLayout(control, adjustedArea, ref remainingArea);
    }

    protected override void OnPaintCanvas(SKCanvas canvas, SKImageInfo info)
    {
        base.OnPaintCanvas(canvas, info);
        
        if (_renderBackend != RenderBackend.Software && _renderer != null)
        {
            try
            {
                _renderer.Render((int)info.Width, (int)info.Height, RenderScene);
                ArmIdleMaintenance();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UIWindow] Hardware rendering failed ({ex.GetType().Name}). Falling back to Software. Error: {ex.Message}");
                _renderer?.Dispose();
                _renderer = null;
                _renderBackend = RenderBackend.Software;
            }
        }

        // Software rendering path - use provided canvas
        RenderScene(canvas, info);
        ArmIdleMaintenance();
    }

    private void ApplyRenderStyles()
    {
        var gpu = _renderBackend != RenderBackend.Software;

        ApplyNativeWindowStyles(gpu);
    }

    private void ApplyNativeWindowStyles(bool gpu)
    {
        if (!IsHandleCreated)
            return;

        var hwnd = Handle;

        // Window styles
        var stylePtr = GetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE);
        var style = stylePtr;
        var clipFlags = (nint)(uint)(SetWindowLongFlags.WS_CLIPCHILDREN |
                                     SetWindowLongFlags.WS_CLIPSIBLINGS);
        style = gpu ? style | clipFlags : style & ~clipFlags;

        // Extended styles
        var exStylePtr = GetWindowLong(hwnd, WindowLongIndexFlags.GWL_EXSTYLE);
        var exStyle = exStylePtr;
        var noRedirect = (nint)(uint)SetWindowLongFlags.WS_EX_NOREDIRECTIONBITMAP;
        var composited = (nint)(uint)SetWindowLongFlags.WS_EX_COMPOSITED;
        if (gpu)
        {
            if (_renderBackend == RenderBackend.OpenGL)
                exStyle |= noRedirect;
            else
                exStyle &= ~noRedirect;
            exStyle &= ~composited;
        }
        else
        {
            exStyle &= ~noRedirect;
        }

        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(hwnd, (int)WindowLongIndexFlags.GWL_STYLE, style);
            SetWindowLongPtr64(hwnd, (int)WindowLongIndexFlags.GWL_EXSTYLE, exStyle);
        }
        else
        {
            SetWindowLong32(hwnd, (int)WindowLongIndexFlags.GWL_STYLE, (int)style);
            SetWindowLong32(hwnd, (int)WindowLongIndexFlags.GWL_EXSTYLE, (int)exStyle);
        }

        // Re-apply non-client metrics.
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SetWindowPosFlags.SWP_NOMOVE |
            SetWindowPosFlags.SWP_NOSIZE |
            SetWindowPosFlags.SWP_NOZORDER |
            SetWindowPosFlags.SWP_NOACTIVATE |
            SetWindowPosFlags.SWP_FRAMECHANGED);
    }

    // REMOVED: RenderSoftwareFrameToGdiBitmap - using UIWindowBase native rendering with Memory DC

    private void RenderScene(SKCanvas canvas, SKImageInfo info)
    {
        GRContext? gr = null;

        // Only use GPU context if the renderer is actually actively using it for this frame.
        // Prevents ElementBase from creating GPU surfaces when we are falling back to CPU rendering
        // (which causes slow readbacks "weak rendering" and potential access violations).
        if (_renderer is DirectX11WindowRenderer dx && dx.IsSkiaGpuActive)
        {
            gr = dx.GrContext;
        }
        else if (_renderer is OpenGlWindowRenderer gl && gl.IsSkiaGpuActive)
        {
            gr = gl.GrContext;
        }
        else if (!(_renderer is DirectX11WindowRenderer) && !(_renderer is OpenGlWindowRenderer))
        {
            // Fallback for other renderers
            gr = (_renderer as IGpuWindowRenderer)?.GrContext;
        }

        var gpuScope = gr != null ? PushGpuContext(gr) : null;
        try
        {
            canvas.Save();
            canvas.ResetMatrix();
            canvas.ClipRect(SkiaSharp.SKRect.Create(info.Width, info.Height));
            // Ensure we clear to an opaque color to avoid any potential transparency artifacts or ghosting
            // from previous frames, especially in DirectX swapchains where buffer contents may be undefined.
            canvas.Clear(ColorScheme.BackColor);
            PaintSurface(canvas, info);
            canvas.Restore();
            _frameElements.Clear();
            for (var i = 0; i < Controls.Count; i++)
                if (Controls[i] is ElementBase element)
                    _frameElements.Add(element);

            StableSortByZOrderAscending(_frameElements);

            if (_needsFullRedraw)
            {
                for (var i = 0; i < _frameElements.Count; i++)
                    _frameElements[i].InvalidateRenderTree();
                _needsFullRedraw = false;
            }


            // DEBUG: Count renders
            var renderedCount = 0;
            var needsRedrawBefore = 0;
            var needsRedrawAfter = 0;

            for (var i = 0; i < _frameElements.Count; i++)
            {
                var element = _frameElements[i];
                if (!element.Visible || element.Width <= 0 || element.Height <= 0)
                    continue;

                renderedCount++;
                if (element.NeedsRedraw) needsRedrawBefore++;

                element.Render(canvas);

                if (element.NeedsRedraw) needsRedrawAfter++;
            }

            if (_showPerfOverlay)
            {
                DrawPerfOverlay(canvas);
                // Show render stats
                var statsPaint = new SKPaint { Color = SKColors.Yellow, TextSize = 12, IsAntialias = true };
                canvas.DrawText($"Rendered: {renderedCount} | Before: {needsRedrawBefore} | After: {needsRedrawAfter}",
                    10, info.Height - 20, statsPaint);
                statsPaint.Dispose();
            }
        }
        finally
        {
            gpuScope?.Dispose();
        }
    }

    private void DrawPerfOverlay(SKCanvas canvas)
    {
        var now = Stopwatch.GetTimestamp();
        if (_perfLastTimestamp == 0)
        {
            _perfLastTimestamp = now;
            return;
        }

        var dt = (now - _perfLastTimestamp) / (double)Stopwatch.Frequency;
        _perfLastTimestamp = now;
        if (dt <= 0)
            return;

        var frameMs = dt * 1000.0;
        _perfSmoothedFrameMs = _perfSmoothedFrameMs <= 0
            ? frameMs
            : _perfSmoothedFrameMs * 0.90 + frameMs * 0.10;

        var fps = 1000.0 / Math.Max(0.001, _perfSmoothedFrameMs);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ColorScheme.ForeColor,
            TextSize = 12
        };

        var backendLabel = _renderBackend.ToString();
        if (_renderBackend == RenderBackend.DirectX11 && _renderer is DirectX11WindowRenderer dx)
            backendLabel = dx.IsSkiaGpuActive ? "DX:GPU" : "DX:CPU";
        else if (_renderBackend == RenderBackend.OpenGL && _renderer is OpenGlWindowRenderer gl)
            backendLabel = gl.IsSkiaGpuActive ? "GL:GPU" : "GL";

        var text = $"{backendLabel}  {fps:0} FPS  {_perfSmoothedFrameMs:0.0} ms";
        canvas.DrawText(text, 8, 16, paint);
    }

    private void PaintSurface(SKCanvas canvas, SKImageInfo info)
    {
        if (info.Width <= 0 || info.Height <= 0)
            return;

        if (!ShowTitle)
        {
            canvas.Clear(ColorScheme.BackColor);
            return;
        }

        var foreColor = ColorScheme.ForeColor;
        var hoverColor = ColorScheme.BorderColor;

        canvas.Clear(ColorScheme.BackColor);

        if (FullDrawHatch)
        {
            using var paint = new SKPaint
            {
                Color = hoverColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.Create2DLine(4, SKMatrix.CreateScale(4, 4))
            };
            canvas.DrawRect(0, 0, Width, Height, paint);
        }

        if (titleColor != SKColor.Empty)
        {
            foreColor = titleColor.Determine();
            hoverColor = foreColor.WithAlpha(20);
            using var paint = new SKPaint { Color = titleColor };
            canvas.DrawRect(0, 0, Width, _titleHeightDPI, paint);
        }
        else if (_gradient.Length == 2 &&
                 !(_gradient[0] == SKColors.Transparent && _gradient[1] == SKColors.Transparent))
        {
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(Width, _titleHeightDPI),
                new[] { _gradient[0], _gradient[1] },
                null,
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(0, 0, Width, _titleHeightDPI, paint);

            foreColor = _gradient[0].Determine();
            hoverColor = foreColor.WithAlpha(20);
        }

        // Ba�l�k alan� d���ndaki i�eri�i tema arkaplan� ile doldur
        using (var contentBgPaint = new SKPaint { Color = ColorScheme.BackColor })
        {
            canvas.DrawRect(0, _titleHeightDPI, Width, Math.Max(0, Height - _titleHeightDPI), contentBgPaint);
        }

        // Kontrol d��meleri �izimi
        if (controlBox)
        {
            var closeHoverColor = new SkiaSharp.SKColor(232, 17, 35);

            if (_inCloseBox)
            {
                using var paint = new SKPaint
                {
                    Color = closeHoverColor.WithAlpha((byte)(closeBoxHoverAnimationManager.GetProgress() * 120)),
                    IsAntialias = true
                };
                canvas.DrawRect(_controlBoxRect, paint);
            }

            using var closePaint = new SKPaint
            {
                Color = _inCloseBox ? SKColors.White : foreColor,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            // �arp� i�areti
            var centerX = _controlBoxRect.Left + _controlBoxRect.Width / 2;
            var centerY = _controlBoxRect.Top + _controlBoxRect.Height / 2;
            var size = 5 * ScaleFactor;

            canvas.DrawLine(
                centerX - size,
                centerY - size,
                centerX + size,
                centerY + size,
                closePaint);

            canvas.DrawLine(
                centerX - size,
                centerY + size,
                centerX + size,
                centerY - size,
                closePaint);
        }

        if (MaximizeBox)
        {
            if (_inMaxBox)
            {
                using var paint = new SKPaint
                {
                    Color = hoverColor.WithAlpha((byte)(maxBoxHoverAnimationManager.GetProgress() * 80)),
                    IsAntialias = true
                };
                canvas.DrawRect(_maximizeBoxRect, paint);
            }

            using var maxPaint = new SKPaint
            {
                Color = foreColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            // Maximize simgesi
            var centerX = _maximizeBoxRect.Left + _maximizeBoxRect.Width / 2;
            var centerY = _maximizeBoxRect.Top + _maximizeBoxRect.Height / 2;
            var size = 5 * ScaleFactor;

            if (WindowState == FormWindowState.Maximized)
            {
                // Restore simgesi
                var offset = 2 * ScaleFactor;
                canvas.DrawRect(
                    centerX - size + offset,
                    centerY - size - offset,
                    size * 2,
                    size * 2,
                    maxPaint);

                canvas.DrawRect(
                    centerX - size - offset,
                    centerY - size + offset,
                    size * 2,
                    size * 2,
                    maxPaint);
            }
            else
            {
                canvas.DrawRect(
                    centerX - size,
                    centerY - size,
                    size * 2,
                    size * 2,
                    maxPaint);
            }
        }

        if (MinimizeBox)
        {
            if (_inMinBox)
            {
                using var paint = new SKPaint
                {
                    Color = hoverColor.WithAlpha((byte)(minBoxHoverAnimationManager.GetProgress() * 80)),
                    IsAntialias = true
                };
                canvas.DrawRect(_minimizeBoxRect, paint);
            }

            using var minPaint = new SKPaint
            {
                Color = foreColor,
                StrokeWidth = 1.1f * ScaleFactor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            var centerX = _minimizeBoxRect.Left + _minimizeBoxRect.Width / 2;
            var centerY = _minimizeBoxRect.Top + _minimizeBoxRect.Height / 2;
            var size = 5 * ScaleFactor;

            canvas.DrawLine(
                centerX - size,
                centerY,
                centerX + size,
                centerY,
                minPaint);
        }

        // Extend Box �izimi
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
                path.AddRoundRect(SKRect.Create(
                    _extendBoxRect.Left + 20 * ScaleFactor,
                    _titleHeightDPI / 2 - hoverSize / 2,
                    _extendBoxRect.Left + 20 * ScaleFactor + hoverSize,
                    _titleHeightDPI / 2 + hoverSize / 2
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
                _titleHeightDPI / 2 - size / 2,
                _extendBoxRect.Left + 24 * ScaleFactor + size,
                _titleHeightDPI / 2 + size / 2);

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

        // Form Menu veya Icon �izimi
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
        else
        {
            if (ShowIcon && Icon != null)
            {
                using var bitmap = Icon.ToBitmap();
                using var skBitmap = bitmap.ToSKBitmap();
                using var image = SKImage.FromBitmap(skBitmap);
                var iconRect = SkiaSharp.SKRect.Create(10, _titleHeightDPI / 2 - faviconSize / 2, faviconSize, faviconSize);
                canvas.DrawImage(image, iconRect);
            }
        }

        // Form ba�l��� �izimi
        if (_windowPageControl == null || _windowPageControl.Count == 0)
        {
            using var font = new SKFont
            {
                Size = Font.Size.Topx(this),
                Typeface = Font.SKTypeface,
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias
            };
            using var textPaint = new SKPaint
            {
                Color = foreColor,
                IsAntialias = true
            };

            var bounds = new SkiaSharp.SKRect();
            font.MeasureText(Text, out bounds);
            var textX = showMenuInsteadOfIcon
                ? _formMenuRect.Left + _formMenuRect.Width + 8 * ScaleFactor
                : faviconSize + 14 * ScaleFactor;
            var textY = _titleHeightDPI / 2 + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;

            TextRenderer.DrawText(canvas, Text, textX, textY, SKTextAlign.Left, font, textPaint);
        }

        // Tab kontrollerinin �izimi
        if (_windowPageControl != null && _windowPageControl.Count > 0)
        {
            if (!pageAreaAnimationManager.IsAnimating() || pageRect == null ||
                pageRect.Count != _windowPageControl.Count)
                UpdateTabRects();

            var animationProgress = pageAreaAnimationManager.GetProgress();

            // Click feedback
            if (pageAreaAnimationManager.IsAnimating())
            {
                using var ripplePaint = new SKPaint
                {
                    Color = foreColor.WithAlpha((byte)(31 - animationProgress * 30)),
                    IsAntialias = true
                };

                var rippleSize = (int)(animationProgress * pageRect[_windowPageControl.SelectedIndex].Width * 1.75);
                var rippleRect = new SkiaSharp.SKRect(
                    animationSource.X - rippleSize / 2,
                    animationSource.Y - rippleSize / 2,
                    animationSource.X + rippleSize / 2,
                    animationSource.Y + rippleSize / 2);

                canvas.Save();
                canvas.ClipRect(pageRect[_windowPageControl.SelectedIndex]);
                canvas.DrawOval(rippleRect, ripplePaint);
                canvas.Restore();
            }

            // fix desing time error
            if (_windowPageControl.SelectedIndex <= -1 || _windowPageControl.SelectedIndex >= _windowPageControl.Count)
                return;

            // Animate page indicator
            if (previousSelectedPageIndex == pageRect.Count)
                previousSelectedPageIndex = -1;

            var previousSelectedPageIndexIfHasOne = previousSelectedPageIndex == -1
                ? _windowPageControl.SelectedIndex
                : previousSelectedPageIndex;
            var previousActivePageRect = pageRect[previousSelectedPageIndexIfHasOne];
            var activePageRect = pageRect[_windowPageControl.SelectedIndex];

            var y = activePageRect.Bottom - 2;
            var x = previousActivePageRect.Left + (activePageRect.Left - previousActivePageRect.Left) * (float)animationProgress;
            var width = previousActivePageRect.Width +
                        (activePageRect.Width - previousActivePageRect.Width) * (float)animationProgress;
            
            if (_tabDesingMode == TabDesingMode.Rectangle)
            {
                using var tabPaint = new SKPaint
                {
                    Color = ColorScheme.BackColor.InterpolateColor(hoverColor, 0.15f),
                    IsAntialias = true
                };

                canvas.DrawRect(activePageRect.Location.X, 0, width, _titleHeightDPI, tabPaint);
                canvas.DrawRect(x, 0, width, _titleHeightDPI, tabPaint);

                using var indicatorPaint = new SKPaint
                {
                    Color = SKColors.DodgerBlue,
                    IsAntialias = true
                };

                canvas.DrawRect(x, _titleHeightDPI - TAB_INDICATOR_HEIGHT, width, TAB_INDICATOR_HEIGHT, indicatorPaint);
            }
            else if (_tabDesingMode == TabDesingMode.Rounded)
            {
                if (titleColor != SKColor.Empty && !titleColor.IsDark())
                    hoverColor = foreColor.WithAlpha(60);

                using var tabPaint = new SKPaint
                {
                    Color = ColorScheme.BackColor.InterpolateColor(hoverColor, 0.2f),
                    IsAntialias = true
                };

                var tabRect = new SkiaSharp.SKRect(x, 6, x + width, _titleHeightDPI);
                var radius = 9 * ScaleFactor;

                using var path = new SKPath();
                path.AddRoundRect(tabRect, radius, radius);
                canvas.DrawPath(path, tabPaint);
            }
            else // Chromed
            {
                if (titleColor != SKColor.Empty && !titleColor.IsDark())
                    hoverColor = foreColor.WithAlpha(60);

                using var tabPaint = new SKPaint
                {
                    Color = ColorScheme.BackColor.InterpolateColor(hoverColor, 0.2f),
                    IsAntialias = true
                };

                var tabRect = new SkiaSharp.SKRect(x, 5, x + width, _titleHeightDPI - 7);
                var radius = 12;

                using var path = new SKPath();
                path.AddRoundRect(tabRect, radius, radius);
                canvas.DrawPath(path, tabPaint);
            }

            // Draw tab headers
            foreach (ElementBase page in _windowPageControl.Controls)
            {
                var currentTabIndex = _windowPageControl.Controls.IndexOf(page);
                var rect = pageRect[currentTabIndex];
                var closeIconSize = 24 * ScaleFactor;

                if (_drawTabIcons)
                {
                    using var font = new SKFont
                    {
                        Size = 12f.Topx(this),
                        Typeface = Font.SKTypeface,
                        Subpixel = true,
                        Edging = SKFontEdging.SubpixelAntialias
                    };
                    using var textPaint = new SKPaint
                    {
                        Color = foreColor,
                        IsAntialias = true
                    };

                    var startingIconBounds = new SkiaSharp.SKRect();
                    font.MeasureText("", out startingIconBounds);
                    var iconX = rect.Left + TAB_HEADER_PADDING * ScaleFactor;

                    var inlinePaddingX = startingIconBounds.Width + TAB_HEADER_PADDING * ScaleFactor;
                    var adjustedRect = SKRect.Create(
                        rect.Left + inlinePaddingX,
                        rect.Top,
                        rect.Width - inlinePaddingX - closeIconSize,
                        rect.Height);

                    var textY = _titleHeightDPI / 2 + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;
                    TextRenderer.DrawText(canvas, "", iconX, textY, SKTextAlign.Center, font, textPaint);

                    var bounds = new SkiaSharp.SKRect();
                    font.MeasureText(page.Text, out bounds);
                    var textX = adjustedRect.Left + adjustedRect.Width / 2;
                    TextRenderer.DrawText(canvas, page.Text, textX, textY, SKTextAlign.Center, font, textPaint);
                }
                else
                {
                    using var font = new SKFont
                    {
                        Size = 9f.Topx(this),
                        Typeface = Font.SKTypeface,
                        Subpixel = true,
                        Edging = SKFontEdging.SubpixelAntialias
                    };
                    using var textPaint = new SKPaint
                    {
                        Color = foreColor,
                        IsAntialias = true
                    };

                    var bounds = new SkiaSharp.SKRect();
                    font.MeasureText(page.Text, out bounds);
                    var textX = rect.Location.X + rect.Width / 2;
                    var textY = _titleHeightDPI / 2 + Math.Abs(font.Metrics.Ascent + font.Metrics.Descent) / 2;
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

                _closeTabBoxRect = new SKRect(x + width - TAB_HEADER_PADDING / 2 - size,
                    _titleHeightDPI / 2 - size / 2, size, size);
                var buttonRect = _closeTabBoxRect;

                canvas.DrawCircle(buttonRect.MidX, buttonRect.MidY, size / 2, buttonPaint);

                using var linePaint = new SKPaint
                {
                    Color = foreColor,
                    StrokeWidth = 1.1f * ScaleFactor,
                    IsAntialias = true,
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
                _newTabBoxRect = new SKRect(lastTabRect.Left + lastTabRect.Width + size / 2,
                    _titleHeightDPI / 2 - size / 2, size, size);
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
            using var borderPaint = new SKPaint
            {
                Color = titleColor != SKColor.Empty
                    ? titleColor.Determine().WithAlpha(30)
                    : ColorScheme.BorderColor,
                StrokeWidth = 1,
                IsAntialias = true
            };

            canvas.DrawLine(Width, _titleHeightDPI - 1, 0, _titleHeightDPI - 1, borderPaint);
        }
    }

    internal override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        CalcSystemBoxPos();
        PerformLayout();

        if (_renderBackend != RenderBackend.Software)
        {
            _renderer?.Resize((int)ClientSize.Width, (int)ClientSize.Height);
            Invalidate();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        CalcSystemBoxPos();

        // Trigger initial layout with current DPI
        InvalidateMeasureRecursive();
        PerformLayout();
    }

    private void UpdateTabRects()
    {
        pageRect = new List<SkiaSharp.SKRect>();

        if (_windowPageControl == null || _windowPageControl.Count == 0)
            return;

        var occupiedWidth = 44 * ScaleFactor;

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

        using var font = new SKFont
        {
            Size = (_drawTabIcons ? 12f : 9f).Topx(this),
            Typeface = Font.SKTypeface,
            Subpixel = true,
            Edging = SKFontEdging.SubpixelAntialias
        };

        var desiredWidths = new List<float>();
        float totalDesiredWidth = 0;

        foreach (ElementBase page in _windowPageControl.Controls)
        {
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

        var currentX = 44 * ScaleFactor;

        for (int i = 0; i < desiredWidths.Count; i++)
        {
            var finalWidth = (desiredWidths[i] * scale) + extraPerTab;

            if (finalWidth > maxSize)
                finalWidth = maxSize;

            pageRect.Add(new SKRect(currentX, 0, finalWidth, _titleHeightDPI));
            currentX += finalWidth;
        }
    }

    public override void UpdateCursor(ElementBase element)
    {
        if (element == null || !element.Enabled || !element.Visible)
        {
            _currentCursor = Cursors.Default;
            base.UpdateCursor(element);
            return;
        }

        var newCursor = element.Cursor ?? Cursors.Default;
        if (_currentCursor != newCursor)
        {
            _currentCursor = newCursor;
            base.UpdateCursor(element);
        }
    }

    public void BringToFront(ElementBase element)
    {
        if (!Controls.Contains(element)) return;

        _maxZOrder++;
        element.ZOrder = _maxZOrder;
        InvalidateElement(element);
    }

    public void SendToBack(ElementBase element)
    {
        if (!Controls.Contains(element)) return;

        var minZOrder = Controls.OfType<ElementBase>().Min(e => e.ZOrder);
        element.ZOrder = minZOrder - 1;
        InvalidateElement(element);
    }

    private void InvalidateElement(ElementBase element)
    {
        if (_layoutSuspendCount > 0) return;

        element.InvalidateRenderTree();

        if (!_needsFullRedraw)
            Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var paint in _paintCache.Values)
                paint.Dispose();
            _paintCache.Clear();

            _cacheBitmap?.Dispose();
            _cacheBitmap = null;
            _cacheSurface?.Dispose();
            _cacheSurface = null;
        }

        base.Dispose(disposing);
    }

    private readonly struct ZOrderSortItem
    {
        public readonly ElementBase Element;
        public readonly int ZOrder;
        public readonly int Sequence;

        public ZOrderSortItem(ElementBase element, int zOrder, int sequence)
        {
            Element = element;
            ZOrder = zOrder;
            Sequence = sequence;
        }
    }
}
