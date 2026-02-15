using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SDUI.Controls
{
    public class WindowPageControl : UserControl
    {
        private EventHandler<int> _onSelectedIndexChanged;

        public event EventHandler<int> SelectedIndexChanged
        {
            add => _onSelectedIndexChanged += value;
            remove => _onSelectedIndexChanged -= value;
        }

        // Stable page list - maintains insertion order independent of z-order
        private readonly List<Control> _pages = new();

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                    return;

                if (_pages.Count > 0)
                {
                    if (value < 0)
                        value = _pages.Count - 1;

                    if (value > _pages.Count - 1)
                        value = 0;
                }
                else
                    value = -1;

                var previousSelectedIndex = _selectedIndex;
                _selectedIndex = value;
                _onSelectedIndexChanged?.Invoke(this, previousSelectedIndex);

                for (int i = 0; i < _pages.Count; i++)
                    _pages[i].Visible = i == _selectedIndex;
            }
        }

        public int Count => _pages.Count;

        /// <summary>
        /// Gets the page at the specified stable index.
        /// </summary>
        public Control GetPage(int index) => _pages[index];

        /// <summary>
        /// Moves a page to the specified index in the stable page list
        /// and updates the z-order to match.
        /// </summary>
        public void SetPageIndex(Control control, int newIndex)
        {
            if (!_pages.Contains(control))
                return;

            _pages.Remove(control);

            if (newIndex >= _pages.Count)
                _pages.Add(control);
            else
                _pages.Insert(newIndex, control);

            // Keep z-order in sync
            Controls.SetChildIndex(control, newIndex);
        }

        public WindowPageControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            e.Control.Dock = DockStyle.Fill;

            if (!_pages.Contains(e.Control))
                _pages.Add(e.Control);

            e.Control.Visible = _pages.Count == 1;

            if (_pages.Count == 1)
                _selectedIndex = 0;
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);

            _pages.Remove(e.Control);

            if (_pages.Count == 0)
                _selectedIndex = -1;
            else if (_selectedIndex >= _pages.Count)
                _selectedIndex = _pages.Count - 1;
        }
    }
}
