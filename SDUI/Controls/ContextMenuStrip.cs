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
    private enum PopupAnchorPlacement
    {
        Point,
        Below,
        Beside
    }

    private const float CheckMarginWidth = 22f;
    private const float BaseItemHeight = 28f;
    private const float BaseItemPadding = 8f;
    private const float BaseVerticalItemGap = 0f;
    private const float BaseMinimumContentWidth = 180f;
    private const float BaseScrollBarThickness = 8f;
    private const float BaseSeparatorMargin = 4f;
    private const float BaseAccordionIndent = 18f;
    private const float BaseAccordionMaxHeight = 250f;
    private const float BasePopupTopAnchorOffset = 6f;
    private const double AccordionAnimationIncrement = 0.18;
    private const float PopupMargin = 8f;
    private const float ScrollBarGap = 4f;
    private readonly AnimationManager _fadeInAnimation;

    private readonly Dictionary<MenuItem, AnimationManager> _itemHoverAnims = new();
    private readonly Dictionary<MenuItem, AnimationManager> _accordionAnims = new();
    private SKPaint? _arrowPaint;

    private SKPath? _chevronPath;

    private SKFont? _defaultSkFont;
    private int _defaultSkFontDpi;
    private SKFont? _defaultSkFontSource;
    private MenuItem? _hoveredItem;
    private SKPaint? _hoverPaint;
    private SKPaint? _iconPaint;
    private EventHandler? _ownerDeactivateHandler;
    private KeyEventHandler? _ownerKeyDownHandler;
    private EventHandler? _ownerLocationChangedHandler;
    private MouseEventHandler? _ownerMouseDownHandler;
    private bool _ownerPreviousKeyPreview;
    private EventHandler? _ownerSizeChangedHandler;
    private WindowBase? _ownerWindow;
    private SKPaint? _separatorPaint;
    private SKPaint? _textPaint;
    private SKPaint? _layerPaint;
    private ElementBase? _anchorElement;
    private SKRect _anchorElementBounds;
    private SKPoint _anchorClientLocation;
    private float _contentHeight;
    private float _lastMetricsDpi;
    private float _verticalItemGap;
    private float _scrollOffset;
    private float _viewportHeight;
    private float _viewportWidth;
    private OpeningEffectType _openingEffect = OpeningEffectType.Fade;
    private PopupAnchorPlacement _anchorPlacement;
    private bool _openingLeftwards;
    private bool _openingUpwards;
    private bool _ownerBoundsRefreshQueued;
    private bool _useAccordionSubmenus;
    private readonly HashSet<MenuItem> _expandedItems = new();
    private MenuItem? _accordionCenterTarget;
    private SKSize _stableAccordionPopupSize;

    private readonly record struct VisibleItemEntry(MenuItem Item, SKRect Rect, int Depth);

    protected override bool HandlesMouseWheelScroll => _vScrollBar != null && _vScrollBar.Visible;
    protected override float MouseWheelScrollLines => 1f;

    protected override float GetMouseWheelScrollStep(ScrollBar scrollBar)
    {
        return Math.Max(1f, (float)Math.Round(scrollBar.SmallChange));
    }

    public ContextMenuStrip()
    {
        Visible = false;
        AutoSize = false;
        TabStop = false;
        Orientation = Orientation.Vertical;
        BackColor = ColorScheme.Surface;
        AutoScroll = false;
        Border = new Thickness(1);
        Radius = new Radius(10);
        Shadow = new BoxShadow(0f, 6f, 18f, 0, SKColors.Black.WithAlpha(56));
        ApplyDpiMetrics(96f);

        if (_vScrollBar != null)
        {
            _vScrollBar.Dock = DockStyle.None;
            _vScrollBar.Visible = false;
            _vScrollBar.MinimumSize = new SKSize(8, 0);
            _vScrollBar.MaximumSize = new SKSize(8, 0);
            _vScrollBar.AutoHide = false;
            _vScrollBar.ScrollAnimationIncrement = 1.0;
            _vScrollBar.ScrollAnimationType = AnimationType.Linear;
            _vScrollBar.SmallChange = ItemHeight;
            _vScrollBar.LargeChange = ItemHeight * 3;
            _vScrollBar.DisplayValueChanged += (_, _) =>
            {
                _scrollOffset = _vScrollBar.DisplayValue;
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

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool UseAccordionSubmenus
    {
        get => _useAccordionSubmenus;
        set
        {
            if (_useAccordionSubmenus == value)
                return;

            _useAccordionSubmenus = value;
            _accordionCenterTarget = null;
            _stableAccordionPopupSize = SKSize.Empty;
            _expandedItems.Clear();
            foreach (var anim in _accordionAnims.Values)
                anim.SetProgress(0);
            CloseSubmenu();
            UpdateScrollState();
            Invalidate();
        }
    }

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
    public ElementBase? SourceElement { get; private set; }

    internal ContextMenuStrip? ParentDropDown { get; set; }

    public event CancelEventHandler? Opening;
    public event CancelEventHandler? Closing;

    public SKSize MeasurePreferredSize()
    {
        return GetPrefSize();
    }

    public void Show(ElementBase? element, SKPoint location)
    {
        ResetElementAnchor();
        ShowCore(element, location);
    }

    internal void ShowAnchoredBelow(ElementBase element, SKRect anchorBounds)
    {
        ConfigureElementAnchor(element, anchorBounds, PopupAnchorPlacement.Below);
        ShowCore(element, element.PointToScreen(new SKPoint(anchorBounds.Left, anchorBounds.Top)));
    }

    internal void ShowAnchoredBeside(ElementBase element, SKRect anchorBounds)
    {
        ConfigureElementAnchor(element, anchorBounds, PopupAnchorPlacement.Beside);
        ShowCore(element, element.PointToScreen(new SKPoint(anchorBounds.Left, anchorBounds.Top)));
    }

    private void ShowCore(ElementBase? element, SKPoint location)
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
        _accordionCenterTarget = null;
        ApplyDpiMetrics(_ownerWindow.DeviceDpi > 0 ? _ownerWindow.DeviceDpi : DeviceDpi);

        if (!_ownerWindow.Controls.Contains(this))
            _ownerWindow.Controls.Add(this);

        // Konumu ve boyutu belirle, sonra z-order'ı en üste çek.
        _anchorClientLocation = _ownerWindow.PointToClient(location);
        PositionDropDown(location);
        Visible = true;
        EnsureTopMostInOwner();

        // WinForms z-order + SDUI'nin kendi ZOrder sistemini güncelle.
        BringToFront();
        if (_ownerWindow is WindowBase uiw)
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
        _ownerWindow = null!;
        SourceElement = null;
        ParentDropDown = null;
        ResetElementAnchor();
        _accordionCenterTarget = null;
        IsOpen = false;
    }

    private void ConfigureElementAnchor(ElementBase element, SKRect anchorBounds, PopupAnchorPlacement placement)
    {
        _anchorElement = element;
        _anchorElementBounds = anchorBounds;
        _anchorPlacement = placement;
    }

    internal void UpdateAnchorBounds(ElementBase element, SKRect anchorBounds)
    {
        if (!ReferenceEquals(_anchorElement, element) || _anchorPlacement == PopupAnchorPlacement.Point)
            return;

        _anchorElementBounds = anchorBounds;

        if (IsOpen)
            RepositionToOwnerBounds();
    }

    private void ResetElementAnchor()
    {
        _anchorElement = null;
        _anchorElementBounds = SKRect.Empty;
        _anchorPlacement = PopupAnchorPlacement.Point;
    }

    private bool TryGetAnchorBoundsInOwner(out SKRect anchorBounds)
    {
        anchorBounds = SKRect.Empty;

        if (_ownerWindow == null || _anchorElement == null || _anchorPlacement == PopupAnchorPlacement.Point)
            return false;

        var topLeftScreen = _anchorElement.PointToScreen(new SKPoint(_anchorElementBounds.Left, _anchorElementBounds.Top));
        var bottomRightScreen = _anchorElement.PointToScreen(new SKPoint(_anchorElementBounds.Right, _anchorElementBounds.Bottom));
        var topLeftClient = _ownerWindow.PointToClient(topLeftScreen);
        var bottomRightClient = _ownerWindow.PointToClient(bottomRightScreen);

        anchorBounds = new SKRect(
            Math.Min(topLeftClient.X, bottomRightClient.X),
            Math.Min(topLeftClient.Y, bottomRightClient.Y),
            Math.Max(topLeftClient.X, bottomRightClient.X),
            Math.Max(topLeftClient.Y, bottomRightClient.Y));
        return true;
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
        if (UseAccordionSubmenus && item.HasDropDown)
        {
            ToggleAccordionItem(item);
            return;
        }

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
        if (UseAccordionSubmenus && _stableAccordionPopupSize != SKSize.Empty)
            size = _stableAccordionPopupSize;

        var client = _ownerWindow.ClientRectangle;

        var marginX = Math.Min(PopupMargin, Math.Max(0f, (client.Width - 1f) * 0.5f));
        var marginY = Math.Min(PopupMargin, Math.Max(0f, (client.Height - 1f) * 0.5f));
        var maxWidth = Math.Max(1f, client.Width - marginX * 2f);

        var minimumWidth = Math.Min(BaseMinimumContentWidth * ScaleFactor, maxWidth);
        size.Width = Math.Min(Math.Max(size.Width, minimumWidth), maxWidth);
        var anchorGap = _anchorPlacement == PopupAnchorPlacement.Below ? -1f * ScaleFactor : 0f;
        var anchorOverlap = 1f * ScaleFactor;
        var hasAnchorBounds = TryGetAnchorBoundsInOwner(out var anchorBounds);
        var verticalTopInset = Math.Max(1f, Border.Top);
        var verticalBottomInset = Math.Max(1f, Border.Bottom);
        var minY = client.Top + verticalTopInset;
        var maxY = client.Bottom - verticalBottomInset;
        var maxPopupHeight = Math.Max(1f, maxY - minY);
        var desiredHeight = Math.Min(size.Height, maxPopupHeight);

        var targetX = anchorClientLocation.X;
        var targetY = anchorClientLocation.Y;
        var availableBelow = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below
            ? Math.Max(1f, maxY - (anchorBounds.Bottom + anchorGap))
            : Math.Max(1f, maxY - anchorClientLocation.Y);
        var availableAbove = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below
            ? Math.Max(1f, anchorBounds.Top - anchorGap - minY)
            : Math.Max(1f, anchorClientLocation.Y - minY);
        var directionSwitchThreshold = Math.Max(ItemHeight, ItemPadding * 2f);
        var openingUpwards = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Beside
            ? false
            : preserveDirection
                ? _openingUpwards
                : availableAbove > availableBelow && desiredHeight > availableBelow;

        if (hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Beside)
        {
            if (TryGetPopupTopInOwner(out var popupTop))
                targetY = popupTop;
            else
            {
                var contentHeight = Math.Max(1f, desiredHeight);
                targetY = anchorBounds.Top + (anchorBounds.Height - contentHeight) * 0.5f;
            }
        }
        else if (hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below)
        {
            targetY = openingUpwards
                ? anchorBounds.Top - anchorGap - desiredHeight
                : anchorBounds.Bottom + anchorGap;
        }
        else if (openingUpwards)
        {
            targetY = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below
                ? anchorBounds.Top - anchorGap - desiredHeight
                : anchorClientLocation.Y - desiredHeight;
        }
        else
        {
            targetY = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below
                ? anchorBounds.Bottom + anchorGap
                : anchorClientLocation.Y;
        }

        var openingLeftwards = hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Beside
            ? preserveDirection
                ? _openingLeftwards
                : anchorBounds.Right - anchorOverlap + size.Width > client.Right - marginX
                    && anchorBounds.Left + anchorOverlap - size.Width >= client.Left + marginX
            : preserveDirection
                ? _openingLeftwards
                : targetX + size.Width > client.Right - marginX && anchorClientLocation.X - size.Width >= client.Left + marginX;

        if (hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Below)
        {
            targetX = anchorBounds.Left;
        }
        else if (hasAnchorBounds && _anchorPlacement == PopupAnchorPlacement.Beside)
        {
            targetX = openingLeftwards
                ? anchorBounds.Left + anchorOverlap - size.Width
                : anchorBounds.Right - anchorOverlap;
        }
        else if (openingLeftwards)
        {
            targetX = anchorClientLocation.X - size.Width;
        }
        else if (targetX + size.Width > client.Right - marginX)
        {
            targetX = client.Right - size.Width - marginX;
        }

        size.Height = desiredHeight;

        targetX = Math.Max(client.Left + marginX, Math.Min(targetX, client.Right - size.Width - marginX));
        targetY = Math.Max(minY, Math.Min(targetY, maxY - size.Height));

        _openingLeftwards = openingLeftwards;
        _openingUpwards = openingUpwards;

        Location = new SKPoint(targetX, targetY);
        Size = size;
        if (UseAccordionSubmenus && _stableAccordionPopupSize == SKSize.Empty)
            _stableAccordionPopupSize = size;
        UpdateScrollState();
    }

    internal void ResetStableAccordionPopupSize()
    {
        _stableAccordionPopupSize = SKSize.Empty;
    }

    private bool TryGetPopupTopInOwner(out float popupTop)
    {
        popupTop = 0f;

        if (_ownerWindow == null || _anchorElement is not ContextMenuStrip popup)
            return false;

        var popupOriginScreen = popup.PointToScreen(SKPoint.Empty);
        var popupOriginClient = _ownerWindow.PointToClient(popupOriginScreen);
        popupTop = popupOriginClient.Y + BasePopupTopAnchorOffset * ScaleFactor;
        return true;
    }

    private void RepositionToOwnerBounds()
    {
        if (!IsOpen || _ownerWindow == null)
            return;

        PositionDropDownCore(_anchorClientLocation, preserveDirection: true);

        if (_anchorPlacement == PopupAnchorPlacement.Point)
        {
            var previousX = Location.X;
            var client = _ownerWindow.ClientRectangle;
            var marginX = Math.Min(PopupMargin, Math.Max(0f, (client.Width - 1f) * 0.5f));
            var minX = client.Left + marginX;
            var maxX = client.Right - Width - marginX;
            var clampedX = Math.Max(minX, Math.Min(previousX, maxX));

            if (Math.Abs(Location.X - clampedX) > 0.001f)
                Location = new SKPoint(clampedX, Location.Y);
        }

        EnsureTopMostInOwner();
        _ownerWindow.Invalidate();
    }

    private void ApplyDpiMetrics(float dpi)
    {
        var effectiveDpi = dpi > 0 ? dpi : 96f;
        if (Math.Abs(_lastMetricsDpi - effectiveDpi) < 0.001f)
            return;

        var scale = effectiveDpi / 96f;
        ItemHeight = BaseItemHeight * scale;
        ItemPadding = BaseItemPadding * scale;
        SeparatorMargin = BaseSeparatorMargin * scale;
        ImageScalingSize = new SKSize(
            (float)Math.Round(20f * scale),
            (float)Math.Round(20f * scale));

        if (_vScrollBar != null)
        {
            var scaledThickness = Math.Max(6f, (float)Math.Round(BaseScrollBarThickness * scale));
            _vScrollBar.Thickness = (int)scaledThickness;
            _vScrollBar.MinimumSize = new SKSize(scaledThickness, 0);
            _vScrollBar.MaximumSize = new SKSize(scaledThickness, 0);
            _vScrollBar.SmallChange = ItemHeight;
            _vScrollBar.LargeChange = ItemHeight * 3f;
        }

        _lastMetricsDpi = effectiveDpi;
        _verticalItemGap = Math.Max(0f, BaseVerticalItemGap * scale);
    }

    private float GetVerticalItemGap()
    {
        if (_verticalItemGap <= 0f)
            _verticalItemGap = Math.Max(0f, BaseVerticalItemGap * ScaleFactor);

        return _verticalItemGap;
    }

    private float GetScrollBarWidth()
    {
        if (_vScrollBar == null || !_vScrollBar.Visible)
            return 0f;

        return _vScrollBar.Thickness;
    }

    private float GetContentHeight()
    {
        if (UseAccordionSubmenus)
            return GetAccordionContentHeight(Items);

        var verticalGap = GetVerticalItemGap();
        var contentHeight = ItemPadding * 2f;
        var firstItem = true;

        foreach (var item in Items)
        {
            if (!item.Visible)
                continue;

            if (!firstItem)
                contentHeight += verticalGap;

            if (item.IsSeparator)
                contentHeight += SeparatorMargin * 2 + 1;
            else
                contentHeight += ItemHeight;

            firstItem = false;
        }

        return contentHeight;
    }

    private float GetAccordionIndent()
    {
        return MathF.Max(14f, BaseAccordionIndent * ScaleFactor);
    }

    private float GetAccordionMaxHeight()
    {
        return MathF.Max(ItemHeight * 3f, BaseAccordionMaxHeight * ScaleFactor);
    }

    private float GetAccordionContentHeight(IReadOnlyList<MenuItem> items)
    {
        var verticalGap = GetVerticalItemGap();
        var contentHeight = ItemPadding * 2f;
        AppendAccordionContentHeight(items, ref contentHeight, verticalGap, 1f);
        return contentHeight;
    }

    private void AppendAccordionContentHeight(IReadOnlyList<MenuItem> items, ref float height, float verticalGap, float revealScale)
    {
        if (revealScale <= 0.001f)
            return;

        var firstItem = true;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item.Visible)
                continue;

            if (!firstItem)
                height += verticalGap * revealScale;

            height += item.IsSeparator
                ? (SeparatorMargin * 2f + 1f) * revealScale
                : ItemHeight * revealScale;

            firstItem = false;

            var childReveal = revealScale * GetAccordionProgress(item);
            if (!item.HasDropDown || childReveal <= 0.001f)
                continue;

            AppendAccordionContentHeight(item.DropDownItems, ref height, verticalGap, childReveal);
        }
    }

    private float GetAccordionContentWidth(IReadOnlyList<MenuItem> items, int depth)
    {
        var maxWidth = ItemPadding * 2f;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item.Visible)
                continue;

            var entryWidth = item.IsSeparator
                ? ItemPadding * 2f
                : MeasureItemWidth(item);
            maxWidth = Math.Max(maxWidth, entryWidth);

            if (!item.HasDropDown)
                continue;

            maxWidth = Math.Max(maxWidth, GetAccordionContentWidth(item.DropDownItems, depth + 1));
        }

        return maxWidth;
    }

    private void UpdateScrollState()
    {
        _contentHeight = GetContentHeight();
        _viewportHeight = Math.Max(1f, Height);

        if (_hScrollBar != null)
            _hScrollBar.Visible = false;

        if (_vScrollBar == null)
        {
            _scrollOffset = 0f;
            _viewportWidth = Math.Max(1f, (float)Math.Floor(Width - ItemPadding * 2));
            return;
        }

        var needsVScroll = _contentHeight > _viewportHeight;
        _vScrollBar.Visible = needsVScroll;

        if (needsVScroll)
        {
            var scrollBarWidth = GetScrollBarWidth();
            var overlayInset = MathF.Max(2f, 4f * ScaleFactor);
            var edgeInset = Math.Max(1f, Border.Right) + overlayInset;
            var scrollBarHeight = Math.Max(1f, (float)Math.Round(Height - edgeInset * 2f));
            var scrollBarLeft = (float)Math.Round(Width - edgeInset - scrollBarWidth);
            var scrollBarTop = (float)Math.Round(edgeInset);

            _vScrollBar.Location = new SKPoint(scrollBarLeft, scrollBarTop);
            _vScrollBar.Size = new SKSize(scrollBarWidth, scrollBarHeight);
            _vScrollBar.Minimum = 0;
            _vScrollBar.Maximum = Math.Max(0, _contentHeight - _viewportHeight);
            _vScrollBar.LargeChange = Math.Max(ItemHeight, _viewportHeight * 0.85f);
            _vScrollBar.SmallChange = Math.Max(8f, ItemHeight + GetVerticalItemGap());
            if (_vScrollBar.Value > _vScrollBar.Maximum)
                _vScrollBar.Value = _vScrollBar.Maximum;
            _scrollOffset = _vScrollBar.DisplayValue;
        }
        else
        {
            _vScrollBar.Value = 0;
            _scrollOffset = 0f;
        }

        _viewportWidth = Math.Max(1f,
            (float)Math.Floor(Width - ItemPadding * 2));
    }

    private List<VisibleItemEntry> GetVisibleItemEntries()
    {
        return GetVisibleItemEntries(_scrollOffset);
    }

    private List<VisibleItemEntry> GetVisibleItemEntries(float scrollOffset)
    {
        var entries = new List<VisibleItemEntry>(Items.Count);
        var verticalGap = GetVerticalItemGap();
        var y = ItemPadding - scrollOffset;
        var x = (float)Math.Round(ItemPadding);
        var width = Math.Max(1f, (float)Math.Round(_viewportWidth));
        AppendVisibleItemEntries(Items, 0, entries, ref y, x, width, verticalGap, 1f);
        return entries;
    }

    private void AppendVisibleItemEntries(IReadOnlyList<MenuItem> items, int depth, List<VisibleItemEntry> entries, ref float y, float x, float width, float verticalGap, float revealScale)
    {
        if (revealScale <= 0.001f)
            return;

        var firstItem = true;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item.Visible)
                continue;

            if (!firstItem)
                y += verticalGap * revealScale;

            if (item.IsSeparator)
            {
                var sepHeight = (float)Math.Round((SeparatorMargin * 2 + 1) * revealScale);
                entries.Add(new VisibleItemEntry(item, SKRect.Create(x, y, width, sepHeight), depth));
                y += sepHeight;
                firstItem = false;
                continue;
            }

            var itemHeight = (float)Math.Round(ItemHeight * revealScale);
            entries.Add(new VisibleItemEntry(item, SKRect.Create(x, y, width, itemHeight), depth));
            y += itemHeight;
            firstItem = false;

            var childReveal = revealScale * GetAccordionProgress(item);
            if (!UseAccordionSubmenus || !item.HasDropDown || childReveal <= 0.001f)
                continue;

            AppendVisibleItemEntries(item.DropDownItems, depth + 1, entries, ref y, x, width, verticalGap, childReveal);
        }
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
                LastHoveredElement = null!;
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
        if (window is WindowBase uiWindow)
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

    private WindowBase? ResolveOwner(ElementBase? element)
    {
        if (Parent is WindowBase w) return w;
        if (element != null)
        {
            if (element.ParentWindow is WindowBase pw) return pw;
            if (element.FindForm() is WindowBase fw) return fw;
        }

        if (Application.ActiveForm is WindowBase aw) return aw;
        return Application.OpenForms.OfType<WindowBase>().FirstOrDefault();
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

        if (_ownerBoundsRefreshQueued || _ownerWindow == null)
            return;

        _ownerBoundsRefreshQueued = true;

        try
        {
            _ownerWindow.BeginInvoke((Action)(() =>
            {
                _ownerBoundsRefreshQueued = false;

                if (IsOpen)
                    RepositionToOwnerBounds();
            }));
        }
        catch
        {
            _ownerBoundsRefreshQueued = false;
        }
    }

    private void OnOwnerMouseDown(object? sender, MouseEventArgs e)
    {
        if (!IsOpen || !AutoClose) return;
        if (!Bounds.Contains(e.Location)) Hide();
    }

    private void OnOwnerDeactivate(object? sender, EventArgs e)
    {
        if (!IsOpen || !AutoClose) return;
        Hide();
    }

    private void OnOwnerKeyDown(object? sender, KeyEventArgs e)
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
        ApplyDpiMetrics(DeviceDpi);

        // İçerik genişlik/yükseklik hesabı (shadow hariç)
        var verticalGap = GetVerticalItemGap();
        var contentWidth = ItemPadding * 2;
        float contentHeight;

        if (UseAccordionSubmenus)
        {
            contentWidth = Math.Max(contentWidth, GetAccordionContentWidth(Items, 0) + ItemPadding * 2);
            contentHeight = Math.Min(GetAccordionContentHeight(Items), GetAccordionMaxHeight());
        }
        else
        {
            contentHeight = ItemPadding * 2f;
            var firstItem = true;

            foreach (var item in Items)
            {
                if (!item.Visible)
                    continue;

                if (!firstItem)
                    contentHeight += verticalGap;

                if (item.IsSeparator)
                {
                    contentHeight += SeparatorMargin * 2 + 1;
                }
                else
                {
                    contentWidth = Math.Max(contentWidth, MeasureItemWidth(item) + ItemPadding * 2);
                    contentHeight += ItemHeight;
                }

                firstItem = false;
            }
        }

        // Minimum genişlik garantisi
        contentWidth = Math.Max(contentWidth, BaseMinimumContentWidth * ScaleFactor);

        // En alttaki öğenin border ile kesilmemesi için ekstra alan yok,
        // çünkü son item'dan sonra zaten ItemPadding var.

        var totalWidth = contentWidth;
        var totalHeight = contentHeight;

        return new SKSize((int)Math.Ceiling(totalWidth), (int)Math.Ceiling(totalHeight));
    }

    internal override void OnDpiChanged(float newDpi, float oldDpi)
    {
        ApplyDpiMetrics(newDpi);
        _stableAccordionPopupSize = SKSize.Empty;
        base.OnDpiChanged(newDpi, oldDpi);
        UpdateScrollState();
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        if (TryRouteScrollableMouseMove(e))
        {
            _hoveredItem = null;
            Invalidate();
            return;
        }

        var previousHoveredItem = _hoveredItem;
        _hoveredItem = null;
        var viewportBottom = _viewportHeight;
        var rects = GetVisibleItemEntries();
        for (var i = 0; i < rects.Count; i++)
        {
            var entry = rects[i];
            if (entry.Rect.Bottom < 0f || entry.Rect.Top > viewportBottom || entry.Item.IsSeparator)
                continue;

            if (entry.Rect.Contains(e.Location))
            {
                _hoveredItem = entry.Item;
                break;
            }
        }

        if (previousHoveredItem != _hoveredItem)
        {
            if (!UseAccordionSubmenus && _hoveredItem?.HasDropDown == true)
                OpenSubmenu(_hoveredItem);
            else if (!UseAccordionSubmenus)
                CloseSubmenu();
        }

        Invalidate();
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            RaiseMouseDown(e);

        if (TryRouteScrollableMouseDown(e))
            return;

        if (e.Button != MouseButtons.Left)
            return;

        var rects = GetVisibleItemEntries();
        var viewportBottom = _viewportHeight;
        for (var i = 0; i < rects.Count; i++)
        {
            var entry = rects[i];
            if (entry.Rect.Bottom < 0f || entry.Rect.Top > viewportBottom || entry.Item.IsSeparator)
                continue;

            if (entry.Rect.Contains(e.Location))
            {
                OnItemClicked(entry.Item);
                return;
            }
        }

        CloseSubmenu();
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredItem = null;
        Invalidate();
    }

    protected override SKRect GetItemBounds(MenuItem item)
    {
        var rects = GetVisibleItemEntries();
        for (var i = 0; i < rects.Count; i++)
        {
            if (ReferenceEquals(rects[i].Item, item))
                return rects[i].Rect;
        }

        return base.GetItemBounds(item);
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
        EnsureSkiaCaches();

        var fadeProgress = (float)_fadeInAnimation.GetProgress();
        var fadeAlpha = (byte)(fadeProgress * 255);

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

        var scale = ScaleFactor;
        var viewportRect = new SKRect(
            0f,
            0f,
            Math.Max(1f, _viewportWidth + ItemPadding * 2),
            _viewportHeight);

        var itemClipSave = canvas.Save();
        canvas.ClipRoundRect(_radius.ToRoundRect(viewportRect), antialias: true);

        var rects = GetVisibleItemEntries();
        var viewportBottom = _viewportHeight;

        for (var itemIndex = 0; itemIndex < rects.Count; itemIndex++)
        {
            var item = rects[itemIndex].Item;
            var itemRect = rects[itemIndex].Rect;
            var itemDepth = rects[itemIndex].Depth;

            // Skip hidden items — visibility should control drawing and layout
            if (!item.Visible)
                continue;

            if (itemRect.Bottom < 0f || itemRect.Top > viewportBottom)
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

            var textX = itemRect.Left + 10 * scale + (UseAccordionSubmenus ? itemDepth * GetAccordionIndent() : 0f);

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
                    using var chk = new SKPath();
                    chk.MoveTo(cx - s * 0.4f, cy - s * 0.15f);
                    chk.LineTo(cx, cy + s * 0.35f);
                    chk.LineTo(cx + s * 0.6f, cy - s * 0.5f);
                    canvas.DrawPath(chk, checkPaint);
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
            var shortcutFont = GetShortcutSkFont();
            var shortcutText = GetShortcutText(item, vertical: true);
            var shortcutWidth = MeasureShortcutTextWidth(shortcutFont, shortcutText);

            _textPaint!.Color = textColor.WithAlpha(fadeAlpha);

            // Reserve space for chevron if item has dropdown
            var textWidth = itemRect.Right - textX;
            if (shortcutText.Length > 0)
                textWidth -= shortcutWidth + 22f * scale;
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
            DrawControlText(canvas, item.Text, textBounds, _textPaint, font, ContentAlignment.MiddleLeft, false, true);

            if (shortcutText.Length > 0)
            {
                var shortcutRight = itemRect.Right - 12f * scale;
                if (ShowSubmenuArrow && item.HasDropDown)
                    shortcutRight -= 34f * scale;

                var shortcutBounds = SkiaSharp.SKRect.Create(
                    Math.Max(textX, shortcutRight - shortcutWidth),
                    itemRect.Top,
                    Math.Max(1f, shortcutWidth),
                    itemRect.Height);

                _textPaint.Color = textColor.WithAlpha((byte)(fadeAlpha * 120 / 255f));
                DrawControlText(canvas, shortcutText, shortcutBounds, _textPaint, shortcutFont, ContentAlignment.MiddleRight, false, true);
                _textPaint.Color = textColor.WithAlpha(fadeAlpha);
            }

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

                if (UseAccordionSubmenus)
                {
                    var accordionProgress = GetAccordionProgress(item);
                    var chevronRotation = GetSpringChevronRotation(accordionProgress);
                    _chevronPath.MoveTo(-chevronSize, -chevronSize);
                    _chevronPath.LineTo(2 * scale, 0f);
                    _chevronPath.LineTo(-chevronSize, chevronSize);
                    _chevronPath.Close();

                    var chevronSave = canvas.Save();
                    canvas.Translate(chevronX, chevronY);
                    canvas.RotateDegrees(chevronRotation);
                    canvas.DrawPath(_chevronPath, _arrowPaint);
                    canvas.RestoreToCount(chevronSave);
                    continue;
                }

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

    private float GetAccordionProgress(MenuItem item)
    {
        if (_accordionAnims.TryGetValue(item, out var anim))
            return (float)anim.GetProgress();

        return _expandedItems.Contains(item) ? 1f : 0f;
    }

    private static float GetSpringChevronRotation(float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        const float overshoot = 2.45f;
        var t = progress - 1f;
        var eased = 1f + (t * t * ((overshoot + 1f) * t + overshoot));
        return eased * 90f;
    }

    private AnimationManager EnsureAccordionAnim(MenuItem item)
    {
        if (_accordionAnims.TryGetValue(item, out var anim))
            return anim;

        anim = new AnimationManager(true)
        {
            Increment = AccordionAnimationIncrement,
            AnimationType = AnimationType.CubicEaseOut,
            InterruptAnimation = true
        };
        anim.OnAnimationProgress += _ =>
        {
            UpdateScrollState();
            Invalidate();
        };
        anim.OnAnimationFinished += _ =>
        {
            UpdateScrollState();

            if (anim.GetProgress() <= 0.001f)
                CollapseAccordionBranch(item, animate: false);

            if (ReferenceEquals(_accordionCenterTarget, item) && anim.GetProgress() >= 0.999f)
            {
                CenterAccordionBranch(item);
                _accordionCenterTarget = null;
            }

            Invalidate();
        };
        _accordionAnims[item] = anim;
        return anim;
    }

    private void ToggleAccordionItem(MenuItem item)
    {
        if (!item.HasDropDown)
            return;

        var expanding = GetAccordionProgress(item) <= 0.001f;
        if (TryGetContainingCollection(Items, item, out var collection) && collection != null)
        {
            for (var i = 0; i < collection.Count; i++)
            {
                var sibling = collection[i];
                if (!ReferenceEquals(sibling, item))
                    CollapseAccordionBranch(sibling, animate: true);
            }
        }

        if (expanding)
        {
            _expandedItems.Add(item);
            _accordionCenterTarget = item;
            EnsureAccordionAnim(item).StartNewAnimation(AnimationDirection.In);
        }
        else
        {
            if (ReferenceEquals(_accordionCenterTarget, item))
                _accordionCenterTarget = null;

            CollapseAccordionBranch(item, animate: true);
        }

        UpdateScrollState();
        Invalidate();
    }

    private void CollapseAccordionBranch(MenuItem item, bool animate)
    {
        if (animate)
        {
            if (_expandedItems.Contains(item) || GetAccordionProgress(item) > 0.001f)
                EnsureAccordionAnim(item).StartNewAnimation(AnimationDirection.Out);
        }
        else
        {
            _expandedItems.Remove(item);
            if (_accordionAnims.TryGetValue(item, out var anim))
                anim.SetProgress(0);
        }

        for (var i = 0; i < item.DropDownItems.Count; i++)
            CollapseAccordionBranch(item.DropDownItems[i], animate);
    }

    private void CenterAccordionBranch(MenuItem item)
    {
        if (_vScrollBar == null || !_vScrollBar.Visible || _contentHeight <= _viewportHeight)
            return;

        var rects = GetVisibleItemEntries(0f);
        var itemIndex = -1;
        for (var i = 0; i < rects.Count; i++)
        {
            if (ReferenceEquals(rects[i].Item, item))
            {
                itemIndex = i;
                break;
            }
        }

        if (itemIndex < 0)
            return;

        var itemDepth = rects[itemIndex].Depth;
        var branchBounds = rects[itemIndex].Rect;
        for (var i = itemIndex + 1; i < rects.Count; i++)
        {
            if (rects[i].Depth <= itemDepth)
                break;

            branchBounds = new SKRect(
                Math.Min(branchBounds.Left, rects[i].Rect.Left),
                Math.Min(branchBounds.Top, rects[i].Rect.Top),
                Math.Max(branchBounds.Right, rects[i].Rect.Right),
                Math.Max(branchBounds.Bottom, rects[i].Rect.Bottom));
        }

        var targetOffset = Math.Clamp(branchBounds.MidY - (_viewportHeight / 2f), 0f, Math.Max(0f, _contentHeight - _viewportHeight));
        _vScrollBar.Value = targetOffset;
    }

    private static bool TryGetContainingCollection(IReadOnlyList<MenuItem> items, MenuItem target, out IReadOnlyList<MenuItem>? collection)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (ReferenceEquals(item, target))
            {
                collection = items;
                return true;
            }

            if (item.HasDropDown && TryGetContainingCollection(item.DropDownItems, target, out collection))
                return true;
        }

        collection = null;
        return false;
    }

    private void EnsureSkiaCaches()
    {
        _separatorPaint ??= new SKPaint { IsAntialias = true, StrokeWidth = 1 };
        _hoverPaint ??= new SKPaint { IsAntialias = true };
        _iconPaint ??= new SKPaint { IsAntialias = true };
        _textPaint ??= new SKPaint { IsAntialias = true };
        _arrowPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        _chevronPath ??= new SKPath();
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

        w += GetShortcutTextReserve(item, vertical: true, font);

        if (ShowSubmenuArrow && item.HasDropDown)
            w += 30 * scale; // Extra space for chevron 

        return w;
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var anim in _accordionAnims.Values)
                anim.Dispose();
            _accordionAnims.Clear();

            _defaultSkFont?.Dispose();
            _defaultSkFont = null;

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
        }

        base.Dispose(disposing);
    }
}