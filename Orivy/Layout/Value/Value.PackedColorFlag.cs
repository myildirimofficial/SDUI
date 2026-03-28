// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SkiaSharp;


namespace Orivy;

internal readonly partial struct Value
{
    private sealed class PackedColorFlag : TypeFlag<SKColor>
    {
        public static PackedColorFlag Instance { get; } = new();

        public override SKColor To(in Value value)
            => value._union.PackedColor.Extract();
    }
}