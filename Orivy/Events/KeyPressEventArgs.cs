namespace Orivy;

public class KeyPressEventArgs : KeyEventArgs
{
    public KeyPressEventArgs(Keys keyCode, Keys modifiers = Keys.None) : base(keyCode, modifiers)
    {
    }
}
