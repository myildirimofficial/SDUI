// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SkiaSharp;

namespace Orivy.Layout;

public interface IArrangedElement
{
    /// <summary>
    ///  Bounding rectangle of the element.
    /// </summary>
    SKRect Bounds { get; }

    /// <summary>
    ///  Sets the bounds for an element. Implementors should call
    ///  CommonProperties.SetSpecifiedBounds.
    ///  See Control.SetBoundsCore.
    /// </summary>
    void SetBounds(SKRect bounds, BoundsSpecified specified);

    /// <summary>
    ///  Query element for its preferred size. There is no guarantee
    ///  that layout engine will assign the element the returned size.
    ///  ProposedSize is a hint about constraints.
    /// </summary>
    SKSize GetPreferredSize(SKSize proposedSize);

    /// <summary>
    ///  DisplayRectangle is the client area of a container element.
    ///  Could possibly disappear if we change control to keep an
    ///  up-to-date copy of display rectangle in the property store.
    /// </summary>
    SKRect DisplayRectangle { get; }

    /// <summary>
    ///  True if the element is currently visible (some layouts, like
    ///  flow, need to skip non-visible elements.)
    /// </summary>
    bool ParticipatesInLayout { get; }

    /// <summary>
    ///  Internally, layout engines will get properties from the
    ///  property store on this interface. In Orcas, this will be
    ///  replaced with a global PropertyManager for DPs.
    /// </summary>
    PropertyStore Properties { get; }

    /// <summary>
    ///  When an extender provided property is changed, we call this
    ///  method to update the layout on the element. In Orcas, we
    ///  will sync the DPs changed event.
    /// </summary>
    void PerformLayout(IArrangedElement affectedElement, string? propertyName);

    /// <summary>
    ///  Returns the element's parent container (on a control, this forwards to Parent)
    /// </summary>
    IArrangedElement? Container { get; }

    /// <summary>
    ///  Returns the element's children (on a control, this forwards to Controls)
    /// </summary>
    ArrangedElementCollection Children { get; }

    void PerformLayout();
    void SuspendLayout();
    void ResumeLayout();
    void ResumeLayout(bool performLayout);
}