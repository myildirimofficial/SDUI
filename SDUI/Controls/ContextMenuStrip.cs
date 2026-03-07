using SDUI.Animation;
using SDUI.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SDUI.Controls;

public enum OpeningEffectType
{
    Fade,
    SlideDownFade
}

public class ContextMenuStrip : MenuStrip
{
    internal const float ShadowMargin = 7f;
    private const int MaxIconCacheEntries = 256;
    private const float CheckMarginWidth = 22f;
    private readonly AnimationManager _fadeInAnimation;

    private readonly Dictionary<MenuItem, AnimationManager> _itemHoverAnims = new();
    private readonly SKMaskFilter?[] _shadowMaskFilters = new SKMaskFilter?[2];

    private readonly SKPaint?[] _shadowPaints = new SKPaint?[2];
    private SKPaint? _arrowPaint;

    // Cached Skia resources (avoid per-frame allocations)
    private SKPaint? _bgPaint;
    private SKPaint? _borderPaint;
    private SKPath? _chevronPath;

    private SKFont? _defaultSkFont;
    private int _defaultSkFontDpi;
    private Font? _defaultSkFontSource;
    private MenuItem _hoveredItem;
    private SKPaint? _hoverPaint;
    private SKPaint? _iconPaint;
    private EventHandler _ownerDeactivateHandler;
    private KeyEventHandler _ownerKeyDownHandler;
    private MouseEventHandler _ownerMouseDownHandler;
    private bool _ownerPreviousKeyPreview;
    private UIWindowBase _ownerWindow;
    private SKPaint? _separatorPaint;
    private SKPaint? _textPaint;
    private SKPaint? _layerPaint;
    private OpeningEffectType _openingEffect = OpeningEffectType.Fade;
    private bool _openingUpwards;

