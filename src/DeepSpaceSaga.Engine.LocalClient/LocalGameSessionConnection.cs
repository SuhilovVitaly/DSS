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
    private readonly string? _saveDirectory;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private bool _disposed;

    /// <param name="saveDirectory">
    /// Directory SaveAsync writes slot files into — each slot becomes
    /// "&lt;sanitized-slot-id&gt;.json" inside it (see SaveSlotNaming). The only place
    /// that decides this directory is the client's composition root (Program.cs) —
    /// this class just remembers whatever it's given. Optional so existing
    /// callers/tests that never save keep working unchanged.
    /// </param>
    public LocalGameSessionConnection(SimulationEngine engine, string? saveDirectory = null)
    {
        _engine = engine;
        _saveDirectory = saveDirectory;
        _snapshotChannel = Channel.CreateUnbounded<AuthoritativeSnapshot>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        _engineLoopTask = Task.Run(() => RunEngineLoopAsync(_cts.Token));
    }

    public static LocalGameSessionConnection CreateFromSettingsFile(string settingsPath, string? saveDirectory = null)
    {
        var engine = SimulationEngine.CreateFromSettingsFile(settingsPath);
        return new LocalGameSessionConnection(engine, saveDirectory);
    }

    /// <summary>
    /// Bootstrap a new connection from a save file (F9 quickload path). Mirrors
    /// CreateFromSettingsFile but loads save-format JSON (gameTimeMs may be &gt; 0).
    /// saveDirectory defaults to savePath's own directory when omitted, so a bare
    /// two-argument call keeps saving future slots (e.g. a quicksave overwrite) next
    /// to the file it just loaded from.
    /// </summary>
    public static LocalGameSessionConnection CreateFromSaveFile(
        string settingsPath, string savePath, string? saveDirectory = null)
    {
        var engine = SimulationEngine.CreateFromSaveFile(settingsPath, savePath);
        return new LocalGameSessionConnection(engine, saveDirectory ?? Path.GetDirectoryName(savePath));
    }

    /// <summary>
    /// True if the save/scenario just loaded into this connection's engine had no
    /// masterSeed, so the engine generated a fresh one. Legitimate composition-root callers
    /// (Program.cs) check this after CreateFromSaveFile specifically — not after
    /// CreateFromSettingsFile, where a missing masterSeed is the expected New Game case —
    /// to decide whether to surface a legacy-save warning to InterfaceLog (Engine itself
    /// cannot reference InterfaceLog; it lives in Client).
    /// </summary>
    public bool MasterSeedWasMissingOnLoad => _engine.MasterSeedWasMissingOnLoad;

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

    public ValueTask SetObjectInteractionStateAsync(
        string? activeObjectId,
        string? selectedObjectId,
        CancellationToken cancellationToken = default)
    {
        _engine.SetObjectInteractionState(activeObjectId, selectedObjectId);
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
    /// see SimulationEngine._worldStateLock) and write it atomically to the given slot:
    /// serialize to a unique temp file, then rename over the slot's target path
    /// (saveDirectory/&lt;sanitized-slot-id&gt;.json, see SaveSlotNaming). A crash mid-write
    /// never corrupts the previous save for that slot. The write itself (temp-file +
    /// rename) is additionally serialized via _saveGate — concurrent SaveAsync calls
    /// (whether to the same slot or different slots) each capture their own independent
    /// snapshot, but their file writes queue up one at a time rather than racing each
    /// other's rename (which Windows can reject outright, and which would otherwise make
    /// "which save actually landed" nondeterministic anyway). Different slots write to
    /// different destination paths, so they never collide on disk regardless.
    /// </summary>
    public async ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_saveDirectory))
            throw new InvalidOperationException(
                "This LocalGameSessionConnection has no save directory configured.");

        var saveState = _engine.CaptureSaveState();
        string json = ScenarioLoader.Serialize(saveState);

        Directory.CreateDirectory(_saveDirectory);

        string targetPath = Path.Combine(_saveDirectory, SaveSlotNaming.ToFileName(slotId));
        string tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);

        await _saveGate.WaitAsync(cancellationToken);
        try
        {
            File.Move(tempPath, targetPath, overwrite: true);
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
