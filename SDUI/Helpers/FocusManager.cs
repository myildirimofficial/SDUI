using System;
using System.Collections.Generic;
using System.Linq;
using SDUI.Controls;
using SkiaSharp;

namespace SDUI.Helpers;

/// <summary>
///     Modern focus management system with proper keyboard navigation
/// </summary>
public class FocusManager
{
    private readonly List<ElementBase> _focusableElements = new();
    private readonly WindowBase _window;
    private ElementBase? _currentFocus;
    private bool _isNavigating;

    public FocusManager(WindowBase window)
    {
        _window = window;
    }

    /// <summary>
    ///     Current focused element
    /// </summary>
    public ElementBase? FocusedElement
    {
        get => _currentFocus;
        private set
        {
            if (_currentFocus == value) return;

            var oldFocus = _currentFocus;
            _currentFocus = value;
            FocusChanged?.Invoke(this, new FocusChangedEventArgs(oldFocus, _currentFocus));
        }
    }

    public event EventHandler<FocusChangedEventArgs>? FocusChanged;

    /// <summary>
    ///     Refreshes the list of focusable elements
    /// </summary>
    public void RefreshFocusableElements()
    {
        _focusableElements.Clear();

        // Collect from window's controls
        foreach (var control in _window.Controls.OfType<ElementBase>()) CollectFromElement(control);

        _focusableElements.Sort((a, b) =>
        {
            var tabCompare = a.TabIndex.CompareTo(b.TabIndex);
            if (tabCompare != 0) return tabCompare;

            // Deterministic tiebreaker when many controls share the default TabIndex (0).
            // Use window-relative location (top-to-bottom, left-to-right), then ZOrder.
            var aLoc = _window.PointToClient(a.PointToScreen(SKPoint.Empty));
            var bLoc = _window.PointToClient(b.PointToScreen(SKPoint.Empty));

            var yCompare = aLoc.Y.CompareTo(bLoc.Y);
            if (yCompare != 0) return yCompare;

            var xCompare = aLoc.X.CompareTo(bLoc.X);
            if (xCompare != 0) return xCompare;

            return a.ZOrder.CompareTo(b.ZOrder);
        });
    }

    private void CollectFromElement(ElementBase element)
    {
        if (element.Visible && element.Enabled && element.TabStop && element.CanSelect) _focusableElements.Add(element);

        // Recursive for nested containers
        if (element is IElement uiElement)
            foreach (var child in uiElement.Controls.OfType<ElementBase>())
                CollectFromElement(child);
    }

    /// <summary>
    ///     Sets focus to specific element
    /// </summary>
    public bool SetFocus(ElementBase? element)
    {
        // Ensure the host window gets WinForms focus so key events flow.
        if (_window.CanFocus)
            _window.Focus();

        if (element != null && (!element.Visible || !element.Enabled || !element.CanSelect))
            return false;

        // Delegate real focus behavior to the existing focus pipeline so
        // OnGotFocus/OnLostFocus and related events fire correctly.
        if (_window is WindowBase uiWindow)
        {
            uiWindow.FocusedElement = element;
        }
        else
        {
            if (element == null)
            {
                if (_currentFocus != null)
                {
                    _currentFocus.Focused = false;
                    _currentFocus.OnLostFocus(EventArgs.Empty);
                    _currentFocus.OnLeave(EventArgs.Empty);
                    _currentFocus.Invalidate();
                }
            }
            else
            {
                element.Focus();
            }
        }

        FocusedElement = element;
        if (element != null)
            EnsureVisible(element);
        return true;
    }

    /// <summary>
    ///     Navigate to next focusable element
    /// </summary>
    public bool FocusNext(bool reverse = false)
    {
        if (_isNavigating) return false;
        _isNavigating = true;

        try
        {
            RefreshFocusableElements();
            if (_focusableElements.Count == 0) return false;

            var currentIndex = _currentFocus != null
                ? _focusableElements.IndexOf(_currentFocus)
                : -1;

            int nextIndex;
            if (reverse)
                nextIndex = currentIndex > 0
                    ? currentIndex - 1
                    : _focusableElements.Count - 1;
            else
                nextIndex = currentIndex < _focusableElements.Count - 1
                    ? currentIndex + 1
                    : 0;

            return SetFocus(_focusableElements[nextIndex]);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    /// <summary>
    ///     Focus first element
    /// </summary>
    public bool FocusFirst()
    {
        RefreshFocusableElements();
        return _focusableElements.Count > 0 && SetFocus(_focusableElements[0]);
    }

    /// <summary>
    ///     Focus last element
    /// </summary>
    public bool FocusLast()
    {
        RefreshFocusableElements();
        return _focusableElements.Count > 0 && SetFocus(_focusableElements[^1]);
    }

    /// <summary>
    ///     Handle keyboard navigation
    /// </summary>
    public bool ProcessKeyNavigation(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Tab:
                var handled = FocusNext(e.Shift);
                e.Handled = handled;
                return handled;

            case Keys.Home when e.Control:
                e.Handled = FocusFirst();
                return e.Handled;

            case Keys.End when e.Control:
                e.Handled = FocusLast();
                return e.Handled;

            case Keys.Escape:
                if (_currentFocus != null)
                {
                    SetFocus(null);
                    e.Handled = true;
                    return true;
                }

                break;
        }

        return false;
    }

    /// <summary>
    ///     Ensures focused element is visible by scrolling containers
    /// </summary>
    private void EnsureVisible(ElementBase element)
    {
        // Find scrollable parent and scroll to make element visible
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is ElementBase parentElement && parentElement.AutoScroll)
            {
                // Calculate if element is outside visible bounds
                var elementBounds = element.Bounds;
                var parentBounds = parentElement.ClientRectangle;

                // Scroll logic would go here
                // This is a placeholder for future scroll implementation
            }

            parent = parent is ElementBase pe ? pe.Parent : null;
        }
    }

    /// <summary>
    ///     Draw focus indicator for the current element
    /// </summary>
    public void DrawFocusIndicator(SKCanvas canvas, SkiaSharp.SKRect bounds, float cornerRadius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ColorScheme.Primary,
            IsStroke = true,
            StrokeWidth = 2f,
            PathEffect = SKPathEffect.CreateDash(new[] { 4f, 2f }, 0)
        };

        var rect = new SkiaSharp.SKRect(
            bounds.Left - 2,
            bounds.Top - 2,
            bounds.Right + 2,
            bounds.Bottom + 2
        );

        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);
    }
}

public class FocusChangedEventArgs : EventArgs
{
    public FocusChangedEventArgs(ElementBase? oldFocus, ElementBase? newFocus)
    {
        OldFocus = oldFocus;
        NewFocus = newFocus;
    }

    public ElementBase? OldFocus { get; }
    public ElementBase? NewFocus { get; }
}