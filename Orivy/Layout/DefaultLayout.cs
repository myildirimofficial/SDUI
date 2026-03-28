
using Orivy.Controls;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Orivy.Layout;
 
internal partial class DefaultLayout : LayoutEngine
{
    internal static DefaultLayout Instance { get; } = new();
 
    private static readonly int s_layoutInfoProperty = PropertyStore.CreateKey();
    private static readonly int s_cachedBoundsProperty = PropertyStore.CreateKey();
 
    /// <summary>
    ///  Loop through the AutoSized controls and expand them if they are smaller than
    ///  their preferred size. If expanding the controls causes overlap, bump the overlapped
    ///  control if it is AutoRelocatable.
    /// </summary>
    private static void LayoutAutoSizedControls(IArrangedElement container)
    {
        ArrangedElementCollection children = container.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            IArrangedElement element = children[i];
            if (CommonProperties.xGetAutoSizedAndAnchored(element))
            {
                SkiaSharp.SKRect bounds = GetCachedBounds(element);
 
                AnchorStyles anchor = GetAnchor(element);
                SKSize proposedConstraints = LayoutUtils.s_maxSize;
 
                if ((anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right))
                {
                    proposedConstraints.Width = bounds.Width;
                }
 
                if ((anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == (AnchorStyles.Top | AnchorStyles.Bottom))
                {
                    proposedConstraints.Height = bounds.Height;
                }
 
                SKSize prefSize = element.GetPreferredSize(proposedConstraints);
                SkiaSharp.SKRect newBounds = bounds;
                if (CommonProperties.GetAutoSizeMode(element) == AutoSizeMode.GrowAndShrink)
                {
                    // this is the case for simple things like radio button, checkbox, etc.
                    newBounds = GetGrowthBounds(element, prefSize);
                }
                else
                {
                    // we had whacked this check, but it turns out it causes undesirable
                    // behavior in things like panel. a panel with no elements sizes to 0,0.
                    if (bounds.Width < prefSize.Width || bounds.Height < prefSize.Height)
                    {
                        SKSize newSize = LayoutUtils.UnionSizes(bounds.Size, prefSize);
                        newBounds = GetGrowthBounds(element, newSize);
                    }
                }
 
                if (newBounds != bounds)
                {
                    SetCachedBounds(element, newBounds);
                }
            }
        }
    }
 
    /// <summary>
    ///  Gets the bounds of the control after growing to newSize (note that depending on
    ///  anchoring the control may grow to the left/upwards rather than to the
    ///  right/downwards. i.e., it may be translated.)
    /// </summary>
    private static SkiaSharp.SKRect GetGrowthBounds(IArrangedElement element, SKSize newSize)
    {
        GrowthDirection direction = GetGrowthDirection(element);
        SkiaSharp.SKRect oldBounds = GetCachedBounds(element);
        SKPoint location = oldBounds.Location;
 
        Debug.Assert(CommonProperties.GetAutoSizeMode(element) == AutoSizeMode.GrowAndShrink || (newSize.Height >= oldBounds.Height && newSize.Width >= oldBounds.Width),
            "newSize expected to be >= current size.");
 
        if ((direction & GrowthDirection.Left) != GrowthDirection.None)
        {
            // We are growing towards the left, translate X
            location.X -= newSize.Width - oldBounds.Width;
        }
 
        if ((direction & GrowthDirection.Upward) != GrowthDirection.None)
        {
            // We are growing towards the top, translate Y
            location.Y -= newSize.Height - oldBounds.Height;
        }

        // With the following, which uses the correct SKRect constructor:
        SkiaSharp.SKRect newBounds = new SkiaSharp.SKRect(location.X, location.Y, location.X + newSize.Width, location.Y + newSize.Height);
 
        Debug.Assert(CommonProperties.GetAutoSizeMode(element) == AutoSizeMode.GrowAndShrink || newBounds.Contains(oldBounds), "How did we resize in such a way we no longer contain our old bounds?");
 
        return newBounds;
    }
 
    /// <summary>
    ///  Examines an elements anchoring to figure out which direction it should grow.
    /// </summary>
    private static GrowthDirection GetGrowthDirection(IArrangedElement element)
    {
        AnchorStyles anchor = GetAnchor(element);
        GrowthDirection growthDirection = GrowthDirection.None;
 
        if ((anchor & AnchorStyles.Right) != AnchorStyles.None
            && (anchor & AnchorStyles.Left) == AnchorStyles.None)
        {
            // Control is anchored to the right, but not the left.
            growthDirection |= GrowthDirection.Left;
        }
        else
        {
            // Otherwise we grow towards the right (common case)
            growthDirection |= GrowthDirection.Right;
        }
 
        if ((anchor & AnchorStyles.Bottom) != AnchorStyles.None
            && (anchor & AnchorStyles.Top) == AnchorStyles.None)
        {
            // Control is anchored to the bottom, but not the top.
            growthDirection |= GrowthDirection.Upward;
        }
        else
        {
            // Otherwise we grow towards the bottom. (common case)
            growthDirection |= GrowthDirection.Downward;
        }
 
        Debug.Assert((growthDirection & GrowthDirection.Left) == GrowthDirection.None
            || (growthDirection & GrowthDirection.Right) == GrowthDirection.None,
            "We shouldn't allow growth to both the left and right.");
        Debug.Assert((growthDirection & GrowthDirection.Upward) == GrowthDirection.None
            || (growthDirection & GrowthDirection.Downward) == GrowthDirection.None,
            "We shouldn't allow both upward and downward growth.");
        return growthDirection;
    }
 
