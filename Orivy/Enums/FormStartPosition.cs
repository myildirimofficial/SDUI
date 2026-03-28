namespace Orivy;

public enum FormStartPosition
{
    /// <summary>
    /// Specifies that the operation or process must be performed manually by the user or caller.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Specifies that the window is positioned in the center of the screen.
    /// </summary>
    CenterScreen = 1,

    /// <summary>
    /// Specifies that the window is positioned at the default location determined by the Windows operating system.
    /// </summary>
    /// <remarks>Use this value to allow the system to choose the initial position of the window, rather than
    /// specifying explicit coordinates.</remarks>
    WindowsDefaultLocation = 2,

    /// <summary>
    /// Specifies that the window should be positioned and sized using the system's default values for new windows.
    /// </summary>
    WindowsDefaultBounds = 3,

    /// <summary>
    /// Specifies that a window is centered with respect to its parent window when displayed.
    /// </summary>
    CenterParent = 4
}
