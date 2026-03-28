using System;
using Orivy.Controls;

namespace Orivy;

public class ElementEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the UIElementEventArgs class with the specified UI element.
    /// </summary>
    /// <param name="element">The UI element associated with the event. Cannot be null.</param>
    public ElementEventArgs(IElement element)
    {
        Element = element;
    }

    /// <summary>
    /// Gets the underlying UI element associated with this instance.
    /// </summary>
    public IElement Element { get; }
}