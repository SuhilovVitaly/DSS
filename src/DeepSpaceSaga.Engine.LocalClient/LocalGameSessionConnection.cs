using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// In-process adapter between Client and Engine.
/// Starts the engine loop and forwards snapshots to the client via Channel.
/// Replaceable by NetworkGameSessionConnection without client changes.
/// </summary>
public sealed class LocalGameSessionConnection : IGameSessionConnection
{
    private readonly SimulationEngine _engine;
    private readonly Channel<AuthoritativeSnapshot> _snapshotChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _engineLoopTask;
    private bool _disposed;

    public LocalGameSessionConnection(SimulationEngine engine)
    {
        _engine = engine;
        _snapshotChannel = Channel.CreateUnbounded<AuthoritativeSnapshot>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        _engineLoopTask = Task.Run(() => RunEngineLoopAsync(_cts.Token));
    }

    private async Task RunEngineLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var snapshot in _engine.RunAsync(ct))
            {
                await _snapshotChannel.Writer.WriteAsync(snapshot, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _snapshotChannel.Writer.TryComplete();
        }
    }

    public ValueTask SendCommandAsync(
        PlayerCommand command,
        CancellationToken cancellationToken = default)
    {
        _engine.ReceiveCommand(command);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var snapshot in _snapshotChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return snapshot;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cts.Cancel();

        try { await _engineLoopTask; } catch (OperationCanceledException) { }

        _cts.Dispose();
        (_engine as IDisposable)?.Dispose();
    }
}
