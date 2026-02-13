using SDUI.Helpers;
using SDUI.Native.Windows;
using SkiaSharp;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using static SDUI.Native.Windows.Methods;

namespace SDUI.Controls;

public partial class UIWindowBase : ElementBase, IDisposable
{
    private IntPtr _hWnd;
    private IntPtr _hInstance;
    private bool _disposed;

    private bool _updatingFromNative;

    private WndProc _wndProcDelegate;

    /// <summary>
    /// Gets the window handle associated with this window instance.
    /// </summary>
    public IntPtr Handle => _hWnd;

    private FocusManager? _focusManager;
    private bool _mouseInClient;
    protected bool enableFullDraggable;

    /// <summary>
    /// Gets or sets the result value returned when the window is closed by the user.
    /// </summary>
    /// <remarks>Set this property to indicate how the user closed the window, such as confirming or canceling
    /// an action. This value is typically used by the calling code to determine the outcome of the dialog
    /// interaction.</remarks>
    public DialogResult DialogResult { get; set; }

    /// <summary>
    /// Gets the current position of the mouse cursor relative to the control.
    /// </summary>
    public SKPoint MousePosition { get; private set; }

    /// <summary>
    /// Gets the current cursor position in screen coordinates via native GetCursorPos.
    /// </summary>
    public static SKPoint CursorScreenPosition
    {
        get
        {
            GetCursorPos(out var pt);
            return new SKPoint(pt.X, pt.Y);
        }
    }

    /// <summary>
    /// Enable or disable win10 ver 1809 + mica backdrop efeckts
    /// </summary>
    private bool _enableMica;
    private bool EnableMica
    {
        get => _enableMica;
        set
        {
            if (_enableMica == value)
                return;
            _enableMica = value;

            if (IsHandleCreated)
            {
                if (value)
                    SDUI.Native.Windows.Helpers.EnableBackdropType(Handle);
                else
                    SDUI.Native.Windows.Helpers.EnableBackdropType(Handle);
            }
        }
    }

    /// <summary>
    /// Represents the color used for the border.
    /// </summary>
    private SKColor borderColor = SKColors.Transparent;
    public SKColor BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;

            if (value != SKColors.Transparent && !OperatingSystem.IsWindows())
                SDUI.Native.Windows.Helpers.ApplyBorderColor(Handle, borderColor);

