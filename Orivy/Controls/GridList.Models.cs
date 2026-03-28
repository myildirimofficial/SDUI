using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Orivy.Controls;

public enum GridListSortDirection
{
    None,
    Ascending,
    Descending
}

public enum GridListColumnSizeMode
{
    Fixed,
    Auto,
    Fill
}

public sealed class GridListSelectionChangedEventArgs : EventArgs
{
    public GridListSelectionChangedEventArgs(int previousSelectedIndex, int selectedIndex)
    {
        PreviousSelectedIndex = previousSelectedIndex;
        SelectedIndex = selectedIndex;
    }

    public int PreviousSelectedIndex { get; }
    public int SelectedIndex { get; }
}

public sealed class GridListColumnClickEventArgs : EventArgs
{
    public GridListColumnClickEventArgs(GridListColumn column, int columnIndex, GridListSortDirection sortDirection)
    {
        Column = column;
        ColumnIndex = columnIndex;
        SortDirection = sortDirection;
    }

    public GridListColumn Column { get; }
    public int ColumnIndex { get; }
    public GridListSortDirection SortDirection { get; }
}

public class GridListCellEventArgs : EventArgs
{
    public GridListCellEventArgs(GridListItem item, GridListColumn column, GridListCell cell, int itemIndex, int columnIndex)
    {
        Item = item;
        Column = column;
        Cell = cell;
        ItemIndex = itemIndex;
        ColumnIndex = columnIndex;
    }

    public GridListItem Item { get; }
    public GridListColumn Column { get; }
    public GridListCell Cell { get; }
    public int ItemIndex { get; }
    public int ColumnIndex { get; }
}

public sealed class GridListCellCheckChangedEventArgs : GridListCellEventArgs
{
    public GridListCellCheckChangedEventArgs(
        GridListItem item,
        GridListColumn column,
        GridListCell cell,
        int itemIndex,
        int columnIndex,
        CheckState previousState,
        CheckState currentState)
        : base(item, column, cell, itemIndex, columnIndex)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    public CheckState PreviousState { get; }
    public CheckState CurrentState { get; }
}

public sealed class GridListColumn
{
    private ContentAlignment _headerTextAlign = ContentAlignment.MiddleLeft;
    private ContentAlignment _cellTextAlign = ContentAlignment.MiddleLeft;
    private float _fillWeight = 1f;
    private string _headerText = string.Empty;
    private float _maxWidth = 1200f;
    private float _minWidth = 56f;
    private string _name = string.Empty;
    private bool _resizable = true;
    private GridListColumnSizeMode _sizeMode;
    private bool _sortable = true;
    private bool _visible = true;
    private float _width = 160f;

    internal GridList? Owner { get; private set; }

    [DefaultValue("")]
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;

