namespace Orivy;

/// <summary>
/// Lightweight mouse button identifier used throughout Orivy.
/// Implemented as a record struct so it behaves like a value type but can have named static instances.
/// </summary>
public readonly record struct MouseButtons(int Value)
{
    public static readonly MouseButtons None = new(0);
    public static readonly MouseButtons Left = new(1);
    public static readonly MouseButtons Right = new(2);
    public static readonly MouseButtons Middle = new(4);
    public static readonly MouseButtons XButton1 = new(8);
    public static readonly MouseButtons XButton2 = new(16);

    public override string ToString() => Value switch
    {
        0 => "None",
        1 => "Left",
        2 => "Right",
        4 => "Middle",
        8 => "XButton1",
        16 => "XButton2",
        _ => $"MouseButtons({Value})"
    };
}
