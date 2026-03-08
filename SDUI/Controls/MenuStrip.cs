using SDUI.Animation;
using SDUI.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace SDUI.Controls;

public class MenuStrip : ElementBase
{
    private const int MaxIconCacheEntries = 256;

    private readonly Dictionary<MenuItem, AnimationManager> _itemHoverAnims = new();

    // backing fields
    private readonly int _submenuAnimationDuration = 150;
    private readonly float _submenuArrowSize = 8f;
    private ContextMenuStrip _activeDropDown;
    private MenuItem _activeDropDownOwner;
    private float _animationProgress;
    private Timer _animationTimer;
    private SKPaint? _arrowPaint;

    private SKPaint? _bgPaint;
    private SKPaint? _bottomBorderPaint;
    private SKPaint? _checkPaint;
    private SKPath? _checkPath;
    private SKPath? _chevronPath;
    private float _cornerRadius = 6f;
    private SKFont? _defaultSkFont;
    private int _defaultSkFontDpi;
    private Font? _defaultSkFontSource;
    private SKColor _hoverBackColor = SKColor.Empty;
    private SKPaint? _hoverBgPaint;
    private MenuItem _hoveredItem;
    private SKColor _hoverForeColor = SKColor.Empty;
    private float _iconSize = 16f;
    private SKSize _imageScalingSize = new(20, 20);
    private SKPaint? _imgPaint;
    private bool _isAnimating;
    private float _itemHeight = 28f;
    private float _itemPadding = 6f;
    private SKColor _menuBackColor = SKColor.Empty;
    private SKColor _menuForeColor = SKColor.Empty;
    private MenuItem _openedItem;
    private Orientation _orientation = Orientation.Horizontal;
    private bool _roundedCorners = true;
    private SKColor _separatorBackColor = SKColor.Empty;
    private SKColor _separatorColor = SKColor.Empty;
    private SKColor _separatorForeColor = SKColor.Empty;
    private float _separatorMargin = 2f;
    private bool _showCheckMargin = true;
    private bool _showHoverEffect = true;
    private bool _showIcons = true;
    private bool _showImageMargin = false;
    private bool _showSubmenuArrow = true;
    private bool _stretch;
    private SKColor _submenuBackColor = SKColor.Empty;
    private SKColor _submenuBorderColor = SKColor.Empty;
    private SKPaint? _textPaint;

    public MenuStrip()
    {
        Padding = new Thickness(8, 2, 8, 2);
        UpdateMenuStripHeight();
        BackColor = ColorScheme.Surface;
        ForeColor = ColorScheme.ForeColor;
        InitializeAnimationTimer();
    }

