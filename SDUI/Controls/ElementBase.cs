using SDUI.Collections;
using SDUI.Helpers;
using SDUI.Layout;
using SDUI.Validations;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace SDUI.Controls;

public abstract partial class ElementBase : IElement, IArrangedElement, IDisposable
{
    private static int s_globalLayoutPassId;
    internal bool _childControlsNeedAnchorLayout { get; set; }
    internal bool _forceAnchorCalculations { get; set; }

    // Layout pass tracking for Measure/Arrange caching
    private SKSize? _cachedMeasure;

    private float _currentDpi = 96f;

    private SKImage _image;

    // Guard to prevent layout during Arrange phase
    private bool _isArranging;

    // Guard to prevent re-entrant PerformLayout calls from causing stack overflows.
    private SKSize _lastMeasureConstraint;
    private int _layoutPassId;

    // Use a counter to support nested SuspendLayout/ResumeLayout like WinForms.
    // When > 0 layout is suspended; when it reaches 0 we allow layouts again.
    protected int _layoutSuspendCount;

    public int LayoutSuspendCount { get => _layoutSuspendCount; set => _layoutSuspendCount = value; }

    private ElementBase _parent;

    public bool IsHandleCreated;
    public bool CanFocus => Enabled && Visible && Selectable;

    public ElementBase()
    {
        IsDesignMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        Controls = new ElementCollection(this);
        Properties = new();

        _font = new Font("Segoe UI", 9f);
        _cursor = Cursors.Default;
        _currentDpi = Screen.GetSystemDpi();

        InitializeScrollBars();
    }

