using System;

namespace Orivy;

public class KeyEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the KeyEventArgs class with the specified key code and modifier keys.
    /// </summary>
    /// <param name="keyCode">The key code representing the primary key involved in the event.</param>
    /// <param name="modifiers">A bitwise combination of modifier keys (such as Shift, Control, or Alt) that were pressed. Defaults to Keys.None
    /// if no modifiers are specified.</param>
    public KeyEventArgs(Keys keyCode, Keys modifiers = Keys.None)
    {
        KeyCode = keyCode;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Gets the keyboard key associated with the event.
    /// </summary>
    public Keys KeyCode { get; }

    /// <summary>
    /// Gets the modifier keys associated with this input gesture.
    /// </summary>
    public Keys Modifiers { get; }

    /// <summary>
    /// Gets a value indicating whether the Shift modifier key is pressed.
    /// </summary>
    public bool Shift => (Modifiers & Keys.Shift) == Keys.Shift;

    /// <summary>
    /// Gets a value indicating whether the Control modifier key is pressed.
    /// </summary>
    public bool Control => (Modifiers & Keys.Control) == Keys.Control;

    /// <summary>
    /// Gets a value indicating whether the Alt modifier key is pressed.
    /// </summary>
    public bool Alt => (Modifiers & Keys.Alt) == Keys.Alt;

    /// <summary>
    /// Gets or sets a value indicating whether the event has been handled and should not be processed further.
    /// </summary>
    public bool Handled { get; set; }
}
