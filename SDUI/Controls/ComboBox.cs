using SDUI.Animation;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SDUI.Controls;

public sealed class ComboBoxItem
{
    private string _text = string.Empty;

    public ComboBoxItem()
    {
    }

    public ComboBoxItem(string text, object? value = null)
    {
        _text = text ?? string.Empty;
        Value = value;
    }

    [DefaultValue("")]
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public object? Value { get; set; }

    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    public object? Tag { get; set; }

    public override string ToString()
    {
        return Text;
    }
}

public class ComboBox : ElementBase
{
    private const int DefaultDropDownItemHeight = 36;
    private const int DefaultDropDownVerticalPadding = 8;
    private const int DefaultHeight = 40;
    private const int DefaultMaxDropDownItems = 8;
    private const int DefaultMinWidth = 140;
    private const int DefaultWidth = 200;
    private const string DefaultMultiSelectSeparator = ", ";

    private readonly AnimationManager _chevronAnimation;
    private readonly ComboBoxDropDown _dropDown;
    private readonly ObjectCollection _items;
    private readonly List<int> _selectedIndices = new();
    private readonly List<object?> _selectedItems = new();
    private KeyEventHandler? _ownerKeyDownHandler;
    private MouseEventHandler? _ownerMouseDownHandler;
    private EventHandler? _ownerDeactivateHandler;
    private SKPaint? _accentPaint;
    private SKPath? _chevronPath;
    private SKPaint? _chevronPaint;
    private SKPaint? _colorPreviewBorderPaint;
    private SKPaint? _colorPreviewPaint;
    private SKFont? _renderFont;
    private int _renderFontDpi;
    private SKFont? _renderFontSource;
    private SKPaint? _separatorPaint;
    private WindowBase? _handlerWindow;
    private bool _multiSelect;
    private bool _ownerHandlersAttached;
    private bool _showDropDownArrow = true;
    private bool _showItemColorPreview;
    private bool _showSelectionIndicator;
    private bool _suppressNextMouseToggle;
    private int _dropDownHeight;
    private int _dropDownItemHeight = DefaultDropDownItemHeight;
    private int _dropDownVerticalPadding = DefaultDropDownVerticalPadding;
    private string _displayMember = string.Empty;
    private int _maxDropDownItems = DefaultMaxDropDownItems;
    private string _placeholderText = "Select an option";
    private int _selectedIndex = -1;
    private object? _selectedItem;
    private string _valueMember = string.Empty;

