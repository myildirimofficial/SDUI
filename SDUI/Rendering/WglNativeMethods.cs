using System.Runtime.InteropServices;

namespace SDUI.Native.Windows;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct PIXELFORMATDESCRIPTOR
{
    public short nSize;
    public short nVersion;
    public uint dwFlags;
    public byte iPixelType;
    public byte cColorBits;
    public byte cRedBits;
    public byte cRedShift;
    public byte cGreenBits;
    public byte cGreenShift;
    public byte cBlueBits;
    public byte cBlueShift;
    public byte cAlphaBits;
    public byte cAlphaShift;
    public byte cAccumBits;
    public byte cAccumRedBits;
    public byte cAccumGreenBits;
    public byte cAccumBlueBits;
    public byte cAccumAlphaBits;
    public byte cDepthBits;
    public byte cStencilBits;
    public byte cAuxBuffers;
    public byte iLayerType;
    public byte bReserved;
    public uint dwLayerMask;
    public uint dwVisibleMask;
    public uint dwDamageMask;
}

internal static class WglNativeMethods
{
    private const string Gdi32 = "gdi32.dll";
    private const string Opengl32 = "opengl32.dll";
    private const string User32 = "user32.dll";

    [DllImport(Gdi32, SetLastError = true)]
    public static extern int ChoosePixelFormat(nint hdc, ref PIXELFORMATDESCRIPTOR pfd);

    [DllImport(Gdi32, SetLastError = true)]
    public static extern bool SetPixelFormat(nint hdc, int format, ref PIXELFORMATDESCRIPTOR pfd);

    [DllImport(User32, SetLastError = true)]
    public static extern nint GetDC(nint hwnd);

    [DllImport(User32, SetLastError = true)]
    public static extern int ReleaseDC(nint hwnd, nint hdc);

    [DllImport(Opengl32, SetLastError = true)]
    public static extern nint wglCreateContext(nint hdc);

    [DllImport(Opengl32, SetLastError = true)]
    public static extern bool wglDeleteContext(nint hglrc);

    [DllImport(Opengl32, SetLastError = true)]
    public static extern bool wglMakeCurrent(nint hdc, nint hglrc);
    
    [DllImport(Opengl32, SetLastError = true)]
    public static extern void glViewport(int x, int y, int width, int height);

    [DllImport(Gdi32, SetLastError = true)]
    public static extern bool SwapBuffers(nint hdc);
}