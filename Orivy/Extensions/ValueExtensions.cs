using Orivy.Controls;

namespace Orivy;

public static class ValueExtensions
{
    public static float Topx(this float pt, ElementBase control)
    {
        return pt * 1.333f * control.ScaleFactor;
    }

    public static float Topx(this int pt, ElementBase control)
    {
        return ((float)pt).Topx(control);
    }
    
    public static float Topx(this double pt, ElementBase control)
    {
        return ((float)pt).Topx(control);
    }
}
