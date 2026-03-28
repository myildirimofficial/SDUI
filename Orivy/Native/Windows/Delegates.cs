using System;

namespace Orivy.Native.Windows;

public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);