    /// <summary>
    ///  Layout for a single anchored control. There's no order dependency when laying out anchored controls.
    /// </summary>
    private static SkiaSharp.SKRect GetAnchorDestination(IArrangedElement element, SkiaSharp.SKRect displayRect, bool measureOnly)
    {
        // Container can not be null since we AnchorControls takes a non-null container.
        return true
            ? ComputeAnchoredBoundsV2(element, displayRect)
            : ComputeAnchoredBounds(element, displayRect, measureOnly);
    }
 
    private static SkiaSharp.SKRect ComputeAnchoredBoundsV2(IArrangedElement element, SkiaSharp.SKRect displayRectangle)
    {
        SkiaSharp.SKRect bounds = GetCachedBounds(element);
        if (displayRectangle.IsEmpty)
        {
            return bounds;
        }
 
        AnchorInfo? anchorInfo = GetAnchorInfo(element);
        if (anchorInfo is null)
        {
            return bounds;
        }
 
        var width = bounds.Width;
        var height = bounds.Height;
        anchorInfo.DisplayRectangle = displayRectangle;
 
        Debug.WriteLineIf(width < 0 || height < 0, $"\t\t'{element}' destination bounds resulted in negative");
 
        // Compute control bounds according to AnchorStyles set on it.
        AnchorStyles anchors = GetAnchor(element);
        if (IsAnchored(anchors, AnchorStyles.Left))
        {
            // If anchored both Left and Right, the control's width should be adjusted according to
            // the parent's width.
            if (IsAnchored(anchors, AnchorStyles.Right))
            {
                width = displayRectangle.Width - (anchorInfo.Right + anchorInfo.Left);
            }
        }
        else
        {
            // If anchored Right but not Left, the control's X-coordinate should be adjusted according
            // to the parent's width.
            if (IsAnchored(anchors, AnchorStyles.Right))
            {
                anchorInfo.Left = displayRectangle.Width - width - anchorInfo.Right;
            }
            else
            {
                // The control neither anchored Right nor Left but anchored Top or Bottom, the control's
                // X-coordinate should be adjusted according to the parent's width.
                var growOrShrink = (displayRectangle.Width - (anchorInfo.Left + anchorInfo.Right + width)) / 2;
                anchorInfo.Left += growOrShrink;
                anchorInfo.Right += growOrShrink;
            }
        }
 
        if (IsAnchored(anchors, AnchorStyles.Top))
        {
            if (IsAnchored(anchors, AnchorStyles.Bottom))
            {
                // If anchored both Top and Bottom, the control's height should be adjusted according to
                // the parent's height.
                height = displayRectangle.Height - (anchorInfo.Bottom + anchorInfo.Top);
            }
        }
        else
        {
            // If anchored Bottom but not Top, the control's Y-coordinate should be adjusted according to
            // the parent's height.
            if (IsAnchored(anchors, AnchorStyles.Bottom))
            {
                anchorInfo.Top = displayRectangle.Height - height - anchorInfo.Bottom;
            }
            else
            {
                // The control neither anchored Top or Bottom but anchored Right or Left, the control's
                // Y-coordinate is adjusted accoring to the parent's height.
                var growOrShrink = (displayRectangle.Height - (anchorInfo.Bottom + anchorInfo.Top + height)) / 2;
                anchorInfo.Top += growOrShrink;
                anchorInfo.Bottom += growOrShrink;
            }
        }
 
        return SkiaSharp.SKRect.Create(anchorInfo.Left, anchorInfo.Top, width, height);
    }
 