    private SKImage _backgroundImage;
    public SKImage BackgroundImage
    {
        get => _backgroundImage;
        set
        {
            if (_backgroundImage == value) return;
            _backgroundImage = value;
            OnBackgroundImageChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    private ContentAlignment _imageAlign = ContentAlignment.MiddleCenter;
    public ContentAlignment ImageAlign
    {
        get => _imageAlign;
        set
        {
            if (_imageAlign == value) return;
            _imageAlign = value;
            OnImageAlignChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    private ImageLayout _backgroundImageLayout = ImageLayout.Tile;
    public ImageLayout BackgroundImageLayout
    {
        get => _backgroundImageLayout;
        set
        {
            if (_backgroundImageLayout == value) return;
            _backgroundImageLayout = value;
            OnBackgroundImageLayoutChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    private bool _rightToLeft = false;
    public bool RightToLeft
    {
        get => _rightToLeft;
        set
        {
            if (_rightToLeft == value) return;
            _rightToLeft = value;
            OnRightToLeftChanged(EventArgs.Empty);
            Invalidate();
        }
    }

    private SKSize _autoScaleDimensions;
    public SKSize AutoScaleDimensions
    {
        get => _autoScaleDimensions;
        set
        {
            if (_autoScaleDimensions == value) return;
            _autoScaleDimensions = value;
        }
    }

    private AutoScaleMode _autoScaleMode = AutoScaleMode.None;
    public AutoScaleMode AutoScaleMode
    {
        get => _autoScaleMode;
        set
        {
            if (_autoScaleMode == value) return;
            _autoScaleMode = value;
        }
    }

    public bool Disposing { get; set; }
    public bool CheckForIllegalCrossThreadCalls { get; set; }
    public bool InvokeRequired => false;

    protected ScrollBar? _vScrollBar;
    protected ScrollBar? _hScrollBar;

    private bool _autoScroll;
    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            if (_autoScroll == value) return;
            _autoScroll = value;
            OnAutoScrollChanged(EventArgs.Empty);
            UpdateScrollBars();
            PerformLayout();
        }
    }

    private SKSize _autoScrollMargin;
    public SKSize AutoScrollMargin
    {
        get => _autoScrollMargin;
        set
        {
            if (_autoScrollMargin == value) return;
            _autoScrollMargin = value;
            PerformLayout();
        }
    }

    public SKImage Image
    {
        get => _image;
        set
        {
            if (_image == value)
                return;

            _image = value;
            InvalidateMeasure();
            Invalidate();
        }
    }

    protected bool IsDesignMode { get; }

    protected bool IsPerformingLayout { get; private set; }

    public UIWindowBase? ParentWindow
    {
        get
        {
            return _parent switch
            {
                UIWindowBase window => window,
                ElementBase element => element.ParentWindow,
                _ => null
            };
        }
    }

    public bool HasParent => _parent != null;
    public object Tag { get; set; }

    public ElementBase Parent
    {
        get => _parent;
        set
        {
            if (_parent == value)
                return;

            _parent = value;
            UpdateCurrentDpiFromParent();
            NeedsRedraw = true;
        }
    }

    /// <summary>
    /// Represents the thickness of the border. This field is initialized to zero thickness by default.
    /// </summary>
    public Thickness _border = new(0);
    public Thickness Border
    {
        get => _border;
        set
        {
            _border = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Represents the thickness of the border. This field is initialized to zero thickness by default.
    /// </summary>
    public Radius _radius = new(0);
    public Radius Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Invalidate();
        }
    }

    private BoxShadow[] _shadows = Array.Empty<BoxShadow>();

    /// <summary>
    /// CSS-like box shadows. Supports multiple shadows rendered back-to-front.
    /// Outer shadows render outside the element bounds; inset shadows render inside.
    /// </summary>
    [Browsable(false)]
    public BoxShadow[] Shadows
    {
        get => _shadows;
        set
        {
            _shadows = value ?? Array.Empty<BoxShadow>();
            Invalidate();
        }
    }

    /// <summary>
    /// Convenience property: sets a single box shadow.
    /// </summary>
    [Category("Appearance")]
    public BoxShadow Shadow
    {
        get => _shadows.Length > 0 ? _shadows[0] : BoxShadow.None;
        set
        {
            _shadows = value.IsEmpty ? Array.Empty<BoxShadow>() : [value];
            Invalidate();
        }
    }

    /// <summary>
    /// Moves the specified element to the highest Z-order within its parent container.
    /// This is a shared helper used by windows, designers, and any container logic.
    /// </summary>
    public void BringToFront(ElementBase element)
    {
        if (element == null || element.Parent == null)
            return;

        var siblings = element.Parent.Controls.OfType<ElementBase>().ToList();
        if (siblings.Count == 0)
            return;

        int max = siblings.Max(s => s.ZOrder);
        element.ZOrder = max + 1;
        element.Parent.InvalidateRenderTree();
    }

    /// <summary>
    /// Moves the specified element to the lowest Z-order within its parent container.
    /// </summary>
    public void SendToBack(ElementBase element)
    {
        if (element == null || element.Parent == null)
            return;

        var siblings = element.Parent.Controls.OfType<ElementBase>().ToList();
        if (siblings.Count == 0)
            return;

        int min = siblings.Min(s => s.ZOrder);
        element.ZOrder = min - 1;
        element.Parent.InvalidateRenderTree();
    }

    public void BringToFront()
    {
        if (Parent == null)
            return;

        var siblings = Parent.Controls.OfType<ElementBase>().ToList();
        if (siblings.Count == 0) return;
        var max = siblings.Max(s => s.ZOrder);
        ZOrder = max + 1;
        Parent.InvalidateRenderTree();
    }

    [Browsable(false)]
    public ElementCollection Controls { get; }

    public void BeginInvoke(Delegate method)
    {
        method.DynamicInvoke();
    }

    public void BeginInvoke(Delegate method, params object[] args)
    {
        method.DynamicInvoke(args);
    }

    public void Invoke(Delegate method)
    {
        method.DynamicInvoke();
    }

    public void Invoke(Delegate method, params object[] args)
    {
        method.DynamicInvoke(args);
    }

    public virtual void Show()
    {
        Visible = true;
    }

    public virtual void Hide()
    {
        Visible = false;
    }

    private void UpdateCurrentDpiFromParent()
    {
        if (_parent is UIWindowBase window && window.IsHandleCreated)
            _currentDpi = Screen.GetDpiForWindowHandle(window.Handle);
        else if (_parent is ElementBase element)
            _currentDpi = element.ScaleFactor * 96f;
        else
            _currentDpi = Screen.GetSystemDpi();
    }

    /// <summary>
    ///     Override this to clear cached font objects when DPI changes.
    /// </summary>
    protected virtual void InvalidateFontCache()
    {
        // Base implementation does nothing - derived controls override to clear their font caches
    }

    private static Thickness ScalePadding(Thickness padding, float scaleFactor)
    {
        if (Math.Abs(scaleFactor - 1f) < 0.001f)
            return padding;

        static int Scale(int value, float factor)
        {
            return Math.Max(0, (int)Math.Round(value * factor));
        }

        return new Thickness(
            Scale(padding.Left, scaleFactor),
            Scale(padding.Top, scaleFactor),
            Scale(padding.Right, scaleFactor),
            Scale(padding.Bottom, scaleFactor));
    }

    protected bool IsChildOf(UIWindowBase window)
    {
        return ParentWindow == window;
    }

    protected bool IsChildOf(ElementBase element)
    {
        return Parent == element;
    }

    public void SendToBack()
    {
        if (Parent == null)
            return;

        var siblings = Parent.Controls.OfType<ElementBase>().ToList();
        if (siblings.Count == 0) return;
        var min = siblings.Min(s => s.ZOrder);
        ZOrder = min - 1;
        Parent.InvalidateRenderTree();
    }

    #region Properties

    private SKPoint _location;

    public virtual SKPoint Location
    {
        get => _location;
        set
        {
            if (_location == value)
                return;

            _location = value;

            OnLocationChanged(EventArgs.Empty);
        }
    }

    [ThreadStatic] private static GRContext? s_currentGpuContext;

    private static SKColorSpace? s_srgbColorSpace;

    private static SKColorSpace? SrgbColorSpace
    {
        get
        {
            if (s_srgbColorSpace == null)
                try
                {
                    s_srgbColorSpace = SKColorSpace.CreateSrgb();
                }
                catch
                {
                    // Skia native initialization failed in this environment; fall back to null (device/default color space).
                    s_srgbColorSpace = null;
                }

            return s_srgbColorSpace;
        }
    }

    internal static IDisposable PushGpuContext(GRContext? context)
    {
        var prior = s_currentGpuContext;
        s_currentGpuContext = context;
        return new GpuContextScope(prior);
    }

    private sealed class GpuContextScope : IDisposable
    {
        private readonly GRContext? _prior;
        private bool _disposed;

        public GpuContextScope(GRContext? prior)
        {
            _prior = prior;
        }

        void Dispose()
        {
            if (_disposed)
                return;

            s_currentGpuContext = _prior;
            _disposed = true;
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }

    private SKSize _minimumSize;

    [Category("Layout")]
    [DefaultValue(typeof(SKSize), "0, 0")]
    public virtual SKSize MinimumSize
    {
        get => _minimumSize;
        set
        {
            if (_minimumSize == value) return;
            _minimumSize = value;
            if (Size.Width < _minimumSize.Width || Size.Height < _minimumSize.Height)
                Size = new(
                    Math.Max(Size.Width, _minimumSize.Width),
                    Math.Max(Size.Height, _minimumSize.Height)
                );
        }
    }

    private SKSize _maximumSize;

    [Category("Layout")]
    [DefaultValue(typeof(SKSize), "0, 0")]
    public virtual SKSize MaximumSize
    {
        get => _maximumSize;
        set
        {
            if (_maximumSize == value) return;
            _maximumSize = value;

            if ((_maximumSize.Width > 0 && Size.Width > _maximumSize.Width) ||
                (_maximumSize.Height > 0 && Size.Height > _maximumSize.Height))
                Size = new SKSize(
                    _maximumSize.Width > 0 ? Math.Min(Size.Width, _maximumSize.Width) : Size.Width,
                    _maximumSize.Height > 0 ? Math.Min(Size.Height, _maximumSize.Height) : Size.Height
                );
        }
    }

    private SKSize _size = new(100, 23);

    public virtual SKSize Size
    {
        get => _size;
        set
        {
            var newSize = value;

            if (MinimumSize.Width > 0)
                newSize.Width = Math.Max(newSize.Width, MinimumSize.Width);
            if (MinimumSize.Height > 0)
                newSize.Height = Math.Max(newSize.Height, MinimumSize.Height);

            if (MaximumSize.Width > 0)
                newSize.Width = Math.Min(newSize.Width, MaximumSize.Width);
            if (MaximumSize.Height > 0)
                newSize.Height = Math.Min(newSize.Height, MaximumSize.Height);

            if (_size == newSize) return;
            _size = newSize;
            OnSizeChanged(EventArgs.Empty);
        }
    }

    public virtual SKRect Bounds
    {
        get => SKRect.Create(Location, Size);
        set
        {
            Location = value.Location;

            Size = value.Size;
        }
    }

    /// <summary>
    ///  Retrieves our internal property storage object. If you have a property
    ///  whose value is not always set, you should store it in here to save space.
    /// </summary>
    public PropertyStore Properties { get; }

    /// <summary>
    /// Gets the rectangle that defines the client area of the control in device-independent pixels.
    /// </summary>
    public virtual SKRect ClientRectangle => SKRect.Create(0, 0, Size.Width, Size.Height);

    /// <summary>
    ///     Gets the rectangle that represents the display area of the control (client area minus padding).
    ///     This is where child controls are positioned.
    /// </summary>
    public virtual SKRect DisplayRectangle
    {
        get
        {
            var padding = Padding;
            return new SKRect(
                padding.Left,
                padding.Top,
                Math.Max(padding.Left, Size.Width - padding.Right),
                Math.Max(padding.Top, Size.Height - padding.Bottom)
            );
        }
    }

    /// <summary>
    /// Gets or sets the size of the client area in device-independent pixels.
    /// </summary>
    public SKSize ClientSize
    {
        get => Size;
        set => Size = value;
    }

    /// <summary>
    /// Gets or sets the minimum size of the virtual area for which scroll bars are displayed, in pixels.
    /// </summary>
    /// <remarks>Set this property to specify the minimum scrollable area when using automatic scrolling. If
    /// the content size is smaller than this value, scroll bars will not appear. Changing this property may trigger a
    /// layout update to recalculate scroll bar visibility.</remarks>
    [Category("Layout")]
    [DefaultValue(typeof(SKSizeI), "0, 0")]
    private SKSize _autoScrollMinSize;
    public virtual SKSize AutoScrollMinSize
    {
        get => _autoScrollMinSize;
        set
        {
            if (_autoScrollMinSize == value) return;
            _autoScrollMinSize = value;

            // Trigger layout so containers can re-evaluate scrollbar visibility
            if (Parent is UIWindowBase parentWindow)
                parentWindow.PerformLayout();
            else if (Parent is ElementBase parentElement)
                parentElement.PerformLayout();
            else
                PerformLayout();
        }
    }

    private bool _visible = true;

    public virtual bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;

            _visible = value;

            OnVisibleChanged(EventArgs.Empty);
        }
    }

    private bool _enabled = true;

    public virtual bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;

            _enabled = value;

            OnEnabledChanged(EventArgs.Empty);
        }
    }

    private SKColor _backColor = SKColors.Transparent;

    public virtual SKColor BackColor
    {
        get => _backColor;
        set
        {
            if (_backColor == value)
                return;

            _backColor = value;

            OnBackColorChanged(EventArgs.Empty);

            Invalidate();
        }
    }

    private SKColor _foreColor = SKColors.Black;

    public virtual SKColor ForeColor
    {
        get => _foreColor;
        set
        {
            if (_foreColor == value)
                return;

            _foreColor = value;

            OnForeColorChanged(EventArgs.Empty);

            Invalidate();
        }
    }

    private Font _font;

    public virtual Font Font
    {
        get => _font;
        set
        {
            if (_font == value)
                return;

            _font = value;

            OnFontChanged(EventArgs.Empty);
            InvalidateMeasure();
            Invalidate();
        }
    }

    private string _text = string.Empty;
    private string _processedText = string.Empty;
    private bool _processEscapes = false;

    /// <summary>
    /// Gets or sets whether escape sequences (\n, \t, \uXXXX) should be processed in the Text property.
    /// When enabled, text processing happens once during property set, not during rendering.
    /// Default: false (for performance)
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ProcessEscapeSequences
    {
        get => _processEscapes;
        set
        {
            if (_processEscapes == value)
                return;

            _processEscapes = value;

            // Reprocess current text with new setting
            if (!string.IsNullOrEmpty(_text))
            {
                _processedText = _processEscapes
                    ? TextRenderer.ProcessEscapeSequences(_text)
                    : _text;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets the text with escape sequences processed (if ProcessEscapeSequences is enabled).
    /// Use this property in rendering code instead of Text.
    /// </summary>
    protected string ProcessedText => _processedText;

    public virtual string Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;

            _text = value;

            // Process escape sequences once here, not during render
            _processedText = _processEscapes && !string.IsNullOrEmpty(value)
                ? TextRenderer.ProcessEscapeSequences(value)
                : value ?? string.Empty;

            OnTextChanged(EventArgs.Empty);
            InvalidateMeasure();
            Invalidate();
        }
    }

    private Thickness _padding;

    public virtual Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value)
                return;

            _padding = value;

            OnPaddingChanged(EventArgs.Empty);
            InvalidateMeasure();
            Invalidate();
        }
    }

    private Thickness _margin;

    public virtual Thickness Margin
    {
        get => _margin;
        set
        {
            if (_margin == value)
                return;

            _margin = value;

            OnMarginChanged(EventArgs.Empty);
            InvalidateMeasure();
            Invalidate();
        }
    }

    private bool _tabStop = true;

    public virtual bool TabStop
    {
        get => _tabStop;
        set
        {
            if (_tabStop == value)
                return;

            _tabStop = value;

            OnTabStopChanged(EventArgs.Empty);
        }
    }

    private int _tabIndex;

    public virtual int TabIndex
    {
        get => _tabIndex;
        set
        {
            if (_tabIndex == value)
                return;

            _tabIndex = value;

            OnTabIndexChanged(EventArgs.Empty);
        }
    }

    private AnchorStyles _anchor = AnchorStyles.Top | AnchorStyles.Left;
    internal Layout.AnchorInfo? _anchorInfo;

    public virtual AnchorStyles Anchor
    {
        get => _anchor;
        set
        {
            if (_anchor == value)
                return;

            _anchor = value;
            // Reset anchor info when anchor style changes
            _anchorInfo = null;

            OnAnchorChanged(EventArgs.Empty);
        }
    }

    private DockStyle _dock = DockStyle.None;

    public virtual DockStyle Dock
    {
        get => _dock;
        set
        {
            if (_dock == value)
                return;

            _dock = value;

            OnDockChanged(EventArgs.Empty);
        }
    }

    private bool _autoSize;

    public virtual bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize == value)
                return;

            _autoSize = value;

            if (value)
                AdjustSize();

            OnAutoSizeChanged(EventArgs.Empty);
        }
    }

    private AutoSizeMode _autoSizeMode = AutoSizeMode.GrowAndShrink;

    public virtual AutoSizeMode AutoSizeMode
    {
        get => _autoSizeMode;
        set
        {
            if (_autoSizeMode == value)
                return;

            _autoSizeMode = value;

            if (AutoSize)
                AdjustSize();

            OnAutoSizeModeChanged(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the stacking order of the element within its parent container.
    /// </summary>
    [Browsable(false)]
    public int ZOrder { get; set; }

    /// <summary>
    /// Gets or sets the width component of the size.
    /// </summary>
    public int Width
    {
        get => (int)Size.Width;
        set => Size = new SKSize(value, Height);
    }

    /// <summary>
    /// Gets or sets the height component of the size.
    /// </summary>
    public int Height
    {
        get => (int)Size.Height;
        set => Size = new SKSize(Width, value);
    }

    private bool _canSelect = true;

    [Category("Behavior")]
    [DefaultValue(true)]
    public virtual bool CanSelect
    {
        get => _canSelect;
        set
        {
            if (_canSelect == value) return;
            _canSelect = value;
        }
    }

    private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;

    [Category("Appearance")]
    [DefaultValue(ContentAlignment.MiddleCenter)]
    public virtual ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            if (_textAlign == value) return;
            _textAlign = value;
            Invalidate();
        }
    }

    private bool _useMnemonic = true;

    [Category("Behavior")]
    [DefaultValue(true)]
    public virtual bool UseMnemonic
    {
        get => _useMnemonic;
        set
        {
            if (_useMnemonic == value) return;
            _useMnemonic = value;
            Invalidate();
        }
    }

    private bool _autoEllipsis;

    [Category("Behavior")]
    [DefaultValue(false)]
    public virtual bool AutoEllipsis
    {
        get => _autoEllipsis;
        set
        {
            if (_autoEllipsis == value) return;
            _autoEllipsis = value;
            Invalidate();
        }
    }

    private bool _selectable = true;

    [Category("Behavior")]
    [DefaultValue(true)]
    public virtual bool Selectable
    {
        get => _selectable && Enabled;
        set
        {
            if (_selectable == value) return;
            _selectable = value;
            Invalidate();
        }
    }

    [Browsable(false)] public bool Focused { get; internal set; }

    private string _name = string.Empty;

    [Category("Design")]
    [DefaultValue("")]
    public virtual string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
        }
    }

    private bool _useVisualStyleBackColor = true;

    [Category("Appearance")]
    [DefaultValue(true)]
    public virtual bool UseVisualStyleBackColor
    {
        get => _useVisualStyleBackColor;
        set
        {
            if (_useVisualStyleBackColor == value) return;
            _useVisualStyleBackColor = value;
            Invalidate();
        }
    }

    private Cursor _cursor = Cursors.Default;

    [Category("Appearance")]
    [DefaultValue(typeof(Cursor), "Default")]
    public virtual Cursor Cursor
    {
        get => _cursor;
        set
        {
            if (_cursor == value) return;
            _cursor = value;
            OnCursorChanged(EventArgs.Empty);
        }
    }


    [Browsable(false)] public virtual int DeviceDpi => (int)Math.Round(_currentDpi);

    [Browsable(false)] public virtual float ScaleFactor => _currentDpi / 96f;

    [Browsable(false)]
    protected virtual Keys ModifierKeys
    {
        get
        {
            var modifiers = Keys.None;

            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0)
                modifiers |= Keys.Shift;

            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
                modifiers |= Keys.Control;

            if ((GetKeyState(VK_MENU) & 0x8000) != 0)
                modifiers |= Keys.Alt;

            return modifiers;
        }
    }

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    public bool IsDisposed { get; private set; }

    private ElementBase _focusedElement;
    private ElementBase _lastHoveredElement;


    public ElementBase FocusedElement
    {
        get => _focusedElement;
        set
        {
            if (_focusedElement == value || value is UIWindowBase) return;

            var oldFocus = _focusedElement;
            _focusedElement = value;

            if (oldFocus != null)
            {
                oldFocus.Focused = false;
                oldFocus.OnLostFocus(EventArgs.Empty);
                oldFocus.OnLeave(EventArgs.Empty);
            }

            if (_focusedElement != null)
            {
                _focusedElement.Focused = true;
                _focusedElement.OnGotFocus(EventArgs.Empty);
                _focusedElement.OnEnter(EventArgs.Empty);
            }
        }
    }

    public ElementBase LastHoveredElement
    {
        get => _lastHoveredElement;
        internal set
        {
            if (value is UIWindowBase)
                return;

            if (_lastHoveredElement != value) _lastHoveredElement = value;
            
            if(_parent is UIWindowBase windowBase)
                windowBase.UpdateCursor(this);
        }
    }

    private ContextMenuStrip _contextMenuStrip;

    [Category("Behavior")]
    [DefaultValue(null)]
    public ContextMenuStrip ContextMenuStrip
    {
        get => _contextMenuStrip;
        set
        {
            if (_contextMenuStrip == value) return;
            _contextMenuStrip = value;
        }
    }

    #endregion

    #region Validation Properties

    private bool _causesValidation = true;

    [Category("Behavior")]
    [DefaultValue(true)]
    public virtual bool CausesValidation
    {
        get => _causesValidation;
        set
        {
            if (_causesValidation == value) return;
            _causesValidation = value;
        }
    }

    private string _validationText = string.Empty;

    [Category("Appearance")]
    [DefaultValue("")]
    public virtual string ValidationText
    {
        get => _validationText;
        set
        {
            if (_validationText == value) return;
            _validationText = value;
            Invalidate();
        }
    }

    private bool _isValid = true;

    [Browsable(false)]
    public virtual bool IsValid
    {
        get => _isValid;
        protected set
        {
            if (_isValid == value) return;
            _isValid = value;
            Invalidate();
        }
    }

    public bool NeedsRedraw { get; set; } = true;

    /// <summary>
    /// When true, all children will have their render tree invalidated on the next render pass.
    /// Set this after size changes, DPI changes, or other events that require a full repaint.
    /// </summary>
    protected bool NeedsFullChildRedraw { get; set; } = true;

    private readonly List<ValidationRule> _validationRules = new();

    [Browsable(false)] public IReadOnlyList<ValidationRule> ValidationRules => _validationRules.AsReadOnly();

    #endregion

    #region Events

    public event EventHandler? Click;

    public event EventHandler? DoubleClick;

    public event MouseEventHandler? MouseMove;

    public event MouseEventHandler? MouseDown;

    public event MouseEventHandler? MouseUp;

    public event MouseEventHandler? MouseClick;

    public event MouseEventHandler? MouseDoubleClick;

    public event EventHandler? MouseEnter;

    public event EventHandler? MouseLeave;

    public event EventHandler? MouseHover;

    public event EventHandler<SKPaintSurfaceEventArgs>? Paint;

    public event EventHandler? LocationChanged;

    public event EventHandler? SizeChanged;

    public event EventHandler? VisibleChanged;

    public event EventHandler? EnabledChanged;

    public event EventHandler? TextChanged;

    public event EventHandler? BackColorChanged;

    public event EventHandler? ForeColorChanged;

    public event EventHandler? FontChanged;

    public event EventHandler? PaddingChanged;

    public event EventHandler? MarginChanged;

    public event EventHandler? TabStopChanged;

    public event EventHandler? TabIndexChanged;

    public event EventHandler? AnchorChanged;

    public event EventHandler? DockChanged;

    public event EventHandler? AutoSizeChanged;

    public event EventHandler? AutoSizeModeChanged;

    public event KeyEventHandler? KeyDown;

    public event KeyEventHandler? KeyUp;

    public event KeyPressEventHandler? KeyPress;

    public event EventHandler? GotFocus;

    public event EventHandler? LostFocus;

    public event EventHandler? Enter;

    public event EventHandler? Leave;

    public event EventHandler? Validated;

    public event CancelEventHandler? Validating;

    public event EventHandler? CursorChanged;

    public event UILayoutEventHandler? Layout;

    public event UIElementEventHandler? ControlAdded;
    public event UIElementEventHandler? ControlRemoved;

    public event MouseEventHandler? MouseWheel;

    public event EventHandler? DpiChanged;
    public event EventHandler? BackgroundImageChanged;
    public event EventHandler? BackgroundImageLayoutChanged;
    public event EventHandler? ImageAlignChanged;
    public event EventHandler? RightToLeftChanged;
    public event EventHandler? AutoScrollChanged;

    // Fired when the element is loaded (parent window has finished loading). Raised once per element.
    public event EventHandler? Load;
    public event EventHandler? Activate;
    public event EventHandler? Deactivate;

    private bool _isLoaded;

    /// <summary>
    ///     Raises the Load event for this element. This is safe to call multiple times; the event will only fire once.
    /// </summary>
    protected virtual void OnLoad(EventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;
        Load?.Invoke(this, e);
    }

    /// <summary>
    ///     Ensures Load has been raised for this element and all its child elements (recursively).
    /// </summary>
    public void EnsureLoadedRecursively()
    {
        OnLoad(EventArgs.Empty);
        for (var i = 0; i < Controls.Count; i++) Controls[i].EnsureLoadedRecursively();
    }

    // Fired when the element is unloaded (parent window is closing or control removed from a loaded window).
    public event EventHandler? Unload;

    /// <summary>
    ///     Raises the Unload event for this element. This is safe to call multiple times; it will fire only when the element
    ///     was previously loaded.
    /// </summary>
    protected virtual void OnUnload(EventArgs e)
    {
        if (!_isLoaded) return; // only unload if previously loaded
        _isLoaded = false;
        Unload?.Invoke(this, e);
    }

    /// <summary>
    ///     Ensures Unload has been raised for this element and all its child elements (recursively).
    /// </summary>
    public void EnsureUnloadedRecursively()
    {
        OnUnload(EventArgs.Empty);
        for (var i = 0; i < Controls.Count; i++) Controls[i].EnsureUnloadedRecursively();
    }

    #endregion

    #region Virtual Methods

    internal SKSize ApplySizeConstraints(SKSize proposedSize)
    {
        return ApplyBoundsConstraints(0, 0, proposedSize.Width, proposedSize.Height).Size;
    }

    internal virtual SKRect ApplyBoundsConstraints(int suggestedX, int suggestedY, float proposedWidth, float proposedHeight)
    {
        // COMPAT: in Everett we would allow you to set negative values in pre-handle mode
        // in Whidbey, if you've set Min/Max size we will constrain you to 0,0. Everett apps didnt
        // have min/max size on control, which is why this works.
        if (MaximumSize != SKSize.Empty || MinimumSize != SKSize.Empty)
        {
            var maximumSize = LayoutUtils.ConvertZeroToUnbounded(MaximumSize);

            var size = LayoutUtils.IntersectSizes(new SKSize(proposedWidth, proposedHeight), maximumSize);
            var newBounds = SKRect.Create(suggestedX, suggestedY, (int)size.Width, (int)size.Height);

            newBounds.Size = LayoutUtils.UnionSizes(newBounds.Size, MinimumSize);

            return newBounds;
        }

        return new SkiaSharp.SKRect(suggestedX, suggestedY, proposedWidth, proposedHeight);
    }

    public virtual void OnPaint(SKCanvas canvas)
    {
        Paint?.Invoke(this, new SKPaintSurfaceEventArgs(canvas.Surface, default));
    }

    public virtual void Invalidate()
    {
        CheckDisposed();

        MarkDirty();

        // Only propagate to window if this isn't already the window
        // This prevents cascade invalidations that kill FPS
        if (!(this is UIWindowBase))
        {
            var window = GetParentWindow();
            window?.Invalidate();
        }
    }

    protected void MarkDirty()
    {
        NeedsRedraw = true;

        // DEBUG: Log excessive invalidations
        if (DebugSettings.EnableRenderLogging)
        {
            DebugSettings.Log($"MarkDirty called on {GetType().Name}");
        }
    }

    internal void InvalidateRenderTree()
    {
        MarkDirty();
        for (var i = 0; i < Controls.Count; i++)
            if (Controls[i] is ElementBase child)
                child.InvalidateRenderTree();
    }

    protected virtual bool UseAutoScrollTranslation => true;
    protected virtual float ChildRenderScale => 1f;
    protected virtual bool UseChildScaleForInput => true;

    private SKPoint GetScrollOffset()
    {
        if (!AutoScroll || _vScrollBar == null || _hScrollBar == null)
            return SKPoint.Empty;

        var x = _hScrollBar.Visible ? _hScrollBar.Value : 0;
        var y = _vScrollBar.Visible ? _vScrollBar.Value : 0;
        return new SKPoint(x, y);
    }

    private static bool IsScrollBar(ElementBase control)
    {
        return control is ScrollBar;
    }

    // Cached buffer for child rendering — avoids per-frame allocations
    private readonly List<ElementBase> _childRenderBuffer = new();

    protected void RenderChildren(SKCanvas canvas)
    {
        _childRenderBuffer.Clear();
        for (var i = 0; i < Controls.Count; i++)
            if (Controls[i] is ElementBase child && child.Visible && child.Width > 0 && child.Height > 0)
                _childRenderBuffer.Add(child);

        if (_childRenderBuffer.Count == 0)
            return;

        // Invalidate all children if a full redraw was requested
        if (NeedsFullChildRedraw)
        {
            for (var i = 0; i < _childRenderBuffer.Count; i++)
                _childRenderBuffer[i].InvalidateRenderTree();
            NeedsFullChildRedraw = false;
        }

        // Stable sort by ZOrder ascending, then by original index
        _childRenderBuffer.Sort(static (a, b) =>
        {
            var cmp = a.ZOrder.CompareTo(b.ZOrder);
            return cmp != 0 ? cmp : a.TabIndex.CompareTo(b.TabIndex);
        });

        var scrollOffset = GetScrollOffset();
        var shouldTranslate = UseAutoScrollTranslation && AutoScroll && (scrollOffset.X != 0 || scrollOffset.Y != 0);
        var scale = ChildRenderScale;
        var shouldScale = Math.Abs(scale - 1f) > 0.001f;

        if (shouldTranslate || shouldScale)
        {
            var saved = canvas.Save();
            if (shouldTranslate)
                canvas.Translate(-scrollOffset.X, -scrollOffset.Y);
            if (shouldScale)
                canvas.Scale(scale, scale);

            for (var i = 0; i < _childRenderBuffer.Count; i++)
            {
                var child = _childRenderBuffer[i];
                if (!IsScrollBar(child))
                    child.Render(canvas);
            }

            canvas.RestoreToCount(saved);

            for (var i = 0; i < _childRenderBuffer.Count; i++)
            {
                var child = _childRenderBuffer[i];
                if (IsScrollBar(child))
                    child.Render(canvas);
            }
        }
        else
        {
            for (var i = 0; i < _childRenderBuffer.Count; i++)
                _childRenderBuffer[i].Render(canvas);
        }
    }

    public void Render(SKCanvas targetCanvas)
    {
        if (IsDisposed || targetCanvas == null || !Visible || Width <= 0 || Height <= 0)
            return;

        try
        {
            var saved = targetCanvas.Save();
            targetCanvas.Translate(Location.X, Location.Y);

            var hasRadius = !_radius.IsEmpty;
            var elementRect = new SkiaSharp.SKRect(0, 0, Width, Height);
            var hasShadows = _shadows.Length > 0;

            // ── Outer shadows (drawn BEFORE clip so they extend beyond element bounds) ──
            if (hasShadows)
            {
                for (var si = 0; si < _shadows.Length; si++)
                {
                    var shadow = _shadows[si];
                    if (shadow.IsEmpty || shadow.Inset)
                        continue;

                    RenderOuterShadow(targetCanvas, elementRect, shadow, hasRadius);
                }
            }

            // ── Prepare round rect once if needed ──
            SKRoundRect? roundRect = null;

            if (hasRadius)
            {
                roundRect = _radius.ToRoundRect(elementRect);
                targetCanvas.ClipRoundRect(roundRect, antialias: true);
            }

            // ── Background ──
            if (BackColor != SKColors.Transparent)
            {
                using var paint = new SKPaint
                {
                    Color = BackColor,
                    IsAntialias = true
                };

                if (roundRect != null)
                    targetCanvas.DrawRoundRect(roundRect, paint);
                else
                    targetCanvas.DrawRect(elementRect, paint);
            }

            OnPaint(targetCanvas);

            // ── Border ──
            if (!_border.IsEmpty)
            {
                using var borderPaint = new SKPaint
                {
                    Color = ColorScheme.BorderColor,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                if (hasRadius)
                {
                    var maxStroke = Math.Max(
                        Math.Max(_border.Left, _border.Top),
                        Math.Max(_border.Right, _border.Bottom));
                    borderPaint.StrokeWidth = maxStroke;
                    var inset = maxStroke / 2f;
                    var borderRect = new SKRect(inset, inset, Width - inset, Height - inset);
                    var borderRRect = _radius.ToRoundRect(borderRect);
                    targetCanvas.DrawRoundRect(borderRRect, borderPaint);
                }
                else
                {
                    if (_border.Top > 0)
                    {
                        borderPaint.StrokeWidth = _border.Top;
                        targetCanvas.DrawLine(0, _border.Top / 2f, Width, _border.Top / 2f, borderPaint);
                    }
                    if (_border.Bottom > 0)
                    {
                        borderPaint.StrokeWidth = _border.Bottom;
                        targetCanvas.DrawLine(0, Height - _border.Bottom / 2f, Width, Height - _border.Bottom / 2f, borderPaint);
                    }
                    if (_border.Left > 0)
                    {
                        borderPaint.StrokeWidth = _border.Left;
                        targetCanvas.DrawLine(_border.Left / 2f, 0, _border.Left / 2f, Height, borderPaint);
                    }
                    if (_border.Right > 0)
                    {
                        borderPaint.StrokeWidth = _border.Right;
                        targetCanvas.DrawLine(Width - _border.Right / 2f, 0, Width - _border.Right / 2f, Height, borderPaint);
                    }
                }
            }

            // ── Inset shadows (drawn AFTER border, clipped to element shape) ──
            if (hasShadows)
            {
                for (var si = 0; si < _shadows.Length; si++)
                {
                    var shadow = _shadows[si];
                    if (shadow.IsEmpty || !shadow.Inset)
                        continue;

                    RenderInsetShadow(targetCanvas, elementRect, shadow, hasRadius);
                }
            }

            // ── Children ──
            targetCanvas.ClipRect(elementRect);
            RenderChildren(targetCanvas);

            targetCanvas.RestoreToCount(saved);
        }
        catch (Exception ex)
        {
            if (DebugSettings.EnableRenderLogging)
                DebugSettings.Log(
                    $"UIElementBase: Render failed for element {GetType().Name} ({Width}x{Height}): {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Renders an outer (drop) shadow by expanding the element rect with spread, offsetting, and applying Gaussian blur.
    /// CSS equivalent: <c>box-shadow: offsetX offsetY blur spread color;</c>
    /// </summary>
    private void RenderOuterShadow(SKCanvas canvas, SkiaSharp.SKRect elementRect, BoxShadow shadow, bool hasRadius)
    {
        var spread = shadow.Spread;
        var shadowRect = new SkiaSharp.SKRect(
            elementRect.Left - spread.TopLeft + shadow.OffsetX,
            elementRect.Top - spread.TopRight + shadow.OffsetY,
            elementRect.Right + spread.BottomLeft + shadow.OffsetX,
            elementRect.Bottom + spread.BottomRight + shadow.OffsetY);

        if (shadowRect.Width <= 0 || shadowRect.Height <= 0)
            return;

        using var paint = new SKPaint
        {
            Color = shadow.Color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        if (shadow.Blur > 0)
            paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur * 0.5f);

        if (hasRadius)
        {
            var scaledRadius = ScaleRadiusForSpread(_radius, spread);
            var rr = scaledRadius.ToRoundRect(shadowRect);
            canvas.DrawRoundRect(rr, paint);
        }
        else
        {
            canvas.DrawRect(shadowRect, paint);
        }
    }

    /// <summary>
    /// Renders an inset shadow inside the element bounds.
    /// Uses an inverted fill technique: draws a large rect with blur that is clipped to the element shape,
    /// offset inward so the blurred edge creates the shadow effect.
    /// CSS equivalent: <c>box-shadow: inset offsetX offsetY blur spread color;</c>
    /// </summary>
    private void RenderInsetShadow(SKCanvas canvas, SkiaSharp.SKRect elementRect, BoxShadow shadow, bool hasRadius)
    {
        var innerSaved = canvas.Save();

        // Clip to element shape
        if (hasRadius)
        {
            var clipRRect = _radius.ToRoundRect(elementRect);
            canvas.ClipRoundRect(clipRRect, antialias: true);
        }
        else
        {
            canvas.ClipRect(elementRect);
        }

        var spread = shadow.Spread;
        // Inset: shrink the "hole" by spread, then the area between the hole and element edge is the shadow
        var holeRect = new SkiaSharp.SKRect(
            elementRect.Left + spread.TopLeft + shadow.OffsetX,
            elementRect.Top + spread.TopRight + shadow.OffsetY,
            elementRect.Right - spread.BottomLeft + shadow.OffsetX,
            elementRect.Bottom - spread.BottomRight + shadow.OffsetY);

        using var paint = new SKPaint
        {
            Color = shadow.Color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        if (shadow.Blur > 0)
            paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur * 0.5f);

        // Draw a frame: large outer path minus inner hole
        using var outerPath = new SKPath();
        var inflated = new SkiaSharp.SKRect(
            elementRect.Left - shadow.Blur * 2,
            elementRect.Top - shadow.Blur * 2,
            elementRect.Right + shadow.Blur * 2,
            elementRect.Bottom + shadow.Blur * 2);
        outerPath.AddRect(inflated);

        if (holeRect.Width > 0 && holeRect.Height > 0)
        {
            if (hasRadius)
            {
                var shrunkRadius = ShrinkRadiusForSpread(_radius, spread);
                var holeRRect = shrunkRadius.ToRoundRect(holeRect);
                using var holePath = new SKPath();
                holePath.AddRoundRect(holeRRect);
                outerPath.AddPath(holePath);
            }
            else
            {
                outerPath.AddRect(holeRect);
            }

            outerPath.FillType = SKPathFillType.EvenOdd;
        }

        canvas.DrawPath(outerPath, paint);
        canvas.RestoreToCount(innerSaved);
    }

    /// <summary>
    /// Scales corner radii outward when spread expands the shadow rect.
    /// CSS spec: outer shadow radius = max(0, borderRadius + spread).
    /// </summary>
    private static Radius ScaleRadiusForSpread(Radius radius, Radius spread)
    {
        return new Radius(
            Math.Max(0, radius.TopLeft + Math.Max(spread.TopLeft, spread.TopRight)),
            Math.Max(0, radius.TopRight + Math.Max(spread.BottomLeft, spread.TopRight)),
            Math.Max(0, radius.BottomLeft + Math.Max(spread.TopLeft, spread.BottomRight)),
            Math.Max(0, radius.BottomRight + Math.Max(spread.BottomLeft, spread.BottomRight)));
    }

    /// <summary>
    /// Shrinks corner radii inward for inset shadow holes.
    /// CSS spec: inner radius = max(0, borderRadius - spread).
    /// </summary>
    private static Radius ShrinkRadiusForSpread(Radius radius, Radius spread)
    {
        return new Radius(
            Math.Max(0, radius.TopLeft - Math.Max(spread.TopLeft, spread.TopRight)),
            Math.Max(0, radius.TopRight - Math.Max(spread.BottomLeft, spread.TopRight)),
            Math.Max(0, radius.BottomLeft - Math.Max(spread.TopLeft, spread.BottomRight)),
            Math.Max(0, radius.BottomRight - Math.Max(spread.BottomLeft, spread.BottomRight)));
    }

    public virtual void Focus()
    {
        if (Parent == null)
            return;

        Focused = true;
        Parent.FocusedElement = this;
        Parent.Invalidate();
        OnGotFocus(EventArgs.Empty);
    }

    public bool IsAncestorSiteInDesignMode { get; internal set; }

    protected virtual void AdjustSize()
    {
        if (!AutoSize)
            return;

        var proposedSize = GetPreferredSize(SKSize.Empty);

        if (AutoSizeMode == AutoSizeMode.GrowOnly)
        {
            proposedSize.Width = Math.Max(Size.Width, proposedSize.Width);
            proposedSize.Height = Math.Max(Size.Height, proposedSize.Height);
        }

        // MinimumSize ve MaximumSize kontrolü
        proposedSize.Width = Math.Max(proposedSize.Width, MinimumSize.Width);
        if (MinimumSize.Height > 0)
            proposedSize.Height = Math.Max(proposedSize.Height, MinimumSize.Height);

        if (MaximumSize.Width > 0)
            proposedSize.Width = Math.Min(proposedSize.Width, MaximumSize.Width);
        if (MaximumSize.Height > 0)
            proposedSize.Height = Math.Min(proposedSize.Height, MaximumSize.Height);

        if (Size != proposedSize)
            Size = proposedSize;
    }

    /// <summary>
    ///     Measures the element and returns its desired size given the available space.
    ///     This is the first phase of the layout pass.
    /// </summary>
    /// <param name="availableSize">The available space that a parent element can allocate to a child element.</param>
    /// <returns>The desired size of the element.</returns>
    public virtual SKSize Measure(SKSize availableSize)
    {
        // Cache measurement within the same layout pass
        if (_cachedMeasure.HasValue && _lastMeasureConstraint == availableSize && _layoutPassId == s_globalLayoutPassId)
            return _cachedMeasure.Value;

        // Default implementation: use GetPreferredSize for backward compatibility
        var desiredSize = GetPreferredSize(availableSize);

        // Apply MinimumSize/MaximumSize constraints
        if (MinimumSize.Width > 0)
            desiredSize.Width = Math.Max(desiredSize.Width, MinimumSize.Width);
        if (MinimumSize.Height > 0)
            desiredSize.Height = Math.Max(desiredSize.Height, MinimumSize.Height);
        if (MaximumSize.Width > 0)
            desiredSize.Width = Math.Min(desiredSize.Width, MaximumSize.Width);
        if (MaximumSize.Height > 0)
            desiredSize.Height = Math.Min(desiredSize.Height, MaximumSize.Height);

        // Cache for this layout pass
        _cachedMeasure = desiredSize;
        _lastMeasureConstraint = availableSize;
        _layoutPassId = s_globalLayoutPassId;

        return desiredSize;
    }

    /// <summary>
    ///     Positions the element and determines its final size.
    ///     This is the second phase of the layout pass.
    /// </summary>
    /// <param name="finalRect">
    ///     The final area within the parent that the element should use to arrange itself and its
    ///     children.
    /// </param>
    public virtual void Arrange(SKRect finalRect)
    {
        // Default implementation: set Bounds directly
        if (Bounds != finalRect)
        {
            _isArranging = true;
            try
            {
                Bounds = finalRect;
            }
            finally
            {
                _isArranging = false;
            }
        }
    }

    /// <summary>
    ///     Invalidates the cached measurement and triggers layout if AutoSize is enabled.
    ///     Call this when properties that affect the element's size change (Text, Font, Image, etc.).
    /// </summary>
    internal void InvalidateMeasure()
    {
        // Clear cached measurement
        _cachedMeasure = null;

        // Trigger layout if AutoSize is enabled
        if (AutoSize)
        {
            // Trigger parent layout to re-measure and re-arrange this element
            if (Parent is UIWindowBase parentWindow)
                parentWindow.PerformLayout();
            else if (Parent is ElementBase parentElement) parentElement.PerformLayout();
        }
    }

    public virtual SKSize GetPreferredSize(SKSize proposedSize)
    {
        return Size;
    }

    #endregion

    #region Protected Event Methods

    protected virtual void OnBackgroundImageChanged(EventArgs e)
    {
        BackgroundImageChanged?.Invoke(this, e);
    }

    protected virtual void OnBackgroundImageLayoutChanged(EventArgs e)
    {
        BackgroundImageLayoutChanged?.Invoke(this, e);
    }

    protected virtual void OnImageAlignChanged(EventArgs e)
    {
        ImageAlignChanged?.Invoke(this, e);
    }

    protected virtual void OnRightToLeftChanged(EventArgs e)
    {
        RightToLeftChanged?.Invoke(this, e);
    }

    protected virtual void OnAutoScrollChanged(EventArgs e)
    {
        AutoScrollChanged?.Invoke(this, e);
    }

    private void InitializeScrollBars()
    {
        if (this is ScrollBar)
            return;

        _vScrollBar = new ScrollBar
        {
            Dock = DockStyle.Right,
            Visible = false,
            Orientation = Orientation.Vertical,
        };
        _hScrollBar = new ScrollBar
        {
            Dock = DockStyle.Bottom,
            Visible = false,
            Orientation = Orientation.Horizontal,
        };

        _vScrollBar.ValueChanged += (s, e) => Invalidate();
        _hScrollBar.ValueChanged += (s, e) => Invalidate();

        Controls.Add(_vScrollBar);
        Controls.Add(_hScrollBar);
    }

    protected void UpdateScrollBars()
    {
        if (!AutoScroll || _vScrollBar == null || _hScrollBar == null)
            return;

        float maxBottom = 0;
        float maxRight = 0;

        foreach (var control in Controls.OfType<ElementBase>())
        {
            if (control == _vScrollBar || control == _hScrollBar)
                continue;

            maxBottom = Math.Max(maxBottom, control.Location.Y + control.Height);
            maxRight = Math.Max(maxRight, control.Location.X + control.Width);
        }

        var contentWidth = Math.Max(maxRight, AutoScrollMinSize.Width) + AutoScrollMargin.Width;
        var contentHeight = Math.Max(maxBottom, AutoScrollMinSize.Height) + AutoScrollMargin.Height;

        var needsVScroll = contentHeight > Height;
        var needsHScroll = contentWidth > Width;

        _vScrollBar.Visible = needsVScroll;
        _hScrollBar.Visible = needsHScroll;

        if (needsVScroll)
        {
            _vScrollBar.Maximum = Math.Max(0, (int)contentHeight - Height);
            _vScrollBar.LargeChange = Math.Max(1, Height / 2);
            if (_vScrollBar.Value > _vScrollBar.Maximum)
                _vScrollBar.Value = _vScrollBar.Maximum;
        }

        if (needsHScroll)
        {
            _hScrollBar.Maximum = Math.Max(0, (int)contentWidth - Width);
            _hScrollBar.LargeChange = Math.Max(1, Width / 2);
            if (_hScrollBar.Value > _hScrollBar.Maximum)
                _hScrollBar.Value = _hScrollBar.Maximum;
        }
    }

    public virtual void OnClick(EventArgs e)
    {
        if (!Enabled || !Visible)
            return;

        Click?.Invoke(this, e);
    }

    public virtual void Refresh()
    {
        if (!Enabled || !Visible)
            return;

        Invalidate();
    }

    internal virtual void OnSizeChanged(EventArgs e)
    {
        SizeChanged?.Invoke(this, e);

        // Don't trigger layout during arrange/layout operation to prevent infinite recursion
        if (!IsPerformingLayout && !_isArranging)
        {
            // If this control has children, layout them within new size
            if (Controls.Count > 0)
                PerformLayout();
        }

        Invalidate();
    }

    internal virtual void OnDoubleClick(EventArgs e)
    {
        DoubleClick?.Invoke(this, e);
    }

    internal virtual void OnMouseMove(MouseEventArgs e)
    {
        MouseMove?.Invoke(this, e);

        var scrollOffset = GetScrollOffset();
        var adjusted = new SKPoint(e.X + scrollOffset.X, e.Y + scrollOffset.Y);
        var scale = ChildRenderScale;

        ElementBase hoveredElement = null;
        // Z-order'a göre tersten kontrol et (üstteki element önce)
        foreach (var control in Controls.OfType<ElementBase>().OrderByDescending(c => c.ZOrder)
                     .Where(c => c.Visible && c.Enabled))
        {
            var hitPoint = (UseAutoScrollTranslation && AutoScroll && !IsScrollBar(control)) ? adjusted : e.Location;
            if (UseChildScaleForInput && !IsScrollBar(control) && Math.Abs(scale - 1f) > 0.001f)
                hitPoint = new SKPoint((int)Math.Round(hitPoint.X / scale), (int)Math.Round(hitPoint.Y / scale));

            if (control.Bounds.Contains(hitPoint))
            {
                hoveredElement = control;
                var childEventArgs = new MouseEventArgs(e.Button, e.Clicks,
                    (int)(hitPoint.X - control.Location.X),
                    (int)(hitPoint.Y - control.Location.Y),
                    e.Delta);
                control.OnMouseMove(childEventArgs);
                break; // İlk eşleşenden sonra dur
            }
        }

        if (hoveredElement != _lastHoveredElement)
        {
            _lastHoveredElement?.OnMouseLeave(EventArgs.Empty);
            hoveredElement?.OnMouseEnter(EventArgs.Empty);
            _lastHoveredElement = hoveredElement;

            // inform parent window so it can change the cursor appropriately
            if (ParentWindow != null)
            {
                var cursorElement = hoveredElement;
                // descend into nested hovered elements to find the deepest one
                while (cursorElement?.LastHoveredElement != null)
                    cursorElement = cursorElement.LastHoveredElement;
                ParentWindow.UpdateCursor(cursorElement);
            }
        }
    }

    protected void RaiseMouseDown(MouseEventArgs e)
    {
        MouseDown?.Invoke(this, e);
    }

    internal virtual void OnMouseDown(MouseEventArgs e)
    {
        MouseDown?.Invoke(this, e);

        var scrollOffset = GetScrollOffset();
        var adjusted = new SKPoint(e.X + scrollOffset.X, e.Y + scrollOffset.Y);
        var scale = ChildRenderScale;

        var elementClicked = false;
        // Z-order'a göre tersten kontrol et (üstteki element önce)
        foreach (var control in Controls.OfType<ElementBase>().OrderByDescending(c => c.ZOrder)
                     .Where(c => c.Visible && c.Enabled))
        {
            var hitPoint = (UseAutoScrollTranslation && AutoScroll && !IsScrollBar(control)) ? adjusted : e.Location;
            if (UseChildScaleForInput && !IsScrollBar(control) && Math.Abs(scale - 1f) > 0.001f)
                hitPoint = new SKPoint((int)Math.Round(hitPoint.X / scale), (int)Math.Round(hitPoint.Y / scale));

            if (control.Bounds.Contains(hitPoint))
            {
                elementClicked = true;
                var window = GetParentWindow();
                ElementBase? prevWindowFocus = null;
                if (window is UIWindowBase uiWindow)
                    prevWindowFocus = uiWindow.FocusedElement;

                var childEventArgs = new MouseEventArgs(e.Button, e.Clicks,
                    (int)(hitPoint.X - control.Location.X),
                    (int)(hitPoint.Y - control.Location.Y),
                    e.Delta);
                control.OnMouseDown(childEventArgs);

                // Maintain focus without overriding a deeper focus set by the child.
                if (window is UIWindowBase uiWindowAfter)
                {
                    // If the child didn't change window focus, focus the direct child.
                    if (uiWindowAfter.FocusedElement == prevWindowFocus)
                        uiWindowAfter.FocusedElement = control;
                }
                else if (window != null)
                {
                    // Fallback for other UIWindowBase implementations
                    window.FocusManager.SetFocus(control);
                }
                else
                {
                    // No window: manage focus locally
                    if (FocusedElement != control)
                        FocusedElement = control;
                }

                break; // İlk eşleşenden sonra dur
            }
        }

        if (!elementClicked)
        {
            var window = GetParentWindow();
            if (window != null)
            {
                // Clicking on the element itself (no child hit) should focus *this*.
                if (CanSelect && Enabled && Visible)
                    window.FocusManager.SetFocus(this);
                else
                    window.FocusManager.SetFocus(null);
            }
            else
            {
                if (CanSelect && Enabled && Visible)
                    Focus();
            }

            if (e.Button == MouseButtons.Right)
            {
                var point = PointToScreen(e.Location);
                var current = this;
                while (current != null)
                {
                    if (current.ContextMenuStrip != null)
                    {
                        current.ContextMenuStrip.Show(this, point);
                        break;
                    }

                    current = current.Parent as ElementBase;
                }
            }
        }
    }

    /// <summary>
    ///     Gets the parent UIWindowBase for this element
    /// </summary>
    public UIWindowBase GetParentWindow()
    {
        IElement current = this;
        while (current != null)
        {
            if (current is UIWindowBase window)
                return window;
            current = current.Parent;
        }

        return null;
    }

    internal virtual void OnMouseUp(MouseEventArgs e)
    {
        MouseUp?.Invoke(this, e);

        var scrollOffset = GetScrollOffset();
        var adjusted = new SKPoint(e.X + scrollOffset.X, e.Y + scrollOffset.Y);
        var scale = ChildRenderScale;

        // Z-order'a göre tersten kontrol et (üstteki element önce)
        foreach (var control in Controls.OfType<ElementBase>().OrderByDescending(c => c.ZOrder)
                     .Where(c => c.Visible && c.Enabled))
        {
            var hitPoint = (UseAutoScrollTranslation && AutoScroll && !IsScrollBar(control)) ? adjusted : e.Location;
            if (UseChildScaleForInput && !IsScrollBar(control) && Math.Abs(scale - 1f) > 0.001f)
                hitPoint = new SKPoint((int)Math.Round(hitPoint.X / scale), (int)Math.Round(hitPoint.Y / scale));

            if (control.Bounds.Contains(hitPoint))
            {
                var childEventArgs = new MouseEventArgs(e.Button, e.Clicks,
                    (int)(hitPoint.X - control.Location.X),
                    (int)(hitPoint.Y - control.Location.Y),
                    e.Delta);

                control.OnMouseUp(childEventArgs);
                break; // İlk eşleşenden sonra dur
            }
        }
    }

    protected internal virtual void OnMouseClick(MouseEventArgs e)
    {
        MouseClick?.Invoke(this, e);

        // Standart WinForms davranışı: MouseClick sonrası Click olayı
        OnClick(EventArgs.Empty);

        // Z-order'a göre tersten kontrol et (üstteki element önce)
        var scrollOffset = GetScrollOffset();
        var adjusted = new SKPoint(e.X + scrollOffset.X, e.Y + scrollOffset.Y);
        var scale = ChildRenderScale;

        foreach (var control in Controls.OfType<ElementBase>().OrderByDescending(c => c.ZOrder)
                     .Where(c => c.Visible && c.Enabled))
        {
            var hitPoint = (UseAutoScrollTranslation && AutoScroll && !IsScrollBar(control)) ? adjusted : e.Location;
            if (UseChildScaleForInput && !IsScrollBar(control) && Math.Abs(scale - 1f) > 0.001f)
                hitPoint = new SKPoint((int)Math.Round(hitPoint.X / scale), (int)Math.Round(hitPoint.Y / scale));

            if (control.Bounds.Contains(hitPoint))
            {
                var childEventArgs = new MouseEventArgs(e.Button, e.Clicks,
                    (int)(hitPoint.X - control.Location.X),
                    (int)(hitPoint.Y - control.Location.Y),
                    e.Delta);

                control.OnMouseClick(childEventArgs);

                if (_focusedElement != control)
                    _focusedElement = control;
                break; // İlk eşleşenden sonra dur
            }
        }
    }

    internal virtual void OnMouseDoubleClick(MouseEventArgs e)
    {
        MouseDoubleClick?.Invoke(this, e);

        var scrollOffset = GetScrollOffset();
        var adjusted = new SKPoint(e.X + scrollOffset.X, e.Y + scrollOffset.Y);
        var scale = ChildRenderScale;

        // Z-order'a göre tersten kontrol et (üstteki element önce)
        foreach (var control in Controls.OfType<ElementBase>().OrderByDescending(c => c.ZOrder)
                     .Where(c => c.Visible && c.Enabled))
        {
            var hitPoint = (UseAutoScrollTranslation && AutoScroll && !IsScrollBar(control)) ? adjusted : e.Location;
            if (UseChildScaleForInput && !IsScrollBar(control) && Math.Abs(scale - 1f) > 0.001f)
                hitPoint = new SKPoint((int)Math.Round(hitPoint.X / scale), (int)Math.Round(hitPoint.Y / scale));

            if (control.Bounds.Contains(hitPoint))
            {
                var childEventArgs = new MouseEventArgs(e.Button, e.Clicks,
                    (int)(hitPoint.X - control.Location.X),
                    (int)(hitPoint.Y - control.Location.Y),
                    e.Delta);

                control.OnMouseDoubleClick(childEventArgs);
                break; // İlk eşleşenden sonra dur
            }
        }
    }

    internal virtual void OnMouseLeave(EventArgs e)
    {
        MouseLeave?.Invoke(this, e);
        //foreach (var control in Controls)
        //{
        //    if (control.Bounds.Contains(e.Location))
        //    {
        //        var childEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - control.Location.X, e.Y - control.Location.Y, e.Delta);
        //        control.OnMouseLeave(childEventArgs);
        //    }
        //}

        _lastHoveredElement?.OnMouseLeave(e);
        _lastHoveredElement = null;
    }

    internal virtual void OnMouseHover(EventArgs e)
    {
        MouseHover?.Invoke(this, e);
        //foreach (var control in Controls)
        //{
        //    if (control.Bounds.Contains(e.Location))
        //    {
        //        var childEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - control.Location.X, e.Y - control.Location.Y, e.Delta);
        //        control.OnMouseLeave(childEventArgs);
        //    }
        //}
    }

    internal virtual void OnMouseEnter(EventArgs e)
    {
        MouseEnter?.Invoke(this, e);
        //foreach (var control in Controls)
        //{
        //    if (control.Bounds.Contains(e.Location))
        //    {
        //        var childEventArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - control.Location.X, e.Y - control.Location.Y, e.Delta);
        //        control.OnMouseLeave(childEventArgs);
        //    }
        //}
    }

    internal virtual void OnLocationChanged(EventArgs e)
    {
        LocationChanged?.Invoke(this, e);
        Invalidate();
    }

    internal virtual void OnVisibleChanged(EventArgs e)
    {
        VisibleChanged?.Invoke(this, e);
        Invalidate();
        if (Parent is UIWindowBase parentWindow) parentWindow.PerformLayout();
        else if (Parent is ElementBase parentElement) parentElement.PerformLayout();
    }

    internal virtual void OnEnabledChanged(EventArgs e)
    {
        EnabledChanged?.Invoke(this, e);
        Invalidate();
    }

    internal virtual void OnTextChanged(EventArgs e)
    {
        TextChanged?.Invoke(this, e);
        if (CausesValidation)
            ValidateElement();
    }

    internal virtual void OnBackColorChanged(EventArgs e)
    {
        BackColorChanged?.Invoke(this, e);
    }

    internal virtual void OnForeColorChanged(EventArgs e)
    {
        ForeColorChanged?.Invoke(this, e);
    }

    internal virtual void OnFontChanged(EventArgs e)
    {
        FontChanged?.Invoke(this, e);
    }

    internal virtual void OnPaddingChanged(EventArgs e)
    {
        PaddingChanged?.Invoke(this, e);
        Invalidate();
    }

    internal virtual void OnMarginChanged(EventArgs e)
    {
        MarginChanged?.Invoke(this, e);
        // Request parent to relayout this control on next pass
        if (Parent != null)
            Invalidate();
    }

    internal virtual void OnTabStopChanged(EventArgs e)
    {
        TabStopChanged?.Invoke(this, e);
    }

    internal virtual void OnTabIndexChanged(EventArgs e)
    {
        TabIndexChanged?.Invoke(this, e);
    }

    internal virtual void OnAnchorChanged(EventArgs e)
    {
        AnchorChanged?.Invoke(this, e);
        // Don't trigger parent layout - anchor changes will be picked up on next parent resize
        // Forcing layout here causes FPS drops
    }

    internal virtual void OnDockChanged(EventArgs e)
    {
        DockChanged?.Invoke(this, e);
        if (Parent is UIWindowBase parentWindow) parentWindow.PerformLayout();
        else if (Parent is ElementBase parentElement) parentElement.PerformLayout();
    }

    internal virtual void OnAutoSizeChanged(EventArgs e)
    {
        AutoSizeChanged?.Invoke(this, e);
    }

    internal virtual void OnAutoSizeModeChanged(EventArgs e)
    {
        AutoSizeModeChanged?.Invoke(this, e);
    }

    private bool HandleTabKey(bool isShift)
    {
        var tabbableElements = Controls.OfType<ElementBase>()
            .Where(e => e.Visible && e.Enabled && e.TabStop)
            .OrderBy(e => e.TabIndex)
            .ToList();

        if (tabbableElements.Count == 0) return false;

        var currentIndex = _focusedElement != null ? tabbableElements.IndexOf(_focusedElement) : -1;

        if (isShift)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = tabbableElements.Count - 1;
        }
        else
        {
            currentIndex++;
            if (currentIndex >= tabbableElements.Count) currentIndex = 0;
        }

        FocusedElement = tabbableElements[currentIndex];
        return true;
    }


    internal virtual void OnKeyDown(KeyEventArgs e)
    {
        KeyDown?.Invoke(this, e);
        if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
            if (HandleTabKey(e.Shift))
            {
                e.Handled = true;
                return;
            }

        if (_focusedElement != null) _focusedElement.OnKeyDown(e);
    }

    internal virtual void OnKeyUp(KeyEventArgs e)
    {
        KeyUp?.Invoke(this, e);

        if (_focusedElement != null) _focusedElement.OnKeyUp(e);
    }

    internal virtual void OnKeyPress(KeyPressEventArgs e)
    {
        KeyPress?.Invoke(this, e);

        if (_focusedElement != null) _focusedElement.OnKeyPress(e);
    }

    internal virtual void OnGotFocus(EventArgs e)
    {
        GotFocus?.Invoke(this, e);
    }

    internal virtual void OnLostFocus(EventArgs e)
    {
        Focused = false;
        LostFocus?.Invoke(this, e);
        if (CausesValidation)
            ValidateElement();
    }

    internal virtual void OnEnter(EventArgs e)
    {
        Enter?.Invoke(this, e);
    }

    internal virtual void OnLeave(EventArgs e)
    {
        Leave?.Invoke(this, e);
    }

    internal virtual void OnValidated(EventArgs e)
    {
        Validated?.Invoke(this, e);
    }

    internal virtual void OnValidating(CancelEventArgs e)
    {
        Validating?.Invoke(this, e);
    }

    internal virtual void OnCursorChanged(EventArgs e)
    {
        CursorChanged?.Invoke(this, e);
        if (Parent is UIWindowBase parentWindow)
            parentWindow.UpdateCursor(this);
    }

    internal virtual void OnMouseWheel(MouseEventArgs e)
    {
        if (!Enabled || !Visible)
            return;

        if (AutoScroll && _vScrollBar != null && _vScrollBar.Visible)
        {
            const int scrollLines = 3;
            var step = Math.Max(1, _vScrollBar.SmallChange);
            var deltaValue = (e.Delta / 120) * scrollLines * step;
            var newValue = Math.Clamp(_vScrollBar.Value - deltaValue, _vScrollBar.Minimum, _vScrollBar.Maximum);
            _vScrollBar.Value = newValue;
            return;
        }

        MouseWheel?.Invoke(this, e);
    }

    /// <summary>
    ///     Initialize DPI on first load without scaling (design-time sizes are for 96 DPI baseline).
    /// </summary>
    internal void InitializeDpi(float dpi)
    {
        _currentDpi = dpi > 0 ? dpi : Screen.GetSystemDpi();

        // Invalidate font caches so they rebuild with correct DPI
        InvalidateFontCache();

        foreach (ElementBase control in Controls) control.InitializeDpi(_currentDpi);

        // Trigger layout to update element sizes based on new DPI
        if (Controls.Count > 0) PerformLayout();

        NeedsRedraw = true;
    }

    internal virtual void OnDpiChanged(float newDpi, float oldDpi)
    {
        if (oldDpi <= 0)
            oldDpi = _currentDpi <= 0 ? Screen.GetSystemDpi() : _currentDpi;

        if (newDpi <= 0)
            newDpi = Screen.GetSystemDpi();

        var scaleFactor = oldDpi <= 0 ? 1f : newDpi / oldDpi;
        _currentDpi = newDpi;

        // Invalidate font caches BEFORE scaling so fonts rebuild with correct DPI
        InvalidateFontCache();

        var previousSize = Size;

        if (Math.Abs(scaleFactor - 1f) > 0.001f)
        {
            // Don't scale Location - layout engine will handle positioning based on parent DPI
            // Only scale Size, Padding, Margin
            if (!AutoSize)
            {
                var scaledSize = new SKSize(
                    Math.Max(1, (int)Math.Round(previousSize.Width * scaleFactor)),
                    Math.Max(1, (int)Math.Round(previousSize.Height * scaleFactor)));

                if (scaledSize != previousSize) Size = scaledSize;
            }

            Padding = ScalePadding(Padding, scaleFactor);
            Margin = ScalePadding(Margin, scaleFactor);
        }
        else if (AutoSize)
        {
            AdjustSize();
        }

        foreach (ElementBase control in Controls) control.OnDpiChanged(newDpi, oldDpi);

        // Trigger layout after DPI change to reposition/resize children
        if (_parent != null && Math.Abs(scaleFactor - 1f) > 0.001f) PerformLayout();

        NeedsRedraw = true;
        DpiChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    #endregion

    #region Validation Methods

    public void AddValidationRule(ValidationRule rule)
    {
        if (!_validationRules.Contains(rule))
        {
            _validationRules.Add(rule);
            ValidateElement();
        }
    }

    public void RemoveValidationRule(ValidationRule rule)
    {
        if (_validationRules.Remove(rule)) ValidateElement();
    }

    public void ClearValidationRules()
    {
        _validationRules.Clear();
        IsValid = true;
        ValidationText = string.Empty;
    }

    protected virtual void ValidateElement()
    {
        if (!CausesValidation)
        {
            IsValid = true;
            ValidationText = string.Empty;
            return;
        }

        foreach (var rule in _validationRules)
            if (!rule.Validate(this, out var errorMessage))
            {
                IsValid = false;
                ValidationText = errorMessage;
                OnValidating(new CancelEventArgs(!IsValid));
                return;
            }

        IsValid = true;
        ValidationText = string.Empty;
        OnValidated(EventArgs.Empty);
    }

    #endregion

    #region Methods

    public UIWindowBase? FindForm()
    {
        if (Parent is UIWindowBase form)
            return form;
        if (Parent is ElementBase parentElement)
            return parentElement.FindForm();

        return null;
    }

    public virtual void Select()
    {
        if (!CanSelect || !Selectable || !Enabled || !Visible)
            return;

        Focus();
    }

    protected virtual bool CanProcessMnemonic()
    {
        return UseMnemonic && Visible && Enabled;
    }

    public virtual bool ProcessMnemonic(char charCode)
    {
        if (!CanProcessMnemonic())
            return false;

        if (IsMnemonic(charCode, Text))
        {
            Select();
            PerformClick();
            return true;
        }

        return false;
    }

    protected static bool IsMnemonic(char charCode, string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        var index = text.IndexOf('&');
        if (index < 0 || index >= text.Length - 1)
            return false;

        return char.ToUpper(text[index + 1]) == char.ToUpper(charCode);
    }

    protected virtual void PerformClick()
    {
        if (CanSelect)
            OnClick(EventArgs.Empty);
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? GetType().Name : Name;
    }

    public SKPoint PointToScreen(SKPoint p)
    {
        if (Parent == null)
            return p;

        if (Parent is UIWindowBase parentWindow && !parentWindow.IsDisposed)
            return parentWindow.PointToScreen(new SKPoint(p.X + Location.X, p.Y + Location.Y));
        if (Parent is ElementBase parentElement)
            return parentElement.PointToScreen(new SKPoint(p.X + Location.X, p.Y + Location.Y));

        return p;
    }

    public SKPoint PointToClient(SKPoint p)
    {
        if (Parent == null)
            return p;

        SKPoint clientPoint;
        if (Parent is UIWindowBase parentWindow)
            clientPoint = parentWindow.PointToClient(p);
        else if (Parent is ElementBase parentElement)
            clientPoint = parentElement.PointToClient(p);
        else
            return p;

        return new SKPoint(clientPoint.X - Location.X, clientPoint.Y - Location.Y);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                // Yönetilen kaynakları temizle
                _font?.Dispose();
                _cursor?.Dispose();
            }

            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ElementBase()
    {
        Dispose(false);
    }

    protected void CheckDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    #endregion

    #region Layout Methods

    public virtual void PerformLayout()
    {
        if (_layoutSuspendCount > 0)
            return;

        if (IsPerformingLayout)
            return;

        try
        {
            IsPerformingLayout = true;

            // Invalidate cached measurements for new layout pass
            s_globalLayoutPassId++;
            _cachedMeasure = null;

            OnLayout(new LayoutEventArgs(null!));
        }
        finally
        {
            IsPerformingLayout = false;
        }
    }

    public virtual void PerformLayout(ElementBase affectedElement, string? propertyName)
    {
        var args = new LayoutEventArgs(affectedElement, propertyName);
        PerformLayout(args);
    }

    public virtual void PerformLayout(LayoutEventArgs args)
    {
        if (_layoutSuspendCount > 0)
            return;

        if (IsPerformingLayout)
            return;

        try
        {
            IsPerformingLayout = true;
            OnLayout(args);
        }
        finally
        {
            IsPerformingLayout = false;
        }
    }

    protected virtual void OnLayout(LayoutEventArgs e)
    {
        Layout?.Invoke(this, e);

        UpdateScrollBars();

        // Use DisplayRectangle for child layout area (already excludes padding)
        var clientArea = DisplayRectangle;
        var remainingArea = clientArea;

        // WinForms dock order: Reverse z-order (last added first) in a single pass
        // This matches WinForms DefaultLayout behavior where docking is z-order dependent
        // and processed in reverse (children.Count - 1 down to 0)
        for (int i = Controls.Count - 1; i >= 0; i--)
        {
            var control = Controls[i];
            if (!control.Visible)
                continue;

            PerformDefaultLayout(control, clientArea, ref remainingArea);
        }
    }

    internal virtual void OnControlAdded(ElementEventArgs e)
    {
        ControlAdded?.Invoke(this, e);

        // If the added element has a parent window that has already completed loading,
        // immediately raise Load for the newly added element (and its subtree).
        if (e.Element != null)
        {
            var parentWindow = e.Element.GetParentWindow();
            if (parentWindow != null && parentWindow.IsLoaded) e.Element.EnsureLoadedRecursively();
        }
    }

    internal virtual void OnControlRemoved(ElementEventArgs e)
    {
        ControlRemoved?.Invoke(this, e);

        // If the removed element was part of a window that is already loaded,
        // raise Unload for that element and its subtree immediately.
        if (e.Element != null)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow != null && parentWindow.IsLoaded) e.Element.EnsureUnloadedRecursively();
        }
    }

    public void SuspendLayout()
    {
        _layoutSuspendCount++;
    }

    public void ResumeLayout()
    {
        ResumeLayout(true);
    }

    public void ResumeLayout(bool performLayout)
    {
        if (_layoutSuspendCount > 0)
            _layoutSuspendCount--;

        if (performLayout && _layoutSuspendCount == 0)
            PerformLayout();
    }

    /// <summary>
    /// Updates the Z-order of all child controls, assigning each control a sequential Z-order value based on its
    /// position in the collection.
    /// </summary>
    /// <remarks>This method iterates through the Controls collection and sets the ZOrder property of each
    /// control to reflect its current index. Use this method after adding, removing, or reordering controls to ensure
    /// their visual stacking order is consistent with their order in the collection.</remarks>
    public void UpdateZOrder()
    {
        for (int i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is ElementBase element)
                element.ZOrder = i;
        }
    }

    #endregion
}
