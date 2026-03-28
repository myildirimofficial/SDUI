namespace Orivy.Native.Windows;

public partial class Methods
{
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;
    public const int HTLEFT = 10;
    public const int HTRIGHT = 11;
    public const int HTTOP = 12;
    public const int HTTOPLEFT = 13;
    public const int HTTOPRIGHT = 14;
    public const int HTBOTTOM = 15;
    public const int HTBOTTOMLEFT = 16;
    public const int HTBOTTOMRIGHT = 17;

    public const uint DWMSBT_AUTO = 0;
    public const uint DWMSBT_NONE = 1;
    public const uint DWMSBT_MAINWINDOW = 2; // Mica
    public const uint DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
    public const uint DWMSBT_TABBEDWINDOW = 4; // Tabbed

    public const int WM_SYSCOMMAND = 0x0112;
    
    public const int CW_USEDEFAULT = unchecked((int)0x80000000);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SM_CXSIZEFRAME = 32;
    public const int SM_CYSIZEFRAME = 33;
    public const int SM_CXPADDEDBORDER = 92;

    public const uint SWP_NOSIZE = 0x0001;   
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const uint MONITOR_DEFAULTTONULL = 0x00000000;
    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int ICON_SMALL2 = 2;
}
