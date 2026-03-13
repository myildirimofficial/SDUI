namespace SDUI.Rendering;

public enum RenderBackend
{
    Software = 0,
    OpenGL = 1,
    DirectX11 = 2,
    Vulkan = 3,
    Metal = 4, // reserved for future use if/when we add Metal support on macOS
}