    public ComboBox()
    {
        _items = new ObjectCollection(this);
        _dropDown = new ComboBoxDropDown(this);

        _chevronAnimation = new AnimationManager(true)
        {
            Increment = 0.18,
            AnimationType = AnimationType.CubicEaseOut,
            InterruptAnimation = true
        };
        _chevronAnimation.OnAnimationProgress += _ => Invalidate();
        _chevronAnimation.OnAnimationFinished += _ => Invalidate();

        _dropDown.Opening += OnDropDownOpening;
        _dropDown.Closing += OnDropDownClosing;
        _dropDown.Closed += OnDropDownClosed;

        AutoEllipsis = true;
        CanSelect = true;
        MinimumSize = new SKSize(DefaultMinWidth, DefaultHeight);
        Padding = new Thickness(14, 0, 48, 0);
        Radius = new Radius(12);
        Size = new SKSize(DefaultWidth, DefaultHeight);
        TabStop = true;
        TextAlign = ContentAlignment.MiddleLeft;

        ApplyTheme();
        ColorScheme.ThemeChanged += OnThemeChanged;

        UpdateRenderedText();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Data")]
    public ObjectCollection Items => _items;

    [Category("Data")]
    [DefaultValue("")]
    public string DisplayMember
    {
        get => _displayMember;
        set
        {
            value ??= string.Empty;
            if (_displayMember == value)
                return;

            _displayMember = value;
            HandleItemsCollectionChanged();
        }
    }

    [Category("Data")]
    [DefaultValue("")]
    public string ValueMember
    {
        get => _valueMember;
        set => _valueMember = value ?? string.Empty;
    }

    [Category("Appearance")]
    [DefaultValue("Select an option")]
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            var next = value ?? string.Empty;
            if (_placeholderText == next)
                return;

            _placeholderText = next;
            UpdateRenderedText();
            ReevaluateVisualStyles();
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowDropDownArrow
    {
        get => _showDropDownArrow;
        set
        {
            if (_showDropDownArrow == value)
                return;

            _showDropDownArrow = value;
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowSelectionIndicator
    {
        get => _showSelectionIndicator;
        set
        {
            if (_showSelectionIndicator == value)
                return;

            _showSelectionIndicator = value;
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowItemColorPreview
    {
        get => _showItemColorPreview;
        set
        {
            if (_showItemColorPreview == value)
                return;

            _showItemColorPreview = value;
            RefreshDropDownLayout();
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool MultiSelect
    {
        get => _multiSelect;
        set
        {
            if (_multiSelect == value)
                return;

            _multiSelect = value;

            if (!_multiSelect && _selectedIndices.Count > 1)
            {
                var keepIndex = _selectedIndex >= 0
                    ? _selectedIndex
                    : _selectedIndices.Count > 0
                        ? _selectedIndices[0]
                        : -1;
                ApplySelectionState(keepIndex >= 0 ? new[] { keepIndex } : Array.Empty<int>(), keepIndex, raiseCommitted: false);
                return;
            }

            UpdateRenderedText();
            _dropDown.SyncFromOwner();
            ReevaluateVisualStyles();
            InvalidateMeasure();
            Invalidate();
        }
    }

    [Category("Layout")]
    [DefaultValue(DefaultDropDownItemHeight)]
    public int DropDownItemHeight
    {
        get => _dropDownItemHeight;
        set
        {
            var clamped = Math.Max(28, value);
            if (_dropDownItemHeight == clamped)
                return;

            _dropDownItemHeight = clamped;
            RefreshDropDownLayout();
        }
    }

    [Category("Layout")]
    [DefaultValue(DefaultDropDownVerticalPadding)]
    public int DropDownVerticalPadding
    {
        get => _dropDownVerticalPadding;
        set
        {
            var clamped = Math.Max(4, value);
            if (_dropDownVerticalPadding == clamped)
                return;

            _dropDownVerticalPadding = clamped;
            RefreshDropDownLayout();
        }
    }

    [Category("Layout")]
    [DefaultValue(DefaultMaxDropDownItems)]
    public int MaxDropDownItems
    {
        get => _maxDropDownItems;
        set
        {
            var clamped = Math.Max(1, value);
            if (_maxDropDownItems == clamped)
                return;

            _maxDropDownItems = clamped;
            RefreshDropDownLayout();
        }
    }

    [Category("Layout")]
    [DefaultValue(0)]
    public int DropDownHeight
    {
        get => _dropDownHeight;
        set
        {
            var clamped = Math.Max(0, value);
            if (_dropDownHeight == clamped)
                return;

            _dropDownHeight = clamped;
            RefreshDropDownLayout();
        }
    }

    [Browsable(false)]
    public bool DroppedDown => _dropDown is { IsOpen: true, IsClosing: false };

    [Category("Behavior")]
    [DefaultValue(typeof(OpeningEffectType), nameof(OpeningEffectType.PopFade))]
    public OpeningEffectType DropDownOpeningEffect
    {
        get => _dropDown.OpeningEffect;
        set
        {
            if (_dropDown.OpeningEffect == value)
                return;

            _dropDown.OpeningEffect = value;
            _dropDown.Invalidate();
        }
    }

    [Browsable(false)]
    public IReadOnlyList<int> SelectedIndices => _selectedIndices;

    [Browsable(false)]
    public IReadOnlyList<object?> SelectedItems => _selectedItems;

    [Category("Data")]
    [DefaultValue(-1)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndexCore(value, raiseCommitted: false);
    }

    [Category("Data")]
    [DefaultValue(null)]
    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetSelectedIndexCore(_items.IndexOf(value), raiseCommitted: false);
    }

    [Browsable(false)]
    public object? SelectedValue
    {
        get => GetItemValue(_selectedItem);
        set => SetSelectedIndexCore(FindItemIndexByValue(value), raiseCommitted: false);
    }

    public override string Text
    {
        get => GetDisplayText(includePlaceholder: false);
        set
        {
            if (MultiSelect)
            {
                SetSelectedIndicesFromText(value ?? string.Empty, raiseCommitted: false);
                return;
            }

            SetSelectedIndexCore(FindItemIndexByText(value ?? string.Empty), raiseCommitted: false);
        }
    }

    public override SKRect DisplayRectangle
    {
        get
        {
            var rect = base.DisplayRectangle;
            var scale = ScaleFactor;

            rect.Top = Math.Max(rect.Top, Border.Top);
            rect.Bottom = Math.Max(rect.Top, Height - Padding.Bottom);

            if (ShowSelectionIndicator && HasSelection)
            {
                var indicatorWidth = Math.Max(3f, 3f * scale);
                rect.Left = Math.Min(rect.Right, 12f * scale + indicatorWidth + 10f * scale);
            }

            if (ShowItemColorPreview)
                rect.Left = Math.Min(rect.Right, GetColorPreviewBounds().Right + 10f * scale);

            if (ShowDropDownArrow)
                rect.Right = Math.Max(rect.Left, GetChevronSlotBounds().Left - 12f * scale);

            return rect;
        }
    }

    public event EventHandler? SelectedIndexChanged;
    public event EventHandler? SelectedItemChanged;
    public event EventHandler? SelectedItemsChanged;
    public event EventHandler? SelectionChangeCommitted;
    public event EventHandler? DropDown;
    public event EventHandler? DropDownClosed;

    public override SKSize GetPreferredSize(SKSize proposedSize)
    {
        var font = GetRenderFont();
        var measuredText = ResolvePreferredMeasurementText();
        font.MeasureText(measuredText, out var textBounds);

        var width = textBounds.Width + Padding.Left + Padding.Right + Border.Left + Border.Right;
        if (ShowDropDownArrow)
            width += GetChevronSlotWidth();
        if (ShowSelectionIndicator && HasSelection)
            width += 10f * ScaleFactor;
        if (ShowItemColorPreview)
            width += GetColorPreviewSlotWidth();

        var textHeight = Math.Max(18f, font.Metrics.Descent - font.Metrics.Ascent);
        var height = Math.Max(DefaultHeight, textHeight + Padding.Top + Padding.Bottom + Border.Top + Border.Bottom);
        width = Math.Max(width, MinimumSize.Width > 0 ? MinimumSize.Width : DefaultMinWidth);
        height = Math.Max(height, MinimumSize.Height > 0 ? MinimumSize.Height : DefaultHeight);

        if (MaximumSize.Width > 0)
            width = Math.Min(width, MaximumSize.Width);
        if (MaximumSize.Height > 0)
            height = Math.Min(height, MaximumSize.Height);

        return new SKSize((float)Math.Ceiling(width), (float)Math.Ceiling(height));
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);

        EnsureDrawingResources();

        var scale = ScaleFactor;

        if (ShowSelectionIndicator && HasSelection)
        {
            var indicatorWidth = Math.Max(3f, 3f * scale);
            var indicatorHeight = Math.Max(12f, Height - 18f * scale);
            var indicatorRect = SKRect.Create(12f * scale, (Height - indicatorHeight) * 0.5f, indicatorWidth, indicatorHeight);
            _accentPaint!.Color = Enabled
                ? ColorScheme.Primary.WithAlpha((byte)(DroppedDown ? 210 : 168))
                : ColorScheme.Primary.WithAlpha(96);
            canvas.DrawRoundRect(indicatorRect, indicatorWidth * 0.5f, indicatorWidth * 0.5f, _accentPaint);
        }

        if (ShowItemColorPreview)
        {
            var previewRect = GetColorPreviewBounds();
            var hasColor = TryGetCurrentDisplayColor(out var previewColor);

            _colorPreviewPaint!.Color = hasColor
                ? previewColor
                : ColorScheme.SurfaceVariant.WithAlpha(180);
            _colorPreviewBorderPaint!.Color = hasColor
                ? ColorScheme.Outline.WithAlpha(96)
                : ColorScheme.Outline.WithAlpha(120);

            var previewRadius = Math.Max(5f, 5f * scale);
            canvas.DrawRoundRect(previewRect, previewRadius, previewRadius, _colorPreviewPaint);
            canvas.DrawRoundRect(previewRect, previewRadius, previewRadius, _colorPreviewBorderPaint);
        }

        if (ShowDropDownArrow)
        {
            var slotRect = GetChevronSlotBounds();
            var separatorX = slotRect.Left - 3f * scale;
            _separatorPaint!.Color = Enabled
                ? (DroppedDown || Focused
                    ? ColorScheme.Primary.WithAlpha(84)
                    : IsPointerOver
                        ? BorderColor.WithAlpha(68)
                        : BorderColor.WithAlpha(52))
                : BorderColor.WithAlpha(40);
            canvas.DrawLine(separatorX, 11f * scale, separatorX, Height - 11f * scale, _separatorPaint);

            var chevronProgress = (float)_chevronAnimation.GetProgress();
            var chevronColor = Enabled
                ? ForeColor.InterpolateColor(
                    ColorScheme.Primary,
                    Math.Min(0.44f, (DroppedDown ? 0.16f : 0f) + chevronProgress * 0.18f + (Focused ? 0.08f : 0f) + (IsPointerOver ? 0.05f : 0f)))
                : ForeColor.WithAlpha(128);
            var chevronWidth = (4.6f + chevronProgress * 0.15f) * scale;
            var chevronDepth = (2.35f + chevronProgress * 0.25f) * scale;
            var chevronRotation = 180f * chevronProgress;

            _chevronPaint!.Color = chevronColor;
            _chevronPath!.Reset();
            _chevronPath.MoveTo(-chevronWidth, -chevronDepth);
            _chevronPath.LineTo(0f, chevronDepth);
            _chevronPath.LineTo(chevronWidth, -chevronDepth);

            var chevronSave = canvas.Save();
            canvas.Translate(slotRect.MidX, slotRect.MidY + 0.45f * scale);
            canvas.RotateDegrees(chevronRotation);
            canvas.DrawPath(_chevronPath, _chevronPaint);
            canvas.RestoreToCount(chevronSave);
        }
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!Enabled || !Visible || e.Button != MouseButtons.Left)
            return;

        if (_suppressNextMouseToggle)
        {
            _suppressNextMouseToggle = false;
            UpdatePressedState(false);
            return;
        }

        ToggleDropDown();
        // WM_LBUTTONUP routes to the ContextMenuStrip popup (higher hit-test priority)
        // so ComboBox.OnMouseUp never fires after ToggleDropDown. Clear pressed state now.
        UpdatePressedState(false);
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!Enabled || _items.Count == 0)
            return;

        SelectRelative(e.Delta > 0 ? -1 : 1, raiseCommitted: true);
    }

    internal override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || !Enabled)
            return;

        switch (e.KeyCode)
        {
            case Keys.Enter:
            case Keys.Space:
                ToggleDropDown();
                e.Handled = true;
                return;

            case Keys.Escape:
                if (DroppedDown)
                {
                    CloseDropDown();
                    e.Handled = true;
                }
                return;

            case Keys.Down:
                if (e.Alt)
                    OpenDropDown();
                else
                    SelectRelative(1, raiseCommitted: true);

                e.Handled = true;
                return;

            case Keys.Up:
                if (e.Alt && DroppedDown)
                    CloseDropDown();
                else
                    SelectRelative(-1, raiseCommitted: true);

                e.Handled = true;
                return;

            case Keys.Home:
                SetSelectedIndexCore(FindFirstSelectableIndex(), raiseCommitted: true);
                e.Handled = true;
                return;

            case Keys.End:
                SetSelectedIndexCore(FindLastSelectableIndex(), raiseCommitted: true);
                e.Handled = true;
                return;

            case Keys.PageDown:
                SelectRelative(Math.Max(1, Math.Min(5, _maxDropDownItems - 1)), raiseCommitted: true);
                e.Handled = true;
                return;

            case Keys.PageUp:
                SelectRelative(-Math.Max(1, Math.Min(5, _maxDropDownItems - 1)), raiseCommitted: true);
                e.Handled = true;
                return;
        }
    }

    internal override void OnLostFocus(EventArgs e)
    {
        if (DroppedDown && !IsDropDownDescendant(ParentWindow?.FocusedElement))
            CloseDropDown();

        base.OnLostFocus(e);
    }

    internal override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible && DroppedDown)
            CloseDropDown();