    [Browsable(false)] public List<MenuItem> Items { get; } = new();

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Stretch
    {
        get => _stretch;
        set
        {
            if (_stretch == value) return;
            _stretch = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(Orientation.Horizontal)]
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value) return;
            _orientation = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(typeof(SKSize), "20, 20")]
    public SKSize ImageScalingSize
    {
        get => _imageScalingSize;
        set
        {
            if (_imageScalingSize == value) return;
            _imageScalingSize = value;
            _iconSize = Math.Min(value.Width, value.Height);
            Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool ShowSubmenuArrow
    {
        get => _showSubmenuArrow;
        set
        {
            if (_showSubmenuArrow == value) return;
            _showSubmenuArrow = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor MenuBackColor
    {
        get
        {
            if (!_menuBackColor.IsEmpty()) return _menuBackColor;
            // "Other color" unification: everything uses Surface by default
            return ColorScheme.Surface;
        }
        set
        {
            if (_menuBackColor == value) return;
            _menuBackColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor MenuForeColor
    {
        get => _menuForeColor.IsEmpty() ? ColorScheme.ForeColor : _menuForeColor;
        set
        {
            if (_menuForeColor == value) return;
            _menuForeColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor HoverBackColor
    {
        get => _hoverBackColor.IsEmpty() ? ColorScheme.SurfaceVariant : _hoverBackColor;
        set
        {
            if (_hoverBackColor == value) return;
            _hoverBackColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor HoverForeColor
    {
        get => _hoverForeColor.IsEmpty() ? MenuForeColor : _hoverForeColor;
        set
        {
            if (_hoverForeColor == value) return;
            _hoverForeColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SubmenuBackColor
    {
        get => _submenuBackColor.IsEmpty() ? ColorScheme.Surface : _submenuBackColor;
        set
        {
            if (_submenuBackColor == value) return;
            _submenuBackColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SubmenuBorderColor
    {
        get => _submenuBorderColor.IsEmpty() ? ColorScheme.Outline : _submenuBorderColor;
        set
        {
            if (_submenuBorderColor == value) return;
            _submenuBorderColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SeparatorColor
    {
        get => _separatorColor.IsEmpty() ? ColorScheme.Outline : _separatorColor;
        set
        {
            if (_separatorColor == value) return;
            _separatorColor = value;
            Invalidate();
        }
    }

    [Category("Layout")]
    public float ItemHeight
    {
        get => _itemHeight;
        set
        {
            if (_itemHeight == value) return;
            _itemHeight = value;
            UpdateMenuStripHeight();
            Invalidate();
        }
    }

    [Category("Layout")]
    public float ItemPadding
    {
        get => _itemPadding;
        set
        {
            if (_itemPadding == value) return;
            _itemPadding = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public bool ShowIcons
    {
        get => _showIcons;
        set
        {
            if (_showIcons == value) return;
            _showIcons = value;
            Invalidate();
        }
    }

    [Category("Behavior")]
    public bool ShowHoverEffect
    {
        get => _showHoverEffect;
        set
        {
            if (_showHoverEffect == value) return;
            _showHoverEffect = value;
            Invalidate();
        }
    }

    [Category("Layout")]
    [DefaultValue(true)]
    public bool ShowCheckMargin
    {
        get => _showCheckMargin;
        set
        {
            if (_showCheckMargin == value) return;
            _showCheckMargin = value;
            Invalidate();
        }
    }

    [Category("Layout")]
    [DefaultValue(true)]
    public bool ShowImageMargin
    {
        get => _showImageMargin;
        set
        {
            if (_showImageMargin == value) return;
            _showImageMargin = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public bool RoundedCorners
    {
        get => _roundedCorners;
        set
        {
            if (_roundedCorners == value) return;
            _roundedCorners = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SeparatorBackColor
    {
        get => _separatorBackColor.IsEmpty() ? ColorScheme.Surface : _separatorBackColor;
        set
        {
            if (_separatorBackColor == value) return;
            _separatorBackColor = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    public SKColor SeparatorForeColor
    {
        get => _separatorForeColor.IsEmpty() ? ColorScheme.Outline : _separatorForeColor;
        set
        {
            if (_separatorForeColor == value) return;
            _separatorForeColor = value;
            Invalidate();
        }
    }

    [Category("Layout")]
    public float SeparatorMargin
    {
        get => _separatorMargin;
        set
        {
            if (_separatorMargin == value) return;
            _separatorMargin = value;
            Invalidate();
        }
    }

    protected override void InvalidateFontCache()
    {
        base.InvalidateFontCache();
        _defaultSkFont?.Dispose();
        _defaultSkFont = null;
        _defaultSkFontSource = null;
        _defaultSkFontDpi = 0;
    }

    internal override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        UpdateMenuStripHeight();
    }

    private SKFont GetDefaultSkFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        var font = Font;
        if (_defaultSkFont == null || !ReferenceEquals(_defaultSkFontSource, font) || _defaultSkFontDpi != dpi)
        {
            _defaultSkFont?.Dispose();
            _defaultSkFont = new SKFont
            {
                Size = font.Size.Topx(this),
                Typeface = font.SKTypeface,
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias
            };
            _defaultSkFontSource = font;
            _defaultSkFontDpi = dpi;
        }

        return _defaultSkFont;
    }

    private void InitializeAnimationTimer()
    {
        _animationTimer = new Timer { Interval = 16 };
        _animationTimer.Elapsed += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        if (!_isAnimating)
            return;

        _animationProgress = Math.Min(1f, _animationProgress + 16f / _submenuAnimationDuration);

        if (_animationProgress >= 1f)
        {
            _isAnimating = false;
            _animationTimer.Stop();
        }

        Invalidate();
    }

    public void AddItem(MenuItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        Items.Add(item);
        item.Parent = this;
        Invalidate();
    }

    public void RemoveItem(MenuItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (Items.Remove(item))
        {
            item.Parent = null;
            Invalidate();
        }
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);
        var bounds = ClientRectangle;
        var contentBounds = GetContentBounds(bounds);

        // Flat modern background
        _bgPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _bgPaint.Color = MenuBackColor;
        canvas.DrawRect(new SkiaSharp.SKRect(0, 0, bounds.Width, bounds.Height), _bgPaint);

        // Subtle bottom border
        _bottomBorderPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _bottomBorderPaint.Color = SeparatorColor.WithAlpha(72);
        canvas.DrawLine(0, bounds.Height - 1, bounds.Width, bounds.Height - 1, _bottomBorderPaint);

        if (Orientation == Orientation.Horizontal)
        {
            // Use ComputeItemRects to respect visibility and spacing logic
            var rects = ComputeItemRects();
            var rectIndex = 0;

            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (!item.Visible) continue;

                if (rectIndex >= rects.Count) break;
                var rf = rects[rectIndex++];
                var r = new SkiaSharp.SKRect(rf.Left, rf.Top, rf.Right, rf.Bottom);
                DrawMenuItem(canvas, item, r);
            }
        }
        else
        {
            var y = contentBounds.Top + ItemPadding;
            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var w = contentBounds.Width - ItemPadding * 2;
                var r = new SkiaSharp.SKRect(contentBounds.Left + ItemPadding, y, contentBounds.Left + ItemPadding + w, y + ItemHeight);
                DrawMenuItem(canvas, item, r);
                y += ItemHeight + ItemPadding;
            }
        }

        if (_activeDropDown == null || !_activeDropDown.IsOpen) _activeDropDownOwner = null;
    }

    private void DrawMenuItem(SKCanvas c, MenuItem item, SkiaSharp.SKRect bounds)
    {
        var hover = item == _hoveredItem || item == _openedItem;
        var anim = EnsureHoverAnim(item);

        if (hover)
            anim.StartNewAnimation(AnimationDirection.In);
        else
            anim.StartNewAnimation(AnimationDirection.Out);

        var prog = (float)anim.GetProgress();
        var scale = ScaleFactor;
        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;

        // High-quality hover background with proper anti-aliasing
        if (ShowHoverEffect && hover)
        {
            var blend = HoverBackColor;
            var alpha = (byte)(150 * prog);
            _hoverBgPaint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                FilterQuality = SKFilterQuality.High
            };
            _hoverBgPaint.Color = blend.WithAlpha(alpha);
            var hoverBounds = GetHoverBounds(bounds, vertical, scale);
            var rr = new SKRoundRect(hoverBounds, 7 * scale);
            c.DrawRoundRect(rr, _hoverBgPaint);
        }

        var contentLeftInset = GetPrimaryTextInset(item, vertical);
        var contentRightInset = GetTrailingTextInset(item, vertical);
        var tx = bounds.Left + contentLeftInset;

        // Checkmark area (left margin for checkbox/radio)
        float checkAreaWidth = 20 * scale;
        if (vertical && (item.CheckState != CheckState.Unchecked || item.Icon != null))
        {
            var checkX = bounds.Left + 8 * scale;
            var checkY = bounds.MidY;
            var checkSize = 12f * scale;

            if (item.CheckState == CheckState.Checked)
            {
                // Draw checkmark (✓)
                _checkPaint ??= new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.8f * scale,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round
                };
                _checkPaint.Color = MenuForeColor;

                _checkPath ??= new SKPath();
                _checkPath.Reset();
                // Draw checkmark as proper V shape
                _checkPath.MoveTo(checkX - checkSize * 0.4f, checkY);
                _checkPath.LineTo(checkX, checkY + checkSize * 0.4f);
                _checkPath.LineTo(checkX + checkSize * 0.6f, checkY - checkSize * 0.4f);
                c.DrawPath(_checkPath, _checkPaint);
            }
            else if (item.CheckState == CheckState.Indeterminate)
            {
                // Draw indeterminate box (filled square)
                _checkPaint ??= new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                _checkPaint.Color = MenuForeColor.WithAlpha(128);

                var boxSize = 4f * scale;
                var boxRect = new SkiaSharp.SKRect(
                    checkX,
                    checkY - boxSize / 2,
                    checkX + boxSize,
                    checkY + boxSize / 2
                );
                c.DrawRect(boxRect, _checkPaint);
            }

            tx += checkAreaWidth;
        }

        // Icon
        if (ShowIcons && item.Icon != null)
        {
            var scaledIconSize = _iconSize * scale;
            var iy = bounds.Top + (bounds.Height - scaledIconSize) / 2;
            _imgPaint ??= new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            c.DrawBitmap(item.Icon, new SKRect(bounds.Left + 4 * scale, iy, bounds.Left + 4 * scale + scaledIconSize, iy + scaledIconSize),
                _imgPaint);
            tx += scaledIconSize + 6 * scale;
        }

        // Text with high quality
        var hoverFore = !HoverForeColor.IsEmpty()
            ? HoverForeColor
            : HoverBackColor.IsEmpty()
                ? MenuForeColor
                : HoverBackColor.Determine();
        var textColor = hover ? hoverFore : MenuForeColor;

        var font = GetDefaultSkFont();
        _textPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _textPaint.Color = textColor;
        
        // Reserve space for chevron in vertical mode
        var drawBounds = new SkiaSharp.SKRect(
            tx,
            bounds.Top,
            Math.Max(tx, bounds.Right - contentRightInset),
            bounds.Bottom);
        c.DrawControlText(item.Text, drawBounds, _textPaint, font, ContentAlignment.MiddleLeft, false, true);

        // Measure text width for arrow positioning
        var textBounds = new SkiaSharp.SKRect();
        font.MeasureText(item.Text.Replace("&", ""), out textBounds);

        // WinUI3 style chevron arrow with high quality
        if (ShowSubmenuArrow && item.HasDropDown)
        {
            _arrowPaint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill, // FILL instead of STROKE for better visibility
                FilterQuality = SKFilterQuality.High
            };

            // Opacity logic: 0.4 (approx 102) constant when resting, Full opacity when hovered
            var arrowAlpha = hover ? (byte)255 : (byte)102; 
            var arrowColor = hover ? textColor : MenuForeColor; // Use active text color on hover
            _arrowPaint.Color = arrowColor.WithAlpha(arrowAlpha);

            var chevronSize = 5f * scale;
            float arrowX;
            var arrowY = bounds.MidY;

            // Check if this is a ContextMenuStrip instance (not MenuStrip with vertical orientation)
            var isContextMenu = GetType() == typeof(ContextMenuStrip);
            
            _chevronPath ??= new SKPath();
            _chevronPath.Reset();

            if (Orientation == Orientation.Vertical || isContextMenu)
            {
                // Vertical: chevron at the right edge
                arrowX = bounds.Right - 12 * scale;
                
                // Right arrow > (filled triangle)
                _chevronPath.MoveTo(arrowX - chevronSize, arrowY - chevronSize);
                _chevronPath.LineTo(arrowX + 2 * scale, arrowY);
                _chevronPath.LineTo(arrowX - chevronSize, arrowY + chevronSize);
                _chevronPath.Close();
            }
            else
            {
                // Horizontal: chevron after text
                arrowX = bounds.Right - GetHorizontalArrowEndPadding() - GetHorizontalArrowSlotWidth() / 2f;

                // Down arrow v (filled triangle)
                _chevronPath.MoveTo(arrowX - chevronSize, arrowY - chevronSize / 2);
                _chevronPath.LineTo(arrowX, arrowY + chevronSize);
                _chevronPath.LineTo(arrowX + chevronSize, arrowY - chevronSize / 2);
                _chevronPath.Close();
            }
            
            c.DrawPath(_chevronPath, _arrowPaint);
        }
    }

    private List<SkiaSharp.SKRect> ComputeItemRects()
    {
        var rects = new List<SkiaSharp.SKRect>(Items.Count);
        var b = GetContentBounds(ClientRectangle);

        if (Orientation == Orientation.Horizontal)
        {
            var x = b.Left + GetHorizontalMenuInset();
            var gap = GetHorizontalItemGap();
            var available = Math.Max(0, b.Width - GetHorizontalMenuInset() * 2);
            float total = 0;
            var widths = new float[Items.Count];
            var visibleCount = 0;

            for (var i = 0; i < Items.Count; i++)
            {
                if (!Items[i].Visible)
                    continue;

                widths[i] = MeasureItemWidth(Items[i]);
                total += widths[i];
                visibleCount++;
            }

            if (visibleCount > 1)
                total += gap * (visibleCount - 1);

            float extra = 0;
            if (Stretch && visibleCount > 1 && total < available)
            {
                var rawExtra = (available - total) / (visibleCount - 1);
                var maxExtraPerGap = Math.Max(2f * ScaleFactor, gap * 0.5f);
                extra = Math.Min(rawExtra, maxExtraPerGap);
            }

            for (var i = 0; i < Items.Count; i++)
            {
                if (!Items[i].Visible)
                    continue;

                var w = widths[i];
                rects.Add(SKRect.Create(x, b.Top, w, ItemHeight));
                x += w + gap + extra;
            }
        }
        else
        {
            // Dikey menüler ve ContextMenuStrip için; separator'lar da
            // ContextMenuStrip'teki satır yerleşimi ile aynı mantığı kullanmalı ki
            // hover alanı ile çizim hizalı olsun.
            var margin = this is ContextMenuStrip ? ContextMenuStrip.ShadowMargin : 0f;
            var y = margin + _itemPadding;
            var w = b.Width - margin * 2 - _itemPadding * 2;
            var x = margin + _itemPadding;

            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                // Skip invisible items when computing rects
                if (!item.Visible) continue;

                if (item.IsSeparator)
                {
                    // İnce çizgi için küçük bir satır yüksekliği ayırıyoruz.
                    var sepHeight = _separatorMargin * 2 + 1;
                    rects.Add(SKRect.Create(x, y, w, sepHeight));
                    y += sepHeight + _itemPadding;
                    continue;
                }

                rects.Add(SKRect.Create(x, y, w, _itemHeight));
                y += _itemHeight + _itemPadding;
            }
        }

        return rects;
    }

    private SkiaSharp.SKRect GetItemBounds(MenuItem item)
    {
        var rects = ComputeItemRects();
        for (var i = 0; i < Items.Count; i++)
            if (Items[i] == item)
            {
                var r = rects[i];
                return new SkiaSharp.SKRect(r.Left, r.Top, r.Right, r.Bottom);
            }

        return SkiaSharp.SKRect.Empty;
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var rects = ComputeItemRects();
        MenuItem hovered = null;
        for (var i = 0; i < Items.Count; i++)
            if (rects[i].Contains(e.Location))
            {
                hovered = Items[i];
                break;
            }

        if (_hoveredItem != hovered)
        {
            _hoveredItem = hovered;
            
            // If hovering over a different item with dropdown, switch to it
            if (_hoveredItem?.HasDropDown == true && _openedItem != _hoveredItem)
            {
                OpenSubmenu(_hoveredItem);
            }
            // If hovering over item without dropdown but a submenu is open, close it
            else if (_hoveredItem != null && !_hoveredItem.HasDropDown && _openedItem != null)
            {
                CloseSubmenu();
            }
            // If mouse left all items (null) and submenu is open, close it
            else if (_hoveredItem == null && _openedItem != null)
            {
                CloseSubmenu();
            }
            
            Invalidate();
        }
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        var rects = ComputeItemRects();
        for (var i = 0; i < Items.Count; i++)
            if (rects[i].Contains(e.Location))
            {
                OnItemClicked(Items[i]);
                return;
            }

        CloseSubmenu();
    }

    protected virtual void OnItemClicked(MenuItem item)
    {
        if (item.HasDropDown)
        {
            if (_openedItem == item)
            {
                /*keep*/
            }
            else
            {
                OpenSubmenu(item);
            }
        }
        else
        {
            item.OnClick();
            CloseSubmenu();
        }
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredItem = null;
        Invalidate();
    }

    protected void OpenSubmenu(MenuItem item)
    {
        if (!item.HasDropDown)
        {
            CloseSubmenu();
            return;
        }

        if (_activeDropDownOwner == item && _activeDropDown != null && _activeDropDown.IsOpen) return;

        CloseSubmenu();
        EnsureDropDownHost();
    _activeDropDown.ParentDropDown = this as ContextMenuStrip;
        _activeDropDown.Items.Clear();

        foreach (var child in item.DropDownItems)
            _activeDropDown.AddItem(CloneMenuItem(child));

        SyncDropDownAppearance();

        var itemBounds = GetItemBounds(item);
        SKPoint screenPoint;
        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;

        if (vertical)
        {
            // Cascading submenus must align by the *content* edge, not the shadow bounds.
            // ContextMenuStrip's background starts at ShadowMargin, so compensate here.
            var shadow = _activeDropDown is ContextMenuStrip ? ContextMenuStrip.ShadowMargin : 0f;

            // Parent content right edge in local coords.
            var parentContentRight = this is ContextMenuStrip
                ? Width - ContextMenuStrip.ShadowMargin
                : itemBounds.Right;

            // Desired child background top-left (local coords), with 1px overlap to avoid seams.
            var desiredBgLeft = parentContentRight - 1f;
            
            // Measure submenu size to check if it fits and for centering
            var submenuSize = _activeDropDown.MeasurePreferredSize();
            
            // Center submenu vertically on the parent menu item
            var desiredBgTop = itemBounds.Top + (itemBounds.Height - submenuSize.Height) / 2f;
            var screenTarget = PointToScreen(new SKPoint((int)desiredBgLeft, (int)desiredBgTop));
            var screenTargetRight = screenTarget.X + submenuSize.Width;
            var screenTargetBottom = screenTarget.Y + submenuSize.Height;
            
            // Use Screen class to get proper working area for the current monitor
            var currentScreen = ParentWindow != null
                ? Screen.FromHandle(ParentWindow.Handle)
                : Screen.FromPoint(screenTarget);
            var screenBounds = currentScreen.WorkingArea;

            // Adjust vertical position if submenu goes off-screen vertically
            if (screenTargetBottom > screenBounds.Bottom)
            {
                var overflow = screenTargetBottom - screenBounds.Bottom;
                desiredBgTop -= overflow;
            }
            if (screenTarget.Y < screenBounds.Top)
            {
                desiredBgTop += (screenBounds.Top - screenTarget.Y);
            }

            // If it goes off-screen to the right, flip to the left side
            if (screenTargetRight > screenBounds.Right)
            {
                var parentContentLeft = this is ContextMenuStrip
                    ? ContextMenuStrip.ShadowMargin
                    : itemBounds.Left;
                // Flip to left side: submenu's background right edge should align with parent's left edge + 1px overlap
                // submenuSize.Width includes shadow on both sides, so actual background width = submenuSize.Width - 2*shadow
                // We want: desiredBgLeft + (submenuSize.Width - 2*shadow) = parentContentLeft - 1
                desiredBgLeft = parentContentLeft - submenuSize.Width + 2f * shadow - 1f;
                // If still off-screen to the left, clamp within bounds
                var screenTargetLeft = PointToScreen(new SKPoint((int)desiredBgLeft, (int)desiredBgTop)).X;
                if (screenTargetLeft < screenBounds.Left)
                {
                    desiredBgLeft += (screenBounds.Left - screenTargetLeft);
                }
            }
            
            // Convert desired background origin to dropdown's control origin (includes shadow space).
            var targetX = (int)Math.Round(desiredBgLeft - shadow);
            var targetY = (int)Math.Round(desiredBgTop - shadow);
            screenPoint = PointToScreen(new SKPoint(targetX, targetY));
        }
        else
        {
            // Below, left aligned - check if docked at bottom
            var shadow = _activeDropDown is ContextMenuStrip ? ContextMenuStrip.ShadowMargin : 0f;

            var targetX = (int)Math.Round(itemBounds.Left - shadow);
            int targetY;

            // If MenuStrip is docked at bottom, open upwards
            if (Dock == DockStyle.Bottom)
            {
                var popupSize = _activeDropDown.MeasurePreferredSize();
                // Keep background aligned even though popup includes shadow space.
                targetY = (int)Math.Round(itemBounds.Top - popupSize.Height - 4 + shadow);
            }
            else
            {
                targetY = (int)Math.Round(itemBounds.Bottom + 4 - shadow);
            }

            screenPoint = PointToScreen(new SKPoint(targetX, targetY));
        }

        _activeDropDownOwner = item;
        _openedItem = item;

        // Initialize dropdown DPI from parent before showing
        if (_activeDropDown != null) _activeDropDown.InitializeDpi(DeviceDpi);

        _activeDropDown.Show(this, screenPoint);

        // İlk açılışta da her zaman en üst z-index'te olsun.
        if (FindForm() is UIWindowBase uiw)
        {
            uiw.BringToFront(_activeDropDown);

            // Re-assert top z-order after current message loop to avoid
            // first-show draw races where popup may appear behind other elements.
            try
            {
                uiw.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        _activeDropDown?.BringToFront();
                        uiw.BringToFront(_activeDropDown);
                        uiw.Invalidate();
                    }
                    catch
                    {
                    }
                }));
            }
            catch
            {
            }
        }

        Invalidate();
    }

    protected void CloseSubmenu()
    {
        if (_activeDropDown != null && _activeDropDown.IsOpen) _activeDropDown.Hide();
        _openedItem = null;
        _activeDropDownOwner = null;
        Invalidate();
    }

    protected void EnsureDropDownHost()
    {
        if (_activeDropDown != null) return;
        _activeDropDown = new ContextMenuStrip { AutoClose = true, Dock = DockStyle.None };
        _activeDropDown.ParentDropDown = this as ContextMenuStrip;
        _activeDropDown.Opening += (_, _) => SyncDropDownAppearance();
        _activeDropDown.Closing += (_, _) =>
        {
            _openedItem = null;
            _activeDropDownOwner = null;
            Invalidate();
        };
    }

    private void SyncDropDownAppearance()
    {
        if (_activeDropDown == null) return;
        _activeDropDown.MenuBackColor = SubmenuBackColor;
        _activeDropDown.MenuForeColor = MenuForeColor;
        _activeDropDown.HoverBackColor = HoverBackColor;
        _activeDropDown.HoverForeColor = HoverForeColor;
        _activeDropDown.SubmenuBackColor = SubmenuBackColor;
        _activeDropDown.SeparatorColor = SeparatorColor;
        _activeDropDown.RoundedCorners = RoundedCorners;
        _activeDropDown.ItemPadding = Math.Max(ItemPadding, 6f);
        _activeDropDown.Orientation = Orientation.Vertical;
        _activeDropDown.ImageScalingSize = ImageScalingSize;
        _activeDropDown.ShowSubmenuArrow = ShowSubmenuArrow;
        _activeDropDown.ShowIcons = ShowIcons;
        _activeDropDown.ShowCheckMargin = ShowCheckMargin;
        _activeDropDown.ShowImageMargin = ShowImageMargin;
    }

    protected internal virtual MenuItem CloneMenuItem(MenuItem source)
    {
        if (source is MenuItemSeparator separator)
        {
            var cloneSeparator = new MenuItemSeparator
            {
                Height = separator.Height, Margin = separator.Margin, LineColor = separator.LineColor,
                ShadowColor = separator.ShadowColor
            };
            return cloneSeparator;
        }

        var clone = new MenuItem
        {
            Text = source.Text, Icon = source.Icon, Image = source.Image, ShortcutKeys = source.ShortcutKeys,
            ShowSubmenuArrow = source.ShowSubmenuArrow, ForeColor = source.ForeColor, BackColor = source.BackColor,
            Enabled = source.Enabled, Visible = source.Visible, Font = source.Font, AutoSize = source.AutoSize,
            Padding = source.Padding, Tag = source.Tag, Checked = source.Checked
        };
        foreach (var child in source.DropDownItems) clone.AddDropDownItem(CloneMenuItem(child));
        clone.Click += (_, _) =>
        {
            source.OnClick();
            _activeDropDown?.Hide();
        };
        return clone;
    }

    protected float MeasureItemWidth(MenuItem item)
    {
        if (item is MenuItemSeparator) return 20f * ScaleFactor;

        var font = GetDefaultSkFont();
        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;

        var tb = new SkiaSharp.SKRect();
        font.MeasureText(item.Text.Replace("&", string.Empty), out tb);
        var w = tb.Width + GetPrimaryTextInset(item, vertical) + GetTrailingTextInset(item, vertical);

        // Check/Icon area logic matching DrawMenuItem
        // If has check state OR icon, we reserve checkAreaWidth (20)
        // If has icon AND ShowIcons, we reserve iconWidth (IconSize + 6)

        bool hasCheckArea = vertical && (item.CheckState != CheckState.Unchecked || item.Icon != null);
        if (hasCheckArea) w += 20 * ScaleFactor;

        if (vertical && ShowIcons && item.Icon != null)
            w += (_iconSize + 6) * ScaleFactor;
        
        return w;
    }

    private float GetHorizontalMenuInset()
    {
        return Math.Max(4f * ScaleFactor, ItemPadding * 0.5f);
    }

    private float GetHorizontalItemGap()
    {
        return Math.Max(4f * ScaleFactor, ItemPadding);
    }

    private void UpdateMenuStripHeight()
    {
        Height = (int)Math.Ceiling(ItemHeight + Padding.Vertical);
    }

    private static SkiaSharp.SKRect GetContentBounds(SkiaSharp.SKRect bounds, Thickness padding)
    {
        return new SkiaSharp.SKRect(
            bounds.Left + padding.Left,
            bounds.Top + padding.Top,
            Math.Max(bounds.Left + padding.Left, bounds.Right - padding.Right),
            Math.Max(bounds.Top + padding.Top, bounds.Bottom - padding.Bottom));
    }

    private SkiaSharp.SKRect GetContentBounds(SkiaSharp.SKRect bounds)
    {
        return GetContentBounds(bounds, Padding);
    }

    private static SkiaSharp.SKRect GetHoverBounds(SkiaSharp.SKRect bounds, bool vertical, float scale)
    {
        if (vertical)
            return bounds;

        var insetX = 1.5f * scale;
        var insetY = 1f * scale;
        return new SkiaSharp.SKRect(
            bounds.Left + insetX,
            bounds.Top + insetY,
            bounds.Right - insetX,
            bounds.Bottom - insetY);
    }

    private float GetHorizontalArrowReserve()
    {
        return GetHorizontalArrowGap() + GetHorizontalArrowSlotWidth() + GetHorizontalArrowEndPadding();
    }

    private float GetHorizontalArrowGap()
    {
        return 2f * ScaleFactor;
    }

    private float GetHorizontalArrowSlotWidth()
    {
        return Math.Max((_submenuArrowSize + 2f) * ScaleFactor, 10f * ScaleFactor);
    }

    private float GetHorizontalArrowEndPadding()
    {
        return 8f * ScaleFactor;
    }

    private float GetPrimaryTextInset(MenuItem item, bool vertical)
    {
        if (vertical)
            return Math.Max(6f * ScaleFactor, item.Padding.Left * ScaleFactor);

        return Math.Max(10f * ScaleFactor, item.Padding.Left * ScaleFactor + 2f * ScaleFactor);
    }

    private float GetTrailingTextInset(MenuItem item, bool vertical)
    {
        if (vertical)
        {
            var reserve = Math.Max(6f * ScaleFactor, item.Padding.Right * ScaleFactor);
            if (ShowSubmenuArrow && item.HasDropDown)
                reserve += 24f * ScaleFactor;
            return reserve;
        }

        var trailing = Math.Max(10f * ScaleFactor, item.Padding.Right * ScaleFactor + 2f * ScaleFactor);
        if (ShowSubmenuArrow && item.HasDropDown)
            trailing += GetHorizontalArrowReserve();
        return trailing;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Elapsed -= AnimationTimer_Tick;
                _animationTimer.Dispose();
            }

            foreach (var anim in _itemHoverAnims.Values)
                anim?.Dispose();
            _itemHoverAnims.Clear();

            _activeDropDown?.Dispose();

            _bgPaint?.Dispose();
            _bottomBorderPaint?.Dispose();
            _hoverBgPaint?.Dispose();
            _checkPaint?.Dispose();
            _imgPaint?.Dispose();
            _textPaint?.Dispose();
            _arrowPaint?.Dispose();
            _chevronPath?.Dispose();
            _defaultSkFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    private AnimationManager EnsureHoverAnim(MenuItem item)
    {
        if (!_itemHoverAnims.TryGetValue(item, out var engine))
        {
            engine = new AnimationManager()
                { Increment = 0.28, AnimationType = AnimationType.EaseOut, InterruptAnimation = true };
            engine.OnAnimationProgress += _ => Invalidate();
            _itemHoverAnims[item] = engine;
        }

        return engine;
    }
}