            Invalidate();
        }
    }

    private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
    public FormBorderStyle FormBorderStyle
    {
        get => _formBorderStyle;
        set
        {
            if (_formBorderStyle == value)
                return;

            _formBorderStyle = value;

            // Apply border style to native window if handle is created
            if (IsHandleCreated)
            {
                ApplyFormBorderStyle();
            }
        }
    }

    /// <summary>
    /// Applies the current FormBorderStyle to the native window.
    /// </summary>
    private void ApplyFormBorderStyle()
    {
        if (!IsHandleCreated)
            return;

        var (style, exStyle) = GetWindowStylesForBorderStyle(_formBorderStyle);

        // Set window style
        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr64(Handle, (int)WindowLongIndexFlags.GWL_STYLE, (IntPtr)style);
            SetWindowLongPtr64(Handle, (int)WindowLongIndexFlags.GWL_EXSTYLE, (IntPtr)exStyle);
        }
        else
        {
            SetWindowLong32(Handle, (int)WindowLongIndexFlags.GWL_STYLE, (int)style);
            SetWindowLong32(Handle, (int)WindowLongIndexFlags.GWL_EXSTYLE, (int)exStyle);
        }

        // Update window to reflect style changes
        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOMOVE |
            SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER |
            SetWindowPosFlags.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Gets the native Windows styles for the specified FormBorderStyle.
    /// </summary>
    private static (uint style, uint exStyle) GetWindowStylesForBorderStyle(FormBorderStyle borderStyle)
    {
        uint style = (uint)WindowStyles.WS_VISIBLE;
        uint exStyle = 0;

        switch (borderStyle)
        {
            case FormBorderStyle.None:
                // Borderless popup window
                style |= (uint)WindowStyles.WS_POPUP;
                break;

            case FormBorderStyle.FixedSingle:
                // Fixed size window with caption and system menu
                style |= (uint)(WindowStyles.WS_OVERLAPPED |
                               WindowStyles.WS_CAPTION |
                               WindowStyles.WS_SYSMENU |
                               WindowStyles.WS_MINIMIZEBOX);
                exStyle |= (uint)SetWindowLongFlags.WS_EX_WINDOWEDGE;
                break;

            case FormBorderStyle.Fixed3D:
                // Fixed size with 3D border
                style |= (uint)(WindowStyles.WS_OVERLAPPED |
                               WindowStyles.WS_CAPTION |
                               WindowStyles.WS_SYSMENU |
                               WindowStyles.WS_MINIMIZEBOX |
                               WindowStyles.WS_BORDER);
                exStyle |= (uint)(SetWindowLongFlags.WS_EX_WINDOWEDGE |
                                 SetWindowLongFlags.WS_EX_CLIENTEDGE);
                break;

            case FormBorderStyle.FixedDialog:
                // Dialog style - fixed size with thick caption
                style |= (uint)(WindowStyles.WS_POPUP |
                               WindowStyles.WS_CAPTION |
                               WindowStyles.WS_SYSMENU);
                exStyle |= (uint)(SetWindowLongFlags.WS_EX_DLGMODALFRAME |
                                 SetWindowLongFlags.WS_EX_WINDOWEDGE);
                break;

            case FormBorderStyle.Sizable:
                // Standard resizable window
                style |= (uint)WindowStyles.WS_OVERLAPPEDWINDOW;
                break;

            case FormBorderStyle.FixedToolWindow:
                // Fixed size tool window (small caption)
                style |= (uint)(WindowStyles.WS_POPUP |
                               WindowStyles.WS_CAPTION |
                               WindowStyles.WS_SYSMENU);
                exStyle |= (uint)(SetWindowLongFlags.WS_EX_TOOLWINDOW |
                                 SetWindowLongFlags.WS_EX_WINDOWEDGE);
                break;

            case FormBorderStyle.SizableToolWindow:
                // Resizable tool window (small caption)
                style |= (uint)WindowStyles.WS_OVERLAPPEDWINDOW;
                exStyle |= (uint)SetWindowLongFlags.WS_EX_TOOLWINDOW;
                break;
        }

        return (style, exStyle);
    }

    private FormStartPosition _formStartPosition = FormStartPosition.WindowsDefaultLocation;
    public FormStartPosition FormStartPosition
    {
        get => _formStartPosition;
        set
        {
            _formStartPosition = value;

            // If handle is already created, apply position change immediately
            if (IsHandleCreated)
            {
                switch (value)
                {
                    case FormStartPosition.CenterScreen:
                        CenterToScreen();
                        break;
                    case FormStartPosition.Manual:
                        // Location already set, just ensure it's applied to the window
                        SetWindowPos(Handle, IntPtr.Zero, (int)Location.X, (int)Location.Y, 0, 0,
                            SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Represents the current state of the window, such as normal, minimized, or maximized.
    /// </summary>
    private FormWindowState _windowState = FormWindowState.Normal;
    public FormWindowState WindowState
    {
        get => _windowState;
        set
        {
            if (_windowState == value)
                return;

            _windowState = value;

            if (!IsHandleCreated)
                return;

            switch (value)
            {
                case FormWindowState.Normal:
                    ShowWindow(Handle, 1); // SW_SHOWNORMAL
                    break;
                case FormWindowState.Minimized:
                    ShowWindow(Handle, 6); // SW_MINIMIZE
                    break;
                case FormWindowState.Maximized:
                    ShowWindow(Handle, 3); // SW_MAXIMIZE
                    break;
            }
        }
    }

    /// <summary>
    ///     Modern focus manager for keyboard navigation
    /// </summary>
    public FocusManager FocusManager
    {
        get
        {
            if (_focusManager == null)
            {
                _focusManager = new FocusManager(this);
                _focusManager.RefreshFocusableElements();
            }

            return _focusManager;
        }
    }

    /// <summary>
    ///     Indicates whether this window has completed its Load phase.
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Gets or sets the width, in pixels, of the window's Desktop Window Manager (DWM) margin.
    /// </summary>
    /// <remarks>A value of -1 typically indicates that the default system margin should be used. Adjusting
    /// this property can affect how window shadows and glass effects are rendered.</remarks>
    public int DwmMargin { get; set; } = -1;

    /// <summary>
    ///     Has aero enabled by windows <c>true</c>; otherwise <c>false</c>
    /// </summary>
    private bool _aeroEnabled
    {
        get
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                var enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return enabled == 1;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control's layout is mirrored for right-to-left languages.
    /// </summary>
    /// <remarks>A mirrored control displays its content in a right-to-left layout, which is typically used
    /// for languages such as Arabic or Hebrew. This property reflects the current layout direction of the control and
    /// may depend on the control's handle and window style settings.</remarks>
    private bool _isMirrored = false;
    public bool IsMirrored
    {
        get
        {
            if (!IsHandleCreated)
            {
                CreateParams cp = CreateParams;
                _isMirrored = (cp.ExStyle & (int)SetWindowLongFlags.WS_EX_LAYOUTRTL) != 0;
            }

            return _isMirrored;
        }
    }

    protected virtual CreateParams CreateParams
    {
        get
        {
            var cp = new CreateParams();
            cp.ClassName =  $"{Name}_Orivy_CoreWindow_{Guid.NewGuid()}";
            cp.Caption = Text;

            // Apply FormStartPosition
            switch (_formStartPosition)
            {
                case FormStartPosition.CenterScreen:
                    var screen = Screen.PrimaryScreen;
                    cp.X = screen.Bounds.Left + (screen.Bounds.Width - Width) / 2;
                    cp.Y = screen.Bounds.Top + (screen.Bounds.Height - Height) / 2;
                    break;
                case FormStartPosition.CenterParent:
                    // TODO: Implement parent centering if needed
                    cp.X = (int)Location.X;
                    cp.Y = (int)Location.Y;
                    break;
                case FormStartPosition.Manual:
                    cp.X = (int)Location.X;
                    cp.Y = (int)Location.Y;
                    break;
                case FormStartPosition.WindowsDefaultLocation:
                default:
                    cp.X = CW_USEDEFAULT;
                    cp.Y = CW_USEDEFAULT;
                    break;
            }

            // Set size - use current Size property values
            cp.Width = Width > 0 ? Width : 800;  // Default width if not set
            cp.Height = Height > 0 ? Height : 600; // Default height if not set

            // Get native styles based on FormBorderStyle
            var (style, exStyle) = GetWindowStylesForBorderStyle(_formBorderStyle);
            cp.Style = (int)style;
            cp.ExStyle = exStyle;

            // Class styles
            if (!_aeroEnabled)
                cp.ClassStyle |= CS_DROPSHADOW;

            cp.ClassStyle |= CS_DBLCLKS;

            return cp;
        }
    }


    private Icon? _icon;
    private bool _showIcon = true;

    /// <summary>
    /// Gets or sets the icon for the window.
    /// </summary>
    public Icon? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value)) return;
            _icon = value;
            UpdateWindowIcon();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether an icon is displayed in the title bar of the window.
    /// </summary>
    public bool ShowIcon
    {
        get => _showIcon;
        set
        {
            if (_showIcon == value) return;
            _showIcon = value;
            UpdateWindowIcon();
        }
    }

    private bool _keyPreview;

    /// <summary>
    /// Gets or sets a value indicating whether the window will receive key events before the event is passed to the control that has focus.
    /// </summary>
    /// <remarks>
    /// When this property is set to true, the window will receive KeyDown, KeyUp, and KeyPress events before they are passed to the focused control.
    /// This is useful for implementing global keyboard shortcuts or intercepting keys at the window level.
    /// The event will still be passed to the focused control unless the Handled property is set to true.
    /// </remarks>
    public bool KeyPreview
    {
        get => _keyPreview;
        set
        {
            if (_keyPreview == value) return;
            _keyPreview = value;
        }
    }

    public event EventHandler Activated;
    public event EventHandler Deactivated;

    static UIWindowBase()
    {
        Application.EnableDpiAwareness();
    }

    public UIWindowBase()
    {
        // Set default window size (ElementBase default is 100x23 which is too small for a window)
        Size = new SKSize(800, 600);

        _hInstance = Methods.GetModuleHandle(null);
        _wndProcDelegate = new WndProc(WndProc);
        CreateHandle();
    }

    /// <summary>
    /// Gets or sets the location of the window. When set, updates the native window position.
    /// </summary>
    public override SKPoint Location
    {
        get => base.Location;
        set
        {
            if (base.Location == value)
                return;

            base.Location = value;

            // Update native window position if handle is created (avoid recursion from WM_MOVE)
            if (IsHandleCreated && !_updatingFromNative)
            {
                SetWindowPos(Handle, IntPtr.Zero, (int)value.X, (int)value.Y, 0, 0,
                    SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }
    }

    /// <summary>
    /// Gets or sets the size of the window. When set, updates the native window size.
    /// </summary>
    public override SKSize Size
    {
        get => base.Size;
        set
        {
            if (base.Size == value)
                return;

            base.Size = value;

            // Update native window size if handle is created (avoid recursion from WM_SIZE)
            if (IsHandleCreated && !_updatingFromNative)
            {
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, (int)value.Width, (int)value.Height,
                    SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOZORDER |
                    SetWindowPosFlags.SWP_NOACTIVATE);
            }
        }
    }

    public void CreateHandle()
    {
        if (IsHandleCreated)
            return;

        CreateParams cp = this.CreateParams;

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
            style = (uint)cp.ClassStyle,
            lpfnWndProc = _wndProcDelegate,
            hInstance = GetModuleHandle(null),
            hCursor = LoadCursor(IntPtr.Zero, 32512),
            hbrBackground = (IntPtr)6,
            lpszClassName = cp.ClassName
        };

        RegisterClassEx(ref wc);

        // For CenterScreen, create window hidden first, then position and show
        var createStyle = (uint)cp.Style;
        if (_formStartPosition == FormStartPosition.CenterScreen)
        {
            createStyle &= ~(uint)WindowStyles.WS_VISIBLE;
        }

        // Create the window
        _hWnd = CreateWindowEx(
            cp.ExStyle,
            cp.ClassName,
            cp.Caption,
            createStyle,
            cp.X, cp.Y, cp.Width, cp.Height,
            cp.Parent, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

        if (_hWnd == IntPtr.Zero)
            throw new Exception("Pencere oluşturulamadı.");

        IsHandleCreated = true;
        OnHandleCreated(EventArgs.Empty);
    }

    public void CenterToScreen()
    {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        var rect = GetWindowRect();

        int x = (screenWidth - rect.Width) / 2;
        int y = (screenHeight - rect.Height) / 2;

        SetWindowPos(_hWnd, IntPtr.Zero, x, y, 0, 0,
            SWP_NOSIZE | SWP_NOZORDER);
    }

    protected bool ProcessCmdKey(ref MSG msg, Keys keyData)
    {
        // Handle keyboard navigation with focus manager
        if (msg.message == (uint)WindowMessage.WM_KEYDOWN || msg.message == (uint)WindowMessage.WM_SYSKEYDOWN)
        {
            // Special keys that should always be processed at window level
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            // Tab navigation
            if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
            {
                var keyArgs = new KeyEventArgs(keyData);
                if (FocusManager.ProcessKeyNavigation(keyArgs))
                    return true;
            }
        }

        return false;
    }

    protected void DragForm(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Mark window as loaded so dynamically added controls can trigger their Load immediately
        IsLoaded = true;

        // Ensure all child elements receive Load before the window is shown
        foreach (var c in Controls)
            if (c is ElementBase child)
                child.EnsureLoadedRecursively();

        if (BackColor != ColorScheme.BackColor)
            BackColor = ColorScheme.BackColor;
    }

    /// <summary>
    /// Displays the window associated with this instance if it is not already visible.
    /// </summary>
    /// <remarks>This method has no effect if the window handle is not valid. Calling this method on an
    /// already visible window has no additional effect.</remarks>
    public override void Show()
    {
        if (_hWnd != IntPtr.Zero)
        {
            Application.RegisterForm(this);
            ShowWindow(_hWnd, 5);
            Application.SetActiveForm(this);
        }
    }

    /// <summary>
    /// Displays the dialog window and starts its message loop, blocking the calling thread until the dialog is closed.
    /// </summary>
    /// <remarks>This method shows the dialog associated with the current window handle and processes window
    /// messages until the dialog is closed. The calling thread will not continue until the dialog is dismissed. This
    /// method should be called from a thread that can safely run a message loop, such as the main UI thread.</remarks>
    public void ShowDialog()
    {
        if (_hWnd == IntPtr.Zero)
            return;

        ShowWindow(_hWnd, 5);
        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    /// <summary>
    /// Hides the window associated with this instance.
    /// </summary>
    /// <remarks>This method overrides the base implementation to hide the underlying native window. After
    /// calling this method, the window will no longer be visible to the user, but it remains in memory and can be shown
    /// again if needed.</remarks>
    public override void Hide()
    {
        if (_hWnd != IntPtr.Zero)
            ShowWindow(_hWnd, 0);
    }

    public void Close()
    {
        if (_hWnd != IntPtr.Zero)
        {
            nint param = 0;
            PostMessage(_hWnd, (int)WindowMessage.WM_CLOSE, 0, ref param);
            OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));
            Application.UnregisterForm(this);
        }
    }

    protected virtual void OnFormClosed(FormClosedEventArgs e)
    {
        // Unload all child elements now the window is closed
        foreach (var c in Controls)
            if (c is ElementBase child)
                child.EnsureUnloadedRecursively();

        IsLoaded = false;
    }

    private SKPoint GetMousePosition(IntPtr lParam)
    {
        int x = (int)(lParam.ToInt64() & 0xFFFF);
        int y = (int)((lParam.ToInt64() >> 16) & 0xFFFF);

        POINT pt = new POINT { X = x, Y = y };
        ScreenToClient(_hWnd, ref pt);
        return new SKPoint(pt.X, pt.Y);
    }

    private static SKPoint GetClientMousePosition(IntPtr lParam)
    {
        int x = (short)(lParam.ToInt64() & 0xFFFF);
        int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        return new SKPoint(x, y);
    }

    private static int GetWheelDelta(IntPtr wParam)
    {
        return (short)((wParam.ToInt64() >> 16) & 0xFFFF);
    }

    private void TrackMouseEvents()
    {
        var track = new TRACKMOUSEEVENT
        {
            cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
            dwFlags = TME_LEAVE | TME_HOVER,
            hwndTrack = _hWnd,
            dwHoverTime = HOVER_DEFAULT
        };

        if (!TrackMouseEvent(ref track))
            throw new InvalidOperationException("TrackMouseEvent failed.");
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch ((WindowMessage)msg)
        {
            case WindowMessage.WM_NCHITTEST:
                {
                    var clientPt = GetMousePosition(lParam);

                    if (WindowState != FormWindowState.Maximized)
                    {
                        var gripDist = 10;
                        var clientSize = ClientSize;

                        // Allow resize on the lower right corner
                        if (clientPt.X >= clientSize.Width - gripDist && clientPt.Y >= clientSize.Height - gripDist &&
                            clientSize.Height >= gripDist)
                        {
                            return IsMirrored ? (IntPtr)HTBOTTOMLEFT : (IntPtr)HTBOTTOMRIGHT;
                        }

                        // Allow resize on the lower left corner
                        if (clientPt.X <= gripDist && clientPt.Y >= clientSize.Height - gripDist && clientSize.Height >= gripDist)
                        {
                            return IsMirrored ? (IntPtr)HTBOTTOMRIGHT : (IntPtr)HTBOTTOMLEFT;
                        }

                        // Allow resize on the upper right corner
                        if (clientPt.X <= gripDist && clientPt.Y <= gripDist && clientSize.Height >= gripDist)
                        {
                            return IsMirrored ? (IntPtr)HTTOPRIGHT : (IntPtr)HTTOPLEFT;
                        }

                        // Allow resize on the upper left corner
                        if (clientPt.X >= clientSize.Width - gripDist && clientPt.Y <= gripDist && clientSize.Height >= gripDist)
                        {
                            return IsMirrored ? (IntPtr)HTTOPLEFT : (IntPtr)HTTOPRIGHT;
                        }

                        // Allow resize on the top border
                        if (clientPt.Y <= 2 && clientSize.Height >= 2)
                        {
                            return (IntPtr)HTTOP;
                        }

                        // Allow resize on the bottom border
                        if (clientPt.Y >= clientSize.Height - gripDist && clientSize.Height >= gripDist)
                        {
                            return (IntPtr)HTBOTTOM;
                        }

                        // Allow resize on the left border
                        if (clientPt.X <= gripDist && clientSize.Height >= gripDist)
                        {
                            return (IntPtr)HTLEFT;
                        }

                        // Allow resize on the right border
                        if (clientPt.X >= clientSize.Width - gripDist && clientSize.Height >= gripDist)
                        {
                            return (IntPtr)HTRIGHT;
                        }
                    }

                    // Not on a resize border - treat as client area for custom title bar handling
                    // Dragging is handled by UIWindow via DragForm() in mouse event handlers
                    return (IntPtr)HTCLIENT;
                }
            case WindowMessage.WM_MOUSEMOVE:
                {
                    var point = GetClientMousePosition(lParam);
                    MousePosition = point;
                    var args = new MouseEventArgs(MouseButtons.None, 0, (int)point.X, (int)point.Y, 0);
                    OnMouseMove(args);

                    if (!_mouseInClient)
                    {
                        _mouseInClient = true;
                        TrackMouseEvents();
                        OnMouseEnter(EventArgs.Empty);
                    }

                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MOUSEHOVER:
                {
                    OnMouseHover(EventArgs.Empty);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MOUSELEAVE:
                {
                    _mouseInClient = false;
                    OnMouseLeave(EventArgs.Empty);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_LBUTTONDOWN:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Left, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseDown(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_LBUTTONUP:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Left, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseUp(args);
                    OnMouseClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_LBUTTONDBLCLK:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Left, 2, (int)point.X, (int)point.Y, 0);
                    OnMouseDoubleClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_RBUTTONDOWN:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Right, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseDown(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_RBUTTONUP:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Right, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseUp(args);
                    OnMouseClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_RBUTTONDBLCLK:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Right, 2, (int)point.X, (int)point.Y, 0);
                    OnMouseDoubleClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MBUTTONDOWN:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Middle, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseDown(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MBUTTONUP:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Middle, 1, (int)point.X, (int)point.Y, 0);
                    OnMouseUp(args);
                    OnMouseClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MBUTTONDBLCLK:
                {
                    var point = GetClientMousePosition(lParam);
                    var args = new MouseEventArgs(MouseButtons.Middle, 2, (int)point.X, (int)point.Y, 0);
                    OnMouseDoubleClick(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MOUSEWHEEL:
            case WindowMessage.WM_MOUSEHWHEEL:
                {
                    var point = GetMousePosition(lParam);
                    var delta = GetWheelDelta(wParam);
                    var args = new MouseEventArgs(MouseButtons.None, 0, (int)point.X, (int)point.Y, delta);
                    OnMouseWheel(args);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_KEYDOWN:
            case WindowMessage.WM_SYSKEYDOWN:
                {
                    var keyCode = (Keys)wParam.ToInt32();
                    var keyArgs = new KeyEventArgs(keyCode, ModifierKeys);
                    var msgInfo = new MSG
                    {
                        hwnd = hWnd,
                        message = msg,
                        wParam = wParam,
                        lParam = lParam
                    };

                    // Process command keys first (shortcuts, navigation)
                    if (ProcessCmdKey(ref msgInfo, keyCode))
                        return IntPtr.Zero;

                    // If KeyPreview is enabled, window gets first chance to handle
                    if (KeyPreview)
                    {
                        OnKeyDown(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // Route to focused element if exists (but not if it's this window to prevent recursion)
                    if (FocusedElement != null && FocusedElement != this)
                    {
                        FocusedElement.OnKeyDown(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // If KeyPreview is disabled and no focused element handled it, window handles it now
                    if (!KeyPreview && (FocusedElement == null || FocusedElement == this))
                    {
                        OnKeyDown(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_KEYUP:
            case WindowMessage.WM_SYSKEYUP:
                {
                    var keyCode = (Keys)wParam.ToInt32();
                    var keyArgs = new KeyEventArgs(keyCode, ModifierKeys);

                    // If KeyPreview is enabled, window gets first chance to handle
                    if (KeyPreview)
                    {
                        OnKeyUp(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // Route to focused element if exists (but not if it's this window to prevent recursion)
                    if (FocusedElement != null && FocusedElement != this)
                    {
                        FocusedElement.OnKeyUp(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // If KeyPreview is disabled and no focused element handled it, window handles it now
                    if (!KeyPreview && (FocusedElement == null || FocusedElement == this))
                    {
                        OnKeyUp(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_CHAR:
                {
                    var keyCode = (Keys)wParam.ToInt32();
                    var keyArgs = new KeyPressEventArgs(keyCode, ModifierKeys);

                    // If KeyPreview is enabled, window gets first chance to handle
                    if (KeyPreview)
                    {
                        OnKeyPress(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // Route to focused element if exists (but not if it's this window to prevent recursion)
                    if (FocusedElement != null && FocusedElement != this)
                    {
                        FocusedElement.OnKeyPress(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    // If KeyPreview is disabled and no focused element handled it, window handles it now
                    if (!KeyPreview && (FocusedElement == null || FocusedElement == this))
                    {
                        OnKeyPress(keyArgs);
                        if (keyArgs.Handled)
                            return IntPtr.Zero;
                    }

                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_SETFOCUS:
                Application.SetActiveForm(this);
                OnGotFocus(EventArgs.Empty);
                return IntPtr.Zero;
            case WindowMessage.WM_KILLFOCUS:
                OnLostFocus(EventArgs.Empty);
                return IntPtr.Zero;
            case WindowMessage.WM_ACTIVATE:
                {
                    var activateType = wParam.ToInt32() & 0xFFFF;
                    if (activateType != 0) // WA_INACTIVE = 0
                    {
                        Application.SetActiveForm(this);
                        OnActivated(EventArgs.Empty);
                    }
                    else
                    {
                        OnDeactivate(EventArgs.Empty);
                    }
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_NCACTIVATE:
                {
                    if (wParam != IntPtr.Zero)
                    {
                        Application.SetActiveForm(this);
                    }
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_MOVE:
                {
                    // Skip WM_MOVE during manual drag to prevent race condition with OnMouseMove
                    if (this is UIWindow uiWin && uiWin.IsOnMoving)
                        return IntPtr.Zero;

                    int x = (short)(lParam.ToInt64() & 0xFFFF);
                    int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

                    _updatingFromNative = true;
                    try
                    {
                        Location = new SKPoint(x, y);
                        OnLocationChanged(EventArgs.Empty);
                    }
                    finally
                    {
                        _updatingFromNative = false;
                    }
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_SIZE:
                {
                    // wParam contains the resize type
                    var sizeType = wParam.ToInt32();
                    int width = (short)(lParam.ToInt64() & 0xFFFF);
                    int height = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

                    // Update WindowState from native notification without triggering ShowWindow
                    switch (sizeType)
                    {
                        case 0: // SIZE_RESTORED
                            _windowState = FormWindowState.Normal;
                            break;
                        case 1: // SIZE_MINIMIZED
                            _windowState = FormWindowState.Minimized;
                            break;
                        case 2: // SIZE_MAXIMIZED
                            _windowState = FormWindowState.Maximized;
                            break;
                    }

                    // Prevent recursion when updating from native window
                    _updatingFromNative = true;
                    try
                    {
                        Size = new SKSize(width, height);
                        OnSizeChanged(EventArgs.Empty);
                        InvalidateWindow();
                    }
                    finally
                    {
                        _updatingFromNative = false;
                    }
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_CLOSE:
                Application.UnregisterForm(this);
                OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));
                return DefWindowProc(hWnd, msg, wParam, lParam);
            case WindowMessage.WM_SHOWWINDOW:
                {
                    // wParam: TRUE if the window is being shown, FALSE if being hidden
                    bool isShowing = wParam != IntPtr.Zero;
                    if (isShowing && !IsLoaded)
                    {
                        IsLoaded = true;
                        OnShown(EventArgs.Empty);
                    }
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_NCDESTROY:
                {
                    // Called after WM_DESTROY, when the window is being completely destroyed
                    OnHandleDestroyed(EventArgs.Empty);
                    return DefWindowProc(hWnd, msg, wParam, lParam);
                }
            case WindowMessage.WM_DESTROY:
                Application.UnregisterForm(this);
                OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));
                PostQuitMessage(0);
                return IntPtr.Zero;
            case WindowMessage.WM_ERASEBKGND:
                // Return non-zero to tell Windows we handled background erase.
                // Prevents the default white fill that causes flicker between frames.
                return (IntPtr)1;
            case WindowMessage.WM_PAINT:
                {
                    // Native rendering - delegate to rendering partial class
                    return HandlePaint(hWnd);
                }
            case WindowMessage.WM_DPICHANGED:
                {
                    var newDpi = (float)(wParam.ToInt32() & 0xFFFF);
                    var oldDpi = (float)DeviceDpi;

                    if (Math.Abs(newDpi - oldDpi) > 0.001f)
                    {
                        var rect = Marshal.PtrToStructure<Rect>(lParam);
                        SetWindowPos(Handle, IntPtr.Zero,
                            rect.Left, rect.Top,
                            rect.Width, rect.Height,
                            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

                        OnDpiChanged(newDpi, oldDpi);
                        InvalidateWindow();
                    }

                    return IntPtr.Zero;
                }
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    protected virtual void OnDeactivate(EventArgs e)
    {
        Deactivated?.Invoke(this, e);
    }

    protected virtual void OnActivated(EventArgs e)
    {
        Activated?.Invoke(this, e);
    }

    protected virtual void OnHandleCreated(EventArgs e)
    {
        SDUI.Native.Windows.Helpers.ApplyRoundCorner(Handle);

        if (_aeroEnabled && DwmMargin >= 0)
        {
            uint v = 2;

            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ref v, 4);
            var margins = new MARGINS
            {
                Bottom = DwmMargin,
                Left = DwmMargin,
                Right = DwmMargin,
                Top = DwmMargin
            };

            DwmExtendFrameIntoClientArea(Handle, ref margins);
        }

        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_FRAMECHANGED | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOMOVE |
            SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

        // Set window icon if available
        UpdateWindowIcon();

        // Ensure this window and child elements receive the correct initial DPI.
        try
        {
            float windowDpi = Screen.GetDpiForWindowHandle(Handle);
            InitializeDpi(windowDpi);
        }
        catch
        {
            // Swallow — DPI helpers may not be available on all platforms; nothing to do.
        }
    }

    internal override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);

        if (!SDUI.Native.Windows.Helpers.IsModern)
            return;

        SDUI.Native.Windows.Helpers.UseImmersiveDarkMode(Handle, ColorScheme.BackColor.IsDark());

        if (ColorScheme.BackColor.IsDark())
        {
            SDUI.Native.Windows.Helpers.EnableBackdropType(Handle, DWMSBT_TABBEDWINDOW);
        }
    }

    /// <summary>
    /// Updates the window icon when Icon or ShowIcon properties change.
    /// </summary>
    private void UpdateWindowIcon()
    {
        if (!IsHandleCreated)
            return;

        if (_showIcon && _icon != null)
        {
            // Set both small and large icons
            IntPtr hIcon = _icon.Handle;
            SendMessagePtr(Handle, (int)WindowMessage.WM_SETICON, (IntPtr)ICON_SMALL, hIcon);
            SendMessagePtr(Handle, (int)WindowMessage.WM_SETICON, (IntPtr)ICON_BIG, hIcon);
        }
        else
        {
            // Remove icons
            SendMessagePtr(Handle, (int)WindowMessage.WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
            SendMessagePtr(Handle, (int)WindowMessage.WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
        }
    }

    /// <summary>
    ///     Request that the window capture mouse input for the specified element.
    ///     This allows the element to continue receiving mouse events even when the cursor moves outside its bounds.
    /// </summary>
    /// <param name="element">The element that should receive captured mouse events</param>
    public virtual void SetMouseCapture(ElementBase element)
    {
        // Default implementation uses native Windows mouse capture
        if (IsHandleCreated && element != null)
        {
            SetCapture(Handle);
        }
    }

    /// <summary>
    ///     Release mouse capture previously set for the specified element.
    /// </summary>
    /// <param name="element">The element that had mouse capture</param>
    public virtual void ReleaseMouseCapture(ElementBase element)
    {
        // Default implementation releases native Windows mouse capture
        if (IsHandleCreated)
        {
            ReleaseCapture();
        }
    }

    /// <summary>
    ///     Raises the Shown event. Called when the window is first displayed.
    /// </summary>
    protected virtual void OnShown(EventArgs e)
    {
        // Default implementation does nothing; derived classes can override
    }

    /// <summary>
    ///     Raises the HandleDestroyed event. Called when the window handle is destroyed.
    /// </summary>
    protected virtual void OnHandleDestroyed(EventArgs e)
    {
        DisposeCachedDIB();
    }

    /// <summary>
    /// Updates the window cursor based on the provided element's cursor.
    /// This method sets the native Windows cursor for the window.
    /// </summary>
    /// <param name="element">The element whose cursor should be displayed. If null, disabled, or invisible, the default cursor is used.</param>
    public virtual void UpdateCursor(ElementBase element)
    {
        if (!IsHandleCreated)
            return;

        Cursor targetCursor;
        if (element == null || !element.Enabled || !element.Visible)
        {
            targetCursor = Cursors.Default;
        }
        else
        {
            targetCursor = element.Cursor ?? Cursors.Default;
        }

        // Set the native Windows cursor
        if (targetCursor != null && targetCursor.Handle != IntPtr.Zero)
        {
            SetCursor(targetCursor.Handle);
        }
    }

    /// <summary>
    /// Converts a point from this window's client coordinates to screen coordinates using native Windows API.
    /// </summary>
    public SKPoint PointToScreen(SKPoint clientPoint)
    {
        if (!IsHandleCreated)
            return clientPoint;

        POINT pt = new POINT
        {
            X = (int)clientPoint.X,
            Y = (int)clientPoint.Y
        };

        ClientToScreen(Handle, ref pt);
        return new SKPoint(pt.X, pt.Y);
    }

    /// <summary>
    /// Converts a point from screen coordinates to this window's client coordinates using native Windows API.
    /// </summary>
    public new SKPoint PointToClient(SKPoint screenPoint)
    {
        if (!IsHandleCreated)
            return screenPoint;

        POINT pt = new POINT
        {
            X = (int)screenPoint.X,
            Y = (int)screenPoint.Y
        };

        ScreenToClient(Handle, ref pt);
        return new SKPoint(pt.X, pt.Y);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_disposed)
        {
            if (_hWnd != IntPtr.Zero)
            {
                _hWnd = IntPtr.Zero;
                // Pencereyi yok etme işlemleri burada yapılabilir
                // DestroyWindow(_hWnd) - genellikle WM_CLOSE tetikler
            }

            // Class'ı sistemden düşür
            UnregisterClass(CreateParams.ClassName, _hInstance);
            _disposed = true;
        }
    }
}