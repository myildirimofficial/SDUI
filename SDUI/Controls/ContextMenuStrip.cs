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
    private const float PopupMargin = 8f;
    private const float ScrollBarGap = 4f;
    private const float MinimumContentWidth = 180f;
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
    private EventHandler _ownerLocationChangedHandler;
    private MouseEventHandler _ownerMouseDownHandler;
    private bool _ownerPreviousKeyPreview;
    private EventHandler _ownerSizeChangedHandler;
    private UIWindowBase _ownerWindow;
    private SKPaint? _separatorPaint;
    private SKPaint? _textPaint;
    private SKPaint? _layerPaint;
    private SKPoint _anchorClientLocation;
    private float _contentHeight;
    private float _scrollOffset;
    private float _viewportHeight;
    private float _viewportWidth;
    private OpeningEffectType _openingEffect = OpeningEffectType.Fade;
    private bool _openingUpwards;

    public ContextMenuStrip()
    {
        Visible = false;
        AutoSize = false;
        TabStop = false;
        Orientation = Orientation.Vertical;
        BackColor = SKColors.Transparent;
        AutoScroll = false;
        ItemHeight = 32f; // Increased height significantly
        ItemPadding = 8f; // Increased vertical margin

        if (_vScrollBar != null)
        {
            _vScrollBar.Dock = DockStyle.None;
            _vScrollBar.Visible = false;
            _vScrollBar.Thickness = 8;
            _vScrollBar.MinimumSize = new SKSize(8, 0);
            _vScrollBar.MaximumSize = new SKSize(8, 0);
            _vScrollBar.AutoHide = false;
            _vScrollBar.ScrollAnimationIncrement = 1.0;
            _vScrollBar.ScrollAnimationType = AnimationType.Linear;
            _vScrollBar.SmallChange = ItemHeight;
            _vScrollBar.LargeChange = ItemHeight * 3;
            _vScrollBar.ValueChanged += (_, _) =>
            {
                _scrollOffset = (float)Math.Round(_vScrollBar.Value);
                Invalidate();
            };
        }

        if (_hScrollBar != null)
        {
            _hScrollBar.Dock = DockStyle.None;
            _hScrollBar.Visible = false;
        }

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
        if (IsOpen)
        {
            Hide();
            return;
        }

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
        _anchorClientLocation = _ownerWindow.PointToClient(location);
        PositionDropDown(location);
        Visible = true;
        EnsureTopMostInOwner();

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
                        EnsureTopMostInOwner();
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

    private void EnsureTopMostInOwner()
    {
        if (_ownerWindow == null)
            return;

        if (_ownerWindow.Controls.Contains(this))
        {
            _ownerWindow.Controls.SetChildIndex(this, _ownerWindow.Controls.Count - 1);
            _ownerWindow.UpdateZOrder();
        }

        var siblings = _ownerWindow.Controls.OfType<ElementBase>();
        var maxZOrder = -1;
        foreach (var sibling in siblings)
        {
            if (ReferenceEquals(sibling, this))
                continue;

            if (sibling.ZOrder > maxZOrder)
                maxZOrder = sibling.ZOrder;
        }

        ZOrder = maxZOrder + 1;
        _ownerWindow.InvalidateRenderTree();
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

        _anchorClientLocation = _ownerWindow.PointToClient(screenLocation);
        PositionDropDownCore(_anchorClientLocation, preserveDirection: false);
    }

    private void PositionDropDownCore(SKPoint anchorClientLocation, bool preserveDirection)
    {
        if (_ownerWindow == null)
            return;

        var size = GetPrefSize();
        var client = _ownerWindow.ClientRectangle;

        var marginX = Math.Min(PopupMargin, Math.Max(0f, (client.Width - 1f) * 0.5f));
        var marginY = Math.Min(PopupMargin, Math.Max(0f, (client.Height - 1f) * 0.5f));
        var maxWidth = Math.Max(1f, client.Width - marginX * 2f);
        var maxHeight = Math.Max(1f, client.Height - marginY * 2f);

        size.Width = Math.Min(Math.Max(size.Width, Math.Min(MinimumContentWidth + ShadowMargin * 2f, maxWidth)), maxWidth);
        size.Height = Math.Min(size.Height, maxHeight);
        var preferredHeight = size.Height;

        var targetX = anchorClientLocation.X;
        var targetY = anchorClientLocation.Y;
        var availableBelow = Math.Max(1f, client.Bottom - marginY - anchorClientLocation.Y);
        var availableAbove = Math.Max(1f, anchorClientLocation.Y - (client.Top + marginY));
        var directionSwitchThreshold = Math.Max(ItemHeight, ItemPadding * 2f);
        var openingUpwards = preserveDirection
            ? _openingUpwards
            : availableAbove > availableBelow && size.Height > availableBelow;

        if (preserveDirection)
        {
            if (!openingUpwards && availableBelow < preferredHeight)
            {
                var shouldFlipUp = availableAbove >= preferredHeight || availableAbove > availableBelow + directionSwitchThreshold;
                if (shouldFlipUp)
                    openingUpwards = true;
            }
            else if (openingUpwards && availableAbove < preferredHeight)
            {
                var shouldFlipDown = availableBelow >= preferredHeight || availableBelow > availableAbove + directionSwitchThreshold;
                if (shouldFlipDown)
                    openingUpwards = false;
            }
        }

        if (openingUpwards)
        {
            size.Height = Math.Min(size.Height, availableAbove);
            targetY = anchorClientLocation.Y - size.Height;
        }
        else
        {
            size.Height = Math.Min(size.Height, availableBelow);
            targetY = anchorClientLocation.Y;
        }

        if (targetX + size.Width > client.Right - marginX)
        {
            var leftPos = targetX - size.Width;
            if (leftPos >= client.Left + marginX)
                targetX = leftPos;
            else
                targetX = client.Right - size.Width - marginX;
        }

        targetX = Math.Max(client.Left + marginX, Math.Min(targetX, client.Right - size.Width - marginX));
        targetY = Math.Max(client.Top + marginY, Math.Min(targetY, client.Bottom - size.Height - marginY));

        _openingUpwards = openingUpwards;

        Location = new SKPoint(targetX, targetY);
        Size = size;
        UpdateScrollState();
    }

    private void RepositionToOwnerBounds()
    {
        if (!IsOpen || _ownerWindow == null)
            return;

        PositionDropDownCore(_anchorClientLocation, preserveDirection: true);
        EnsureTopMostInOwner();
        _ownerWindow.Invalidate();
    }

    private float GetScrollBarWidth()
    {
        if (_vScrollBar == null || !_vScrollBar.Visible)
            return 0f;

        return _vScrollBar.Thickness;
    }

    private bool IsScrollViewportActive()
    {
        return _vScrollBar != null && _vScrollBar.Visible;
    }

    private float GetContentHeight()
    {
        var contentHeight = ItemPadding;

        foreach (var item in Items)
        {
            if (!item.Visible)
                continue;

            if (item.IsSeparator)
                contentHeight += SeparatorMargin * 2 + 1 + ItemPadding;
            else
                contentHeight += ItemHeight + ItemPadding;
        }

        return contentHeight;
    }

    private void UpdateScrollState()
    {
        _contentHeight = GetContentHeight();
        _viewportHeight = Math.Max(1f, (float)Math.Floor(Height - ShadowMargin * 2));

        if (_hScrollBar != null)
            _hScrollBar.Visible = false;

        if (_vScrollBar == null)
        {
            _scrollOffset = 0f;
            _viewportWidth = Math.Max(1f, (float)Math.Floor(Width - ShadowMargin * 2 - ItemPadding * 2));
            return;
        }

        var needsVScroll = _contentHeight > _viewportHeight;
        _vScrollBar.Visible = needsVScroll;

        if (needsVScroll)
        {
            var scrollBarWidth = GetScrollBarWidth();
            var scrollBarHeight = Math.Max(1f, (float)Math.Round(_viewportHeight));
            var scrollBarLeft = (float)Math.Round(Width - ShadowMargin - scrollBarWidth);
            var scrollBarTop = (float)Math.Round(ShadowMargin);

            _vScrollBar.Location = new SKPoint(scrollBarLeft, scrollBarTop);
            _vScrollBar.Size = new SKSize(scrollBarWidth, scrollBarHeight);
            _vScrollBar.Minimum = 0;
            _vScrollBar.Maximum = Math.Max(0, _contentHeight - _viewportHeight);
            _vScrollBar.LargeChange = Math.Max(ItemHeight, _viewportHeight * 0.85f);
            _vScrollBar.SmallChange = Math.Max(8f, ItemHeight + ItemPadding);
            if (_vScrollBar.Value > _vScrollBar.Maximum)
                _vScrollBar.Value = _vScrollBar.Maximum;
            _scrollOffset = (float)Math.Round(_vScrollBar.Value);
        }
        else
        {
            _vScrollBar.Value = 0;
            _scrollOffset = 0f;
        }

        _viewportWidth = Math.Max(1f,
            (float)Math.Floor(Width - ShadowMargin * 2 - ItemPadding * 2 - (needsVScroll ? GetScrollBarWidth() + ScrollBarGap : 0f)));
    }

    private List<(MenuItem Item, SKRect Rect)> GetVisibleItemRects()
    {
        var rects = new List<(MenuItem Item, SKRect Rect)>(Items.Count);
        var y = (float)Math.Round(ShadowMargin + ItemPadding - _scrollOffset);
        var x = (float)Math.Round(ShadowMargin + ItemPadding);
        var width = Math.Max(1f, (float)Math.Round(_viewportWidth));

        foreach (var item in Items)
        {
            if (!item.Visible)
                continue;

            if (item.IsSeparator)
            {
                var sepHeight = (float)Math.Round(SeparatorMargin * 2 + 1);
                rects.Add((item, SKRect.Create(x, y, width, sepHeight)));
                y += sepHeight + ItemPadding;
                continue;
            }

            rects.Add((item, SKRect.Create(x, y, width, (float)Math.Round(ItemHeight))));
            y += ItemHeight + ItemPadding;
        }

        return rects;
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateScrollState();
    }

    private bool TryRouteScrollableMouseMove(MouseEventArgs e)
    {
        if (!TryGetInputTarget(e, out var target, out var childEventArgs) || target == null || childEventArgs == null)
        {
            if (LastHoveredElement != null)
            {
                LastHoveredElement.OnMouseLeave(EventArgs.Empty);
                LastHoveredElement = null;
            }

            return false;
        }

        target.OnMouseMove(childEventArgs);

        if (!ReferenceEquals(target, LastHoveredElement))
        {
            LastHoveredElement?.OnMouseLeave(EventArgs.Empty);
            target.OnMouseEnter(EventArgs.Empty);
            LastHoveredElement = target;
        }

        return true;
    }

    private bool TryRouteScrollableMouseDown(MouseEventArgs e)
    {
        if (!TryGetInputTarget(e, out var target, out var childEventArgs) || target == null || childEventArgs == null)
            return false;

        target.OnMouseDown(childEventArgs);

        var window = GetParentWindow();
        if (window is UIWindowBase uiWindow)
        {
            uiWindow.FocusedElement = target;
        }
        else if (window != null)
        {
            window.FocusManager.SetFocus(target);
        }
        else if (FocusedElement != target)
        {
            FocusedElement = target;
        }

        return true;
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
        if (_ownerWindow == null)
            return;

        _ownerSizeChangedHandler ??= OnOwnerBoundsChanged;
        _ownerLocationChangedHandler ??= OnOwnerBoundsChanged;
        _ownerWindow.SizeChanged += _ownerSizeChangedHandler;
        _ownerWindow.LocationChanged += _ownerLocationChangedHandler;

        if (!AutoClose)
            return;

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
        if (_ownerSizeChangedHandler != null) _ownerWindow.SizeChanged -= _ownerSizeChangedHandler;
        if (_ownerLocationChangedHandler != null) _ownerWindow.LocationChanged -= _ownerLocationChangedHandler;
        if (_ownerMouseDownHandler != null) _ownerWindow.MouseDown -= _ownerMouseDownHandler;
        if (_ownerDeactivateHandler != null) _ownerWindow.Deactivate -= _ownerDeactivateHandler;
        if (_ownerKeyDownHandler != null) _ownerWindow.KeyDown -= _ownerKeyDownHandler;
        _ownerWindow.KeyPreview = _ownerPreviousKeyPreview;
    }

    private void OnOwnerBoundsChanged(object? sender, EventArgs e)
    {
        RepositionToOwnerBounds();
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
        contentWidth = Math.Max(contentWidth, MinimumContentWidth);

        // En alttaki öğenin border ile kesilmemesi için ekstra alan yok,
        // çünkü son item'dan sonra zaten ItemPadding var.

        // Shadow için her yönden ekstra alan ekle
        var totalWidth = contentWidth + ShadowMargin * 2;
        var totalHeight = contentHeight + ShadowMargin * 2;

        return new SKSize((int)Math.Ceiling(totalWidth), (int)Math.Ceiling(totalHeight));
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsScrollViewportActive())
        {
            base.OnMouseMove(e);
        }
        else if (TryRouteScrollableMouseMove(e))
        {
            _hoveredItem = null;
            Invalidate();
            return;
        }

        var previousHoveredItem = _hoveredItem;
        _hoveredItem = null;
        var viewportBottom = ShadowMargin + _viewportHeight;
        var rects = GetVisibleItemRects();
        for (var i = 0; i < rects.Count; i++)
        {
            var entry = rects[i];
            if (entry.Rect.Bottom < ShadowMargin || entry.Rect.Top > viewportBottom || entry.Item.IsSeparator)
                continue;

            if (entry.Rect.Contains(e.Location))
            {
                _hoveredItem = entry.Item;
                break;
            }
        }

        if (previousHoveredItem != _hoveredItem && IsScrollViewportActive())
        {
            if (_hoveredItem?.HasDropDown == true)
                OpenSubmenu(_hoveredItem);
            else
                CloseSubmenu();
        }

        Invalidate();
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsScrollViewportActive())
        {
            base.OnMouseDown(e);
            return;
        }

        RaiseMouseDown(e);
        if (TryRouteScrollableMouseDown(e))
            return;

        if (e.Button != MouseButtons.Left)
            return;

        var rects = GetVisibleItemRects();
        var viewportBottom = ShadowMargin + _viewportHeight;
        for (var i = 0; i < rects.Count; i++)
        {
            var entry = rects[i];
            if (entry.Rect.Bottom < ShadowMargin || entry.Rect.Top > viewportBottom || entry.Item.IsSeparator)
                continue;

            if (entry.Rect.Contains(e.Location))
            {
                OnItemClicked(entry.Item);
                return;
            }
        }

        CloseSubmenu();
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        if (_vScrollBar != null && _vScrollBar.Visible)
        {
            var step = Math.Max(1f, (float)Math.Round(_vScrollBar.SmallChange));
            var deltaValue = (e.Delta / 120f) * step;
            _vScrollBar.Value = Math.Clamp(_vScrollBar.Value - deltaValue, _vScrollBar.Minimum, _vScrollBar.Maximum);
            Invalidate();
            return;
        }

        base.OnMouseWheel(e);
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
        var surfaceAlpha = _openingUpwards ? (byte)Math.Max((int)fadeAlpha, 235) : fadeAlpha;

        // Apply animation effect based on OpeningEffect property
        var animationSaveCount = -1;
        if (_openingEffect == OpeningEffectType.SlideDownFade)
        {
            _layerPaint ??= new SKPaint { IsAntialias = true };
            _layerPaint.Color = SKColors.White.WithAlpha(fadeAlpha);
            animationSaveCount = canvas.SaveLayer(_layerPaint);

            // Restore the original slide feel while respecting upward opening direction.
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
        _bgPaint!.Color = MenuBackColor.WithAlpha(surfaceAlpha);
        canvas.DrawRoundRect(contentRect, CORNER_RADIUS, CORNER_RADIUS, _bgPaint);

        // Border
        _borderPaint!.Color = SeparatorColor.WithAlpha((byte)(surfaceAlpha * 0.35f));
        var borderRect = new SkiaSharp.SKRect(
            contentRect.Left + 0.5f,
            contentRect.Top + 0.5f,
            contentRect.Right - 0.5f,
            contentRect.Bottom - 0.5f);
        canvas.DrawRoundRect(borderRect, CORNER_RADIUS, CORNER_RADIUS, _borderPaint);

        var scale = ScaleFactor;
        var viewportRect = new SKRect(
            contentRect.Left,
            contentRect.Top,
            contentRect.Left + Math.Max(1f, _viewportWidth + ItemPadding * 2),
            contentRect.Top + _viewportHeight);

        var itemClipSave = canvas.Save();
        canvas.ClipRoundRect(_radius.ToRoundRect(viewportRect), antialias: true);

        var rects = GetVisibleItemRects();
        var viewportBottom = ShadowMargin + _viewportHeight;

        for (var itemIndex = 0; itemIndex < rects.Count; itemIndex++)
        {
            var item = rects[itemIndex].Item;
            var itemRect = rects[itemIndex].Rect;

            // Skip hidden items — visibility should control drawing and layout
            if (!item.Visible)
                continue;

            if (itemRect.Bottom < ShadowMargin || itemRect.Top > viewportBottom)
                continue;

            if (item.IsSeparator)
            {
                _separatorPaint!.Color = SeparatorColor.WithAlpha(fadeAlpha);
                canvas.DrawLine(
                    itemRect.Left + 8,
                    itemRect.Top + SeparatorMargin,
                    itemRect.Right - 8,
                    itemRect.Top + SeparatorMargin,
                    _separatorPaint);
                continue;
            }

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
        }

        canvas.RestoreToCount(itemClipSave);

        // Restore layer if SlideDownFade effect was applied
        if (animationSaveCount >= 0)
        {
            canvas.RestoreToCount(animationSaveCount);
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