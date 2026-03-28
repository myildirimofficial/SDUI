using Orivy.Layout;
using SkiaSharp;
using System.Collections.Generic;


namespace Orivy.Controls;

public abstract partial class ElementBase
{
    PropertyStore IArrangedElement.Properties => _properties ??= new PropertyStore();
    private PropertyStore? _properties;

    #region IArrangedElement Explicit Implementation

    /// <summary>
    /// Sets the bounds for this element. This is the implementation required by IArrangedElement.
    /// </summary>
    void IArrangedElement.SetBounds(SKRect bounds, BoundsSpecified specified)
    {
        // Update specified bounds tracking (used by layout engines)
        CommonProperties.UpdateSpecifiedBounds(this, (int)bounds.Location.X, (int)bounds.Location.Y, (int)bounds.Width, (int)bounds.Height, specified);

        // Apply the bounds
        if (Bounds != bounds)
        {
            _isArranging = true;
            try
            {
                Bounds = bounds;
            }
            finally
            {
                _isArranging = false;
            }
        }
    }

    /// <summary>
    /// Determines whether this element participates in layout operations.
    /// </summary>
    bool IArrangedElement.ParticipatesInLayout => Visible;

    /// <summary>
    /// Returns the container (parent) of this element for layout purposes.
    /// </summary>
    IArrangedElement? IArrangedElement.Container => Parent as IArrangedElement;

    /// <summary>
    /// Returns the children of this element for layout purposes.
    /// </summary>
    ArrangedElementCollection IArrangedElement.Children
    {
        get
        {
            // Create a list of IArrangedElement from our Controls
            var arrangedElements = new List<IArrangedElement>();
            foreach (var control in Controls)
            {
                if (control is IArrangedElement arranged)
                    arrangedElements.Add(arranged);
            }
            return new ArrangedElementCollection(arrangedElements);
        }
    }

    /// <summary>
    /// Overload that accepts IArrangedElement for layout compatibility.
    /// </summary>
    void IArrangedElement.PerformLayout(IArrangedElement affectedElement, string? propertyName)
    {
        if (affectedElement is ElementBase element)
            PerformLayout(element, propertyName);
        else
            PerformLayout();
    }

    #endregion
}