    public ContextMenuStrip()
    {
        Visible = false;
        AutoSize = false;
        TabStop = false;
        Orientation = Orientation.Vertical;
        BackColor = SKColors.Transparent;
        ItemHeight = 32f; // Increased height significantly
        ItemPadding = 8f; // Increased vertical margin

        _fadeInAnimation = new AnimationManager
        {
            Increment = 0.20,
            AnimationType = AnimationType.EaseOut,
            Singular = true,
            InterruptAnimation = false
        };
        _fadeInAnimation.OnAnimationProgress += _ => Invalidate();
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool AutoClose { get; set; } = true;

    [Category("Appearance")]
    [DefaultValue(OpeningEffectType.Fade)]
    public OpeningEffectType OpeningEffect
    {
        get => _openingEffect;
        set
        {
            if (_openingEffect == value) return;
            _openingEffect = value;
        }
    }

    [Browsable(false)]
    public bool IsOpen { get; private set; }

    [Browsable(false)]
    public ElementBase SourceElement { get; private set; }

    internal ContextMenuStrip? ParentDropDown { get; set; }

    public event CancelEventHandler Opening;
    public event CancelEventHandler Closing;

    public SKSize MeasurePreferredSize()
    {
        return GetPrefSize();
    }

    public void Show(ElementBase element, SKPoint location)
    {
        if (IsOpen) return;
        var owner = ResolveOwner(element);
        if (owner == null) return;


        var canceling = new CancelEventArgs();
        Opening?.Invoke(this, canceling);
        if (canceling.Cancel)
            return;

        SourceElement = element;
        _ownerWindow = owner;

        if (!_ownerWindow.Controls.Contains(this))
            _ownerWindow.Controls.Add(this);

        // Konumu ve boyutu belirle, sonra z-order'ı en üste çek.
        PositionDropDown(location);
        Visible = true;

        // WinForms z-order + SDUI'nin kendi ZOrder sistemini güncelle.
        BringToFront();
        if (_ownerWindow is UIWindowBase uiw)
        {
            uiw.BringToFront(this);

            // Ensure z-order is reasserted after current message processing to avoid
            // race where other controls draw over the popup on the first show.
            try
            {
                uiw.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        BringToFront();
                        uiw.BringToFront(this);
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

        AttachHandlers();

        _fadeInAnimation.SetProgress(0);
        _fadeInAnimation.StartNewAnimation(AnimationDirection.In);

        _ownerWindow.Invalidate();
        IsOpen = true;
    }

    public void Show(SKPoint location)
    {
        Show(null, location);
    }

    public new void Hide()
    {
        if (!IsOpen) return;

        var canceling = new CancelEventArgs();
        Closing?.Invoke(this, canceling);
        if (canceling.Cancel)
            return;

        // Close any open submenus before hiding
        CloseSubmenu();

        DetachHandlers();
        Visible = false;
        _ownerWindow?.Invalidate();
        _ownerWindow = null;
        SourceElement = null;
        ParentDropDown = null;
        IsOpen = false;
    }

    protected override void OnItemClicked(MenuItem item)
    {
        if (item.HasDropDown)
        {
            base.OnItemClicked(item);
            return;
        }

        item.OnClick();
        ClosePopupChain();
    }

    private void ClosePopupChain()
    {
        ContextMenuStrip? current = this;
        while (current != null)
        {
            var parent = current.ParentDropDown;
            current.Hide();
            current = parent;
        }
    }

    private void PositionDropDown(SKPoint screenLocation)
    {
        if (_ownerWindow == null) return;

        var windowLocation = _ownerWindow.PointToClient(screenLocation);
        var size = GetPrefSize();
        var client = _ownerWindow.ClientRectangle;

        const int MARGIN = 8;
        var maxWidth = client.Width - MARGIN * 2;
        var maxHeight = client.Height - MARGIN * 2;

        size.Width = Math.Min(size.Width, maxWidth);
        size.Height = Math.Min(size.Height, maxHeight);

        var targetX = windowLocation.X;
        var targetY = windowLocation.Y;

        if (targetX + size.Width > client.Right - MARGIN)
        {
            var leftPos = targetX - size.Width;
            if (leftPos >= client.Left + MARGIN)
                targetX = leftPos;
            else
                targetX = client.Right - size.Width - MARGIN;
        }

        if (targetY + size.Height > client.Bottom - MARGIN)
        {
            var topPos = targetY - size.Height;
            if (topPos >= client.Top + MARGIN)
            {
                targetY = topPos;
                _openingUpwards = true;
            }
            else
            {
                targetY = client.Bottom - size.Height - MARGIN;
                _openingUpwards = false;
            }
        }
        else
        {
            _openingUpwards = false;
        }

        targetX = Math.Max(client.Left + MARGIN, Math.Min(targetX, client.Right - size.Width - MARGIN));
        targetY = Math.Max(client.Top + MARGIN, Math.Min(targetY, client.Bottom - size.Height - MARGIN));

        Location = new SKPoint(targetX, targetY);
        Size = size;
    }

    private UIWindowBase ResolveOwner(ElementBase element)
    {
        if (Parent is UIWindowBase w) return w;
        if (element != null)
        {
            if (element.ParentWindow is UIWindowBase pw) return pw;
            if (element.FindForm() is UIWindowBase fw) return fw;
        }

        if (Application.ActiveForm is UIWindowBase aw) return aw;
        return Application.OpenForms.OfType<UIWindowBase>().FirstOrDefault();
    }

    private void AttachHandlers()
    {
        if (_ownerWindow == null || !AutoClose) return;
        _ownerMouseDownHandler ??= OnOwnerMouseDown;
        _ownerDeactivateHandler ??= OnOwnerDeactivate;
        _ownerKeyDownHandler ??= OnOwnerKeyDown;
        _ownerWindow.MouseDown += _ownerMouseDownHandler;
        _ownerPreviousKeyPreview = _ownerWindow.KeyPreview;
        _ownerWindow.KeyPreview = true;
        _ownerWindow.Deactivate += _ownerDeactivateHandler;
        _ownerWindow.KeyDown += _ownerKeyDownHandler;
    }

    private void DetachHandlers()
    {
        if (_ownerWindow == null) return;
        if (_ownerMouseDownHandler != null) _ownerWindow.MouseDown -= _ownerMouseDownHandler;
        if (_ownerDeactivateHandler != null) _ownerWindow.Deactivate -= _ownerDeactivateHandler;
        if (_ownerKeyDownHandler != null) _ownerWindow.KeyDown -= _ownerKeyDownHandler;
        _ownerWindow.KeyPreview = _ownerPreviousKeyPreview;
    }

    private void OnOwnerMouseDown(object sender, MouseEventArgs e)
    {
        if (!IsOpen || !AutoClose) return;
        if (!Bounds.Contains(e.Location)) Hide();
    }

    private void OnOwnerDeactivate(object sender, EventArgs e)
    {
        if (!IsOpen || !AutoClose) return;
        Hide();
    }

    private void OnOwnerKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsOpen || !AutoClose) return;
        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private SKSize GetPrefSize()
    {
        const float MinContentWidth = 180f; // Minimum içerik genişliği

        // İçerik genişlik/yükseklik hesabı (shadow hariç)
        var contentWidth = ItemPadding * 2;
        var contentHeight = ItemPadding; // Üst padding

        foreach (var item in Items)
        {
            // Respect MenuItem.Visible — skip hidden items from size calculations
            if (!item.Visible)
                continue;

            if (item.IsSeparator)
            {
                contentHeight += SeparatorMargin * 2 + 1 + ItemPadding;
            }
            else
            {
                contentWidth = Math.Max(contentWidth, MeasureItemWidth(item) + ItemPadding * 2);
                contentHeight += ItemHeight + ItemPadding;
            }
        }

        // Minimum genişlik garantisi
        contentWidth = Math.Max(contentWidth, MinContentWidth);

        // En alttaki öğenin border ile kesilmemesi için ekstra alan yok,
        // çünkü son item'dan sonra zaten ItemPadding var.

        // Shadow için her yönden ekstra alan ekle
        var totalWidth = contentWidth + ShadowMargin * 2;
        var totalHeight = contentHeight + ShadowMargin * 2;

        return new SKSize((int)Math.Ceiling(totalWidth), (int)Math.Ceiling(totalHeight));
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var y = ShadowMargin + ItemPadding;
        _hoveredItem = null;
        foreach (var item in Items)
        {
            if (item.IsSeparator)
            {
                y += SeparatorMargin * 2 + 1 + ItemPadding;
                continue;
            }

            var w = Width - ShadowMargin * 2 - ItemPadding * 2;
            var r = SKRect.Create(ShadowMargin + ItemPadding, y, w, ItemHeight);
            if (r.Contains(e.Location))
            {
                _hoveredItem = item;
                break;
            }

            y += ItemHeight + ItemPadding;
        }

        Invalidate();
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredItem = null;
        Invalidate();
    }

    private AnimationManager EnsureItemHoverAnim(MenuItem item)
    {
        if (!_itemHoverAnims.TryGetValue(item, out var engine))
        {
            engine = new AnimationManager
            { Increment = 0.25, AnimationType = AnimationType.EaseOut, Singular = true, InterruptAnimation = true };
            engine.OnAnimationProgress += _ => Invalidate();
            _itemHoverAnims[item] = engine;
        }

        return engine;
    }

    public override void OnPaint(SKCanvas canvas)
    {
        // Don't call base.OnPaint(canvas) because MenuStrip (base) draws a rectangular background
        // which conflicts with ContextMenuStrip's rounded shadow path.
        // base.OnPaint(canvas);

        var bounds = ClientRectangle;

        EnsureSkiaCaches();

        var fadeProgress = (float)_fadeInAnimation.GetProgress();
        var fadeAlpha = (byte)(fadeProgress * 255);
        const float CORNER_RADIUS = 10f;

        // Apply animation effect based on OpeningEffect property
        if (_openingEffect == OpeningEffectType.SlideDownFade)
        {
            _layerPaint ??= new SKPaint { IsAntialias = true };
            _layerPaint.Color = SKColors.White.WithAlpha(fadeAlpha);
            canvas.SaveLayer(_layerPaint);

            // Slide animation: 8px movement
            var translateY = (_openingUpwards ? 1f - fadeProgress : fadeProgress - 1f) * 8f;
            canvas.Translate(0, translateY);
        }

        // Start fresh: Clear the canvas area to fully transparent before drawing the shadow/popup.
        // This is crucial because the parent window might have drawn something underneath?
        // Actually, SDUI renderers usually handle the background for the Window, but since this is a child control,
        // we might be drawing on top of existing pixels. 
        // Skia usually composes correctly, but if "kare gibi render ediyor" (rendering like a square) means "seeing square artifacts",
        // it's likely the base class drawing a rect.

        var contentRect = new SkiaSharp.SKRect(
            ShadowMargin,
            ShadowMargin,
            bounds.Width - ShadowMargin,
            bounds.Height - ShadowMargin);

        // Multi-layer shadow system (extra subtle)
        canvas.Save();
        EnsureShadowResources();
        for (var i = 0; i < 2; i++)
        {
            var offsetY = 0.75f + i * 0.85f;
            // Increased shadow opacity significantly to make it visible
            var shadowAlpha = (byte)((64 - i * 24) * fadeProgress);

            var shadowPaint = _shadowPaints[i]!;
            shadowPaint.Color = SKColors.Black.WithAlpha(shadowAlpha);

            canvas.Save();
            canvas.Translate(0, offsetY);
            canvas.DrawRoundRect(contentRect, CORNER_RADIUS, CORNER_RADIUS, shadowPaint);
            canvas.Restore();
        }

        canvas.Restore();

        // High-quality background
        _bgPaint!.Color = MenuBackColor.WithAlpha(fadeAlpha);
        canvas.DrawRoundRect(contentRect, CORNER_RADIUS, CORNER_RADIUS, _bgPaint);

        // Border
        _borderPaint!.Color = SeparatorColor.WithAlpha((byte)(fadeAlpha * 0.35f));
        var borderRect = new SkiaSharp.SKRect(
            contentRect.Left + 0.5f,
            contentRect.Top + 0.5f,
            contentRect.Right - 0.5f,
            contentRect.Bottom - 0.5f);
        canvas.DrawRoundRect(borderRect, CORNER_RADIUS, CORNER_RADIUS, _borderPaint);

        var scale = ScaleFactor;
        var y = contentRect.Top + ItemPadding;

        foreach (var item in Items)
        {
            // Skip hidden items — visibility should control drawing and layout
            if (!item.Visible)
                continue;

            if (item.IsSeparator)
            {
                _separatorPaint!.Color = SeparatorColor.WithAlpha(fadeAlpha);
                canvas.DrawLine(
                    contentRect.Left + ItemPadding + 8,
                    y + SeparatorMargin,
                    contentRect.Right - ItemPadding - 8,
                    y + SeparatorMargin,
                    _separatorPaint);
                y += SeparatorMargin * 2 + 1 + ItemPadding;
                continue;
            }

            var w = contentRect.Width - ItemPadding * 2;
            var itemRect = new SkiaSharp.SKRect(
                contentRect.Left + ItemPadding,
                y,
                contentRect.Left + ItemPadding + w,
                y + ItemHeight);

            var isHovered = item == _hoveredItem;
            var anim = EnsureItemHoverAnim(item);

            if (isHovered) anim.StartNewAnimation(AnimationDirection.In);
            else anim.StartNewAnimation(AnimationDirection.Out);

            var hoverProgress = (float)anim.GetProgress();

            if (hoverProgress > 0.001f || isHovered)
            {
                // Soft hover logic identical to MenuStrip
                var hoverAlpha = (byte)(150 * hoverProgress);
                _hoverPaint!.Color = HoverBackColor.WithAlpha((byte)(fadeAlpha * hoverAlpha / 255f));
                canvas.DrawRoundRect(itemRect, 7 * scale, 7 * scale, _hoverPaint);
            }

            var textX = itemRect.Left + 10 * scale; // Increased left padding for text

            // Reserve space for check mark if enabled
            if (ShowCheckMargin)
            {
                if (item.Checked)
                {
                    var cx = itemRect.Left + 12 * scale + CheckMarginWidth / 2f;
                    var cy = itemRect.MidY;
                    var s = Math.Min(8f * scale, ItemHeight / 3f);
                    // Draw checkmark with Stroke style
                    using var checkPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1.8f * scale,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeJoin = SKStrokeJoin.Round,
                        Color = MenuForeColor.WithAlpha(fadeAlpha)
                    };
                    var chk = new SKPath();
                    // Draw checkmark as proper V shape - left to center to right
                    chk.MoveTo(cx - s * 0.4f, cy - s * 0.15f);
                    chk.LineTo(cx, cy + s * 0.35f);
                    chk.LineTo(cx + s * 0.6f, cy - s * 0.5f);
                    canvas.DrawPath(chk, checkPaint);
                    chk.Dispose();
                }

                textX += CheckMarginWidth * scale;
            }

            var imageAreaWidth = (ImageScalingSize.Width + 8) * scale;

            if (ShowImageMargin)
            {
                if (ShowIcons && item.Icon != null)
                {
                    var scaledIconWidth = ImageScalingSize.Width * scale;
                    var scaledIconHeight = ImageScalingSize.Height * scale;
                    var iconY = itemRect.Top + (ItemHeight - scaledIconHeight) / 2;
                    var iconBitmap = item.Icon;
                    _iconPaint!.Color = SKColors.White.WithAlpha(fadeAlpha);
                    canvas.DrawBitmap(iconBitmap,
                        new SkiaSharp.SKRect(textX, iconY, textX + scaledIconWidth, iconY + scaledIconHeight),
                        _iconPaint);
                }

                textX += imageAreaWidth;
            }
            else
            {
                if (ShowIcons && item.Icon != null)
                {
                    var scaledIconWidth = ImageScalingSize.Width * scale;
                    var scaledIconHeight = ImageScalingSize.Height * scale;
                    var iconY = itemRect.Top + (ItemHeight - scaledIconHeight) / 2;
                    var iconBitmap = item.Icon;
                    _iconPaint!.Color = SKColors.White.WithAlpha(fadeAlpha);
                    canvas.DrawBitmap(iconBitmap,
                        new SkiaSharp.SKRect(textX, iconY, textX + scaledIconWidth, iconY + scaledIconHeight),
                        _iconPaint);
                    textX += scaledIconWidth + 8 * scale;
                }
            }

            var hoverFore = !HoverForeColor.IsEmpty()
                ? HoverForeColor
                : HoverBackColor.IsEmpty()
                    ? MenuForeColor
                    : HoverBackColor.Determine();
            var textColor = isHovered ? hoverFore : MenuForeColor;

            var font = GetDefaultSkFont();
            _textPaint!.Color = textColor.WithAlpha(fadeAlpha);

            // Reserve space for chevron if item has dropdown
            var textWidth = itemRect.Right - textX;
            if (ShowSubmenuArrow && item.HasDropDown)
            {
                // Chevron is right anchored. 
                // We want text to end 8px (scaled) before the chevron starts.
                // Chevron icon is roughly 6px wide.
                // RightPadding is now tight (14px).

                var widthToReserve = (14 + 6 + 8) * scale; // RightPadding + IconWidth + Gap
                textWidth -= widthToReserve;
            }

            var textBounds = SkiaSharp.SKRect.Create(textX, itemRect.Top, textWidth, itemRect.Height);
            canvas.DrawControlText(item.Text, textBounds, _textPaint, font, ContentAlignment.MiddleLeft, false, true);

            if (ShowSubmenuArrow && item.HasDropDown)
            {
                var chevronSize = 5f * scale;
                var chevronX = itemRect.Right - 14 * scale; // Align to Right - 14px (More space to ensure full 12px gap)
                var chevronY = itemRect.MidY;

                // Chevron gets active text color and full opacity on hover, 0.4 opacity otherwise
                var arrowColor = isHovered ? hoverFore : MenuForeColor;
                var arrowAlphaBase = isHovered ? 255 : 102;
                _arrowPaint!.Color = arrowColor.WithAlpha((byte)(fadeAlpha * arrowAlphaBase / 255f));

                _chevronPath!.Reset();

                // Right arrow > (filled triangle)
                _chevronPath.MoveTo(chevronX - chevronSize, chevronY - chevronSize);
                _chevronPath.LineTo(chevronX + 2 * scale, chevronY);
                _chevronPath.LineTo(chevronX - chevronSize, chevronY + chevronSize);
                _chevronPath.Close();

                canvas.DrawPath(_chevronPath, _arrowPaint);
            }

            y += ItemHeight + ItemPadding;
        }

        // Restore layer if SlideDownFade effect was applied
        if (_openingEffect == OpeningEffectType.SlideDownFade)
        {
            canvas.Restore();
        }
    }

    private void EnsureSkiaCaches()
    {
        _bgPaint ??= new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        _borderPaint ??= new SKPaint
        { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1, FilterQuality = SKFilterQuality.High };
        _separatorPaint ??= new SKPaint { IsAntialias = true, StrokeWidth = 1 };
        _hoverPaint ??= new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        _iconPaint ??= new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        _textPaint ??= new SKPaint { IsAntialias = true };
        _arrowPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill, // Fill for better visibility
            FilterQuality = SKFilterQuality.High
        };
        _chevronPath ??= new SKPath();
    }

