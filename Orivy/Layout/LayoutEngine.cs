using SkiaSharp;
using System;

namespace Orivy.Layout;

public abstract class LayoutEngine
{
    internal static IArrangedElement CastToArrangedElement(object obj)
    {
        if (obj is not IArrangedElement element)
        {
            throw new NotSupportedException();
        }

        return element;
    }

    internal virtual SKSize GetPreferredSize(IArrangedElement container, SKSize proposedConstraints)
    {
        return SKSize.Empty;
    }

    public virtual void InitLayout(object child, BoundsSpecified specified)
    {
        ArgumentNullException.ThrowIfNull(child);

        InitLayoutCore(CastToArrangedElement(child), specified);
    }

    private protected virtual void InitLayoutCore(IArrangedElement element, BoundsSpecified bounds)
    {
    }

    internal virtual void ProcessSuspendedLayoutEventArgs(IArrangedElement container, LayoutEventArgs args)
    {
    }

    public virtual bool Layout(object container, LayoutEventArgs layoutEventArgs)
    {
        ArgumentNullException.ThrowIfNull(container);
        return LayoutCore(CastToArrangedElement(container), layoutEventArgs);
    }

    private protected virtual bool LayoutCore(IArrangedElement container, LayoutEventArgs layoutEventArgs)
    {
        return false;
    }
}