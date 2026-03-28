using Orivy.Collections;
using SkiaSharp;


namespace Orivy.Controls;

public interface IElement
{
    string Name { get; set; }
    string Text { get; set; }

    SKPoint Location { get; set; }
    SKSize Size { get; set; }
    bool Visible { get; set; }
    bool Enabled { get; set; }
    ElementBase Parent { get; set; }
    SKColor BackColor { get; set; }
    SKColor ForeColor { get; set; }
    SKFont Font { get; set; }

    ElementBase FocusedElement { get; set; }

    ElementCollection Controls { get; }
    object Tag { get; set; }
    DockStyle Dock { get; set; }
    int ZOrder { get; set; }
    int TabIndex { get; set; }
    bool TabStop { get; set; }
    int LayoutSuspendCount { get; set; }
    SKRect DisplayRectangle { get; }

    void Render(SKCanvas canvas);
    void Invalidate();
    void Refresh();
    void UpdateZOrder();
    void BringToFront();
    WindowBase GetParentWindow();
    void EnsureLoadedRecursively();
    void EnsureUnloadedRecursively();
}