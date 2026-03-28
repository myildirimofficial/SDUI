using Orivy.Controls;
using System;

namespace Orivy;

public sealed class LayoutEventArgs : EventArgs
{
    private readonly WeakReference<IElement>? _affectedComponent;

    public LayoutEventArgs(IElement? affectedComponent, string? affectedProperty)
    {
        _affectedComponent = affectedComponent is not null ? new(affectedComponent) : null;
        AffectedProperty = affectedProperty;
    }
    public LayoutEventArgs(IElement? affectedComponent)
    {
        _affectedComponent = affectedComponent is not null ? new(affectedComponent) : null;
        AffectedProperty = string.Empty;
    }

    public IElement? AffectedComponent
    {
        get
        {
            IElement? target = null;
            _affectedComponent?.TryGetTarget(out target);
            return target;
        }
    }

    public IElement? AffectedControl => AffectedComponent;

    public string? AffectedProperty { get; }
}
