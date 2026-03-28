namespace Orivy;

[System.Flags]
internal enum GrowthDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Upward = 4,
    Downward = 8
}