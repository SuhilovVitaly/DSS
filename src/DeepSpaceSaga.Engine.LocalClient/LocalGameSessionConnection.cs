using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

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
    private readonly string? _savePath;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private bool _disposed;

    /// <param name="savePath">
    /// Path SaveAsync writes to. The only place that decides this path is the client's
    /// composition root (Program.cs) — this class just remembers whatever it's given.
    /// Optional so existing callers/tests that never save keep working unchanged.
    /// </param>
    public LocalGameSessionConnection(SimulationEngine engine, string? savePath = null)
    {
        _engine = engine;
        _savePath = savePath;
        _snapshotChannel = Channel.CreateUnbounded<AuthoritativeSnapshot>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        _engineLoopTask = Task.Run(() => RunEngineLoopAsync(_cts.Token));
    }

    public static LocalGameSessionConnection CreateFromSettingsFile(string settingsPath, string? savePath = null)
    {
        var engine = SimulationEngine.CreateFromSettingsFile(settingsPath);
        return new LocalGameSessionConnection(engine, savePath);
    }

    /// <summary>
    /// Bootstrap a new connection from a save file (F9 quickload path). Mirrors
    /// CreateFromSettingsFile but loads save-format JSON (gameTimeMs may be &gt; 0).
    /// </summary>
    public static LocalGameSessionConnection CreateFromSaveFile(string settingsPath, string savePath)
    {
        var engine = SimulationEngine.CreateFromSaveFile(settingsPath, savePath);
        return new LocalGameSessionConnection(engine, savePath);
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

    public ValueTask SetSimulationSpeedAsync(
        SimulationSpeed speed,
        CancellationToken cancellationToken = default)
    {
        _engine.SetSpeed(speed);
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

    /// <summary>
    /// Capture the current world state (thread-safe against the background engine loop —
    /// see SimulationEngine._worldStateLock) and write it atomically: serialize to a unique
    /// temp file, then rename over the target path. A crash mid-write never corrupts the
    /// previous save. The write itself (temp-file + rename) is additionally serialized via
    /// _saveGate — concurrent SaveAsync calls each capture their own independent snapshot,
    /// but their file writes queue up one at a time rather than racing each other's rename
    /// onto the same destination path (which Windows can reject outright, and which would
    /// otherwise make "which save actually landed" nondeterministic anyway).
    /// </summary>
    public async ValueTask SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_savePath))
            throw new InvalidOperationException(
                "This LocalGameSessionConnection has no save path configured.");

        var saveState = _engine.CaptureSaveState();
        string json = ScenarioLoader.Serialize(saveState);

        string? directory = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = $"{_savePath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);

        await _saveGate.WaitAsync(cancellationToken);
        try
        {
            File.Move(tempPath, _savePath, overwrite: true);
        }
        finally
        {
            _saveGate.Release();
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
        _saveGate.Dispose();
        (_engine as IDisposable)?.Dispose();
    }
}
