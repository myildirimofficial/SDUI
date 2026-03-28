using System;

namespace Orivy.Layout;

internal struct NullLayoutTransaction : IDisposable
{
    public readonly void Dispose()
    {
    }
}