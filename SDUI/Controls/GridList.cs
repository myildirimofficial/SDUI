using SDUI.Animation;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SDUI.Controls;

public class GridList : ElementBase
{
    private const float DefaultHeaderHeight = 38f;
    private const float DefaultRowHeight = 36f;
    private const float DefaultGroupHeaderHeight = 30f;
    private const float DefaultCellPadding = 10f;
    private const float ResizeGripWidth = 10f;
    private const float ResizeHitWidth = 18f;
    private const float RowResizeGripHeight = 6f;
    private const float CheckBoxSize = 16f;
    private const float IconSize = 16f;

    // Group collapse state is tracked per rendered group instance (group index) rather than group text.
    // This avoids collapsing every group that shares the same name when multiple groups have identical labels.
    private readonly Dictionary<int, bool> _collapsedGroups = new();
    private readonly Dictionary<int, AnimationManager> _groupAnimations = new();
    private readonly List<ColumnLayout> _columnLayouts = new();
    private readonly List<int> _displayItemIndices = new();
    private readonly List<LayoutEntry> _layoutEntries = new();
    private readonly HashSet<int> _selectedIndices = new();

    private float _bodyViewportHeight;
    private float _bodyViewportWidth;
    private float _contentHeight;
    private float _contentWidth;
    private bool _geometryDirty = true;
    private bool _headerVisible = true;
    private bool _stickyHeader = true;
    private bool _allowColumnResize = true;
    private bool _allowColumnSort = true;
    private bool _fullRowSelect = true;
    private bool _multiSelect;
    private bool _groupingEnabled;
    private bool _checkBoxes;
    private bool _allowRowResize;
    private bool _resizeAllRows;
    private bool _showGridLines = true;
    private bool _autoSortOnHeaderClick = true;
    private float _headerHeight;
    private float _rowHeight;
    private float _groupHeaderHeight;
    private float _cellPadding;
    private bool _hoveredHeader;
    private int _hoveredColumnIndex = -1;
    private int _hoveredHeaderResizeColumnIndex = -1;
    private int _hoveredGroupIndex = -1;
    private int _hoveredItemIndex = -1;
    private int _hoveredRowResizeIndex = -1;
    private float _horizontalOffset;
    private bool _isResizingColumn;
    private bool _isResizingRow;
    private int _pressedColumnIndex = -1;
    private int _pressedItemIndex = -1;
    private float _resizeOriginX;
    private float _resizeOriginY;
    private float _resizeOriginWidth;
    private int _resizingColumnIndex = -1;
    private int _resizingRowIndex = -1;
    private float _resizeOriginRowHeight;
    private int _selectedIndex = -1;
    private GridListSortDirection _sortDirection;
    private int _sortColumnIndex = -1;
    private float _verticalOffset;
    private SKColor _headerBackColor;
    private SKColor _headerForeColor;
    private SKColor _groupHeaderBackColor;
    private SKColor _alternatingRowBackColor;
    private SKColor _hoverRowBackColor;
    private SKColor _selectionBackColor;
    private SKColor _gridLineColor;

    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly SKPaint _fillPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _gridLinePaint = new() { IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private readonly SKPaint _textPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };

    public GridList()
    {
        CanSelect = true;
        TabStop = true;
        Cursor = Cursors.Default;
        BackColor = SKColors.Transparent;
        Border = new Thickness(1);
        BorderColor = SKColors.Transparent;
        Radius = new Radius(12);
        HeaderHeight = DefaultHeaderHeight;
        RowHeight = DefaultRowHeight;
        GroupHeaderHeight = DefaultGroupHeaderHeight;
        CellPadding = DefaultCellPadding;
        AlternatingRowBackColor = SKColors.Empty;
        HeaderBackColor = SKColors.Empty;
        HeaderForeColor = SKColors.Empty;
        GroupHeaderBackColor = SKColors.Empty;
        SelectionBackColor = SKColors.Empty;
        HoverRowBackColor = SKColors.Empty;
        GridLineColor = SKColors.Empty;

        Columns = new GridListColumnCollection(this);
        Items = new GridListItemCollection(this);

        if (_vScrollBar != null)
        {
            _vScrollBar.Dock = DockStyle.None;
            _vScrollBar.Visible = false;
            _vScrollBar.AutoHide = true;
            _vScrollBar.Thickness = Math.Max(6, (int)Math.Round(8f * ScaleFactor));
            _vScrollBar.ScrollAnimationIncrement = 1.0;
            _vScrollBar.ScrollAnimationType = AnimationType.Linear;
            _vScrollBar.DisplayValueChanged += (_, _) =>
            {
                _verticalOffset = _vScrollBar.DisplayValue;
            };
        }

        if (_hScrollBar != null)
        {
            _hScrollBar.Dock = DockStyle.None;
            _hScrollBar.Visible = false;
            _hScrollBar.AutoHide = true;
            _hScrollBar.Thickness = Math.Max(6, (int)Math.Round(8f * ScaleFactor));
            _hScrollBar.ScrollAnimationIncrement = 1.0;
            _hScrollBar.ScrollAnimationType = AnimationType.Linear;
            _hScrollBar.DisplayValueChanged += (_, _) =>
            {
                _horizontalOffset = _hScrollBar.DisplayValue;
            };
        }

        ColorScheme.ThemeChanged += OnThemeChanged;
    }

    public GridListColumnCollection Columns { get; }

    public GridListItemCollection Items { get; }

    [DefaultValue(true)]
    public bool HeaderVisible
    {
        get => _headerVisible;
        set => SetGeometryProperty(ref _headerVisible, value);
    }

    [DefaultValue(true)]
    public bool StickyHeader
    {
        get => _stickyHeader;
        set => SetGeometryProperty(ref _stickyHeader, value);
    }

    [DefaultValue(true)]
    public bool AllowColumnResize
    {
        get => _allowColumnResize;
        set => SetVisualProperty(ref _allowColumnResize, value);
    }

    [DefaultValue(true)]
    public bool AllowColumnSort
    {
        get => _allowColumnSort;
        set => SetVisualProperty(ref _allowColumnSort, value);
    }

    [DefaultValue(true)]
    public bool FullRowSelect
    {
        get => _fullRowSelect;
        set => SetVisualProperty(ref _fullRowSelect, value);
    }

    [DefaultValue(false)]
    public bool MultiSelect
    {
        get => _multiSelect;
        set => SetVisualProperty(ref _multiSelect, value);
    }

    [DefaultValue(false)]
    public bool GroupingEnabled
    {
        get => _groupingEnabled;
        set => SetGeometryProperty(ref _groupingEnabled, value);
    }

    [DefaultValue(false)]
    public bool CheckBoxes
    {
        get => _checkBoxes;
        set => SetGeometryProperty(ref _checkBoxes, value);
    }

    [DefaultValue(false)]
    public bool AllowRowResize
    {
        get => _allowRowResize;
        set => SetVisualProperty(ref _allowRowResize, value);
    }

    [DefaultValue(false)]
    public bool ResizeAllRows
    {
        get => _resizeAllRows;
        set => SetVisualProperty(ref _resizeAllRows, value);
    }

    [DefaultValue(true)]
    public bool ShowGridLines
    {
        get => _showGridLines;
        set => SetVisualProperty(ref _showGridLines, value);
    }

    [DefaultValue(true)]
    public bool AutoSortOnHeaderClick
    {
        get => _autoSortOnHeaderClick;
        set => SetVisualProperty(ref _autoSortOnHeaderClick, value);
    }

    [DefaultValue(DefaultHeaderHeight)]
    public float HeaderHeight
    {
        get => _headerHeight;
        set => SetGeometryProperty(ref _headerHeight, Math.Max(24f, value));
    }

    [DefaultValue(DefaultRowHeight)]
    public float RowHeight
    {
        get => _rowHeight;
        set => SetGeometryProperty(ref _rowHeight, Math.Max(22f, value));
    }

