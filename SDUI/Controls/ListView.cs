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

    public ListView()
        : base()
    {
        SetStyle(
                 ControlStyles.AllPaintingInWmPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.EnableNotifyMessage,
            true
        );
        LvwColumnSorter = new ListViewColumnSorter();
        ListViewItemSorter = LvwColumnSorter;
        View = View.Details;
        FullRowSelect = true;
        UpdateStyles();
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnableDoubleBuffering();
        ApplyTheme();
    }

    protected override void OnNotifyMessage(Message m)
    {
        if (m.Msg != 0x14)
        {
            base.OnNotifyMessage(m);
        }
    }

    protected override void OnColumnClick(ColumnClickEventArgs e)
    {
        base.OnColumnClick(e);
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

    public void SelectAllItems()
    {
        Focus();
        SetItemState(-1, 2, 2);
    }

    public void DeselectAllItems()
    {
        SetItemState(-1, 2, 0);
    }

    public void SetItemState(int itemIndex, int mask, int value)
    {
        LVITEM lvItem = new LVITEM();
        lvItem.stateMask = mask;
        lvItem.state = value;
        SendMessageLVItem(Handle, LVM_SETITEMSTATE, itemIndex, ref lvItem);
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
        var pHeader = SendMessage(this.Handle, LVM_GETHEADER, 0, 0);
        var pColumn = new IntPtr(column);
        var headerItem = new HDITEM { mask = HDITEM.Mask.Format };
        SendMessage(pHeader, HDM_GETITEM, pColumn, ref headerItem);

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
        IntPtr lParam = new IntPtr(LVS_EX_DOUBLEBUFFER | 0x00000020);
        SendMessage(Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, lParam);
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

        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WM_NOTIFY)
        {
            var pnmhdr = (NMHDR)m.GetLParam(typeof(NMHDR));

            if (pnmhdr.code == NM_CUSTOMDRAW)
            {
                var nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));

                switch (nmcd.dwDrawStage)
                {
                    case (int)CDDS.CDDS_PREPAINT:
                        m.Result = new IntPtr((int)CDRF.CDRF_NOTIFYITEMDRAW);
                        break;
                    case (int)CDDS.CDDS_ITEMPREPAINT:

                        SetTextColor(nmcd.hdc, ColorTranslator.ToWin32(ColorScheme.ForeColor));

                        m.Result = new IntPtr((int)CDRF.CDRF_DODEFAULT);

                        break;
                    default:
                        m.Result = new IntPtr((int)CDRF.CDRF_NOTIFYPOSTPAINT);
                        break;
                }
            }
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x02000000;
            return cp;
        }
    }
}
