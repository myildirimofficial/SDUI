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
    private const Keys ShortcutModifierMask = Keys.Shift | Keys.Control | Keys.Alt;

    private readonly Dictionary<MenuItem, AnimationManager> _itemHoverAnims = new();

    // backing fields
    private const double DefaultSubmenuCloseDelayMs = 140d;
    private readonly int _submenuAnimationDuration = 150;
    private readonly float _submenuArrowSize = 8f;
    private ContextMenuStrip? _activeDropDown;
    private MenuItem? _activeDropDownOwner;
    private float _animationProgress;
    private Timer? _animationTimer;
    private SKPaint? _arrowPaint;

    private SKPaint? _bgPaint;
    private SKPaint? _bottomBorderPaint;
    private SKPaint? _checkPaint;
    private SKPath? _checkPath;
    private SKPath? _chevronPath;
    private SKFont? _defaultSkFont;
    private int _defaultSkFontDpi;
    private SKFont? _defaultSkFontSource;
    private SKFont? _shortcutSkFont;
    private int _shortcutSkFontDpi;
    private SKFont? _shortcutSkFontSource;
    private SKColor _hoverBackColor = SKColor.Empty;
    private SKPaint? _hoverBgPaint;
    private MenuItem? _hoveredItem;
    private SKColor _hoverForeColor = SKColor.Empty;
    private float _iconSize = 16f;
    private SKSize _imageScalingSize = new(20, 20);
    private SKPaint? _imgPaint;
    private bool _isAnimating;
    private float _itemHeight = 28f;
    private float _itemPadding = 6f;
    private SKColor _menuBackColor = SKColor.Empty;
    private SKColor _menuForeColor = SKColor.Empty;
    private MenuItem? _openedItem;
    private Orientation _orientation = Orientation.Horizontal;
    private bool _roundedCorners = true;
    private SKColor _separatorBackColor = SKColor.Empty;
    private SKColor _separatorColor = SKColor.Empty;
    private SKColor _separatorForeColor = SKColor.Empty;
    private float _separatorMargin = 4f;
    private bool _showCheckMargin = true;
    private bool _showHoverEffect = true;
    private bool _showIcons = true;
    private bool _showImageMargin = false;
    private bool _showShortcutKeys = true;
    private bool _showSubmenuArrow = true;
    private bool _stretch;
    private SKColor _submenuBackColor = SKColor.Empty;
    private SKColor _submenuBorderColor = SKColor.Empty;
    private Timer? _submenuCloseTimer;
    private SKPaint? _textPaint;

    public MenuStrip()
    {
        Padding = new Thickness(8, 2, 8, 2);
        UpdateMenuStripHeight();
        BackColor = ColorScheme.Surface;
        ForeColor = ColorScheme.ForeColor;
        InitializeAnimationTimer();
        ColorScheme.ThemeChanged += OnThemeChanged;
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
            BackColor = value;
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
            SyncDropDownAppearance();
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
            SyncDropDownAppearance();
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
    [DefaultValue(true)]
    public bool ShowShortcutKeys
    {
        get => _showShortcutKeys;
        set
        {
            if (_showShortcutKeys == value) return;
            _showShortcutKeys = value;
            SyncDropDownAppearance();
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
        _shortcutSkFont?.Dispose();
        _shortcutSkFont = null;
        _shortcutSkFontSource = null;
        _shortcutSkFontDpi = 0;
    }

    internal override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        UpdateMenuStripHeight();
    }

    private SKFont GetDefaultSkFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        var font = ResolvedFont;
        if (_defaultSkFont == null || !ReferenceEquals(_defaultSkFontSource, font) || _defaultSkFontDpi != dpi)
        {
            _defaultSkFont?.Dispose();
            _defaultSkFont = new SKFont(font.Typeface ?? SKTypeface.Default)
            {
                Size = font.Size.Topx(this),
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full
            };
            _defaultSkFontSource = font;
            _defaultSkFontDpi = dpi;
        }

        return _defaultSkFont;
    }

    protected SKFont GetShortcutSkFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        var font = ResolvedFont;
        if (_shortcutSkFont == null || !ReferenceEquals(_shortcutSkFontSource, font) || _shortcutSkFontDpi != dpi)
        {
            _shortcutSkFont?.Dispose();

            var shortcutWeight = font.IsBold() ? SKFontStyleWeight.Bold : SKFontStyleWeight.SemiBold;
            using var shortcutTypeface = SKTypeface.FromFamilyName(
                font.GetFamilyName(),
                new SKFontStyle(shortcutWeight, SKFontStyleWidth.Normal, font.IsItalic() ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright));

            _shortcutSkFont = new SKFont(shortcutTypeface ?? SKTypeface.Default)
            {
                Size = 8.Topx(this),
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full
            };
            _shortcutSkFontSource = font;
            _shortcutSkFontDpi = dpi;
        }

        return _shortcutSkFont;
    }

    private List<(MenuItem Item, SKRect Rect)> GetItemEntries()
    {
        var entries = new List<(MenuItem Item, SKRect Rect)>(Items.Count);
        var rects = ComputeItemRects();
        var rectIndex = 0;

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            if (!item.Visible)
                continue;

            if (rectIndex >= rects.Count)
                break;

            entries.Add((item, rects[rectIndex++]));
        }

        return entries;
    }

    private void RefreshOpenSubmenuAnchor()
    {
        if (_activeDropDown == null || !_activeDropDown.IsOpen || _openedItem == null)
            return;

        var itemBounds = GetItemBounds(_openedItem);
        if (itemBounds.IsEmpty)
            return;

        _activeDropDown.UpdateAnchorBounds(this, itemBounds);
    }

    private void InitializeAnimationTimer()
    {
        _animationTimer = new Timer { Interval = 16 };
        _animationTimer.Elapsed += AnimationTimer_Tick;

        _submenuCloseTimer = new Timer { Interval = DefaultSubmenuCloseDelayMs, AutoReset = false };
        _submenuCloseTimer.Elapsed += SubmenuCloseTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        if (!_isAnimating)
            return;

        _animationProgress = Math.Min(1f, _animationProgress + 16f / _submenuAnimationDuration);

        if (_animationProgress >= 1f)
        {
            _isAnimating = false;
            _animationTimer?.Stop();
        }

        Invalidate();
    }

    private void SubmenuCloseTimer_Tick(object? sender, ElapsedEventArgs e)
    {
        ExecuteOnMenuThread(() =>
        {
            if (_openedItem == null || _hoveredItem?.HasDropDown == true)
                return;

            CloseSubmenu();
        });
    }

    protected void CancelPendingSubmenuClose()
    {
        _submenuCloseTimer?.Stop();
    }

    protected void ScheduleSubmenuClose()
    {
        if (_openedItem == null)
            return;

        if (_hoveredItem?.HasDropDown == true)
            return;

        _submenuCloseTimer?.Stop();
        _submenuCloseTimer?.Start();
    }

    private void ExecuteOnMenuThread(Action action)
    {
        var window = FindForm() as WindowBase;
        if (window == null)
        {
            action();
            return;
        }

        try
        {
            window.BeginInvoke(action);
        }
        catch
        {
            action();
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_menuBackColor.IsEmpty())
            BackColor = ColorScheme.Surface;

        if (_menuForeColor.IsEmpty())
            ForeColor = ColorScheme.ForeColor;

        SyncDropDownAppearance();
        Invalidate();
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RefreshOpenSubmenuAnchor();
    }

    internal override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        RefreshOpenSubmenuAnchor();
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
            item.Parent = null!;
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
            var entries = GetItemEntries();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                DrawMenuItem(canvas, entry.Item, entry.Rect);
            }
        }
        else
        {
            var entries = GetItemEntries();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                DrawMenuItem(canvas, entry.Item, entry.Rect);
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
                Style = SKPaintStyle.Fill
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
                IsAntialias = true
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
        var shortcutText = GetShortcutText(item, vertical);
        var shouldDrawShortcut = shortcutText.Length > 0;

        var font = GetDefaultSkFont();
        var shortcutFont = shouldDrawShortcut ? GetShortcutSkFont() : font;
        _textPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _textPaint.Color = textColor;
        
        // Reserve space for chevron in vertical mode
        var drawBounds = new SkiaSharp.SKRect(
            tx,
            bounds.Top,
            Math.Max(tx, bounds.Right - contentRightInset),
            bounds.Bottom);
        DrawControlText(c, item.Text, drawBounds, _textPaint, font, ContentAlignment.MiddleLeft, false, true);

        if (shouldDrawShortcut)
        {
            var shortcutRight = bounds.Right - Math.Max(8f * scale, item.Padding.Right * scale + 2f * scale);
            if (ShowSubmenuArrow && item.HasDropDown)
                shortcutRight -= 30f * scale;

            var shortcutWidth = MeasureShortcutTextWidth(shortcutFont, shortcutText);
            var shortcutBounds = new SkiaSharp.SKRect(
                Math.Max(tx, shortcutRight - shortcutWidth),
                bounds.Top,
                Math.Max(tx, shortcutRight),
                bounds.Bottom);

            _textPaint.Color = textColor.WithAlpha(148);
            DrawControlText(c, shortcutText, shortcutBounds, _textPaint, shortcutFont, ContentAlignment.MiddleRight, false, true);
            _textPaint.Color = textColor;
        }

        // Measure text width for arrow positioning
        var textBounds = new SkiaSharp.SKRect();
        font.MeasureText(item.Text.Replace("&", ""), out textBounds);

        // WinUI3 style chevron arrow with high quality
        if (ShowSubmenuArrow && item.HasDropDown)
        {
            _arrowPaint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
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
            const float margin = 0f;
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

    protected virtual SkiaSharp.SKRect GetItemBounds(MenuItem item)
    {
        var entries = GetItemEntries();
        for (var i = 0; i < entries.Count; i++)
            if (ReferenceEquals(entries[i].Item, item))
                return entries[i].Rect;

        return SkiaSharp.SKRect.Empty;
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var entries = GetItemEntries();
        MenuItem? hovered = null;
        for (var i = 0; i < entries.Count; i++)
            if (entries[i].Rect.Contains(e.Location))
            {
                hovered = entries[i].Item;
                break;
            }

        if (_hoveredItem != hovered)
        {
            _hoveredItem = hovered;
            
            // If hovering over a different item with dropdown, switch to it
            if (_hoveredItem?.HasDropDown == true && _openedItem != _hoveredItem)
            {
                CancelPendingSubmenuClose();
                OpenSubmenu(_hoveredItem);
            }
            // If hovering over item without dropdown but a submenu is open, close it
            else if (_hoveredItem != null && !_hoveredItem.HasDropDown && _openedItem != null)
            {
                ScheduleSubmenuClose();
            }
            // If mouse left all items (null) and submenu is open, close it
            else if (_hoveredItem == null && _openedItem != null)
            {
                ScheduleSubmenuClose();
            }
            else if (_hoveredItem?.HasDropDown == true)
            {
                CancelPendingSubmenuClose();
            }
            
            Invalidate();
        }
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        CancelPendingSubmenuClose();
        var entries = GetItemEntries();
        for (var i = 0; i < entries.Count; i++)
            if (entries[i].Rect.Contains(e.Location))
            {
                OnItemClicked(entries[i].Item);
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
        ScheduleSubmenuClose();
        Invalidate();
    }

    protected void OpenSubmenu(MenuItem item)
    {
        CancelPendingSubmenuClose();

        if (!item.HasDropDown)
        {
            CloseSubmenu();
            return;
        }

        if (_activeDropDownOwner == item && _activeDropDown != null && _activeDropDown.IsOpen) return;

        CloseSubmenu();
        EnsureDropDownHost();
        var activeDropDown = _activeDropDown!;
        activeDropDown.ParentDropDown = this as ContextMenuStrip;
        activeDropDown.ResetStableAccordionPopupSize();
        activeDropDown.Items.Clear();

        foreach (var child in item.DropDownItems)
            activeDropDown.AddItem(CloneMenuItem(child));

        SyncDropDownAppearance();

        var itemBounds = GetItemBounds(item);
        var vertical = Orientation == Orientation.Vertical || this is ContextMenuStrip;

        _activeDropDownOwner = item;
        _openedItem = item;

        // Initialize dropdown DPI from parent before showing
        activeDropDown.InitializeDpi(DeviceDpi);

        if (vertical)
            activeDropDown.ShowAnchoredBeside(this, itemBounds);
        else
            activeDropDown.ShowAnchoredBelow(this, itemBounds);

        // İlk açılışta da her zaman en üst z-index'te olsun.
        if (FindForm() is WindowBase uiw)
        {
            uiw.BringToFront(activeDropDown);

            // Re-assert top z-order after current message loop to avoid
            // first-show draw races where popup may appear behind other elements.
            try
            {
                uiw.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        _activeDropDown?.BringToFront();
                        if (_activeDropDown != null)
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
        CancelPendingSubmenuClose();
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
        _activeDropDown.UseAccordionSubmenus = this is ContextMenuStrip contextMenu && contextMenu.UseAccordionSubmenus;
        _activeDropDown.MenuBackColor = SubmenuBackColor;
        _activeDropDown.BackColor = SubmenuBackColor;
        _activeDropDown.MenuForeColor = MenuForeColor;
        _activeDropDown.HoverBackColor = HoverBackColor;
        _activeDropDown.HoverForeColor = HoverForeColor;
        _activeDropDown.SubmenuBackColor = SubmenuBackColor;
        _activeDropDown.SeparatorColor = SeparatorColor;
        _activeDropDown.SeparatorMargin = Math.Max(SeparatorMargin, _activeDropDown.ItemPadding * 0.5f);
        _activeDropDown.RoundedCorners = RoundedCorners;
        _activeDropDown.Radius = RoundedCorners
            ? new Radius((int)Math.Round(10f * _activeDropDown.ScaleFactor))
            : new Radius(0);
        _activeDropDown.ItemPadding = Math.Max(ItemPadding, 6f);
        _activeDropDown.Orientation = Orientation.Vertical;
        _activeDropDown.ImageScalingSize = ImageScalingSize;
        _activeDropDown.ShowSubmenuArrow = ShowSubmenuArrow;
        _activeDropDown.ShowIcons = ShowIcons;
        _activeDropDown.ShowCheckMargin = ShowCheckMargin;
        _activeDropDown.ShowImageMargin = ShowImageMargin;
        _activeDropDown.ShowShortcutKeys = ShowShortcutKeys;
        _activeDropDown.Border = new Thickness(1);
        _activeDropDown.Invalidate();
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
            Enabled = source.Enabled, Visible = source.Visible, AutoSize = source.AutoSize,
            Padding = source.Padding, Tag = source.Tag, Checked = source.Checked
        };
        if (source.HasCustomFont)
            clone.Font = source.ResolvedFont;
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

        if (vertical)
            w += GetShortcutTextReserve(item, vertical, font);
        
        return w;
    }

    protected string GetShortcutText(MenuItem item, bool vertical)
    {
        if (!vertical || !ShowShortcutKeys || item.ShortcutKeys == Keys.None)
            return string.Empty;

        var parts = new List<string>(4);
        var shortcut = item.ShortcutKeys;

        if ((shortcut & Keys.Control) == Keys.Control)
            parts.Add("Ctrl");

        if ((shortcut & Keys.Shift) == Keys.Shift)
            parts.Add("Shift");

        if ((shortcut & Keys.Alt) == Keys.Alt)
            parts.Add("Alt");

        var keyCode = shortcut & ~ShortcutModifierMask;
        var keyName = GetShortcutKeyName(keyCode);
        if (keyName.Length > 0)
            parts.Add(keyName);

        return string.Join("+", parts);
    }

    private static string GetShortcutKeyName(Keys keyCode)
    {
        return keyCode switch
        {
            Keys.None => string.Empty,
            Keys.Enter => "Enter",
            Keys.Escape => "Esc",
            Keys.Space => "Space",
            Keys.Left => "Left",
            Keys.Up => "Up",
            Keys.Right => "Right",
            Keys.Down => "Down",
            Keys.Back => "Backspace",
            Keys.Tab => "Tab",
            Keys.Home => "Home",
            Keys.End => "End",
            Keys.D0 => "0",
            Keys.D1 => "1",
            Keys.D2 => "2",
            Keys.D3 => "3",
            Keys.D4 => "4",
            Keys.D5 => "5",
            Keys.D6 => "6",
            Keys.D7 => "7",
            Keys.D8 => "8",
            Keys.D9 => "9",
            _ => keyCode.ToString()
        };
    }

    protected float MeasureShortcutTextWidth(SKFont font, string shortcutText)
    {
        if (string.IsNullOrWhiteSpace(shortcutText))
            return 0f;

        var bounds = new SKRect();
        font.MeasureText(shortcutText, out bounds);
        return bounds.Width;
    }

    protected float GetShortcutTextReserve(MenuItem item, bool vertical, SKFont font)
    {
        var shortcutText = GetShortcutText(item, vertical);
        if (shortcutText.Length == 0)
            return 0f;

        return MeasureShortcutTextWidth(GetShortcutSkFont(), shortcutText) + 22f * ScaleFactor;
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
            reserve += GetShortcutTextReserve(item, vertical, GetDefaultSkFont());
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
                _animationTimer = null;
            }

            if (_submenuCloseTimer != null)
            {
                _submenuCloseTimer.Stop();
                _submenuCloseTimer.Elapsed -= SubmenuCloseTimer_Tick;
                _submenuCloseTimer.Dispose();
                _submenuCloseTimer = null;
            }

            ColorScheme.ThemeChanged -= OnThemeChanged;

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
            _defaultSkFont = null;
            _shortcutSkFont?.Dispose();
            _shortcutSkFont = null;
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