    [DefaultValue(DefaultGroupHeaderHeight)]
    public float GroupHeaderHeight
    {
        get => _groupHeaderHeight;
        set => SetGeometryProperty(ref _groupHeaderHeight, Math.Max(20f, value));
    }

    [DefaultValue(DefaultCellPadding)]
    public float CellPadding
    {
        get => _cellPadding;
        set => SetGeometryProperty(ref _cellPadding, Math.Max(2f, value));
    }

    public SKColor HeaderBackColor
    {
        get => _headerBackColor.IsEmpty() ? ColorScheme.SurfaceContainer : _headerBackColor;
        set => SetVisualProperty(ref _headerBackColor, value);
    }

    public SKColor HeaderForeColor
    {
        get => _headerForeColor.IsEmpty() ? ColorScheme.ForeColor : _headerForeColor;
        set => SetVisualProperty(ref _headerForeColor, value);
    }

    public SKColor GroupHeaderBackColor
    {
        get => _groupHeaderBackColor.IsEmpty() ? ColorScheme.SurfaceContainerHigh : _groupHeaderBackColor;
        set => SetVisualProperty(ref _groupHeaderBackColor, value);
    }

    public SKColor AlternatingRowBackColor
    {
        get => _alternatingRowBackColor.IsEmpty() ? ColorScheme.SurfaceContainer.WithAlpha(58) : _alternatingRowBackColor;
        set => SetVisualProperty(ref _alternatingRowBackColor, value);
    }

    public SKColor HoverRowBackColor
    {
        get => _hoverRowBackColor.IsEmpty() ? ColorScheme.Primary.WithAlpha(22) : _hoverRowBackColor;
        set => SetVisualProperty(ref _hoverRowBackColor, value);
    }

    public SKColor SelectionBackColor
    {
        get => _selectionBackColor.IsEmpty() ? ColorScheme.Primary.WithAlpha(44) : _selectionBackColor;
        set => SetVisualProperty(ref _selectionBackColor, value);
    }

    public SKColor GridLineColor
    {
        get => _gridLineColor.IsEmpty() ? ColorScheme.BorderColor.WithAlpha(64) : _gridLineColor;
        set => SetVisualProperty(ref _gridLineColor, value);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    [Browsable(false)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetSelectedIndexCore(value, clearExisting: true, raiseEvent: true);
    }

    [Browsable(false)]
    public GridListItem? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;
        set => SelectedIndex = value != null ? Items.IndexOf(value) : -1;
    }

    [Browsable(false)]
    public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;

    [Browsable(false)]
    public GridListSortDirection SortDirection => _sortDirection;

    [Browsable(false)]
    public int SortColumnIndex => _sortColumnIndex;

    public event EventHandler<int>? SelectedIndexChanged;
    public event EventHandler<GridListSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<GridListColumnClickEventArgs>? ColumnClick;
    public event EventHandler<GridListCellEventArgs>? CellClick;
    public event EventHandler<GridListCellCheckChangedEventArgs>? CellCheckChanged;

    internal void OnColumnsChanged(bool layoutAffected)
    {
        InvalidateFromModelChange(layoutAffected);
    }

    internal void OnItemsChanged(bool layoutAffected)
    {
        InvalidateFromModelChange(layoutAffected);
    }

    internal void ClearSelection()
    {
        _selectedIndices.Clear();
        _selectedIndex = -1;
    }