    private void EnsureShadowResources()
    {
        // Two-layer shadow system
        var blur0 = 3.0f;
        var blur1 = 5.75f;

        if (_shadowMaskFilters[0] == null)
            _shadowMaskFilters[0] = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur0);
        if (_shadowMaskFilters[1] == null)
            _shadowMaskFilters[1] = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur1);

        if (_shadowPaints[0] == null)
            _shadowPaints[0] = new SKPaint { IsAntialias = true, MaskFilter = _shadowMaskFilters[0] };
        if (_shadowPaints[1] == null)
            _shadowPaints[1] = new SKPaint { IsAntialias = true, MaskFilter = _shadowMaskFilters[1] };
    }

    // Include reserved margins for checks and images when measuring dropdown item width
    protected new float MeasureItemWidth(MenuItem item)
    {
        var scale = ScaleFactor;
        if (item is MenuItemSeparator) return 20f * scale;

        var font = GetDefaultSkFont();
        font.MeasureText(item.Text, out var tb);
        var w = tb.Width + 24 * scale; // Base margin

        if (ShowCheckMargin)
            w += CheckMarginWidth * scale;

        if (ShowImageMargin)
            w += (ImageScalingSize.Width + 8) * scale;
        else if (ShowIcons && item.Icon != null)
            w += (ImageScalingSize.Width + 8) * scale;

        if (ShowSubmenuArrow && item.HasDropDown)
            w += 30 * scale; // Extra space for chevron 

        return w;
    }

    private SKFont GetDefaultSkFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        if (_defaultSkFont == null || !ReferenceEquals(_defaultSkFontSource, Font) || _defaultSkFontDpi != dpi)
        {
            _defaultSkFont?.Dispose();
            _defaultSkFont = new SKFont
            {
                Size = Font.Size.Topx(this),
                Typeface = Font.SKTypeface,
                Subpixel = true,
                Edging = SKFontEdging.SubpixelAntialias
            };
            _defaultSkFontSource = Font;
            _defaultSkFontDpi = dpi;
        }

        return _defaultSkFont;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _defaultSkFont?.Dispose();
            _defaultSkFont = null;

            _bgPaint?.Dispose();
            _bgPaint = null;
            _borderPaint?.Dispose();
            _borderPaint = null;
            _separatorPaint?.Dispose();
            _separatorPaint = null;
            _hoverPaint?.Dispose();
            _hoverPaint = null;
            _iconPaint?.Dispose();
            _iconPaint = null;
            _textPaint?.Dispose();
            _textPaint = null;
            _arrowPaint?.Dispose();
            _arrowPaint = null;
            _chevronPath?.Dispose();
            _chevronPath = null;
            _layerPaint?.Dispose();
            _layerPaint = null;

            for (var i = 0; i < _shadowPaints.Length; i++)
            {
                _shadowPaints[i]?.Dispose();
                _shadowPaints[i] = null;
            }

            for (var i = 0; i < _shadowMaskFilters.Length; i++)
            {
                _shadowMaskFilters[i]?.Dispose();
                _shadowMaskFilters[i] = null;
            }
        }

        base.Dispose(disposing);
    }
}