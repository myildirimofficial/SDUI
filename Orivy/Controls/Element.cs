namespace Orivy.Controls;

public class Element : ElementBase
{
}

public class Container : ElementBase
{
    public override SkiaSharp.SKColor BackColor
    {
        get => SkiaSharp.SKColors.Transparent;
        set { }
    }
}

public class TextBox : ElementBase
{
}