    private static SkiaSharp.SKRect ComputeAnchoredBounds(IArrangedElement element, SkiaSharp.SKRect displayRect, bool measureOnly)
    {
        AnchorInfo layout = GetAnchorInfo(element)!;
 
        var left = layout.Left + displayRect.Left;
        var top = layout.Top + displayRect.Top;
        var right = layout.Right + displayRect.Left;
        var bottom = layout.Bottom + displayRect.Top;
 
        AnchorStyles anchor = GetAnchor(element);
 
        if (IsAnchored(anchor, AnchorStyles.Right))
        {
            right += displayRect.Width;
 
            if (!IsAnchored(anchor, AnchorStyles.Left))
            {
                left += displayRect.Width;
            }
        }
        else if (!IsAnchored(anchor, AnchorStyles.Left))
        {
            var center = displayRect.Width / 2;
            right += center;
            left += center;
        }
 
        if (IsAnchored(anchor, AnchorStyles.Bottom))
        {
            bottom += displayRect.Height;
 
            if (!IsAnchored(anchor, AnchorStyles.Top))
            {
                top += displayRect.Height;
            }
        }
        else if (!IsAnchored(anchor, AnchorStyles.Top))
        {
            var center = displayRect.Height / 2;
            bottom += center;
            top += center;
        }
 
        if (!measureOnly)
        {
            // the size is actually zero, set the width and heights appropriately.
            if (right < left)
            {
                right = left;
            }
 
            if (bottom < top)
            {
                bottom = top;
            }
        }
        else
        {
            SkiaSharp.SKRect cachedBounds = GetCachedBounds(element);
            // in this scenario we've likely been passed a 0 sized display rectangle to determine our height.
            // we will need to translate the right and bottom edges as necessary to the positive plane.
 
            // right < left means the control is anchored both left and right.
            // cachedBounds != control.Bounds means  the control's size has changed
            // any, all, or none of these can be true.
            if (right < left || cachedBounds.Width != element.Bounds.Width || cachedBounds.Left != element.Bounds.Left)
            {
                if (cachedBounds != element.Bounds)
                {
                    left = Math.Max(Math.Abs(left), Math.Abs(cachedBounds.Left));
                }
 
                right = left + Math.Max(element.Bounds.Width, cachedBounds.Width) + Math.Abs(right);
            }
            else
            {
                left = left > 0 ? left : element.Bounds.Left;
                right = right > 0 ? right : element.Bounds.Right + Math.Abs(right);
            }
 
            // bottom < top means the control is anchored both top and bottom.
            // cachedBounds != control.Bounds means  the control's size has changed
            // any, all, or none of these can be true.
            if (bottom < top || cachedBounds.Height != element.Bounds.Height || cachedBounds.Top != element.Bounds.Top)
            {
                if (cachedBounds != element.Bounds)
                {
                    top = Math.Max(Math.Abs(top), Math.Abs(cachedBounds.Top));
                }
 
                bottom = top + Math.Max(element.Bounds.Height, cachedBounds.Height) + Math.Abs(bottom);
            }
            else
            {
                top = top > 0 ? top : element.Bounds.Top;
                bottom = bottom > 0 ? bottom : element.Bounds.Bottom + Math.Abs(bottom);
            }
        }
 
        return SkiaSharp.SKRect.Create(left, top, right - left, bottom - top);
    }
 