        base.OnVisibleChanged(e);
    }

    internal override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled && DroppedDown)
            CloseDropDown();

        base.OnEnabledChanged(e);
    }

    internal override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        UpdateDropDownAnchor();
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateDropDownAnchor();
    }

    protected override void InvalidateFontCache()
    {
        base.InvalidateFontCache();

        _renderFont?.Dispose();
        _renderFont = null;
        _renderFontSource = null;
        _renderFontDpi = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ColorScheme.ThemeChanged -= OnThemeChanged;

            if (DroppedDown)
                CloseDropDown();

            DetachOwnerWindowHandlers();

            _dropDown.Opening -= OnDropDownOpening;
            _dropDown.Closing -= OnDropDownClosing;
            _dropDown.Closed -= OnDropDownClosed;
            _dropDown.Dispose();
            _chevronAnimation.Dispose();

            _accentPaint?.Dispose();
            _accentPaint = null;
            _chevronPaint?.Dispose();
            _chevronPaint = null;
            _colorPreviewPaint?.Dispose();
            _colorPreviewPaint = null;
            _colorPreviewBorderPaint?.Dispose();
            _colorPreviewBorderPaint = null;
            _separatorPaint?.Dispose();
            _separatorPaint = null;
            _chevronPath?.Dispose();
            _chevronPath = null;
            _renderFont?.Dispose();
            _renderFont = null;
        }

        base.Dispose(disposing);
    }

    internal string GetItemText(object? item)
    {
        if (item is null)
            return string.Empty;

        if (item is ComboBoxItem comboItem)
            return comboItem.Text ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(_displayMember))
        {
            var descriptor = TypeDescriptor.GetProperties(item).Find(_displayMember, true);
            if (descriptor != null)
                return Convert.ToString(descriptor.GetValue(item)) ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }

    internal bool IsItemEnabled(object? item)
    {
        return item is not ComboBoxItem comboItem || comboItem.Enabled;
    }

    internal void CommitSelectionFromDropDown(int index)
    {
        if (MultiSelect)
        {
            ToggleSelectedIndexCore(index, raiseCommitted: true);
            return;
        }

        SetSelectedIndexCore(index, raiseCommitted: true);
    }

    internal float GetDesiredPopupHeight()
    {
        if (_dropDownHeight > 0)
            return _dropDownHeight;

        var visibleItemCount = Math.Min(Math.Max(1, _maxDropDownItems), Math.Max(1, _items.Count));
        return visibleItemCount * _dropDownItemHeight + _dropDownVerticalPadding * 2f;
    }

    internal void ApplyDropDownMetrics()
    {
        _dropDown.ItemHeight = _dropDownItemHeight;
        _dropDown.ItemPadding = _dropDownVerticalPadding;
        _dropDown.MinPopupWidth = Width;
        _dropDown.MaxPopupHeight = GetDesiredPopupHeight();
    }

    private void OnDropDownOpening(object? sender, CancelEventArgs e)
    {
        _chevronAnimation.StartNewAnimation(AnimationDirection.In);
        AttachOwnerWindowHandlers();
        ReevaluateVisualStyles();
        DropDown?.Invoke(this, EventArgs.Empty);
    }

    private void OnDropDownClosing(object? sender, CancelEventArgs e)
    {
        _chevronAnimation.StartNewAnimation(AnimationDirection.Out);
        DetachOwnerWindowHandlers();
        ReevaluateVisualStyles();
        DropDownClosed?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void OnDropDownClosed(object? sender, EventArgs e)
    {
        // Fired after the close animation completes and the dropdown is fully hidden.
        // Re-evaluate once more to ensure the visual state reflects DroppedDown=false.
        ReevaluateVisualStyles();
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        ConfigureVisualStyles(styles =>
        {
            styles
                .DefaultTransition(TimeSpan.FromMilliseconds(170), AnimationType.CubicEaseOut)
                .Base(baseStyle => baseStyle
                    .Background(ColorScheme.Surface)
                    .Foreground(ColorScheme.ForeColor)
                    .Border(1)
                    .BorderColor(ColorScheme.Outline.WithAlpha(118))
                    .Radius(12)
                    .Shadow(BoxShadow.None))
                .OnHover(rule => rule
                    .Background(ColorScheme.SurfaceContainer)
                    .BorderColor(ColorScheme.Primary.WithAlpha(72)))
                .OnPressed(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .BorderColor(ColorScheme.Primary.WithAlpha(110))
                    .Opacity(0.98f))
                .OnFocused(rule => rule
                    .Background(ColorScheme.SurfaceContainer)
                    .BorderColor(ColorScheme.Primary.WithAlpha(145)))
                .When((element, state) => ((ComboBox)element).DroppedDown, rule => rule
                    .Background(ColorScheme.SurfaceContainerHigh)
                    .BorderColor(ColorScheme.Primary.WithAlpha(160)))
                .When((element, state) => !((ComboBox)element).HasSelection, rule => rule
                    .Foreground(ColorScheme.ForeColor.WithAlpha(150)))
                .OnDisabled(rule => rule
                    .Background(ColorScheme.SurfaceVariant)
                    .Foreground(ColorScheme.ForeColor.WithAlpha(160))
                    .BorderColor(ColorScheme.Outline.WithAlpha(96))
                    .Opacity(0.82f)
                    .Shadow(BoxShadow.None));
        }, clearExisting: true);

        RefreshDropDownLayout();
        ReevaluateVisualStyles();
    }

    private void EnsureDrawingResources()
    {
        _accentPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        _separatorPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1f, 1f * ScaleFactor)
        };
        _chevronPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1.45f, 1.45f * ScaleFactor),
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        _colorPreviewPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        _colorPreviewBorderPaint ??= new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1f, 1f * ScaleFactor)
        };
        _chevronPath ??= new SKPath();
    }

    private SKFont GetRenderFont()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : 96;
        var sourceFont = Font;
        if (_renderFont == null || !ReferenceEquals(_renderFontSource, sourceFont) || _renderFontDpi != dpi)
        {
            _renderFont?.Dispose();
            _renderFont = CreateRenderFont(sourceFont);
            _renderFontSource = sourceFont;
            _renderFontDpi = dpi;
        }

        return _renderFont;
    }

    private string ResolvePreferredMeasurementText()
    {
        var fallback = GetDisplayText(includePlaceholder: true);
        var bestText = string.IsNullOrEmpty(fallback) ? " " : fallback;
        var bestWidth = 0f;
        var font = GetRenderFont();

        for (var i = 0; i < _items.Count; i++)
        {
            var candidate = GetItemText(_items[i]);
            font.MeasureText(candidate, out var candidateBounds);
            if (candidateBounds.Width > bestWidth)
            {
                bestWidth = candidateBounds.Width;
                bestText = candidate;
            }
        }

        return bestText;
    }

    private float GetChevronSlotWidth()
    {
        return ShowDropDownArrow ? Math.Max(40f, 40f * ScaleFactor) : 0f;
    }

    private float GetColorPreviewSlotWidth()
    {
        return ShowItemColorPreview ? Math.Max(24f, 24f * ScaleFactor) : 0f;
    }

    private SKRect GetChevronSlotBounds()
    {
        var slotWidth = GetChevronSlotWidth();
        return SKRect.Create(Width - slotWidth - 4f * ScaleFactor, 0f, slotWidth, Height);
    }

    private SKRect GetColorPreviewBounds()
    {
        var scale = ScaleFactor;
        var previewSize = Math.Max(14f, 14f * scale);
        var left = Math.Max(Padding.Left, 14f * scale);

        if (ShowSelectionIndicator && HasSelection)
        {
            var indicatorWidth = Math.Max(3f, 3f * scale);
            left = Math.Max(left, 12f * scale + indicatorWidth + 10f * scale);
        }

        return SKRect.Create(left, (Height - previewSize) * 0.5f, previewSize, previewSize);
    }

    private object? GetItemValue(object? item)
    {
        if (item is null)
            return null;

        if (item is ComboBoxItem comboItem)
            return comboItem.Value ?? comboItem.Text;

        if (!string.IsNullOrWhiteSpace(_valueMember))
        {
            var descriptor = TypeDescriptor.GetProperties(item).Find(_valueMember, true);
            if (descriptor != null)
                return descriptor.GetValue(item);
        }

        return item;
    }

    private int FindItemIndexByText(string text)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (string.Equals(GetItemText(_items[i]), text, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private string GetDisplayText(bool includePlaceholder)
    {
        if (MultiSelect)
        {
            if (_selectedIndices.Count > 0)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < _selectedIndices.Count; i++)
                {
                    var itemText = GetItemText(_items[_selectedIndices[i]]);
                    if (string.IsNullOrEmpty(itemText))
                        continue;

                    if (builder.Length > 0)
                        builder.Append(DefaultMultiSelectSeparator);

                    builder.Append(itemText);
                }

                if (builder.Length > 0)
                    return builder.ToString();
            }

            return includePlaceholder ? _placeholderText : string.Empty;
        }

        if (_selectedIndex >= 0)
            return GetItemText(_selectedItem);

        return includePlaceholder ? _placeholderText : string.Empty;
    }

    private void UpdateRenderedText()
    {
        var displayText = GetDisplayText(includePlaceholder: true);
        if (!string.Equals(base.Text, displayText, StringComparison.Ordinal))
            base.Text = displayText;
    }

    private void SetSelectedIndicesFromText(string text, bool raiseCommitted)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ApplySelectionState(Array.Empty<int>(), -1, raiseCommitted);
            return;
        }

        var tokens = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var indices = new List<int>(tokens.Length);

        for (var i = 0; i < tokens.Length; i++)
        {
            var index = FindItemIndexByText(tokens[i]);
            if (index >= 0 && !indices.Contains(index))
                indices.Add(index);
        }

        ApplySelectionState(indices, indices.Count > 0 ? indices[^1] : -1, raiseCommitted);
    }

    private int FindItemIndexByValue(object? value)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (Equals(GetItemValue(_items[i]), value))
                return i;
        }

        return -1;
    }

    private int FindFirstSelectableIndex()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (IsItemEnabled(_items[i]))
                return i;
        }

        return -1;
    }

    private int FindLastSelectableIndex()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (IsItemEnabled(_items[i]))
                return i;
        }

        return -1;
    }

    private int FindNextSelectableIndex(int startIndex, int direction)
    {
        var index = startIndex;
        while (true)
        {
            index += direction;
            if (index < 0 || index >= _items.Count)
                return -1;

            if (IsItemEnabled(_items[index]))
                return index;
        }
    }

    private void SelectRelative(int direction, bool raiseCommitted)
    {
        if (_items.Count == 0 || direction == 0)
            return;

        var step = Math.Sign(direction);
        var hops = Math.Abs(direction);
        var currentIndex = _selectedIndex >= 0 ? _selectedIndex : step > 0 ? -1 : _items.Count;

        for (var i = 0; i < hops; i++)
        {
            var nextIndex = FindNextSelectableIndex(currentIndex, step);
            if (nextIndex < 0)
                break;

            currentIndex = nextIndex;
        }

        if (currentIndex == _items.Count)
            currentIndex = FindLastSelectableIndex();

        SetSelectedIndexCore(currentIndex, raiseCommitted);
    }

    private void SetSelectedIndexCore(int value, bool raiseCommitted)
    {
        if (value < -1 || value >= _items.Count)
            value = -1;

        if (value >= 0 && !IsItemEnabled(_items[value]))
            value = -1;

        ApplySelectionState(value >= 0 ? new[] { value } : Array.Empty<int>(), value, raiseCommitted);
    }

    private void ToggleSelectedIndexCore(int value, bool raiseCommitted)
    {
        if (value < 0 || value >= _items.Count || !IsItemEnabled(_items[value]))
            return;

        if (!MultiSelect)
        {
            SetSelectedIndexCore(value, raiseCommitted);
            return;
        }

        var indices = new List<int>(_selectedIndices);
        var existingIndex = indices.IndexOf(value);
        if (existingIndex >= 0)
            indices.RemoveAt(existingIndex);
        else
            indices.Add(value);

        var activeIndex = existingIndex >= 0
            ? indices.Count > 0 ? indices[^1] : -1
            : value;

        ApplySelectionState(indices, activeIndex, raiseCommitted);
    }

    private void ApplySelectionState(IEnumerable<int> selection, int activeIndex, bool raiseCommitted)
    {
        var normalized = NormalizeSelection(selection);

        if (!MultiSelect && normalized.Count > 1)
        {
            var keepIndex = activeIndex >= 0 && normalized.Contains(activeIndex)
                ? activeIndex
                : normalized[0];
            normalized.Clear();
            normalized.Add(keepIndex);
        }

        if (activeIndex >= 0 && !normalized.Contains(activeIndex))
        {
            if (activeIndex < _items.Count && IsItemEnabled(_items[activeIndex]))
            {
                if (!MultiSelect)
                    normalized.Clear();

                normalized.Add(activeIndex);
            }
            else
            {
                activeIndex = -1;
            }
        }

        if (normalized.Count == 0)
            activeIndex = -1;
        else if (activeIndex < 0)
            activeIndex = normalized[^1];

        normalized.Sort();

        var previousIndices = new List<int>(_selectedIndices);
        var previousIndex = _selectedIndex;
        var previousItem = _selectedItem;

        if (previousIndex == activeIndex
            && Equals(previousItem, activeIndex >= 0 ? _items[activeIndex] : null)
            && SelectionEquals(previousIndices, normalized))
        {
            return;
        }

        _selectedIndices.Clear();
        _selectedIndices.AddRange(normalized);
        _selectedItems.Clear();
        for (var i = 0; i < _selectedIndices.Count; i++)
            _selectedItems.Add(_items[_selectedIndices[i]]);

        var selectionChanged = !SelectionEquals(previousIndices, _selectedIndices);
        _selectedIndex = activeIndex;
        _selectedItem = activeIndex >= 0 ? _items[activeIndex] : null;

        UpdateRenderedText();
        _dropDown.SyncSelectionState();
        ReevaluateVisualStyles();
        Invalidate();
        InvalidateMeasure();

        if (previousIndex != _selectedIndex)
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

        if (!Equals(previousItem, _selectedItem))
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);

        if (selectionChanged)
            SelectedItemsChanged?.Invoke(this, EventArgs.Empty);

        if (raiseCommitted)
            SelectionChangeCommitted?.Invoke(this, EventArgs.Empty);
    }

    private List<int> NormalizeSelection(IEnumerable<int> selection)
    {
        var normalized = new List<int>();

        foreach (var index in selection)
        {
            if (index < 0 || index >= _items.Count)
                continue;

            if (!IsItemEnabled(_items[index]) || normalized.Contains(index))
                continue;

            normalized.Add(index);
        }

        return normalized;
    }

    private static bool SelectionEquals(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private void ToggleDropDown()
    {
        if (DroppedDown)
        {
            CloseDropDown();
            return;
        }

        OpenDropDown();
    }

    private void OpenDropDown()
    {
        if (!Enabled || !Visible || _items.Count == 0 || ParentWindow == null)
            return;

        ApplyDropDownMetrics();
        _dropDown.SyncFromOwner();
        _dropDown.ShowForOwner();
        ReevaluateVisualStyles();
        Invalidate();
    }

    private void CloseDropDown()
    {
        if (!DroppedDown)
            return;

        _dropDown.Hide();
        ReevaluateVisualStyles();
        Invalidate();
    }

    private void RefreshDropDownLayout()
    {
        ApplyDropDownMetrics();
        _dropDown.SyncFromOwner();
        UpdateDropDownAnchor();
        Invalidate();
    }

    private void HandleItemsCollectionChanged()
    {
        var retainedSelection = new List<int>(_selectedItems.Count);
        for (var i = 0; i < _selectedItems.Count; i++)
        {
            var index = _items.IndexOf(_selectedItems[i]);
            if (index >= 0 && !retainedSelection.Contains(index))
                retainedSelection.Add(index);
        }

        var activeIndex = _selectedItem != null ? _items.IndexOf(_selectedItem) : -1;
        ApplySelectionState(retainedSelection, activeIndex, raiseCommitted: false);

        _dropDown.SyncFromOwner();
        ReevaluateVisualStyles();
        InvalidateMeasure();
        Invalidate();
    }

    private void UpdateDropDownAnchor()
    {
        if (!DroppedDown)
            return;

        ApplyDropDownMetrics();
        _dropDown.UpdateAnchorBounds(this, ClientRectangle);
    }

    private void AttachOwnerWindowHandlers()
    {
        var window = ParentWindow;
        if (window == null || _ownerHandlersAttached)
            return;

        _ownerMouseDownHandler ??= OnOwnerWindowMouseDown;
        _ownerDeactivateHandler ??= OnOwnerWindowDeactivate;
        _ownerKeyDownHandler ??= OnOwnerWindowKeyDown;

        _handlerWindow = window;
        _handlerWindow.MouseDown += _ownerMouseDownHandler;
        _handlerWindow.Deactivate += _ownerDeactivateHandler;
        _handlerWindow.KeyDown += _ownerKeyDownHandler;
        _ownerHandlersAttached = true;
    }

    private void DetachOwnerWindowHandlers()
    {
        if (!_ownerHandlersAttached || _handlerWindow == null)
        {
            _ownerHandlersAttached = false;
            _handlerWindow = null;
            return;
        }

        _handlerWindow.MouseDown -= _ownerMouseDownHandler;
        _handlerWindow.Deactivate -= _ownerDeactivateHandler;
        _handlerWindow.KeyDown -= _ownerKeyDownHandler;
        _ownerHandlersAttached = false;
        _handlerWindow = null;
    }

    private void OnOwnerWindowMouseDown(object sender, MouseEventArgs e)
    {
        if (!DroppedDown)
            return;

        var comboBounds = GetWindowRelativeBounds(this);
        if (comboBounds.Contains(e.Location))
        {
            _suppressNextMouseToggle = true;
            CloseDropDown();
            return;
        }

        var popupBounds = GetWindowRelativeBounds(_dropDown);
        if (popupBounds.Contains(e.Location))
            return;

        CloseDropDown();
    }

    private void OnOwnerWindowDeactivate(object? sender, EventArgs e)
    {
        if (DroppedDown)
            CloseDropDown();
    }

    private void OnOwnerWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (!DroppedDown)
            return;

        if (e.KeyCode == Keys.Escape)
        {
            CloseDropDown();
            e.Handled = true;
        }
    }

    private bool IsDropDownDescendant(ElementBase? element)
    {
        var current = element;
        while (current != null)
        {
            if (ReferenceEquals(current, _dropDown))
                return true;

            current = current.Parent as ElementBase;
        }

        return false;
    }

    public bool GetItemSelected(int index)
    {
        return index >= 0 && index < _items.Count && _selectedIndices.Contains(index);
    }

    public void SetItemSelected(int index, bool selected)
    {
        if (index < 0 || index >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (!MultiSelect)
        {
            if (selected)
                SetSelectedIndexCore(index, raiseCommitted: false);
            else if (_selectedIndex == index)
                SetSelectedIndexCore(-1, raiseCommitted: false);

            return;
        }

        var indices = new List<int>(_selectedIndices);
        var existingIndex = indices.IndexOf(index);

        if (selected && existingIndex < 0)
            indices.Add(index);
        else if (!selected && existingIndex >= 0)
            indices.RemoveAt(existingIndex);

        var activeIndex = selected
            ? index
            : indices.Count > 0 ? indices[^1] : -1;

        ApplySelectionState(indices, activeIndex, raiseCommitted: false);
    }

    public void ClearSelectedItems()
    {
        ApplySelectionState(Array.Empty<int>(), -1, raiseCommitted: false);
    }

    internal bool TryGetItemColor(object? item, out SKColor color)
    {
        if (item is SKColor directColor)
        {
            color = directColor;
            return true;
        }

        var value = GetItemValue(item);
        if (value is SKColor selectedColor)
        {
            color = selectedColor;
            return true;
        }

        if (item != null)
        {
            var descriptor = TypeDescriptor.GetProperties(item).Find("Color", true);
            if (descriptor?.GetValue(item) is SKColor reflectedColor)
            {
                color = reflectedColor;
                return true;
            }
        }

        color = SKColors.Transparent;
        return false;
    }

    internal bool ShouldShowColorIcons()
    {
        if (!ShowItemColorPreview)
            return false;

        for (var i = 0; i < _items.Count; i++)
        {
            if (TryGetItemColor(_items[i], out _))
                return true;
        }

        return false;
    }

    internal SKBitmap CreateColorPreviewIcon(SKColor color)
    {
        var edge = Math.Max(14, (int)Math.Round(16f * ScaleFactor));
        var bitmap = new SKBitmap(edge, edge, true);

        using var canvas = new SKCanvas(bitmap);
        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = color
        };
        using var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(1f, edge / 14f),
            Color = ColorScheme.Outline.WithAlpha(118)
        };

        canvas.Clear(SKColors.Transparent);
        var inset = Math.Max(1f, edge / 10f);
        var rect = SKRect.Create(inset, inset, edge - inset * 2f, edge - inset * 2f);
        var radius = Math.Max(4f, rect.Width * 0.3f);
        canvas.DrawRoundRect(rect, radius, radius, fillPaint);
        canvas.DrawRoundRect(rect, radius, radius, strokePaint);

        return bitmap;
    }

    private bool TryGetCurrentDisplayColor(out SKColor color)
    {
        if (_selectedItem != null && TryGetItemColor(_selectedItem, out color))
            return true;

        color = SKColors.Transparent;
        return false;
    }

    private bool HasSelection => _selectedIndices.Count > 0;

    public sealed class ObjectCollection : IList
    {
        private readonly List<object?> _items = new();
        private readonly ComboBox _owner;

        internal ObjectCollection(ComboBox owner)
        {
            _owner = owner;
        }

        public int Count => _items.Count;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public object? this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                _owner.HandleItemsCollectionChanged();
            }
        }

        public int Add(object? value)
        {
            _items.Add(value);
            _owner.HandleItemsCollectionChanged();
            return _items.Count - 1;
        }

        public void AddRange(IEnumerable values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
                _items.Add(value);

            _owner.HandleItemsCollectionChanged();
        }

        public void Clear()
        {
            if (_items.Count == 0)
                return;

            _items.Clear();
            _owner.HandleItemsCollectionChanged();
        }

        public bool Contains(object? value)
        {
            return _items.Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_items).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(object? value)
        {
            return _items.IndexOf(value);
        }

        public void Insert(int index, object? value)
        {
            _items.Insert(index, value);
            _owner.HandleItemsCollectionChanged();
        }

        public void Remove(object? value)
        {
            if (_items.Remove(value))
                _owner.HandleItemsCollectionChanged();
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            _owner.HandleItemsCollectionChanged();
        }
    }

    private sealed class ComboBoxDropDown : ContextMenuStrip
    {
        private readonly List<MenuItem> _menuItems = new();
        private readonly List<SKBitmap> _ownedIcons = new();
        private readonly ComboBox _owner;
        private SKPaint? _selectionTickPaint;
        private SKPath? _selectionTickPath;

        public ComboBoxDropDown(ComboBox owner)
        {
            _owner = owner;
            AutoClose = false;
            Border = new Thickness(1);
            Radius = new Radius(12);
            Shadow = new BoxShadow(0f, 6f, 18f, 0, ColorScheme.ShadowColor);
            ShowCheckMargin = false;
            ShowIcons = false;
            ShowImageMargin = false;
            ShowShortcutKeys = false;
            ShowSubmenuArrow = false;
            OpeningEffect = OpeningEffectType.PopFade;
            TabStop = false;
            ApplyOwnerAppearance();
        }

        public override void OnPaint(SKCanvas canvas)
        {
            base.OnPaint(canvas);
            DrawSelectionIndicators(canvas);
        }

        internal void ApplyOwnerAppearance()
        {
            BackColor = ColorScheme.Surface;
            ForeColor = ColorScheme.ForeColor;
            MenuBackColor = ColorScheme.Surface;
            MenuForeColor = ColorScheme.ForeColor;
            HoverBackColor = ColorScheme.Primary.WithAlpha(24);
            BorderColor = _owner.DroppedDown
                ? ColorScheme.Primary.WithAlpha(84)
                : ColorScheme.Outline.WithAlpha(108);
            SeparatorColor = ColorScheme.Outline.WithAlpha(88);
            Shadow = new BoxShadow(0f, 6f, 18f, 0, ColorScheme.ShadowColor);
        }

        internal void SyncFromOwner()
        {
            ApplyOwnerAppearance();
            ReleaseOwnedIcons();
            _menuItems.Clear();
            Items.Clear();

            var showColorIcons = _owner.ShouldShowColorIcons();
            ShowIcons = showColorIcons;
            ShowImageMargin = showColorIcons;

            for (var i = 0; i < _owner.Items.Count; i++)
            {
                var sourceItem = _owner.Items[i];
                var menuItem = new MenuItem(_owner.GetItemText(sourceItem))
                {
                    Tag = i,
                    Checked = _owner.MultiSelect && _owner.GetItemSelected(i),
                    Enabled = _owner.IsItemEnabled(sourceItem),
                    Padding = new Thickness(3, 3, _owner.MultiSelect ? 28 : 12, 3)
                };

                if (showColorIcons && _owner.TryGetItemColor(sourceItem, out var previewColor))
                {
                    var icon = _owner.CreateColorPreviewIcon(previewColor);
                    menuItem.Icon = icon;
                    _ownedIcons.Add(icon);
                }

                menuItem.Click += OnMenuItemClick;
                AddItem(menuItem);
                _menuItems.Add(menuItem);
            }
        }

        internal void SyncSelectionState()
        {
            for (var i = 0; i < _menuItems.Count; i++)
                _menuItems[i].Checked = _owner.MultiSelect && _owner.GetItemSelected(i);

            Invalidate();
        }

        internal void ShowForOwner()
        {
            if (_owner.Items.Count == 0)
                return;

            ShowAnchoredBelow(_owner, _owner.ClientRectangle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseOwnedIcons();
                _selectionTickPaint?.Dispose();
                _selectionTickPaint = null;
                _selectionTickPath?.Dispose();
                _selectionTickPath = null;
            }

            base.Dispose(disposing);
        }

        private void OnMenuItemClick(object? sender, EventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not int index)
                return;

            _owner.CommitSelectionFromDropDown(index);
        }

        protected override void OnItemClicked(MenuItem item)
        {
            if (_owner.MultiSelect && !item.HasDropDown)
            {
                item.OnClick();
                SyncSelectionState();
                return;
            }

            base.OnItemClicked(item);
        }

        private void DrawSelectionIndicators(SKCanvas canvas)
        {
            if (!_owner.MultiSelect)
                return;

            EnsureSelectionResources();

            var scale = ScaleFactor;
            for (var i = 0; i < _menuItems.Count; i++)
            {
                var item = _menuItems[i];
                if (!item.Checked || !item.Visible)
                    continue;

                var itemRect = GetItemBounds(item);
                if (itemRect.IsEmpty || itemRect.Bottom < 0f || itemRect.Top > Height)
                    continue;

                var tickCenterX = itemRect.Right - 14f * scale;
                var tickCenterY = itemRect.MidY;

                _selectionTickPaint!.Color = item.Enabled
                    ? ColorScheme.Primary.WithAlpha(226)
                    : ColorScheme.ForeColor.WithAlpha(118);
                _selectionTickPath!.Reset();
                _selectionTickPath.MoveTo(tickCenterX - 4.4f * scale, tickCenterY + 0.1f * scale);
                _selectionTickPath.LineTo(tickCenterX - 1.5f * scale, tickCenterY + 3.1f * scale);
                _selectionTickPath.LineTo(tickCenterX + 4.7f * scale, tickCenterY - 3.4f * scale);
                canvas.DrawPath(_selectionTickPath, _selectionTickPaint);
            }
        }

        private void ReleaseOwnedIcons()
        {
            for (var i = 0; i < _ownedIcons.Count; i++)
                _ownedIcons[i].Dispose();

            _ownedIcons.Clear();
        }

        private void EnsureSelectionResources()
        {
            _selectionTickPaint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1.55f, 1.55f * ScaleFactor),
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            _selectionTickPath ??= new SKPath();
        }
    }
}
