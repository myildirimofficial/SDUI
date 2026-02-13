
using SkiaSharp;

namespace SDUI.Controls;

public abstract partial class ElementBase
{
    protected void PerformDefaultLayout(ElementBase control, SKRect clientArea, ref SKRect remainingArea)
    {
        var dock = control.Dock;

        // Handle Dock first (WinForms priority)
        if (dock != DockStyle.None)
        {
            var newBounds = SKRect.Empty;

            switch (dock)
            {
                case DockStyle.Top:
                    newBounds = SKRect.Create(
                        remainingArea.Left,
                        remainingArea.Top,
                        remainingArea.Width,
                        control.Height);

                    remainingArea = new SKRect(
                        remainingArea.Left,
                        remainingArea.Top + control.Height,
                        remainingArea.Right,
                        remainingArea.Bottom
                    );
                    break;

                case DockStyle.Bottom:
                    newBounds = SKRect.Create(
                        remainingArea.Left,
                        remainingArea.Bottom - control.Height,
                        remainingArea.Width,
                        control.Height);

                    remainingArea = new SKRect(
                        remainingArea.Left,
                        remainingArea.Top,
                        remainingArea.Right,
                        remainingArea.Bottom - control.Height
                    );
                    break;

                case DockStyle.Left:
                    newBounds = SKRect.Create(
                        remainingArea.Left,
                        remainingArea.Top,
                        control.Width,
                        remainingArea.Height);

                    remainingArea = new SKRect(
                        remainingArea.Left + control.Width,
                        remainingArea.Top,
                        remainingArea.Right,
                        remainingArea.Bottom
                    );
                    break;

                case DockStyle.Right:
                    newBounds = SKRect.Create(
                        remainingArea.Right - control.Width,
                        remainingArea.Top,
                        control.Width,
                        remainingArea.Height);

                    remainingArea = new SKRect(
                        remainingArea.Left,
                        remainingArea.Top,
                        remainingArea.Right - control.Width,
                        remainingArea.Bottom
                    );
                    break;

                case DockStyle.Fill:
                    newBounds = remainingArea;
                    break;
            }

            if (control.Bounds != newBounds)
                control.Bounds = newBounds;
        }
        // Handle Anchor if no Dock
        else if (control.Anchor != AnchorStyles.None)
        {
            var anchor = control.Anchor;
            var x = control.Location.X;
            var y = control.Location.Y;
            float width = control.Width;
            float height = control.Height;

            // Left anchor
            if ((anchor & AnchorStyles.Left) == AnchorStyles.Left)
            {
                // X stays the same
            }
            else if ((anchor & AnchorStyles.Right) == AnchorStyles.Right)
            {
                // Move with right edge
                x = clientArea.Right - (clientArea.Width - control.Location.X - control.Width) - control.Width;
            }

            // Top anchor
            if ((anchor & AnchorStyles.Top) == AnchorStyles.Top)
            {
                // Y stays the same
            }
            else if ((anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
            {
                // Move with bottom edge
                y = clientArea.Bottom - (clientArea.Height - control.Location.Y - control.Height) - control.Height;
            }

            // Width resize
            if ((anchor & AnchorStyles.Left) == AnchorStyles.Left && 
                (anchor & AnchorStyles.Right) == AnchorStyles.Right)
            {
                width = clientArea.Width - control.Location.X - (clientArea.Width - control.Location.X - control.Width);
            }

            // Height resize
            if ((anchor & AnchorStyles.Top) == AnchorStyles.Top && 
                (anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
            {
                height = clientArea.Height - control.Location.Y - (clientArea.Height - control.Location.Y - control.Height);
            }

            var newBounds = SKRect.Create(x, y, width, height);
            if (control.Bounds != newBounds)
                control.Bounds = newBounds;
        }
    }
}
