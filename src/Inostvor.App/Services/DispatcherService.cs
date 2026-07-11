using Microsoft.UI.Dispatching;
using Inostvor.Core.Abstractions;

namespace Inostvor.App.Services;

/// <summary>WinUI implementacija <see cref="IDispatcherService"/> nad DispatcherQueue UI threada.</summary>
public sealed class DispatcherService : IDispatcherService
{
    private readonly DispatcherQueue _queue;

    public DispatcherService(DispatcherQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        _queue = queue;
    }

    public bool HasThreadAccess => _queue.HasThreadAccess;

    public void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_queue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _queue.TryEnqueue(() => action());
        }
    }
}
