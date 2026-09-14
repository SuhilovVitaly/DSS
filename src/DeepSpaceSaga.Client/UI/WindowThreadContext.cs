using System.Collections.Concurrent;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Async UI continuations are pumped exclusively by the native window thread.</summary>
internal sealed class WindowThreadContext : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    public override void Post(SendOrPostCallback d, object? state) => _queue.Enqueue((d, state));
    public override SynchronizationContext CreateCopy() => this;
    internal void Drain()
    {
        if (Environment.CurrentManagedThreadId != _threadId) throw new InvalidOperationException("Wrong window thread.");
        int count = _queue.Count;
        while (count-- > 0 && _queue.TryDequeue(out var work)) work.Callback(work.State);
    }
}
