// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using SkiaSharp;

namespace Orivy.Layout;

internal partial class LayoutUtils
{
    private struct PreferredSizeCache
    {
        public SKSize ConstrainingSize;

        public SKSize PreferredSize;

        public PreferredSizeCache(SKSize constrainingSize, SKSize preferredSize)
        {
            ConstrainingSize = constrainingSize;
            PreferredSize = preferredSize;
        }
    }
}