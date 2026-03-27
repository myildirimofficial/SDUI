using System;
using System.Threading;

namespace SDUI.Binding;

public sealed class BindingHandle : IDisposable
{
    private Action? _disposeAction;

    internal BindingHandle(Action disposeAction)
    {
        _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }
}