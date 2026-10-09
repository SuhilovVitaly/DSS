using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>UI-owned bounded cache. Workers decode; only Pump publishes ready bitmaps.</summary>
internal sealed class AsyncObjectImageCache : IDisposable
{
    private const int Capacity = 128, PendingCapacity = 16;
    private readonly Dictionary<string, SKBitmap?> _ready = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Task<SKBitmap?>> _pending = new(StringComparer.Ordinal);
    private readonly Queue<string> _order = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _decoderSlot = new(1);
    private readonly Func<string, SKBitmap?> _decode;
    private bool _disposed;

    internal AsyncObjectImageCache(Func<string, SKBitmap?> decode) => _decode = decode;
    internal int ReadyCount => _ready.Count;
    internal int PendingCount => _pending.Count;
    internal Task PendingWork => Task.WhenAll(_pending.Values);
    internal Task ShutdownTask { get; private set; } = Task.CompletedTask;
    internal SKBitmap? Get(string path) => _ready.GetValueOrDefault(path);

    internal void Request(string path)
    {
        if (_disposed || _ready.ContainsKey(path) || _pending.ContainsKey(path) || _pending.Count >= PendingCapacity) return;
        var token = _stop.Token;
        _pending.Add(path, Task.Run(async () =>
        {
            SKBitmap? result = null;
            bool entered = false;
            try
            {
                await _decoderSlot.WaitAsync(token).ConfigureAwait(false);
                entered = true;
                token.ThrowIfCancellationRequested();
                result = _decode(path);
                if (token.IsCancellationRequested) { result?.Dispose(); return null; }
                return result;
            }
            catch (OperationCanceledException) { result?.Dispose(); return null; }
            catch (Exception ex)
            {
                result?.Dispose();
                try { InterfaceLog.Write($"Object image unavailable: {path}: {ex.Message}"); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                return null;
            }
            finally { if (entered) _decoderSlot.Release(); }
        }));
    }

    internal void Pump()
    {
        if (_disposed) return;
        foreach (var entry in _pending.Where(p => p.Value.IsCompleted).ToArray())
        {
            _pending.Remove(entry.Key);
            if (_ready.Count == Capacity)
            {
                string oldest = _order.Dequeue();
                _ready[oldest]?.Dispose();
                _ready.Remove(oldest);
            }
            _ready.Add(entry.Key, entry.Value.GetAwaiter().GetResult());
            _order.Enqueue(entry.Key);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stop.Cancel();
        foreach (var bitmap in _ready.Values) bitmap?.Dispose();
        _ready.Clear();
        _order.Clear();
        // A non-cancellable native decoder may finish later. Its result is disposed,
        // never posted to a screen that has been removed. No UI-thread blocking.
        var work = _pending.Values.ToArray();
        _pending.Clear();
        ShutdownTask = Task.WhenAll(work).ContinueWith(completed =>
        {
            foreach (var bitmap in completed.Result) bitmap?.Dispose();
            _decoderSlot.Dispose();
            _stop.Dispose();
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