    private void SetGeometryProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        _geometryDirty = true;
        Invalidate();
    }

    private void SetVisualProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        Invalidate();
    }

    private void InvalidateFromModelChange(bool layoutAffected)
    {
        if (layoutAffected)
            _geometryDirty = true;

        Invalidate();
    }

    protected override bool HandlesMouseWheelScroll =>
        (_vScrollBar != null && (_vScrollBar.Visible || _vScrollBar.Maximum > 0)) ||
        (_hScrollBar != null && (_hScrollBar.Visible || _hScrollBar.Maximum > 0));

    protected override float MouseWheelScrollLines => 1f;

    protected override float GetMouseWheelScrollStep(ScrollBar scrollBar)
    {
        return Math.Max(8f, scrollBar.SmallChange);
    }

    internal override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        _geometryDirty = true;
    }

    internal override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _vScrollBar?.SetHostHover(_vScrollBar.Visible);
        _hScrollBar?.SetHostHover(_hScrollBar.Visible);
    }

    internal override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredHeader = false;
        _hoveredColumnIndex = -1;
        _hoveredHeaderResizeColumnIndex = -1;
        _hoveredGroupIndex = -1;
        _hoveredItemIndex = -1;
        _hoveredRowResizeIndex = -1;
        Cursor = Cursors.Default;
        _vScrollBar?.SetHostHover(false);
        _hScrollBar?.SetHostHover(false);
        Invalidate();
    }

    internal override void OnMouseMove(MouseEventArgs e)
    {
        if (TryGetInputTarget(e, out var target, out var childEventArgs) && target != null && childEventArgs != null)
        {
            target.OnMouseMove(childEventArgs);
            return;
        }

        EnsureLayoutState();

        if (_isResizingColumn)
        {
            Cursor = Cursors.SizeWE;
            var column = GetColumn(_resizingColumnIndex);
            if (column != null)
            {
                column.Width = _resizeOriginWidth + (e.X - _resizeOriginX);
                _geometryDirty = true;
                Invalidate();
            }

            return;
        }

        if (_isResizingRow)
        {
            Cursor = Cursors.SizeNS;
            var nextHeight = Math.Max(22f, _resizeOriginRowHeight + (e.Y - _resizeOriginY));
            if (ResizeAllRows)
            {
                RowHeight = nextHeight;
            }
            else if (_resizingRowIndex >= 0 && _resizingRowIndex < Items.Count)
            {
                Items[_resizingRowIndex].Height = nextHeight;
            }

            _geometryDirty = true;
            Invalidate();
            return;
        }

        var hoverInfo = HitTest(e.Location);
        _hoveredHeader = hoverInfo.Kind == HitKind.Header || hoverInfo.Kind == HitKind.HeaderResize;
        _hoveredColumnIndex = hoverInfo.ColumnIndex;
        _hoveredHeaderResizeColumnIndex = hoverInfo.Kind == HitKind.HeaderResize ? hoverInfo.ColumnIndex : -1;
        _hoveredGroupIndex = hoverInfo.GroupIndex;
        _hoveredItemIndex = hoverInfo.ItemIndex;
        _hoveredRowResizeIndex = hoverInfo.Kind == HitKind.RowResize ? hoverInfo.ItemIndex : -1;
        Cursor = hoverInfo.Kind switch
        {
            HitKind.HeaderResize => Cursors.SizeWE,
            HitKind.RowResize => Cursors.SizeNS,
            _ => Cursors.Default
        };
        Invalidate();
    }

    internal override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        if (TryGetInputTarget(e, out var target, out var childEventArgs) && target != null && childEventArgs != null)
        {
            target.OnMouseDown(childEventArgs);
            return;
        }

        EnsureLayoutState();

        var hit = HitTest(e.Location);
        if (hit.Kind == HitKind.HeaderResize && AllowColumnResize && hit.ColumnIndex >= 0)
        {
            var column = GetColumn(hit.ColumnIndex);
            if (column != null)
            {
                if (TryGetColumnLayout(hit.ColumnIndex, out var columnLayout))
                {
                    column.Width = columnLayout.Width;
                    column.SizeMode = GridListColumnSizeMode.Fixed;
                    _resizeOriginWidth = columnLayout.Width;
                }
                else
                {
                    column.SizeMode = GridListColumnSizeMode.Fixed;
                    _resizeOriginWidth = column.Width;
                }

                _isResizingColumn = true;
                _resizingColumnIndex = hit.ColumnIndex;
                _resizeOriginX = e.X;
                GetParentWindow()?.SetMouseCapture(this);
            }

            return;
        }

        if (hit.Kind == HitKind.RowResize && AllowRowResize && hit.ItemIndex >= 0)
        {
            _isResizingRow = true;
            _resizingRowIndex = hit.ItemIndex;
            _resizeOriginY = e.Y;
            _resizeOriginRowHeight = GetResolvedRowHeight(hit.ItemIndex);
            Cursor = Cursors.SizeNS;
            GetParentWindow()?.SetMouseCapture(this);
            return;
        }

        _pressedColumnIndex = hit.ColumnIndex;
        _pressedItemIndex = hit.ItemIndex;

        switch (hit.Kind)
        {
            case HitKind.GroupHeader:
                ToggleGroupCollapsed(hit.GroupIndex);
                break;
            case HitKind.ItemCell:
                if (hit.ItemIndex >= 0)
                    HandleItemMouseDown(hit, e);
                break;
            case HitKind.Header:
                if (AllowColumnSort && AutoSortOnHeaderClick && hit.ColumnIndex >= 0)
                    ToggleSort(hit.ColumnIndex);
                break;
        }
    }

    internal override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button != MouseButtons.Left)
            return;

        if (_isResizingColumn)
        {
            _isResizingColumn = false;
            _resizingColumnIndex = -1;
            Cursor = Cursors.Default;
            GetParentWindow()?.ReleaseMouseCapture(this);
            return;
        }

        if (_isResizingRow)
        {
            _isResizingRow = false;
            _resizingRowIndex = -1;
            Cursor = Cursors.Default;
            GetParentWindow()?.ReleaseMouseCapture(this);
            return;
        }

        if (TryGetInputTarget(e, out var target, out var childEventArgs) && target != null && childEventArgs != null)
        {
            target.OnMouseUp(childEventArgs);
            return;
        }

        var hit = HitTest(e.Location);
        if (hit.Kind == HitKind.ItemCell && hit.ItemIndex == _pressedItemIndex && hit.ColumnIndex == _pressedColumnIndex)
        {
            RaiseCellClick(hit.ItemIndex, hit.ColumnIndex);
        }

        if (hit.Kind == HitKind.Header && hit.ColumnIndex == _pressedColumnIndex)
        {
            var column = GetColumn(hit.ColumnIndex);
            if (column != null)
                ColumnClick?.Invoke(this, new GridListColumnClickEventArgs(column, hit.ColumnIndex, _sortDirection));
        }

        _pressedColumnIndex = -1;
        _pressedItemIndex = -1;
    }

    internal override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.Button != MouseButtons.Left)
            return;

        if (TryGetInputTarget(e, out var target, out var childEventArgs) && target != null && childEventArgs != null)
        {
            target.OnMouseDoubleClick(childEventArgs);
            return;
        }

        var hit = HitTest(e.Location);
        if (hit.Kind == HitKind.HeaderResize && hit.ColumnIndex >= 0)
        {
            AutoSizeColumn(hit.ColumnIndex);
            return;
        }

        if (hit.Kind == HitKind.RowResize && hit.ItemIndex >= 0)
        {
            ResetRowSize(hit.ItemIndex);
        }
    }

    internal override void OnMouseWheel(MouseEventArgs e)
    {
        if (!Enabled || !Visible)
            return;

        var wantsHorizontal = WantsHorizontalMouseWheel(e);
        if (wantsHorizontal && _hScrollBar != null && (_hScrollBar.Visible || _hScrollBar.Maximum > 0))
        {
            var deltaValue = GetMouseWheelDelta(e, _hScrollBar);
            _hScrollBar.ApplyWheelDelta(e.IsHorizontalWheel ? deltaValue : -deltaValue);
            return;
        }

        base.OnMouseWheel(e);
    }

    internal override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (Items.Count == 0)
            return;

        switch (e.KeyCode)
        {
            case Keys.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Keys.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Keys.PageDown:
                MoveSelection(Math.Max(1, (int)Math.Floor(Math.Max(RowHeight, _bodyViewportHeight) / Math.Max(1f, RowHeight))));
                e.Handled = true;
                break;
            case Keys.PageUp:
                MoveSelection(-Math.Max(1, (int)Math.Floor(Math.Max(RowHeight, _bodyViewportHeight) / Math.Max(1f, RowHeight))));
                e.Handled = true;
                break;
            case Keys.Home:
                SelectedIndex = FindNextSelectableIndex(0, 1);
                e.Handled = true;
                break;
            case Keys.End:
                SelectedIndex = FindNextSelectableIndex(Items.Count - 1, -1);
                e.Handled = true;
                break;
            case Keys.Space:
                if (_selectedIndex >= 0)
                {
                    var checkboxColumn = FindFirstCheckBoxColumn();
                    if (checkboxColumn >= 0)
                    {
                        ToggleCellCheckState(_selectedIndex, checkboxColumn);
                        e.Handled = true;
                    }
                }

                break;
        }
    }

    internal override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _geometryDirty = true;
        Invalidate();
    }

    public override void OnPaint(SKCanvas canvas)
    {
        base.OnPaint(canvas);
        EnsureLayoutState();
        using var renderFont = CreateRenderFont(Font);

        _fillPaint.Color = BackColor == SKColors.Transparent ? ColorScheme.Surface : BackColor;
        _textPaint.Color = ForeColor;
        _borderPaint.Color = GridLineColor;
        _gridLinePaint.Color = GridLineColor;

        var outerRect = GetOuterViewport();
        outerRect = new SKRect(
            (float)Math.Floor(outerRect.Left),
            (float)Math.Floor(outerRect.Top),
            (float)Math.Ceiling(outerRect.Right),
            (float)Math.Ceiling(outerRect.Bottom));

        // Ensure we always clear the background (especially when BackColor is Transparent)
        canvas.DrawRect(outerRect, _fillPaint);

        var bodyViewport = GetBodyViewportRect(outerRect);
        var saveCount = canvas.Save();
        canvas.ClipRect(bodyViewport);

        // Offsetleri tam sayıya yuvarla
        var roundedHorizontalOffset = (float)Math.Round(_horizontalOffset);
        var roundedVerticalOffset = (float)Math.Round(_verticalOffset);

        for (var i = 0; i < _layoutEntries.Count; i++)
        {
            var entry = _layoutEntries[i];
            var drawRect = entry.Bounds;

            if (HeaderVisible && !StickyHeader && entry.Kind == EntryKind.Header)
                drawRect.Offset(bodyViewport.Left - roundedHorizontalOffset, outerRect.Top - roundedVerticalOffset);
            else
                drawRect.Offset(bodyViewport.Left - roundedHorizontalOffset, bodyViewport.Top - roundedVerticalOffset);

            if (drawRect.Bottom < bodyViewport.Top || drawRect.Top > bodyViewport.Bottom)
                continue;

            switch (entry.Kind)
            {
                case EntryKind.Header:
                    DrawHeader(canvas, drawRect, roundedHorizontalOffset, renderFont);
                    break;
                case EntryKind.GroupHeader:
                    DrawGroupHeader(canvas, drawRect, entry.GroupText ?? string.Empty, entry.GroupKey ?? string.Empty, entry.GroupIndex, renderFont);
                    break;
                case EntryKind.Item:
                    DrawItemRow(canvas, drawRect, entry.ItemIndex, renderFont);
                    break;
            }
        }

        canvas.RestoreToCount(saveCount);

        if (HeaderVisible && StickyHeader)
        {
            var stickyHeaderRect = GetStickyHeaderRect(outerRect);
            DrawHeader(canvas, stickyHeaderRect, roundedHorizontalOffset, renderFont);

            using var dividerPaint = new SKPaint
            {
                Color = GridLineColor.WithAlpha(180),
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f
            };
            canvas.DrawLine(stickyHeaderRect.Left, stickyHeaderRect.Bottom, stickyHeaderRect.Right, stickyHeaderRect.Bottom, dividerPaint);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ColorScheme.ThemeChanged -= OnThemeChanged;

            foreach (var animation in _groupAnimations.Values)
                animation.Dispose();

            _groupAnimations.Clear();
        }

        base.Dispose(disposing);
    }

    public void SortByColumn(int columnIndex, GridListSortDirection direction)
    {
        if (columnIndex < 0 || columnIndex >= Columns.Count)
            return;

        _sortColumnIndex = columnIndex;
        _sortDirection = direction;
        _geometryDirty = true;
        Invalidate();
    }

    private void ToggleSort(int columnIndex)
    {
        var column = GetColumn(columnIndex);
        if (column == null || !column.Sortable)
            return;

        if (_sortColumnIndex != columnIndex)
        {
            _sortColumnIndex = columnIndex;
            _sortDirection = GridListSortDirection.Ascending;
        }
        else
        {
            _sortDirection = _sortDirection switch
            {
                GridListSortDirection.None => GridListSortDirection.Ascending,
                GridListSortDirection.Ascending => GridListSortDirection.Descending,
                _ => GridListSortDirection.None
            };
        }

        _geometryDirty = true;
        Invalidate();
    }

    private void MoveSelection(int delta)
    {
        var startIndex = _selectedIndex < 0 ? 0 : _selectedIndex + delta;
        var direction = delta >= 0 ? 1 : -1;
        var target = FindNextSelectableIndex(startIndex, direction);
        if (target >= 0)
            SelectedIndex = target;
    }

    private int FindNextSelectableIndex(int startIndex, int direction)
    {
        if (Items.Count == 0)
            return -1;

        var index = Math.Clamp(startIndex, 0, Items.Count - 1);
        while (index >= 0 && index < Items.Count)
        {
            if (Items[index].Visible)
                return index;
            index += direction;
        }

        return -1;
    }

    private void SetSelectedIndexCore(int index, bool clearExisting, bool raiseEvent)
    {
        if (Items.Count == 0)
        {
            index = -1;
        }
        else if (index >= 0)
        {
            index = Math.Clamp(index, 0, Items.Count - 1);
        }

        var previous = _selectedIndex;

        if (clearExisting)
            _selectedIndices.Clear();

        if (index >= 0)
            _selectedIndices.Add(index);

        _selectedIndex = index;
        EnsureSelectedItemVisible();
        Invalidate();

        if (raiseEvent && previous != _selectedIndex)
        {
            SelectedIndexChanged?.Invoke(this, previous);
            SelectionChanged?.Invoke(this, new GridListSelectionChangedEventArgs(previous, _selectedIndex));
        }
    }

    private void EnsureSelectedItemVisible()
    {
        if (_selectedIndex < 0)
            return;

        EnsureLayoutState();
        for (var i = 0; i < _layoutEntries.Count; i++)
        {
            var entry = _layoutEntries[i];
            if (entry.Kind != EntryKind.Item || entry.ItemIndex != _selectedIndex)
                continue;

            var itemTop = entry.Bounds.Top;
            var itemBottom = entry.Bounds.Bottom;
            var viewportHeight = _bodyViewportHeight;

            if (itemTop < _verticalOffset)
                _vScrollBar!.Value = itemTop;
            else if (itemBottom > _verticalOffset + viewportHeight)
                _vScrollBar!.Value = Math.Max(0, itemBottom - viewportHeight);
            return;
        }
    }

    private void HandleItemMouseDown(HitInfo hit, MouseEventArgs e)
    {
        if (hit.ItemIndex < 0 || hit.ItemIndex >= Items.Count || hit.ColumnIndex < 0 || hit.ColumnIndex >= Columns.Count)
            return;

        if (!MultiSelect || (ModifierKeys & Keys.Control) == 0)
        {
            SetSelectedIndexCore(hit.ItemIndex, clearExisting: true, raiseEvent: true);
        }
        else
        {
            var previous = _selectedIndex;
            if (_selectedIndices.Contains(hit.ItemIndex))
                _selectedIndices.Remove(hit.ItemIndex);
            else
                _selectedIndices.Add(hit.ItemIndex);

            _selectedIndex = hit.ItemIndex;
            Invalidate();

            if (previous != _selectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, previous);
                SelectionChanged?.Invoke(this, new GridListSelectionChangedEventArgs(previous, _selectedIndex));
            }
        }

        if (hit.CheckBoxRect.Contains(e.Location))
            ToggleCellCheckState(hit.ItemIndex, hit.ColumnIndex);
    }

    private void ToggleCellCheckState(int itemIndex, int columnIndex)
    {
        var cell = GetCell(itemIndex, columnIndex, createMissing: true);
        var column = GetColumn(columnIndex);
        if (cell == null || column == null)
            return;

        var previous = cell.CheckState;
        cell.CheckState = previous == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
        CellCheckChanged?.Invoke(this,
            new GridListCellCheckChangedEventArgs(Items[itemIndex], column, cell, itemIndex, columnIndex, previous, cell.CheckState));
    }

    private void RaiseCellClick(int itemIndex, int columnIndex)
    {
        var column = GetColumn(columnIndex);
        var cell = GetCell(itemIndex, columnIndex, createMissing: true);
        if (column == null || cell == null)
            return;

        CellClick?.Invoke(this, new GridListCellEventArgs(Items[itemIndex], column, cell, itemIndex, columnIndex));
    }

    private void ToggleGroupCollapsed(int groupIndex)
    {
        if (groupIndex < 0)
            return;

        var collapsed = _collapsedGroups.TryGetValue(groupIndex, out var isCollapsed) && isCollapsed;
        var nextCollapsed = !collapsed;
        _collapsedGroups[groupIndex] = nextCollapsed;
        var animation = EnsureGroupAnimation(groupIndex);
        animation.SetProgress(GetGroupExpansionProgress(groupIndex));
        animation.StartNewAnimation(nextCollapsed ? AnimationDirection.Out : AnimationDirection.In);
        _geometryDirty = true;
        Invalidate();
    }

    private void EnsureLayoutState()
    {
        if (!_geometryDirty)
            return;

        using var renderFont = CreateRenderFont(Font);
        var outer = GetOuterViewport();
        var initialWidth = Math.Max(1f, outer.Width);
        BuildColumnLayouts(initialWidth, renderFont);
        BuildDisplayEntries();
        UpdateScrollState();

        BuildColumnLayouts(Math.Max(1f, _bodyViewportWidth), renderFont);
        BuildDisplayEntries();
        UpdateScrollState();
        _geometryDirty = false;
    }

    private void BuildColumnLayouts(float availableWidth, SKFont font)
    {
        _columnLayouts.Clear();

        var resolvedWidths = new float[Columns.Count];
        var fillColumns = new List<int>();
        var fixedWidthTotal = 0f;
        var fillWeightTotal = 0f;

        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (!column.Visible)
                continue;

            if (column.SizeMode == GridListColumnSizeMode.Fill)
            {
                fillColumns.Add(i);
                fillWeightTotal += Math.Max(0.01f, column.FillWeight);
                continue;
            }

            var width = column.SizeMode == GridListColumnSizeMode.Auto
                ? MeasurePreferredColumnWidth(i, font, font)
                : column.Width;

            resolvedWidths[i] = Math.Clamp(width, column.MinWidth, column.MaxWidth);
            fixedWidthTotal += resolvedWidths[i];
        }

        if (fillColumns.Count > 0)
        {
            var remainingWidth = Math.Max(0f, availableWidth - fixedWidthTotal);
            for (var fillIndex = 0; fillIndex < fillColumns.Count; fillIndex++)
            {
                var columnIndex = fillColumns[fillIndex];
                var column = Columns[columnIndex];
                var share = remainingWidth * (column.FillWeight / fillWeightTotal);
                resolvedWidths[columnIndex] = Math.Clamp(Math.Max(column.MinWidth, share), column.MinWidth, column.MaxWidth);
            }
        }

        var x = 0f;
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (!column.Visible)
                continue;

            var width = resolvedWidths[i] > 0f ? resolvedWidths[i] : Math.Clamp(column.Width, column.MinWidth, column.MaxWidth);
            _columnLayouts.Add(new ColumnLayout(i, x, width));
            x += width;
        }

        _contentWidth = x;
    }

    private void BuildDisplayEntries()
    {
        _displayItemIndices.Clear();
        _layoutEntries.Clear();

        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Visible)
                _displayItemIndices.Add(i);
        }

        if (_sortColumnIndex >= 0 && _sortDirection != GridListSortDirection.None)
            _displayItemIndices.Sort(CompareDisplayItems);

        var y = 0f;
        if (HeaderVisible && !StickyHeader)
        {
            _layoutEntries.Add(LayoutEntry.Header(new SKRect(0, y, _contentWidth, y + HeaderHeight)));
            y += HeaderHeight;
        }

        string currentGroupKey = string.Empty;
        string currentGroupText = string.Empty;
        var currentGroupExpansion = 1f;
        var groupIndex = -1;

        for (var i = 0; i < _displayItemIndices.Count; i++)
        {
            var itemIndex = _displayItemIndices[i];
            var item = Items[itemIndex];

            if (GroupingEnabled)
            {
                var nextGroupKey = string.IsNullOrEmpty(item.GroupKey) ? string.Empty : item.GroupKey;
                var nextGroupText = string.IsNullOrEmpty(item.GroupText) ? nextGroupKey : item.GroupText;
                if (!string.Equals(currentGroupKey, nextGroupKey, StringComparison.Ordinal))
                {
                    currentGroupKey = nextGroupKey;
                    currentGroupText = nextGroupText;
                    groupIndex++;
                    _layoutEntries.Add(LayoutEntry.GroupHeader(new SKRect(0, y, _contentWidth, y + GroupHeaderHeight), currentGroupKey, currentGroupText, groupIndex));
                    y += GroupHeaderHeight;
                    currentGroupExpansion = GetGroupExpansionProgress(groupIndex);
                }

                if (currentGroupExpansion <= 0.001f)
                    continue;

                var itemHeight = Math.Max(0f, GetResolvedRowHeight(itemIndex) * currentGroupExpansion);
                _layoutEntries.Add(LayoutEntry.Item(new SKRect(0, y, _contentWidth, y + itemHeight), itemIndex));
                y += itemHeight;
                continue;
            }

            var rowHeight = GetResolvedRowHeight(itemIndex);
            _layoutEntries.Add(LayoutEntry.Item(new SKRect(0, y, _contentWidth, y + rowHeight), itemIndex));
            y += rowHeight;
        }

        _contentHeight = y;
    }

    private void UpdateScrollState()
    {
        var outer = GetOuterViewport();
        var showStickyHeader = HeaderVisible && StickyHeader;
        var availableWidth = Math.Max(1f, outer.Width);
        var availableHeight = Math.Max(1f, outer.Height - (showStickyHeader ? HeaderHeight : 0f));
        var overlayInset = MathF.Max(2f, 4f * ScaleFactor);

        var needsVScroll = _contentHeight > availableHeight;
        var needsHScroll = _contentWidth > availableWidth;

        _bodyViewportWidth = availableWidth;
        _bodyViewportHeight = availableHeight;

        if (_vScrollBar != null)
        {
            _vScrollBar.Visible = needsVScroll;
            if (needsVScroll)
            {
                _vScrollBar.Location = new SKPoint(Math.Max(0f, outer.Right - _vScrollBar.Thickness - overlayInset), showStickyHeader ? outer.Top + HeaderHeight + overlayInset : outer.Top + overlayInset);
                _vScrollBar.Size = new SKSize(_vScrollBar.Thickness, Math.Max(1f, availableHeight - overlayInset * 2 - (needsHScroll ? _hScrollBar.Thickness : 0) ));
                _vScrollBar.Minimum = 0;
                _vScrollBar.Maximum = Math.Max(0, _contentHeight - availableHeight);
                _vScrollBar.SmallChange = Math.Max(8f, RowHeight);
                _vScrollBar.LargeChange = Math.Max(RowHeight, availableHeight * 0.85f);
                if (_vScrollBar.Value > _vScrollBar.Maximum)
                    _vScrollBar.Value = _vScrollBar.Maximum;
                _verticalOffset = _vScrollBar.DisplayValue;
                _vScrollBar.BringToFront();
            }
            else
            {
                _vScrollBar.Value = 0;
                _verticalOffset = 0f;
            }
        }

        if (_hScrollBar != null)
        {
            _hScrollBar.Visible = needsHScroll;
            if (needsHScroll)
            {
                _hScrollBar.Location = new SKPoint(outer.Left + overlayInset, Math.Max(0f, outer.Bottom - _hScrollBar.Thickness - overlayInset));
                _hScrollBar.Size = new SKSize(Math.Max(1f, availableWidth - overlayInset * 2 - (needsVScroll ? _vScrollBar.Thickness : 0)), _hScrollBar.Thickness);
                _hScrollBar.Minimum = 0;
                _hScrollBar.Maximum = Math.Max(0, _contentWidth - availableWidth);
                _hScrollBar.SmallChange = Math.Max(8f, 32f * ScaleFactor);
                _hScrollBar.LargeChange = Math.Max(32f, availableWidth * 0.85f);
                if (_hScrollBar.Value > _hScrollBar.Maximum)
                    _hScrollBar.Value = _hScrollBar.Maximum;
                _horizontalOffset = _hScrollBar.DisplayValue;
                _hScrollBar.BringToFront();
            }
            else
            {
                _hScrollBar.Value = 0;
                _horizontalOffset = 0f;
            }
        }

        _vScrollBar?.SetHostHover(_vScrollBar.Visible && IsPointerOver);
        _hScrollBar?.SetHostHover(_hScrollBar.Visible && IsPointerOver);
    }

    private int CompareDisplayItems(int leftIndex, int rightIndex)
    {
        var leftCell = GetCell(leftIndex, _sortColumnIndex, createMissing: false);
        var rightCell = GetCell(rightIndex, _sortColumnIndex, createMissing: false);

        var compare = CompareCellValues(leftCell, rightCell);
        return _sortDirection == GridListSortDirection.Descending ? -compare : compare;
    }

    private static int CompareCellValues(GridListCell? leftCell, GridListCell? rightCell)
    {
        var leftValue = leftCell?.Value ?? leftCell?.Text ?? string.Empty;
        var rightValue = rightCell?.Value ?? rightCell?.Text ?? string.Empty;

        if (leftValue is IComparable comparable && rightValue != null && leftValue.GetType() == rightValue.GetType())
            return comparable.CompareTo(rightValue);

        return string.Compare(leftValue?.ToString(), rightValue?.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }

    private float GetResolvedRowHeight(int itemIndex)
    {
        if (ResizeAllRows)
            return RowHeight;

        if (itemIndex < 0 || itemIndex >= Items.Count)
            return RowHeight;

        var customHeight = Items[itemIndex].Height;
        return customHeight > 0.001f ? Math.Max(22f, customHeight) : RowHeight;
    }

    private void ResetRowSize(int itemIndex)
    {
        if (ResizeAllRows)
        {
            RowHeight = DefaultRowHeight;
            return;
        }

        if (itemIndex < 0 || itemIndex >= Items.Count)
            return;

        Items[itemIndex].Height = 0f;
    }

    private void AutoSizeColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Columns.Count)
            return;

        var column = Columns[columnIndex];
        using var renderFont = CreateRenderFont(Font);
        var preferredWidth = MeasurePreferredColumnWidth(columnIndex, renderFont, renderFont);
        column.Width = preferredWidth;
        column.SizeMode = GridListColumnSizeMode.Fixed;
        _geometryDirty = true;
        Invalidate();
    }

    private float MeasurePreferredColumnWidth(int columnIndex, SKFont headerFont, SKFont textFont)
    {
        var column = Columns[columnIndex];
        var maxWidth = CellPadding * 2f;

        if (column.HeaderIcon != null)
            maxWidth += IconSize + CellPadding * 0.75f;

        maxWidth += headerFont.MeasureText(column.HeaderText ?? string.Empty);
        maxWidth += 18f;
        if (AllowColumnResize && column.Resizable)
            maxWidth += ResizeGripWidth + 6f;

        for (var itemIndex = 0; itemIndex < Items.Count; itemIndex++)
        {
            var item = Items[itemIndex];
            if (!item.Visible)
                continue;

            var measuredWidth = CellPadding * 2f;
            if (ShouldShowCheckBox(column, columnIndex))
                measuredWidth += CheckBoxSize + CellPadding * 0.75f;

            var cell = GetCell(itemIndex, columnIndex, createMissing: false);
            var icon = column.ShowIcons ? cell?.Icon ?? item.Icon : null;
            if (icon != null)
                measuredWidth += IconSize + CellPadding * 0.75f;

            var text = cell?.Text;
            if (string.IsNullOrEmpty(text) && cell?.Value != null)
                text = cell.Value.ToString();

            if (!string.IsNullOrEmpty(text))
                measuredWidth += textFont.MeasureText(text);

            maxWidth = Math.Max(maxWidth, measuredWidth);
        }

        return Math.Clamp(maxWidth + 6f, column.MinWidth, column.MaxWidth);
    }

    private AnimationManager EnsureGroupAnimation(int groupIndex)
    {
        if (_groupAnimations.TryGetValue(groupIndex, out var animation))
            return animation;

        animation = new AnimationManager(true)
        {
            Increment = 0.18,
            AnimationType = AnimationType.CubicEaseOut,
            InterruptAnimation = true
        };
        animation.SetProgress(_collapsedGroups.TryGetValue(groupIndex, out var collapsed) && collapsed ? 0d : 1d);
        animation.OnAnimationProgress += _ =>
        {
            _geometryDirty = true;
            Invalidate();
        };
        animation.OnAnimationFinished += _ =>
        {
            _geometryDirty = true;
            Invalidate();
        };
        _groupAnimations[groupIndex] = animation;
        return animation;
    }

    private float GetGroupExpansionProgress(int groupIndex)
    {
        if (groupIndex < 0)
            return 1f;

        if (_groupAnimations.TryGetValue(groupIndex, out var animation))
            return Math.Clamp((float)animation.GetProgress(), 0f, 1f);

        return _collapsedGroups.TryGetValue(groupIndex, out var collapsed) && collapsed ? 0f : 1f;
    }

    private SKRect GetOuterViewport()
    {
        return new SKRect(Border.Left, Border.Top, Width - Border.Right, Height - Border.Bottom);
    }

    private SKRect GetStickyHeaderRect(SKRect outer)
    {
        return new SKRect(outer.Left, outer.Top, outer.Left + _bodyViewportWidth, outer.Top + HeaderHeight);
    }

    private SKRect GetBodyViewportRect(SKRect outer)
    {
        if (HeaderVisible && StickyHeader)
            return new SKRect(outer.Left, outer.Top + HeaderHeight, outer.Left + _bodyViewportWidth, outer.Top + HeaderHeight + _bodyViewportHeight);

        return new SKRect(outer.Left, outer.Top, outer.Left + _bodyViewportWidth, outer.Top + _bodyViewportHeight);
    }

    private bool TryGetColumnLayout(int columnIndex, out ColumnLayout columnLayout)
    {
        for (var i = 0; i < _columnLayouts.Count; i++)
        {
            if (_columnLayouts[i].ColumnIndex != columnIndex)
                continue;

            columnLayout = _columnLayouts[i];
            return true;
        }

        columnLayout = default;
        return false;
    }

    private void DrawHeader(SKCanvas canvas, SKRect bounds, float horizontalScroll, SKFont font)
    {
        _fillPaint.Color = HeaderBackColor;
        canvas.DrawRect(bounds, _fillPaint);

        for (var i = 0; i < _columnLayouts.Count; i++)
        {
            var layout = _columnLayouts[i];
            var cellRect = new SKRect(bounds.Left + layout.X - horizontalScroll, bounds.Top, bounds.Left + layout.X + layout.Width - horizontalScroll, bounds.Bottom);
            if (cellRect.Right < bounds.Left || cellRect.Left > bounds.Right)
                continue;

            var isHovered = _hoveredHeader && _hoveredColumnIndex == layout.ColumnIndex;
            if (isHovered)
            {
                _fillPaint.Color = HeaderBackColor.Brightness(0.05f);
                canvas.DrawRect(cellRect, _fillPaint);
            }

            var column = Columns[layout.ColumnIndex];
            var contentRect = cellRect;
            contentRect.Inflate(-CellPadding, 0);
            contentRect.Right -= AllowColumnResize && column.Resizable ? ResizeGripWidth + 4f : 0f;

            if (column.HeaderIcon != null)
            {
                var iconRect = new SKRect(contentRect.Left, contentRect.MidY - IconSize / 2f, contentRect.Left + IconSize, contentRect.MidY + IconSize / 2f);
                canvas.DrawImage(column.HeaderIcon, iconRect);
                contentRect.Left = iconRect.Right + CellPadding * 0.75f;
            }

            _textPaint.Color = HeaderForeColor;
            DrawControlText(canvas, column.HeaderText, contentRect, _textPaint, font, column.HeaderTextAlign, false, true);

            if (_sortColumnIndex == layout.ColumnIndex && _sortDirection != GridListSortDirection.None)
                DrawSortGlyph(canvas, cellRect, _sortDirection);

            if (AllowColumnResize && column.Resizable)
                DrawColumnResizeGrip(canvas, cellRect, isHovered, _hoveredHeaderResizeColumnIndex == layout.ColumnIndex || _resizingColumnIndex == layout.ColumnIndex);

            if (ShowGridLines)
            {
                canvas.DrawLine(cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom, _gridLinePaint);
            }
        }

        if (ShowGridLines)
        {
            canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, _gridLinePaint);
        }
    }

    private void DrawSortGlyph(SKCanvas canvas, SKRect cellRect, GridListSortDirection direction)
    {
        var centerX = cellRect.Right - CellPadding - 8f;
        var centerY = cellRect.MidY;
        DrawChevronGlyph(
            canvas,
            new SKPoint(centerX, centerY),
            HeaderForeColor.WithAlpha(200),
            1.5f,
            4f,
            direction == GridListSortDirection.Ascending ? 180f : 0f);
    }

    private void DrawColumnResizeGrip(SKCanvas canvas, SKRect cellRect, bool emphasized, bool resizeHot)
    {
        var gripColor = resizeHot ? ColorScheme.Primary : HeaderForeColor.WithAlpha(110);
        using var gripPaint = new SKPaint
        {
            Color = gripColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = resizeHot ? 2f : 1f,
            StrokeCap = SKStrokeCap.Round
        };

        var x = cellRect.Right - gripPaint.StrokeWidth / 2f;
        var y1 = cellRect.Top + 8f;
        var y2 = cellRect.Bottom - 8f;

        canvas.DrawLine(x, y1, x, y2, gripPaint);
    }

    private void DrawChevronGlyph(SKCanvas canvas, SKPoint center, SKColor color, float strokeWidth, float size, float rotationDegrees)
    {
        using var chevronPaint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        using var chevronPath = new SKPath();
        chevronPath.MoveTo(-size, -size * 0.5f);
        chevronPath.LineTo(0f, size * 0.5f);
        chevronPath.LineTo(size, -size * 0.5f);

        var saveCount = canvas.Save();
        canvas.Translate(center.X, center.Y);
        canvas.RotateDegrees(rotationDegrees);
        canvas.DrawPath(chevronPath, chevronPaint);
        canvas.RestoreToCount(saveCount);
    }

    private void DrawGroupHeader(SKCanvas canvas, SKRect bounds, string text, string groupKey, int groupIndex, SKFont font)
    {
        var expansion = GetGroupExpansionProgress(groupIndex);
        var isHovered = _hoveredGroupIndex >= 0 && _hoveredGroupIndex == groupIndex;
        _fillPaint.Color = isHovered ? GroupHeaderBackColor.Brightness(0.04f) : GroupHeaderBackColor;
        canvas.DrawRect(bounds, _fillPaint);

        using var accentPaint = new SKPaint { Color = GroupHeaderBackColor.Brightness(0.12f).WithAlpha(180), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var accentBorder = new SKPaint { Color = ForeColor.WithAlpha(28), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        var accentRect = new SKRect(bounds.Left + CellPadding, bounds.Top + 5f, bounds.Left + CellPadding + 22f, bounds.Bottom - 5f);
        canvas.DrawRoundRect(accentRect, 10f, 10f, accentPaint);
        canvas.DrawRoundRect(accentRect, 10f, 10f, accentBorder);

        var chevronCenter = new SKPoint(accentRect.MidX, accentRect.MidY);
        DrawChevronGlyph(canvas, chevronCenter, ForeColor.WithAlpha(220), 1.8f, 3.6f, -90f + expansion * 90f);

        var textRect = new SKRect(accentRect.Right + CellPadding, bounds.Top, bounds.Right - CellPadding, bounds.Bottom);
        _textPaint.Color = ForeColor;
        DrawControlText(canvas, text, textRect, _textPaint, font, ContentAlignment.MiddleLeft, false, true);

        if (ShowGridLines)
        {
            canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, _gridLinePaint);
        }
    }

    private void DrawItemRow(SKCanvas canvas, SKRect bounds, int itemIndex, SKFont font)
    {
        if (bounds.Height <= 0.5f)
            return;

        var item = Items[itemIndex];
        var isSelected = _selectedIndices.Contains(itemIndex);
        var isHovered = _hoveredItemIndex == itemIndex;
        var baseRowHeight = GetResolvedRowHeight(itemIndex);
        var revealProgress = baseRowHeight <= 0.001f ? 1f : Math.Clamp(bounds.Height / baseRowHeight, 0f, 1f);
        var saveCount = canvas.Save();
        canvas.ClipRect(bounds);

        if (isSelected)
        {
            _fillPaint.Color = WithOpacity(SelectionBackColor, revealProgress);
            canvas.DrawRect(bounds, _fillPaint);
        }
        else if (isHovered)
        {
            _fillPaint.Color = WithOpacity(HoverRowBackColor, revealProgress);
            canvas.DrawRect(bounds, _fillPaint);
        }
        else if ((itemIndex & 1) == 1)
        {
            _fillPaint.Color = WithOpacity(AlternatingRowBackColor, revealProgress);
            canvas.DrawRect(bounds, _fillPaint);
        }

        for (var i = 0; i < _columnLayouts.Count; i++)
        {
            var layout = _columnLayouts[i];
            var column = Columns[layout.ColumnIndex];
            var cellRect = new SKRect(bounds.Left + layout.X - _horizontalOffset, bounds.Top, bounds.Left + layout.X + layout.Width - _horizontalOffset, bounds.Bottom);
            if (cellRect.Right < bounds.Left || cellRect.Left > bounds.Right)
                continue;

            var contentRect = new SKRect(cellRect.Left + CellPadding, cellRect.Top, cellRect.Right - CellPadding, cellRect.Bottom);
            var cell = GetCell(itemIndex, layout.ColumnIndex, createMissing: false);

            if (ShouldShowCheckBox(column, layout.ColumnIndex))
            {
                var checkboxRect = GetCheckBoxRect(contentRect);
                DrawCheckBox(canvas, checkboxRect, cell?.CheckState ?? CheckState.Unchecked, isSelected);
                contentRect.Left = checkboxRect.Right + CellPadding * 0.75f;
            }

            var icon = column.ShowIcons ? cell?.Icon ?? item.Icon : null;
            if (icon != null)
            {
                var iconRect = new SKRect(contentRect.Left, contentRect.MidY - IconSize / 2f, contentRect.Left + IconSize, contentRect.MidY + IconSize / 2f);
                canvas.DrawImage(icon, iconRect);
                contentRect.Left = iconRect.Right + CellPadding * 0.75f;
            }

            var text = cell?.Text;
            if (string.IsNullOrEmpty(text) && cell?.Value != null)
                text = cell.Value.ToString();

            if (!string.IsNullOrEmpty(text))
            {
                var foreColor = cell != null && cell.ForeColor != SKColor.Empty ? cell.ForeColor : (item.Enabled ? ForeColor : ForeColor.WithAlpha(140));
                _textPaint.Color = WithOpacity(foreColor, revealProgress);
                DrawControlText(canvas, text, contentRect, _textPaint, font, column.CellTextAlign, true, false);
            }

            if (ShowGridLines)
            {
                canvas.DrawLine(cellRect.Right, cellRect.Top, cellRect.Right, cellRect.Bottom, _gridLinePaint);
            }
        }

        if (AllowRowResize && (_hoveredRowResizeIndex == itemIndex || _isResizingRow && _resizingRowIndex == itemIndex))
        {
            using var resizePaint = new SKPaint
            {
                Color = ColorScheme.Primary.WithAlpha(160),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.4f,
                StrokeCap = SKStrokeCap.Round
            };
            var y = bounds.Bottom - 1f;
            canvas.DrawLine(bounds.Left + 12f, y, bounds.Right - 12f, y, resizePaint);
        }

        if (ShowGridLines)
            canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, _gridLinePaint);

        canvas.RestoreToCount(saveCount);
    }

    private static SKColor WithOpacity(SKColor color, float opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(color.Alpha * opacity), 0d, 255d);
        return color.WithAlpha(alpha);
    }

    private static SKRect GetCheckBoxRect(SKRect contentRect)
    {
        return new SKRect(contentRect.Left, contentRect.MidY - CheckBoxSize / 2f, contentRect.Left + CheckBoxSize, contentRect.MidY + CheckBoxSize / 2f);
    }

    private void DrawCheckBox(SKCanvas canvas, SKRect rect, CheckState state, bool isSelected)
    {
        using var checkBack = new SKPaint
        {
            Color = state == CheckState.Unchecked ? ColorScheme.Surface : ColorScheme.Primary.WithAlpha(isSelected ? (byte)220 : (byte)180),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var checkBorder = new SKPaint
        {
            Color = state == CheckState.Unchecked ? ColorScheme.BorderColor : ColorScheme.Primary,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f
        };

        canvas.DrawRoundRect(rect, 4f, 4f, checkBack);
        canvas.DrawRoundRect(rect, 4f, 4f, checkBorder);

        if (state == CheckState.Unchecked)
            return;

        using var checkPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        if (state == CheckState.Checked)
        {
            using var path = new SKPath();
            path.MoveTo(rect.Left + 3, rect.MidY);
            path.LineTo(rect.Left + 7, rect.Bottom - 4);
            path.LineTo(rect.Right - 3, rect.Top + 4);
            canvas.DrawPath(path, checkPaint);
        }
        else
        {
            canvas.DrawLine(rect.Left + 3, rect.MidY, rect.Right - 3, rect.MidY, checkPaint);
        }
    }

    private bool ShouldShowCheckBox(GridListColumn column, int columnIndex)
    {
        return column.ShowCheckBox || (CheckBoxes && FindFirstVisibleColumnIndex() == columnIndex);
    }

    private int FindFirstVisibleColumnIndex()
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].Visible)
                return i;
        }

        return -1;
    }

    private int FindFirstCheckBoxColumn()
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].Visible && ShouldShowCheckBox(Columns[i], i))
                return i;
        }

        return -1;
    }

    private GridListColumn? GetColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < Columns.Count ? Columns[columnIndex] : null;
    }

    private GridListCell? GetCell(int itemIndex, int columnIndex, bool createMissing)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count || columnIndex < 0 || columnIndex >= Columns.Count)
            return null;

        var item = Items[itemIndex];
        while (createMissing && item.Cells.Count <= columnIndex)
            item.Cells.Add(new GridListCell());

        return columnIndex < item.Cells.Count ? item.Cells[columnIndex] : null;
    }

    private HitInfo HitTest(SKPoint location)
    {
        EnsureLayoutState();

        var outer = GetOuterViewport();
        var stickyHeaderRect = HeaderVisible && StickyHeader ? GetStickyHeaderRect(outer) : SKRect.Empty;
        if (!stickyHeaderRect.IsEmpty && stickyHeaderRect.Contains(location))
            return HitTestHeader(location, stickyHeaderRect, horizontalOffset: _horizontalOffset);

        var bodyViewport = GetBodyViewportRect(outer);
        if (!bodyViewport.Contains(location))
            return HitInfo.None;

        var contentX = location.X - bodyViewport.Left + _horizontalOffset;
        var contentY = location.Y - bodyViewport.Top + _verticalOffset;

        for (var i = 0; i < _layoutEntries.Count; i++)
        {
            var entry = _layoutEntries[i];
            if (contentY < entry.Bounds.Top || contentY > entry.Bounds.Bottom)
                continue;

            if (entry.Kind == EntryKind.Header)
                return HitTestHeader(location, new SKRect(bodyViewport.Left, bodyViewport.Top + entry.Bounds.Top - _verticalOffset, bodyViewport.Left + _bodyViewportWidth, bodyViewport.Top + entry.Bounds.Bottom - _verticalOffset), _horizontalOffset);

            if (entry.Kind == EntryKind.GroupHeader)
                return HitInfo.ForGroup(entry.GroupKey, entry.GroupText, entry.GroupIndex);

            if (entry.Kind == EntryKind.Item)
                return HitTestItemCell(location, contentX, contentY, entry);
        }

        return HitInfo.None;
    }

    private HitInfo HitTestHeader(SKPoint location, SKRect headerRect, float horizontalOffset)
    {
        if (AllowColumnResize)
        {
            var hitAreaWidth = Math.Max(ResizeGripWidth + 8f, ResizeHitWidth * ScaleFactor);
            var halfHitArea = hitAreaWidth * 0.5f;

            for (var i = 0; i < _columnLayouts.Count; i++)
            {
                var layout = _columnLayouts[i];
                if (!Columns[layout.ColumnIndex].Resizable)
                    continue;

                var cellRect = new SKRect(headerRect.Left + layout.X - horizontalOffset, headerRect.Top, headerRect.Left + layout.X + layout.Width - horizontalOffset, headerRect.Bottom);
                var resizeRect = new SKRect(
                    Math.Max(headerRect.Left, cellRect.Right - halfHitArea),
                    cellRect.Top,
                    Math.Min(headerRect.Right, cellRect.Right + halfHitArea),
                    cellRect.Bottom);

                if (resizeRect.Contains(location))
                    return HitInfo.ForHeaderResize(layout.ColumnIndex);
            }
        }

        for (var i = 0; i < _columnLayouts.Count; i++)
        {
            var layout = _columnLayouts[i];
            var cellRect = new SKRect(headerRect.Left + layout.X - horizontalOffset, headerRect.Top, headerRect.Left + layout.X + layout.Width - horizontalOffset, headerRect.Bottom);
            if (!cellRect.Contains(location))
                continue;

            return HitInfo.ForHeader(layout.ColumnIndex);
        }

        return HitInfo.None;
    }

    private HitInfo HitTestItemCell(SKPoint location, float contentX, float contentY, LayoutEntry entry)
    {
        if (AllowRowResize && entry.Bounds.Height >= GetResolvedRowHeight(entry.ItemIndex) * 0.65f && Math.Abs(contentY - entry.Bounds.Bottom) <= RowResizeGripHeight)
            return HitInfo.ForRowResize(entry.ItemIndex);

        for (var i = 0; i < _columnLayouts.Count; i++)
        {
            var layout = _columnLayouts[i];
            if (contentX < layout.X || contentX > layout.X + layout.Width)
                continue;

            var checkRect = SKRect.Empty;
            var column = Columns[layout.ColumnIndex];
            if (ShouldShowCheckBox(column, layout.ColumnIndex))
            {
                var outer = GetOuterViewport();
                var bodyViewport = GetBodyViewportRect(outer);
                var drawRect = entry.Bounds;
                drawRect.Offset(bodyViewport.Left - _horizontalOffset, bodyViewport.Top - _verticalOffset);
                var cellRect = new SKRect(drawRect.Left + layout.X, drawRect.Top, drawRect.Left + layout.X + layout.Width, drawRect.Bottom);
                var contentRect = new SKRect(cellRect.Left + CellPadding, cellRect.Top, cellRect.Right - CellPadding, cellRect.Bottom);
                checkRect = GetCheckBoxRect(contentRect);
            }

            return HitInfo.ForItem(entry.ItemIndex, layout.ColumnIndex, checkRect);
        }

        return HitInfo.None;
    }

    private readonly record struct ColumnLayout(int ColumnIndex, float X, float Width);

    private enum EntryKind
    {
        Header,
        GroupHeader,
        Item
    }

    private readonly record struct LayoutEntry(EntryKind Kind, SKRect Bounds, int ItemIndex, string? GroupKey, string? GroupText, int GroupIndex)
    {
        public static LayoutEntry Header(SKRect bounds) => new(EntryKind.Header, bounds, -1, null, null, -1);
        public static LayoutEntry GroupHeader(SKRect bounds, string? groupKey, string? groupText, int groupIndex) => new(EntryKind.GroupHeader, bounds, -1, groupKey, groupText, groupIndex);
        public static LayoutEntry Item(SKRect bounds, int itemIndex) => new(EntryKind.Item, bounds, itemIndex, null, null, -1);
    }

    private enum HitKind
    {
        None,
        Header,
        HeaderResize,
        RowResize,
        GroupHeader,
        ItemCell
    }

    private readonly record struct HitInfo(HitKind Kind, int ItemIndex, int ColumnIndex, string? GroupKey, string? GroupText, int GroupIndex, SKRect CheckBoxRect)
    {
        public static HitInfo None => new(HitKind.None, -1, -1, null, null, -1, SKRect.Empty);
        public static HitInfo ForHeader(int columnIndex) => new(HitKind.Header, -1, columnIndex, null, null, -1, SKRect.Empty);
        public static HitInfo ForHeaderResize(int columnIndex) => new(HitKind.HeaderResize, -1, columnIndex, null, null, -1, SKRect.Empty);
        public static HitInfo ForRowResize(int itemIndex) => new(HitKind.RowResize, itemIndex, -1, null, null, -1, SKRect.Empty);
        public static HitInfo ForGroup(string? groupKey, string? groupText, int groupIndex) => new(HitKind.GroupHeader, -1, -1, groupKey, groupText, groupIndex, SKRect.Empty);
        public static HitInfo ForItem(int itemIndex, int columnIndex, SKRect checkBoxRect) => new(HitKind.ItemCell, itemIndex, columnIndex, null, null, -1, checkBoxRect);
    }
}
