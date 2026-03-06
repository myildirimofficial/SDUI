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
    private float _itemHeight = 24f;
    private float _itemPadding = 7f; // temel boşluk, ekstra spacing sınırlanacak
    private SKColor _menuBackColor = SKColor.Empty;
    private SKColor _menuForeColor = SKColor.Empty;
    private MenuItem _openedItem;
    private Orientation _orientation = Orientation.Horizontal;
    private bool _roundedCorners = true;
    private SKColor _separatorBackColor = SKColor.Empty;
    private SKColor _separatorColor = SKColor.Empty;
    private SKColor _separatorForeColor = SKColor.Empty;
    private float _separatorHeight = 1f;
    private float _separatorMargin = 4f;
    private bool _showCheckMargin = true;
    private bool _showHoverEffect = true;
    private bool _showIcons = true;
    private bool _showImageMargin = false;
    private bool _showSubmenuArrow = true;
    private bool _stretch = true;
    private SKColor _submenuBackColor = SKColor.Empty;
    private SKColor _submenuBorderColor = SKColor.Empty;
    private SKPaint? _textPaint;

    public MenuStrip()
    {
        Height = (int)_itemHeight;
        BackColor = ColorScheme.Surface;
        ForeColor = ColorScheme.ForeColor;
        InitializeAnimationTimer();
    }

    [Browsable(false)] public List<MenuItem> Items { get; } = new();

    [Category("Behavior")]
    [DefaultValue(true)]
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
            Height = (int)value;
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

        // Flat modern background
        _bgPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _bgPaint.Color = MenuBackColor;
        canvas.DrawRect(new SkiaSharp.SKRect(0, 0, bounds.Width, bounds.Height), _bgPaint);

        // Subtle bottom border
        _bottomBorderPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _bottomBorderPaint.Color = SeparatorColor.WithAlpha(100);
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
            var y = ItemPadding;
            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var w = bounds.Width - ItemPadding * 2;
                var r = new SkiaSharp.SKRect(ItemPadding, y, ItemPadding + w, y + ItemHeight);
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

        // High-quality hover background with proper anti-aliasing
        if (ShowHoverEffect && hover)
        {
            var blend = _hoverBackColor;
            // Reduced opacity for softer look (approx 10% to 25%) - much lighter than before
            var alpha = (byte)(25 + 40 * prog);
            _hoverBgPaint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                FilterQuality = SKFilterQuality.High
            };
            _hoverBgPaint.Color = blend.WithAlpha(alpha);
            var rr = new SKRoundRect(bounds, 5 * scale);
            c.DrawRoundRect(rr, _hoverBgPaint);
        }

        var tx = bounds.Left + 6 * scale;

        // Checkmark area (left margin for checkbox/radio)
        float checkAreaWidth = 20 * scale;
        if (item.CheckState != CheckState.Unchecked || item.Icon != null)
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
        var drawWidth = bounds.Right - tx;
        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;
        if (vertical && ShowSubmenuArrow && item.HasDropDown)
        {
            drawWidth -= 35 * scale; // Reserve space for chevron
        }
        
        var drawBounds = SkiaSharp.SKRect.Create(tx, bounds.Top, drawWidth, bounds.Height);
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

            var chevronSize = 5f * scale; // Increased from 5 to 6 for better visibility
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
                arrowX = tx + textBounds.Width + 10 * scale;

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
        var b = ClientRectangle;

        if (Orientation == Orientation.Horizontal)
        {
            var x = ItemPadding + 6 * ScaleFactor;
            var available = b.Width - ItemPadding * 2 - 12 * ScaleFactor;
            float total = 0;
            var widths = new float[Items.Count];

            for (var i = 0; i < Items.Count; i++)
            {
                widths[i] = MeasureItemWidth(Items[i]);
                total += widths[i];
                if (i < Items.Count - 1)
                    total += ItemPadding;
            }

            float extra = 0;
            if (Stretch && Items.Count > 1 && total < available)
            {
                var rawExtra = (available - total) / (Items.Count - 1);
                var maxExtraPerGap = ItemPadding;
                extra = Math.Min(rawExtra, maxExtraPerGap);
            }

            for (var i = 0; i < Items.Count; i++)
            {
                var w = widths[i];
                rects.Add(SKRect.Create(x, 0, w, ItemHeight));
                x += w + ItemPadding + (i < Items.Count - 1 ? extra : 0);
            }
        }
        else
        {
            // Dikey menüler ve ContextMenuStrip için; separator'lar da
            // ContextMenuStrip'teki satır yerleşimi ile aynı mantığı kullanmalı ki
            // hover alanı ile çizim hizalı olsun.
            var margin = this is ContextMenuStrip ? ContextMenuStrip.ShadowMargin : 0f;
            var y = margin + ItemPadding;
            var w = b.Width - margin * 2 - ItemPadding * 2;
            var x = margin + ItemPadding;

            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                // Skip invisible items when computing rects
                if (!item.Visible) continue;

                if (item.IsSeparator)
                {
                    // İnce çizgi için küçük bir satır yüksekliği ayırıyoruz.
                    var sepHeight = SeparatorMargin * 2 + 1;
                    rects.Add(SKRect.Create(x, y, w, sepHeight));
                    y += sepHeight + ItemPadding;
                    continue;
                }

                rects.Add(SKRect.Create(x, y, w, ItemHeight));
                y += ItemHeight + ItemPadding;
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

    private void OpenSubmenu(MenuItem item)
    {
        if (!item.HasDropDown)
        {
            CloseSubmenu();
            return;
        }

        if (_activeDropDownOwner == item && _activeDropDown != null && _activeDropDown.IsOpen) return;

        CloseSubmenu();
        EnsureDropDownHost();
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

        var tb = new SkiaSharp.SKRect();
        font.MeasureText(item.Text, out tb);
        var w = tb.Width + 12 * ScaleFactor; // Scaled padding (6 left + 6 right)

        // Check/Icon area logic matching DrawMenuItem
        // If has check state OR icon, we reserve checkAreaWidth (20)
        // If has icon AND ShowIcons, we reserve iconWidth (IconSize + 6)
        
        bool hasCheckArea = item.CheckState != CheckState.Unchecked || item.Icon != null;
        if (hasCheckArea) w += 20 * ScaleFactor;

        if (ShowIcons && item.Icon != null)
            w += (_iconSize + 6) * ScaleFactor;

        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;
        
        if (ShowSubmenuArrow && item.HasDropDown)
        {
            // Reserve space for arrow.
            // Vertical uses right-aligned arrow logic (reserved space).
            // Horizontal uses appended arrow logic (text + gap + arrow).
            if (vertical)
                w += 30 * ScaleFactor;
            else
                w += 4 * ScaleFactor; // Support for horizontal menu arrows
        }

        return w;
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