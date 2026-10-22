using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

internal sealed record DebugTuningFileStatus(long Revision, DateTimeOffset? WrittenAtUtc, string? Error);

/// <summary>Confirmed revisions only; serialized, coalesced, atomic disk writes outside rendering.</summary>
internal sealed class DebugTuningFileWriter : IAsyncDisposable
{
    private sealed record Request(DebugTuningSnapshot? Snapshot, TaskCompletionSource<bool>? Barrier = null);
    private readonly Channel<Request> _requests = Channel.CreateUnbounded<Request>(new() { SingleReader = true });
    private readonly object _gate = new();
    private readonly Task _worker;
    private readonly Action<string, string> _commit;
    private readonly Action<string>? _warning;
    private DebugTuningSnapshot? _latestObserved;
    private DebugTuningFileStatus _status = new(-1, null, null);
    private bool _active;
    private int _disposed;
    internal string DirectoryPath { get; }
    internal string CurrentPath => Path.Combine(DirectoryPath, "debug-tuning.json");
    internal DebugTuningFileStatus Status => Volatile.Read(ref _status);

    internal DebugTuningFileWriter(string? directory = null, bool active = true,
        Action<string, string>? commit = null, Action<string>? warning = null)
    {
        DirectoryPath = directory ?? Path.Combine(AppContext.BaseDirectory, "DebugTuning");
        _active = active;
        _commit = commit ?? ((temporary, destination) => File.Move(temporary, destination, overwrite: true));
        _warning = warning;
        _worker = Task.Run(WriteLoopAsync);
    }

    internal void Observe(AuthoritativeSnapshot? snapshot)
    {
        if (snapshot?.DebugTuning is { } tuning) Observe(tuning);
    }

    internal void Observe(DebugTuningSnapshot snapshot)
    {
        if (snapshot.Values.IsDefault || snapshot.Defaults.IsDefault) return;
        lock (_gate)
        {
            if (_disposed != 0 || _latestObserved is { } previous && previous.Revision >= snapshot.Revision) return;
            _latestObserved = snapshot;
            if (_active && snapshot.Revision > 0) _requests.Writer.TryWrite(new(snapshot));
        }
    }

    internal void Activate()
    {
        lock (_gate)
        {
            if (_disposed != 0 || _active) return;
            _active = true;
            if (_latestObserved is { Revision: > 0 } snapshot) _requests.Writer.TryWrite(new(snapshot));
        }
    }

    internal async Task DeactivateAsync()
    {
        lock (_gate) _active = false;
        await FlushAsync().ConfigureAwait(false);
    }

    internal Task FlushAsync()
    {
        var barrier = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_requests.Writer.TryWrite(new(null, barrier))) return _worker;
        return barrier.Task;
    }

    private async Task WriteLoopAsync()
    {
        DebugTuningSnapshot? pending = null;
        long attemptedAt = 0;
        var barriers = new List<TaskCompletionSource<bool>>();
        try
        {
            while (await _requests.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                Drain();
                if (pending is not null && pending.Revision != Status.Revision)
                {
                    double wait;
                    while (attemptedAt != 0 && (wait = 250 - Stopwatch.GetElapsedTime(attemptedAt).TotalMilliseconds) > 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(Math.Ceiling(wait))).ConfigureAwait(false);
                    Drain();
                    try
                    {
                        var written = DateTimeOffset.UtcNow;
                        WriteAtomic(CurrentPath, DebugTuningFileFormat.Serialize(pending.Values, written), _commit);
                        Volatile.Write(ref _status, new(pending.Revision, written, null));
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                    {
                        Volatile.Write(ref _status, Status with { Error = error.Message });
                        _warning?.Invoke("Debug tuning file write failed: " + CurrentPath + ": " + error.Message);
                    }
                    attemptedAt = Stopwatch.GetTimestamp();
                }
                foreach (var barrier in barriers) barrier.TrySetResult(true);
                barriers.Clear();
            }
        }
        finally
        {
            foreach (var barrier in barriers) barrier.TrySetResult(true);
            while (_requests.Reader.TryRead(out var request)) request.Barrier?.TrySetResult(true);
        }
        void Drain()
        {
            while (_requests.Reader.TryRead(out var request))
            {
                if (request.Snapshot is { } snapshot) pending = snapshot;
                if (request.Barrier is { } barrier) barriers.Add(barrier);
            }
        }
    }

    internal static void WriteAtomic(string path, string json, Action<string, string>? commit = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (commit is not null) commit(temporary, path);
            else File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    internal static void WriteDefaults(string directory, DebugTuningSnapshot catalog) =>
        WriteAtomic(Path.Combine(directory, "debug-tuning.defaults.json"),
            DebugTuningFileFormat.Serialize(catalog.Defaults, DateTimeOffset.UtcNow, catalog: true));

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) { await _worker.ConfigureAwait(false); return; }
        bool flush;
        lock (_gate) { flush = _active; _active = false; }
        if (flush) await FlushAsync().ConfigureAwait(false);
        _requests.Writer.TryComplete();
        await _worker.ConfigureAwait(false);
    }
}
