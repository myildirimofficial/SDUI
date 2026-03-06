using SkiaSharp;
using System;
using System.Diagnostics;

using System.Windows.Forms;

namespace SDUI.Controls;

public class WindowPageControl : ElementBase
{
    private EventHandler<int> _onSelectedIndexChanged;

    private int _selectedIndex = -1;

    public WindowPageControl()
    {
        BackColor = SKColors.Transparent;
    }

    private bool IsPageControl(ElementBase element)
    {
        return element is not ScrollBar;
    }

    private int GetPageCount()
    {
        var count = 0;
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is ElementBase element && IsPageControl(element))
                count++;
        }

        return count;
    }

    public ElementBase? GetPageAt(int pageIndex)
    {
        if (pageIndex < 0)
            return null;

        var currentPageIndex = 0;
        for (var i = 0; i < Controls.Count; i++)
        {
            if (Controls[i] is not ElementBase element || !IsPageControl(element))
                continue;

            if (currentPageIndex == pageIndex)
                return element;

            currentPageIndex++;
        }

        return null;
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var sys = Stopwatch.StartNew();

            if (_selectedIndex == value)
                return;

            var pageCount = GetPageCount();
            if (pageCount > 0)
            {
                if (value < 0)
                    value = pageCount - 1;

                if (value > pageCount - 1)
                    value = 0;
            }
            else
            {
                value = -1;
            }

            var previousSelectedIndex = _selectedIndex;
            _selectedIndex = value;
            _onSelectedIndexChanged?.Invoke(this, previousSelectedIndex);

            var currentPageIndex = 0;
            for (var i = 0; i < Controls.Count; i++)
            {
                if (Controls[i] is not ElementBase element || !IsPageControl(element))
                    continue;

                element.Visible = currentPageIndex == _selectedIndex;
                currentPageIndex++;
            }

            Debug.WriteLine($"Index: {_selectedIndex} Finished: {sys.ElapsedMilliseconds} ms");
        }
    }

    public int Count => GetPageCount();

    public event EventHandler<int> SelectedIndexChanged
    {
        add => _onSelectedIndexChanged += value;
        remove => _onSelectedIndexChanged -= value;
    }

    internal override void OnControlAdded(ElementEventArgs e)
    {
        base.OnControlAdded(e);

        if (e.Element is not ElementBase element || !IsPageControl(element))
            return;

        element.Dock = DockStyle.Fill;
        element.BackColor = SKColors.Transparent;
        element.Visible = Count == 1;

        if (Count == 1)
            _selectedIndex = 0;
    }

    internal override void OnControlRemoved(ElementEventArgs e)
    {
        base.OnControlRemoved(e);

        if (e.Element is not ElementBase element || !IsPageControl(element))
            return;

        if (Count == 0)
            _selectedIndex = -1;
        else if (_selectedIndex >= Count)
            SelectedIndex = Count - 1;
    }
}