            _name = value ?? string.Empty;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    [DefaultValue("")]
    public string HeaderText
    {
        get => _headerText;
        set
        {
            if (_headerText == value)
                return;

            _headerText = value ?? string.Empty;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    [DefaultValue(160f)]
    public float Width
    {
        get => _width;
        set
        {
            var clamped = Math.Clamp(value, MinWidth, MaxWidth);
            if (Math.Abs(_width - clamped) < 0.001f)
                return;

            _width = clamped;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(56f)]
    public float MinWidth
    {
        get => _minWidth;
        set
        {
            var clamped = Math.Max(24f, value);
            if (Math.Abs(_minWidth - clamped) < 0.001f)
                return;

            _minWidth = clamped;
            if (_maxWidth < _minWidth)
                _maxWidth = _minWidth;
            if (_width < _minWidth)
                _width = _minWidth;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(1200f)]
    public float MaxWidth
    {
        get => _maxWidth;
        set
        {
            var clamped = Math.Max(MinWidth, value);
            if (Math.Abs(_maxWidth - clamped) < 0.001f)
                return;

            _maxWidth = clamped;
            if (_width > _maxWidth)
                _width = _maxWidth;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(true)]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;

            _visible = value;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(true)]
    public bool Sortable
    {
        get => _sortable;
        set
        {
            if (_sortable == value)
                return;

            _sortable = value;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    [DefaultValue(true)]
    public bool Resizable
    {
        get => _resizable;
        set
        {
            if (_resizable == value)
                return;

            _resizable = value;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    [DefaultValue(GridListColumnSizeMode.Fixed)]
    public GridListColumnSizeMode SizeMode
    {
        get => _sizeMode;
        set
        {
            if (_sizeMode == value)
                return;

            _sizeMode = value;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(1f)]
    public float FillWeight
    {
        get => _fillWeight;
        set
        {
            var clamped = Math.Max(0.01f, value);
            if (Math.Abs(_fillWeight - clamped) < 0.001f)
                return;

            _fillWeight = clamped;
            Owner?.OnColumnsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(false)]
    public bool ShowCheckBox { get; set; }

    [DefaultValue(true)]
    public bool ShowIcons { get; set; } = true;

    [DefaultValue(ContentAlignment.MiddleLeft)]
    public ContentAlignment HeaderTextAlign
    {
        get => _headerTextAlign;
        set
        {
            if (_headerTextAlign == value)
                return;

            _headerTextAlign = value;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    [DefaultValue(ContentAlignment.MiddleLeft)]
    public ContentAlignment CellTextAlign
    {
        get => _cellTextAlign;
        set
        {
            if (_cellTextAlign == value)
                return;

            _cellTextAlign = value;
            Owner?.OnColumnsChanged(layoutAffected: false);
        }
    }

    public SKImage? HeaderIcon { get; set; }

    internal void AttachOwner(GridList owner)
    {
        Owner = owner;
    }
}

public sealed class GridListCell
{
    private CheckState _checkState;
    private string _text = string.Empty;
    private object? _value;

    internal GridListItem? ParentItem { get; private set; }

    public object? Value
    {
        get => _value;
        set
        {
            if (ReferenceEquals(_value, value))
                return;

            _value = value;
            ParentItem?.NotifyCellChanged(layoutAffected: false);
        }
    }

    [DefaultValue("")]
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;

            _text = value ?? string.Empty;
            ParentItem?.NotifyCellChanged(layoutAffected: false);
        }
    }

    [DefaultValue(typeof(CheckState), "Unchecked")]
    public CheckState CheckState
    {
        get => _checkState;
        set
        {
            if (_checkState == value)
                return;

            _checkState = value;
            ParentItem?.NotifyCellChanged(layoutAffected: false);
        }
    }

    [DefaultValue(false)]
    public bool Checked
    {
        get => _checkState == CheckState.Checked;
        set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
    }

    public SKImage? Icon { get; set; }

    public SKColor ForeColor { get; set; } = SKColor.Empty;

    internal void AttachParent(GridListItem parent)
    {
        ParentItem = parent;
    }
}

public sealed class GridListItem
{
    private bool _enabled = true;
    private string _groupKey = string.Empty;
    private string _groupText = string.Empty;
    private float _height;
    private bool _visible = true;

    public GridListItem()
    {
        Cells = new GridListCellCollection(this);
    }

    internal GridList? Owner { get; private set; }

    public GridListCellCollection Cells { get; }

    public object? Tag { get; set; }

    [DefaultValue(true)]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
                return;

            _visible = value;
            Owner?.OnItemsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(true)]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;

            _enabled = value;
            Owner?.OnItemsChanged(layoutAffected: false);
        }
    }

    [DefaultValue("")]
    public string GroupKey
    {
        get => _groupKey;
        set
        {
            if (_groupKey == value)
                return;

            _groupKey = value ?? string.Empty;
            Owner?.OnItemsChanged(layoutAffected: true);
        }
    }

    [DefaultValue("")]
    public string GroupText
    {
        get => _groupText;
        set
        {
            if (_groupText == value)
                return;

            _groupText = value ?? string.Empty;
            Owner?.OnItemsChanged(layoutAffected: true);
        }
    }

    [DefaultValue(0f)]
    public float Height
    {
        get => _height;
        set
        {
            var clamped = Math.Max(0f, value);
            if (Math.Abs(_height - clamped) < 0.001f)
                return;

            _height = clamped;
            Owner?.OnItemsChanged(layoutAffected: true);
        }
    }

    public SKImage? Icon { get; set; }

    internal void AttachOwner(GridList owner)
    {
        Owner = owner;
        for (var i = 0; i < Cells.Count; i++)
            Cells[i].AttachParent(this);
    }

    internal void NotifyCellChanged(bool layoutAffected)
    {
        Owner?.OnItemsChanged(layoutAffected);
    }
}

public sealed class GridListColumnCollection : Collection<GridListColumn>
{
    private readonly GridList _owner;

    internal GridListColumnCollection(GridList owner)
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, GridListColumn item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachOwner(_owner);
        base.InsertItem(index, item);
        _owner.OnColumnsChanged(layoutAffected: true);
    }

    protected override void SetItem(int index, GridListColumn item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachOwner(_owner);
        base.SetItem(index, item);
        _owner.OnColumnsChanged(layoutAffected: true);
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _owner.OnColumnsChanged(layoutAffected: true);
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        _owner.OnColumnsChanged(layoutAffected: true);
    }
}

public sealed class GridListItemCollection : Collection<GridListItem>
{
    private readonly GridList _owner;

    internal GridListItemCollection(GridList owner)
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, GridListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachOwner(_owner);
        base.InsertItem(index, item);
        _owner.OnItemsChanged(layoutAffected: true);
    }

    protected override void SetItem(int index, GridListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachOwner(_owner);
        base.SetItem(index, item);
        _owner.OnItemsChanged(layoutAffected: true);
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _owner.OnItemsChanged(layoutAffected: true);
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        _owner.ClearSelection();
        _owner.OnItemsChanged(layoutAffected: true);
    }
}

public sealed class GridListCellCollection : Collection<GridListCell>
{
    private readonly GridListItem _owner;

    internal GridListCellCollection(GridListItem owner)
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, GridListCell item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachParent(_owner);
        base.InsertItem(index, item);
        _owner.NotifyCellChanged(layoutAffected: false);
    }

    protected override void SetItem(int index, GridListCell item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.AttachParent(_owner);
        base.SetItem(index, item);
        _owner.NotifyCellChanged(layoutAffected: false);
    }

    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        _owner.NotifyCellChanged(layoutAffected: false);
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        _owner.NotifyCellChanged(layoutAffected: false);
    }
}