    private static void LayoutAnchoredControls(IArrangedElement container)
    {
        SkiaSharp.SKRect displayRectangle = container.DisplayRectangle;
        if (CommonProperties.GetAutoSize(container) && ((displayRectangle.Width == 0) || (displayRectangle.Height == 0)))
        {
            // We haven't set ourselves to the preferred size yet. Proceeding will
            // just set all the control widths to zero.
            return;
        }
 
        ArrangedElementCollection children = container.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            IArrangedElement element = children[i];
            if (!CommonProperties.GetNeedsAnchorLayout(element))
            {
                continue;
            }
 
            Debug.Assert(GetAnchorInfo(element) is not null, "AnchorInfo should be initialized before LayoutAnchorControls().");
            SetCachedBounds(element, GetAnchorDestination(element, displayRectangle, measureOnly: false));
        }
    }
 
    private static SKSize LayoutDockedControls(IArrangedElement container, bool measureOnly)
    {
        Debug.Assert(!HasCachedBounds(container), "Do not call this method with an active cached bounds list.");

        // If measuring, we start with an empty rectangle and add as needed.
        // If doing actual layout, we start with the container's rect and subtract as we layout.
        SkiaSharp.SKRect remainingBounds = measureOnly ? SkiaSharp.SKRect.Empty : container.DisplayRectangle;
        SKSize preferredSize = SKSize.Empty;
 
        // Docking layout is order dependent. After much debate, we decided to use z-order as the
        // docking order. (Introducing a DockOrder property was a close second)
        ArrangedElementCollection children = container.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            IArrangedElement element = children[i];
            Debug.Assert(element.Bounds == GetCachedBounds(element), "Why do we have cachedBounds for a docked element?");
            if (CommonProperties.GetNeedsDockLayout(element))
            {
                // Some controls modify their bounds when you call SetBoundsCore. We
                // therefore need to read the value of bounds back when adjusting our layout rectangle.
                switch (GetDock(element))
                {
                    case DockStyle.Top:
                        {
                            SKSize elementSize = GetVerticalDockedSize(element, remainingBounds.Size, measureOnly);
                            SkiaSharp.SKRect newElementBounds = SkiaSharp.SKRect.Create(
                                remainingBounds.Left,
                                remainingBounds.Top,
                                elementSize.Width,
                                elementSize.Height);
 
                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
 
                            if (!measureOnly)
                            {
                                // What we are really doing here: top += control.Bounds.Height;
                                remainingBounds.Top += element.Bounds.Height;
                            }
                            break;
                        }
 
                    case DockStyle.Bottom:
                        {
                            SKSize elementSize = GetVerticalDockedSize(element, remainingBounds.Size, measureOnly);
                            SkiaSharp.SKRect newElementBounds = SkiaSharp.SKRect.Create(
                                remainingBounds.Left,
                                remainingBounds.Bottom - elementSize.Height,
                                elementSize.Width,
                                elementSize.Height);
 
                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
 
                            if (!measureOnly)
                            {
                                // What we are really doing here: bottom -= control.Bounds.Height;
                                remainingBounds.Bottom -= element.Bounds.Height;
                            }
 
                            break;
                        }
 
                    case DockStyle.Left:
                        {
                            SKSize elementSize = GetHorizontalDockedSize(element, remainingBounds.Size, measureOnly);
                            SkiaSharp.SKRect newElementBounds = SkiaSharp.SKRect.Create(
                                remainingBounds.Left,
                                remainingBounds.Top,
                                elementSize.Width,
                                elementSize.Height);
 
                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
 
                            if (!measureOnly)
                            {
                                // What we are really doing here: left += control.Bounds.Width;
                                remainingBounds.Left += element.Bounds.Width;
                            }
                            break;
                        }
 
                    case DockStyle.Right:
                        {
                            SKSize elementSize = GetHorizontalDockedSize(element, remainingBounds.Size, measureOnly);
                            SkiaSharp.SKRect newElementBounds = SkiaSharp.SKRect.Create(
                                remainingBounds.Right - elementSize.Width,
                                remainingBounds.Top,
                                elementSize.Width,
                                elementSize.Height);
 
                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
 
                            if (!measureOnly)
                            {
                                // What we are really doing here: right -= control.Bounds.Width;
                                remainingBounds.Right -= element.Bounds.Width;
                            }
                            break;
                        }
 
                    case DockStyle.Fill:
                        {
                            SKSize elementSize = remainingBounds.Size;
                            SkiaSharp.SKRect newElementBounds = SkiaSharp.SKRect.Create(
                                remainingBounds.Left,
                                remainingBounds.Top,
                                elementSize.Width,
                                elementSize.Height);
 
                            TryCalculatePreferredSizeDockedControl(element, newElementBounds, measureOnly, ref preferredSize, ref remainingBounds);
                        }
 
                        break;
                    default:
                        Debug.Fail("Unsupported value for dock.");
                        break;
                }
            }
        }
 
        return preferredSize;
    }
 
    /// <summary>
    ///  Helper method that either sets the control bounds or does the preferredSize computation based on
    ///  the value of measureOnly.
    /// </summary>
    private static void TryCalculatePreferredSizeDockedControl(IArrangedElement element, SkiaSharp.SKRect newElementBounds, bool measureOnly, ref SKSize preferredSize, ref SkiaSharp.SKRect remainingBounds)
    {
        if (measureOnly)
        {
            SKSize neededSize = new(
                Math.Max(0, newElementBounds.Width - remainingBounds.Width),
                Math.Max(0, newElementBounds.Height - remainingBounds.Height));
 
            DockStyle dockStyle = GetDock(element);
            if (dockStyle is DockStyle.Top or DockStyle.Bottom)
            {
                neededSize.Width = 0;
            }
 
            if (dockStyle is DockStyle.Left or DockStyle.Right)
            {
                neededSize.Height = 0;
            }
 
            if (dockStyle != DockStyle.Fill)
            {
                preferredSize += neededSize;
                remainingBounds.Size += neededSize;
            }
            else if (dockStyle == DockStyle.Fill && CommonProperties.GetAutoSize(element))
            {
                SKSize elementPrefSize = element.GetPreferredSize(neededSize);
                remainingBounds.Size += elementPrefSize;
                preferredSize += elementPrefSize;
            }
        }
        else
        {
            element.SetBounds(newElementBounds, BoundsSpecified.None);
 
#if DEBUG
            var control = (ElementBase)element;
            newElementBounds.Size = control.ApplySizeConstraints(newElementBounds.Size);
 
            // This usually happens when a Control overrides its SetBoundsCore or sets size during OnResize
            // to enforce constraints like AutoSize. Generally you can just move this code to Control.GetAdjustedSize
            // and then PreferredSize will also pick up these constraints. See ComboBox as an example.
            if (CommonProperties.GetAutoSize(element) && !CommonProperties.GetSelfAutoSizeInDefaultLayout(element))
            {
                Debug.Assert(
                    (newElementBounds.Width < 0 || element.Bounds.Width == newElementBounds.Width) &&
                    (newElementBounds.Height < 0 || element.Bounds.Height == newElementBounds.Height),
                    "Element modified its bounds during docking -- PreferredSize will be wrong. See comment near this assert.");
            }
#endif
        }
    }
 
    private static SKSize GetVerticalDockedSize(IArrangedElement element, SKSize remainingSize, bool measureOnly)
    {
        SKSize newSize = xGetDockedSize(element, /* constraints = */ new SKSize(remainingSize.Width, 1));
        if (!measureOnly)
        {
            newSize.Width = remainingSize.Width;
        }
        else
        {
            newSize.Width = Math.Max(newSize.Width, remainingSize.Width);
        }
 
        Debug.Assert((measureOnly && (newSize.Width >= remainingSize.Width)) || (newSize.Width == remainingSize.Width),
            "Error detected in GetVerticalDockedSize: Dock size computed incorrectly during layout.");
        return newSize;
    }
 
    private static SKSize GetHorizontalDockedSize(IArrangedElement element, SKSize remainingSize, bool measureOnly)
    {
        SKSize newSize = xGetDockedSize(element, /* constraints = */ new SKSize(1, remainingSize.Height));
        if (!measureOnly)
        {
            newSize.Height = remainingSize.Height;
        }
        else
        {
            newSize.Height = Math.Max(newSize.Height, remainingSize.Height);
        }
 
        Debug.Assert((measureOnly && (newSize.Height >= remainingSize.Height)) || (newSize.Height == remainingSize.Height),
            "Error detected in GetHorizontalDockedSize: Dock size computed incorrectly during layout.");
        return newSize;
    }
 
    private static SKSize xGetDockedSize(IArrangedElement element, SKSize constraints)
    {
        constraints = new SKSize(
            Math.Max(0, constraints.Width),
            Math.Max(0, constraints.Height));

        SKSize desiredSize;
        if (CommonProperties.GetAutoSize(element))
        {
            // Ask control for its desired size using the provided constraints.
            // (e.g., a control docked to top will constrain width to remaining width
            // and minimize height.)
            desiredSize = element.GetPreferredSize(constraints);
        }
        else
        {
            desiredSize = element.Bounds.Size;
        }

        if (desiredSize.Width < 0 || desiredSize.Height < 0)
        {
            Debug.WriteLine($"[DefaultLayout] Clamping negative dock size for '{element}': {desiredSize} with constraints {constraints}.");
            desiredSize = new SKSize(
                Math.Max(0, desiredSize.Width),
                Math.Max(0, desiredSize.Height));
        }
 
        Debug.Assert(desiredSize.Width >= 0 && desiredSize.Height >= 0, "Error detected in xGetDockSize: Element size was negative.");
        return desiredSize;
    }
 
    private protected override bool LayoutCore(IArrangedElement container, LayoutEventArgs args)
    {
        return TryCalculatePreferredSize(container, measureOnly: false, preferredSize: out SKSize _);
    }
 
    /// <remarks>
    ///  <para>PreferredSize is only computed if measureOnly = true.</para>
    /// </remarks>
    private static bool TryCalculatePreferredSize(IArrangedElement container, bool measureOnly, out SKSize preferredSize)
    {
        ArrangedElementCollection children = container.Children;
        // PreferredSize is garbage unless measureOnly is specified
        preferredSize = new SKSize(-7103, -7105);
 
        // Short circuit for items with no children
        if (!measureOnly && children.Count == 0)
        {
            return CommonProperties.GetAutoSize(container);
        }
 
        bool dock = false;
        bool anchor = false;
        bool autoSize = false;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            IArrangedElement element = children[i];
            if (CommonProperties.GetNeedsDockAndAnchorLayout(element))
            {
                if (!dock && CommonProperties.GetNeedsDockLayout(element))
                {
                    dock = true;
                }
 
                if (!anchor && CommonProperties.GetNeedsAnchorLayout(element))
                {
                    anchor = true;
                }
 
                if (!autoSize && CommonProperties.xGetAutoSizedAndAnchored(element))
                {
                    autoSize = true;
                }
            }
        }
 
        SKSize preferredSKSizeorDocking = SKSize.Empty;
        SKSize preferredSKSizeorAnchoring;
 
        if (dock)
        {
            preferredSKSizeorDocking = LayoutDockedControls(container, measureOnly);
        }
 
        if (anchor && !measureOnly)
        {
            // In the case of anchors, where we currently are defines the preferred size,
            // so don't recalculate the positions of everything.
            LayoutAnchoredControls(container);
        }
 
        if (autoSize)
        {
            LayoutAutoSizedControls(container);
        }
 
        if (!measureOnly)
        {
            // Set the anchored controls to their computed positions.
            ApplyCachedBounds(container);
        }
        else
        {
            // Finish the preferredSize computation and clear cached anchored positions.
            preferredSKSizeorAnchoring = GetAnchorPreferredSize(container);
 
            Thickness containerPadding;
            if (container is ElementBase control)
            {
                // Calling this will respect Control.DefaultPadding.
                containerPadding = control.Padding;
            }
            else
            {
                // Not likely to happen but handle this gracefully.
                containerPadding = CommonProperties.GetPadding(container, Thickness.Empty);
            }
 
            preferredSKSizeorAnchoring.Width -= containerPadding.Left;
            preferredSKSizeorAnchoring.Height -= containerPadding.Top;
 
            ClearCachedBounds(container);
            preferredSize = LayoutUtils.UnionSizes(preferredSKSizeorDocking, preferredSKSizeorAnchoring);
        }
 
        return CommonProperties.GetAutoSize(container);
    }
 
    /// <summary>
    ///  Updates the control's anchors information based on the control's current bounds.
    /// </summary>
    private static void UpdateAnchorInfo(IArrangedElement element)
    {
        Debug.Assert(!HasCachedBounds(element.Container), "Do not call this method with an active cached bounds list.");
 
        if (element.Container is null)
        {
            return;
        }
 
        AnchorInfo? anchorInfo = GetAnchorInfo(element);
        if (anchorInfo is null)
        {
            anchorInfo = new AnchorInfo();
            SetAnchorInfo(element, anchorInfo);
        }

        SkiaSharp.SKRect cachedBounds = GetCachedBounds(element);
        AnchorInfo oldAnchorInfo = new()
        {
            Left = anchorInfo.Left,
            Top = anchorInfo.Top,
            Right = anchorInfo.Right,
            Bottom = anchorInfo.Bottom
        };

        SkiaSharp.SKRect elementBounds = element.Bounds;
        anchorInfo.Left = elementBounds.Left;
        anchorInfo.Top = elementBounds.Top;
        anchorInfo.Right = elementBounds.Right;
        anchorInfo.Bottom = elementBounds.Bottom;

        SkiaSharp.SKRect parentDisplayRect = element.Container.DisplayRectangle;
        var parentWidth = parentDisplayRect.Width;
        var parentHeight = parentDisplayRect.Height;
 
        // The anchors is relative to the parent DisplayRectangle, so offset the anchors
        // by the DisplayRect origin
        anchorInfo.Left -= parentDisplayRect.Left;
        anchorInfo.Top -= parentDisplayRect.Top;
        anchorInfo.Right -= parentDisplayRect.Left;
        anchorInfo.Bottom -= parentDisplayRect.Top;
 
        AnchorStyles anchor = GetAnchor(element);
        if (IsAnchored(anchor, AnchorStyles.Right))
        {
            if ((anchorInfo.Right - parentWidth > 0) && (oldAnchorInfo.Right < 0))
            {
                // Parent was resized to fit its parent, or screen, we need to reuse old anchors info to prevent losing control beyond right edge.
                anchorInfo.Right = oldAnchorInfo.Right;
                if (!IsAnchored(anchor, AnchorStyles.Left))
                {
                    // Control might have been resized, update Left anchors.
                    anchorInfo.Left = oldAnchorInfo.Right - cachedBounds.Width;
                }
            }
            else
            {
                anchorInfo.Right -= parentWidth;
 
                if (!IsAnchored(anchor, AnchorStyles.Left))
                {
                    anchorInfo.Left -= parentWidth;
                }
            }
        }
        else if (!IsAnchored(anchor, AnchorStyles.Left))
        {
            anchorInfo.Right -= parentWidth / 2;
            anchorInfo.Left -= parentWidth / 2;
        }
 
        if (IsAnchored(anchor, AnchorStyles.Bottom))
        {
            if ((anchorInfo.Bottom - parentHeight > 0) && (oldAnchorInfo.Bottom < 0))
            {
                // The parent was resized to fit its parent or the screen, we need to reuse the old anchors info
                // to prevent positioning the control beyond the bottom edge.
                anchorInfo.Bottom = oldAnchorInfo.Bottom;
 
                if (!IsAnchored(anchor, AnchorStyles.Top))
                {
                    // The control might have been resized, update the Top anchor.
                    anchorInfo.Top = oldAnchorInfo.Bottom - cachedBounds.Height;
                }
            }
            else
            {
                anchorInfo.Bottom -= parentHeight;
 
                if (!IsAnchored(anchor, AnchorStyles.Top))
                {
                    anchorInfo.Top -= parentHeight;
                }
            }
        }
        else if (!IsAnchored(anchor, AnchorStyles.Top))
        {
            anchorInfo.Bottom -= parentHeight / 2;
            anchorInfo.Top -= parentHeight / 2;
        }
    }
 
    /// <summary>
    ///  Updates anchors calculations if the control is parented and the parent's layout is resumed.
    /// </summary>
    /// <devdoc>
    ///  This is the new behavior introduced in .NET 8.0. Refer to
    ///  https://github.com/dotnet/winforms/blob/tree/main/docs/design/anchor-layout-changes-in-net80.md for more details.
    ///  Developers may opt-out of this new behavior using switch <see cref="AppContextSwitches.AnchorLayoutV2"/>.
    /// </devdoc>
    internal static void UpdateAnchorInfoV2(ElementBase control)
    {
        if (!CommonProperties.GetNeedsAnchorLayout(control))
        {
            return;
        }
 
        var parent = control.Parent;
 
        // Check if control is ready for anchors calculation.
        if (parent is null)
        {
            return;
        }
 
        AnchorInfo? anchorInfo = GetAnchorInfo(control);
 
        // AnchorsInfo is not computed yet. Check if control is ready for AnchorInfo calculation at this time.
        if (anchorInfo is null)
        {
            // Design time scenarios suspend layout while deserializing the designer. This is an extra suspension
            // outside of serialized source and happen only in design-time scenario. Hence, checking for
            // LayoutSuspendCount > 1.
            bool ancestorInDesignMode = control.IsAncestorSiteInDesignMode;
            if ((ancestorInDesignMode && parent.LayoutSuspendCount > 1)
                || (!ancestorInDesignMode && parent.LayoutSuspendCount != 0))
            {
                // Mark parent to indicate that one of its child control requires AnchorsInfo to be calculated.
                parent._childControlsNeedAnchorLayout = true;
                return;
            }
        }
 
        if (anchorInfo is not null && !control._forceAnchorCalculations)
        {
            // Only control's Size or Parent change, prompts recalculation of anchors. Otherwise,
            // we skip updating anchors for the control.
            return;
        }
 
        if (anchorInfo is null)
        {
            anchorInfo = new AnchorInfo();
            SetAnchorInfo(control, anchorInfo);
        }
 
        // Reset parent flag as we now ready to iterate over all children requiring AnchorInfo calculation.
        parent._childControlsNeedAnchorLayout = false;

        SkiaSharp.SKRect displayRectangle = control.Parent!.DisplayRectangle;
        SkiaSharp.SKRect elementBounds = GetCachedBounds(control);
        var x = elementBounds.Left;
        var y = elementBounds.Top;
 
        anchorInfo.DisplayRectangle = displayRectangle;
        anchorInfo.Left = x;
        anchorInfo.Top = y;
 
        anchorInfo.Right = displayRectangle.Width - (x + elementBounds.Width);
        anchorInfo.Bottom = displayRectangle.Height - (y + elementBounds.Height);
    }
 
    public static AnchorStyles GetAnchor(IArrangedElement element) => CommonProperties.xGetAnchor(element);
 
    public static void SetAnchor(IArrangedElement element, AnchorStyles value)
    {
        AnchorStyles oldValue = GetAnchor(element);
        if (oldValue != value)
        {
            if (CommonProperties.GetNeedsDockLayout(element))
            {
                // We set dock back to none to cause the control to size back to its original bounds.
                SetDock(element, DockStyle.None);
            }
 
            CommonProperties.xSetAnchor(element, value);
 
            if (CommonProperties.GetNeedsAnchorLayout(element))
            {
                UpdateAnchorInfo(element);
            }
            else
            {
                SetAnchorInfo(element, value: null);
            }
 
            if (element.Container is not null)
            {
                bool rightReleased = IsAnchored(oldValue, AnchorStyles.Right) && !IsAnchored(value, AnchorStyles.Right);
                bool bottomReleased = IsAnchored(oldValue, AnchorStyles.Bottom) && !IsAnchored(value, AnchorStyles.Bottom);
                if (element.Container.Container is not null && (rightReleased || bottomReleased))
                {
                    // If the right or bottom anchors is being released, we have a special case where the control's
                    // margin may affect preferredSize where it didn't previously. Rather than do an expensive
                    // check for this in OnLayout, we just detect the case her and force a relayout.
                    LayoutTransaction.DoLayout(element.Container.Container, element, PropertyNames.Anchor);
                }
 
                LayoutTransaction.DoLayout(element.Container, element, PropertyNames.Anchor);
            }
        }
    }
 
    public static DockStyle GetDock(IArrangedElement element) => CommonProperties.xGetDock(element);
 
    public static void SetDock(IArrangedElement element, DockStyle value)
    {
        Debug.Assert(!HasCachedBounds(element.Container), "Do not call this method with an active cached bounds list.");
 
        if (GetDock(element) != value)
        {
            bool dockNeedsLayout = CommonProperties.GetNeedsDockLayout(element);
            CommonProperties.xSetDock(element, value);
 
            using (new LayoutTransaction(element.Container as ElementBase, element, PropertyNames.Dock))
            {
                // if the item is autosized, calling setbounds performs a layout, which
                // if we haven't set the anchors info properly yet makes dock/anchors layout cranky.
                if (value == DockStyle.None)
                {
                    if (dockNeedsLayout)
                    {
                        // We are transitioning from docked to not docked, restore the original bounds.
                        element.SetBounds(CommonProperties.GetSpecifiedBounds(element), BoundsSpecified.None);
 
                        // Restore Anchor information as its now relevant again.
                        if (CommonProperties.GetNeedsAnchorLayout(element))
                        {
                            UpdateAnchorInfo(element);
                        }
                    }
                }
                else
                {
                    // Now setup the new bounds.
                    element.SetBounds(CommonProperties.GetSpecifiedBounds(element), BoundsSpecified.All);
                }
            }
        }
 
        Debug.Assert(GetDock(element) == value, "Error setting Dock value.");
    }
 
    public static void ScaleAnchorInfo(IArrangedElement element, SKSize factor)
    {
        AnchorInfo? anchorInfo = GetAnchorInfo(element);
 
        // some controls don't have AnchorInfo, i.e. Panels
        if (anchorInfo is not null)
        {
            double heightFactor = factor.Height;
            double widthFactor = factor.Width;
 
            anchorInfo.Left = (int)Math.Round(anchorInfo.Left * widthFactor);
            anchorInfo.Top = (int)Math.Round(anchorInfo.Top * heightFactor);
            anchorInfo.Right = (int)Math.Round(anchorInfo.Right * widthFactor);
            anchorInfo.Bottom = (int)Math.Round(anchorInfo.Bottom * heightFactor);
 
            SetAnchorInfo(element, anchorInfo);
        }
    }
 
    private static SkiaSharp.SKRect GetCachedBounds(IArrangedElement element)
    {
        if (element.Container is { } container)
        {
            if (container.Properties.TryGetValue(s_cachedBoundsProperty, out IDictionary? dictionary))
            {
                object? bounds = dictionary[element];
                if (bounds is not null)
                {
                    return (SkiaSharp.SKRect)bounds;
                }
            }
        }
 
        return element.Bounds;
    }
 
    private static bool HasCachedBounds(IArrangedElement? container) =>
        container is not null && container.Properties.ContainsKey(s_cachedBoundsProperty);
 
    private static void ApplyCachedBounds(IArrangedElement container)
    {
        if (CommonProperties.GetAutoSize(container))
        {
            // Avoiding calling DisplayRectangle before checking AutoSize for Everett compat
            SkiaSharp.SKRect displayRectangle = container.DisplayRectangle;
            if ((displayRectangle.Width == 0) || (displayRectangle.Height == 0))
            {
                ClearCachedBounds(container);
                return;
            }
        }
 
        if (!container.Properties.TryGetValue(s_cachedBoundsProperty, out IDictionary? dictionary))
        {
            return;
        }
 
#if DEBUG
        // In debug builds, we need to modify the collection, so we add a break and an
        // outer loop to prevent attempting to IEnumerator.MoveNext() on a modified
        // collection.
        while (dictionary.Count > 0)
        {
#endif
            foreach (DictionaryEntry entry in dictionary)
            {
                IArrangedElement element = (IArrangedElement)entry.Key;
 
                Debug.Assert(element.Container == container, "We have non-children in our containers cached bounds store.");
#if DEBUG
                // We are about to set the bounds to the cached value. We clear the cached value
                // before SetBounds because some controls fiddle with the bounds on SetBounds
                // and will callback InitLayout with a different bounds and BoundsSpecified.
                dictionary.Remove(entry.Key);
#endif
                SkiaSharp.SKRect bounds = (SkiaSharp.SKRect)entry.Value!;
                element.SetBounds(bounds, BoundsSpecified.None);
#if DEBUG
                break;
            }
#endif
        }
 
        ClearCachedBounds(container);
    }
 
    private static void ClearCachedBounds(IArrangedElement container) => container.Properties.RemoveValue(s_cachedBoundsProperty);
 
    private static void SetCachedBounds(IArrangedElement element, SkiaSharp.SKRect bounds)
    {
        if (element.Container is { } container && bounds != GetCachedBounds(element))
        {
            if (!container.Properties.TryGetValue(s_cachedBoundsProperty, out IDictionary? dictionary))
            {
                dictionary = container.Properties.AddValue(s_cachedBoundsProperty, new HybridDictionary());
            }
 
            dictionary[element] = bounds;
        }
    }
 
    internal static AnchorInfo? GetAnchorInfo(IArrangedElement element) =>
        element.Properties.GetValueOrDefault<AnchorInfo>(s_layoutInfoProperty);
 
    internal static void SetAnchorInfo(IArrangedElement element, AnchorInfo? value) =>
        element.Properties.AddOrRemoveValue(s_layoutInfoProperty, value);
 
    private protected override void InitLayoutCore(IArrangedElement element, BoundsSpecified specified)
    {
        Debug.Assert(specified == BoundsSpecified.None || GetCachedBounds(element) == element.Bounds,
            "Attempt to InitLayout while element has active cached bounds.");
 
        if (specified != BoundsSpecified.None &&
            (CommonProperties.GetNeedsAnchorLayout(element) && ((ElementBase)element)._childControlsNeedAnchorLayout))
        {
            UpdateAnchorInfo(element);
        }
    }
 
    internal override SKSize GetPreferredSize(IArrangedElement container, SKSize proposedBounds)
    {
        Debug.Assert(!HasCachedBounds(container), "Do not call this method with an active cached bounds list.");
 
        TryCalculatePreferredSize(container, measureOnly: true, preferredSize: out SKSize prefSize);
        return prefSize;
    }
 
    private static SKSize GetAnchorPreferredSize(IArrangedElement container)
    {
        SKSize prefSize = SKSize.Empty;
 
        ArrangedElementCollection children = container.Children;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            IArrangedElement element = container.Children[i];
            if (!CommonProperties.GetNeedsDockLayout(element) && element.ParticipatesInLayout)
            {
                AnchorStyles anchor = GetAnchor(element);
                Thickness margin = CommonProperties.GetMargin(element);
                SkiaSharp.SKRect elementSpace = LayoutUtils.InflateRect(GetCachedBounds(element), margin);
 
                if (IsAnchored(anchor, AnchorStyles.Left) && !IsAnchored(anchor, AnchorStyles.Right))
                {
                    // If we are anchored to the left we make sure the container is large enough not to clip us
                    // (unless we are right anchored, in which case growing the container will just resize us.)
                    prefSize.Width = Math.Max(prefSize.Width, elementSpace.Right);
                }
 
                if (!IsAnchored(anchor, AnchorStyles.Bottom))
                {
                    // If we are anchored to the top we make sure the container is large enough not to clip us
                    // (unless we are bottom anchored, in which case growing the container will just resize us.)
                    prefSize.Height = Math.Max(prefSize.Height, elementSpace.Bottom);
                }
 
                if (IsAnchored(anchor, AnchorStyles.Right))
                {
                    AnchorInfo? anchorInfo = GetAnchorInfo(element);
                    SkiaSharp.SKRect bounds = GetCachedBounds(element);
                    prefSize.Width = Math.Max(prefSize.Width, anchorInfo is null ? bounds.Right : bounds.Right + anchorInfo.Right);
                }
 
                if (IsAnchored(anchor, AnchorStyles.Bottom))
                {
                    // If we are right anchored, see what the anchors distance between our right edge and
                    // the container is, and make sure our container is large enough to accomodate us.
                    SkiaSharp.SKRect anchorDest = GetAnchorDestination(element, SkiaSharp.SKRect.Empty, measureOnly: true);
                    AnchorInfo? anchorInfo = GetAnchorInfo(element);
                    SkiaSharp.SKRect bounds = GetCachedBounds(element);
                    prefSize.Height = Math.Max(prefSize.Height, anchorInfo is null ? bounds.Bottom : bounds.Bottom + anchorInfo.Bottom);
                }
            }
        }
 
        return prefSize;
    }
 
    public static bool IsAnchored(AnchorStyles anchor, AnchorStyles desiredAnchor)
    {
        return (anchor & desiredAnchor) == desiredAnchor;
    }
}
