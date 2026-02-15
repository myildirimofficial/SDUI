using SDUI.Helpers;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static SDUI.NativeMethods;

namespace SDUI.Controls;

public class ListView : System.Windows.Forms.ListView
{
    private ListViewColumnSorter LvwColumnSorter { get; set; }
    private bool _isApplyingTheme = true;
    private bool _isUpdating = false;

    public ListView()
        : base()
    {
        SetStyle(
                 ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.EnableNotifyMessage
                | ControlStyles.ResizeRedraw,
            true
        );
        
        DoubleBuffered = true;
        LvwColumnSorter = new ListViewColumnSorter();
        ListViewItemSorter = LvwColumnSorter;
        View = View.Details;
        FullRowSelect = true;
        OwnerDraw = false; // Let system handle drawing for better performance
        UpdateStyles();
    }

    /// <summary>
    /// Suspends painting to improve performance during bulk operations
    /// </summary>
    public new void BeginUpdate()
    {
        if (_isUpdating || !IsHandleCreated || IsDisposed)
            return;

        try
        {
            _isUpdating = true;
            SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            base.BeginUpdate();
        }
        catch (Exception ex)
        {
            _isUpdating = false;
            System.Diagnostics.Debug.WriteLine($"BeginUpdate failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Resumes painting after bulk operations
    /// </summary>
    public new void EndUpdate()
    {
        if (!_isUpdating || !IsHandleCreated || IsDisposed)
            return;

        try
        {
            base.EndUpdate();
            SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            _isUpdating = false;
            
            // Use RedrawWindow for proper refresh after WM_SETREDRAW
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, 
                RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW | RDW_FRAME);
        }
        catch (Exception ex)
        {
            _isUpdating = false;
            System.Diagnostics.Debug.WriteLine($"EndUpdate failed: {ex.Message}");
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        // Invalidate removed - handled by WM_NOTIFY messages
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnableDoubleBuffering();
        ApplyTheme();
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out WM_ERASEBKGND (0x14) to reduce flicker
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }

    protected override void OnColumnClick(ColumnClickEventArgs e)
    {
        base.OnColumnClick(e);
        
        BeginUpdate();
        try
        {
            for (int i = 0; i < Columns.Count; i++)
                SetSortArrow(i, SortOrder.None);

            if (e.Column == LvwColumnSorter.SortColumn)
            {
                LvwColumnSorter.Order =
                    (LvwColumnSorter.Order == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                LvwColumnSorter.SortColumn = e.Column;
                LvwColumnSorter.Order = SortOrder.Ascending;
            }

            SetSortArrow(e.Column, LvwColumnSorter.Order);

            if (!VirtualMode)
                Sort();
        }
        finally
        {
            EndUpdate();
        }
    }

    public void SelectAllItems()
    {
        if (Items.Count == 0)
            return;

        BeginUpdate();
        try
        {
            Focus();
            SetItemState(-1, 2, 2);
        }
        finally
        {
            EndUpdate();
        }
    }

    public void DeselectAllItems()
    {
        if (SelectedIndices.Count == 0)
            return;

        BeginUpdate();
        try
        {
            SetItemState(-1, 2, 0);
        }
        finally
        {
            EndUpdate();
        }
    }

    public void SetItemState(int itemIndex, int mask, int value)
    {
        LVITEM lvItem = new LVITEM();
        lvItem.stateMask = mask;
        lvItem.state = value;
        SendMessageLVItem(Handle, LVM_SETITEMSTATE, itemIndex, ref lvItem);
        
        if (itemIndex >= 0 && itemIndex < Items.Count)
            EnsureVisible(itemIndex);
    }

    public int SetGroupInfo(IntPtr hWnd, int nGroupID, uint nSate)
    {
        var lvg = new LVGROUP();
        lvg.cbSize = (uint)Marshal.SizeOf(lvg);
        lvg.mask = LVGF_STATE | LVGF_GROUPID | LVGF_HEADER;
        SendMessage(hWnd, LVM_GETGROUPINFO, nGroupID, ref lvg);
        lvg.state = nSate;
        lvg.mask = LVGF_STATE;
        SendMessage(hWnd, LVM_SETGROUPINFO, nGroupID, ref lvg);
        return -1;
    }

    public void SetSortArrow(int column, SortOrder sortOrder)
    {
        if (column < 0 || column >= Columns.Count)
            return;

        var pHeader = SendMessage(Handle, LVM_GETHEADER, 0, 0);
        if (pHeader == IntPtr.Zero)
            return;

        var pColumn = new IntPtr(column);
        var headerItem = new HDITEM { mask = HDITEM.Mask.Format };
        
        if (SendMessage(pHeader, HDM_GETITEM, pColumn, ref headerItem) == IntPtr.Zero)
            return;

        switch (sortOrder)
        {
            case SortOrder.Ascending:
                headerItem.fmt &= ~HDITEM.Format.SortDown;
                headerItem.fmt |= HDITEM.Format.SortUp;
                break;
            case SortOrder.Descending:
                headerItem.fmt &= ~HDITEM.Format.SortUp;
                headerItem.fmt |= HDITEM.Format.SortDown;
                break;
            case SortOrder.None:
                headerItem.fmt &= ~(HDITEM.Format.SortDown | HDITEM.Format.SortUp);
                break;
        }

        SendMessage(pHeader, HDM_SETITEM, pColumn, ref headerItem);
    }

    private void EnableDoubleBuffering()
    {
        if (!IsHandleCreated)
            return;

        // Get current extended styles first
        const int LVM_GETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 55;
        IntPtr currentStyles = SendMessage(Handle, LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero);
        
        // Extended styles for double buffering and smooth scrolling
        const int LVS_EX_FULLROWSELECT = 0x00000020;
        const int LVS_EX_BORDERSELECT = 0x00008000;
        const int LVS_EX_LABELTIP = 0x00004000;
        const int LVS_EX_TRANSPARENTBKGND = 0x00400000; // Better for themed controls
        const int LVS_EX_TRANSPARENTSHADOWTEXT = 0x00800000;
        
        // Combine with existing styles - LVS_EX_DOUBLEBUFFER is the key for scroll flicker
        int newStyles = currentStyles.ToInt32() 
            | LVS_EX_DOUBLEBUFFER 
            | LVS_EX_FULLROWSELECT 
            | LVS_EX_BORDERSELECT 
            | LVS_EX_LABELTIP;
        
        SendMessage(Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, new IntPtr(newStyles));
    }

    internal void ApplyTheme()
    {
        if (!IsHandleCreated || DesignMode)
            return;

        var _isDarkMode = ColorScheme.BackColor.IsDark();

        try
        {
            int useImmersiveDarkMode = _isDarkMode ? 1 : 0;

            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref useImmersiveDarkMode, sizeof(int));
            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                ref useImmersiveDarkMode, sizeof(int));

            if (_isDarkMode)
            {
                SetWindowTheme(Handle, "DarkMode_Explorer", null);
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.FromArgb(241, 241, 241);

                IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, "DarkMode_ItemsView", null);
                }
            }
            else
            {
                SetWindowTheme(Handle, "Explorer", null);
                BackColor = SystemColors.Window;
                ForeColor = SystemColors.WindowText;

                IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, "ItemsView", null);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme application failed: {ex.Message}");

            if (_isDarkMode)
            {
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.FromArgb(241, 241, 241);
            }
            else
            {
                BackColor = SystemColors.Window;
                ForeColor = SystemColors.WindowText;
            }
        }
        finally
        {
            _isApplyingTheme = false;
        }

        // Invalidate removed - system will repaint after theme change
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0014: // WM_ERASEBKGND
                // Prevent flicker - return 1 to indicate handled
                m.Result = (IntPtr)1;
                return;

            case 0x000F: // WM_PAINT
                // Skip painting if we're in update mode
                if (_isUpdating)
                {
                    // Let Windows know we handled it
                    ValidateRect(Handle, IntPtr.Zero);
                    m.Result = IntPtr.Zero;
                    return;
                }
                break;

            case WM_NOTIFY:
                if (!IsHandleCreated || IsDisposed)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }

                var pnmhdr = (NMHDR)m.GetLParam(typeof(NMHDR));

                if (pnmhdr.code == NM_CUSTOMDRAW)
                {
                    var nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));

                    switch (nmcd.dwDrawStage)
                    {
                        case (int)CDDS.CDDS_PREPAINT:
                            m.Result = new IntPtr((int)CDRF.CDRF_NOTIFYITEMDRAW);
                            return;
                        
                        case (int)CDDS.CDDS_ITEMPREPAINT:
                            SetTextColor(nmcd.hdc, ColorTranslator.ToWin32(ColorScheme.ForeColor));
                            m.Result = new IntPtr((int)CDRF.CDRF_DODEFAULT);
                            return;
                        
                        default:
                            m.Result = new IntPtr((int)CDRF.CDRF_DODEFAULT);
                            return;
                    }
                }
                break;
        }

        base.WndProc(ref m);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            // WS_EX_COMPOSITED - enables double-buffered painting for the window and all its children
            // This is key for smooth scrolling without flicker
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    private const int WM_SETREDRAW = 0x000B;
}
