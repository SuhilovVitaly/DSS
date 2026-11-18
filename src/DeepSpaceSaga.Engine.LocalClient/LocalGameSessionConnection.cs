using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// In-process adapter between Client and Engine.
/// Starts the engine loop and forwards snapshots to the client via Channel.
/// Replaceable by NetworkGameSessionConnection without client changes.
/// </summary>
public sealed partial class LocalGameSessionConnection : IGameSessionConnection
{
    private readonly SimulationEngine _engine;
    private readonly SnapshotMailbox _snapshots = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _engineLoopTask;
    private readonly string? _saveDirectory;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    internal Action<string>? SaveStageForTests { get; set; }
    internal Func<string, string, CancellationToken, Task>? WriteSaveTempForTests { get; set; }
    // Owned-exit receipts by BattleId and the subset whose continuation checkpoint reached disk.
    // Guarded by _checkpointGate, which serializes the engine call + storage phase so concurrent
    // duplicate exits settle exactly once.
    private readonly Dictionary<string, TacticalBattleResponse> _settledExits = new(StringComparer.Ordinal);
    private readonly HashSet<string> _checkpointedBattles = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _checkpointGate = new(1, 1);
    private volatile bool _disposed;
    private readonly TaskCompletionSource _snapshotReaderStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
        _engineLoopTask = Task.Run(() => RunEngineLoopAsync(_cts.Token));
    }

    public static LocalGameSessionConnection CreateFromSettingsFile(string settingsPath, string? saveDirectory = null,
        string? debugTuningPath = null, Action<string>? startupWarning = null)
    {
        var engine = SimulationEngine.CreateFromSettingsFile(settingsPath);
        engine.ApplyStartupDebugTuningFile(debugTuningPath ?? DefaultDebugTuningPath, startupWarning);
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
        string settingsPath, string savePath, string? saveDirectory = null, string? debugTuningPath = null, Action<string>? startupWarning = null)
    {
        var engine = SimulationEngine.CreateFromSaveFile(settingsPath, savePath);
        engine.ApplyStartupDebugTuningFile(debugTuningPath ?? DefaultDebugTuningPath, startupWarning);
        return new LocalGameSessionConnection(engine, saveDirectory ?? Path.GetDirectoryName(savePath));
    }

    /// <summary>
    /// Bootstrap a new connection from an explicitly chosen scenario file (the New Game -&gt;
    /// scenario picker path) instead of settings' defaultScenario. Mirrors
    /// CreateFromSettingsFile — unlike CreateFromSaveFile, saveDirectory is NOT defaulted
    /// from scenarioPath's directory (a scenario file under Scenarios/ is not a place to
    /// write future saves into); callers pass the real Saves/ directory explicitly, same as
    /// CreateFromSettingsFile's callers do.
    /// </summary>
    public static LocalGameSessionConnection CreateFromScenarioFile(
        string settingsPath, string scenarioPath, string? saveDirectory = null, string? debugTuningPath = null, Action<string>? startupWarning = null)
    {
        var engine = SimulationEngine.CreateFromScenarioFile(settingsPath, scenarioPath);
        engine.ApplyStartupDebugTuningFile(debugTuningPath ?? DefaultDebugTuningPath, startupWarning);
        return new LocalGameSessionConnection(engine, saveDirectory);
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
    private static string DefaultDebugTuningPath => Path.Combine(AppContext.BaseDirectory, "DebugTuning", "debug-tuning.json");
    public static DebugTuningSnapshot LoadDebugTuningDefaults(string settingsPath) =>
        Content.EngineContentLoader.LoadDebugTuningDefaults(settingsPath);

    public static DemoDebugSettings ConfigurePirateSquadChance(int? percent = null) =>
        SimulationEngine.ConfigurePirateSquadChance(percent);

    // Process state is consulted by Engine before the first publication, including loaded sessions.
    public static DemoDebugSettings ConfigureFogOfWar(bool? enabled = null) =>
        SimulationEngine.ConfigureFogOfWar(enabled);

    private async Task RunEngineLoopAsync(CancellationToken ct)
    {
        try
        {
            await _snapshotReaderStarted.Task.WaitAsync(ct).ConfigureAwait(false);
            await foreach (var snapshot in _engine.RunAsync(ct).ConfigureAwait(false))
                _snapshots.Publish(snapshot);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception error)
        {
            _snapshots.Complete(error);
        }
        finally
        {
            _snapshots.Complete();
        }
    }

    public ValueTask SendCommandAsync(
        PlayerCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _engine.ReceiveCommand(command);
        return ValueTask.CompletedTask;
    }

    public ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default) =>
        TacticalCall(_engine.GetTacticalBattleOptions, cancellationToken);
    public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) =>
        TacticalCall(_engine.GetTacticalBattleState, cancellationToken);
    public async ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default)
    {
        // Keep a recoverable living world even for the first tutorial sortie.
        if (request is not null && request.SourceShipId is not null && !string.IsNullOrWhiteSpace(_saveDirectory) && _engine.GetTacticalBattleState() is null)
            await SaveAsync(SaveSlotNaming.ContinuationSlotId, cancellationToken).ConfigureAwait(false);
        return await TacticalCall(() => _engine.StartTacticalBattle(request!), cancellationToken).ConfigureAwait(false);
    }
    public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        TacticalLootCall(() => _engine.MoveTacticalBattle(request), cancellationToken);
    public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        TacticalLootCall(() => _engine.FireTacticalBattle(request), cancellationToken);
    public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default) =>
        TacticalLootCall(() => _engine.DebugDestroyTacticalUnit(request), cancellationToken);

    private string? _pendingLootCheckpoint;
    private async ValueTask<TacticalBattleResponse> TacticalLootCall(Func<TacticalBattleResponse> action, CancellationToken token)
    {
        await _checkpointGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pendingLootCheckpoint is { } pending) await WriteContinuationCheckpointAsync(pending, token).ConfigureAwait(false);
            var response = action();
            if (response.Accepted && response.Settlement is { } settlement &&
                !_checkpointedBattles.Contains(settlement.BattleId) &&
                _engine.CaptureSnapshot().DemoCampaign?.Rewards?.Loot is { Phase: LootPhases.Open } loot &&
                loot.BattleId == settlement.BattleId)
            {
                _pendingLootCheckpoint = settlement.BattleId;
                _engine.SetEncounterStoragePending(true);
                await WriteContinuationCheckpointAsync(settlement.BattleId, token).ConfigureAwait(false);
            }
            return response;
        }
        finally { _checkpointGate.Release(); }
    }

    /// <summary>
    /// Exit the active battle and own the campaign-continuation storage phase (EP-0029 US-0012).
    /// An owned sortie settles exactly once in the engine; its accepted receipt is cached by
    /// BattleId, and the post-commit world is written atomically to the reserved continuation slot
    /// (see <see cref="SaveAsync"/>). Repeating the same BattleId — even with a new CommandId —
    /// retries only a previously failed checkpoint write and never re-settles. A storage failure
    /// after the commit surfaces as a recoverable <see cref="IOException"/> while the cached
    /// receipt stays retryable; diagnostic (non-owned) exits and rejections pass through unchanged.
    /// </summary>
    public async ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (request is null)
            return _engine.ExitTacticalBattle(request!);
        if (string.IsNullOrEmpty(request.BattleId))
            return _engine.ExitTacticalBattle(request); // engine rejects an unroutable battle id

        await _checkpointGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pendingLootCheckpoint is { } pending) await WriteContinuationCheckpointAsync(pending, cancellationToken).ConfigureAwait(false);
            if (_settledExits.TryGetValue(request.BattleId, out var accepted))
            {
                if (!_checkpointedBattles.Contains(request.BattleId))
                    await WriteContinuationCheckpointAsync(request.BattleId, cancellationToken).ConfigureAwait(false);
                return accepted;
            }

            bool campaignBattle = _engine.CaptureSnapshot().DemoCampaign?.WorldEncounter is { Phase: WorldEncounterPhases.PendingBattle };
            if (campaignBattle) _engine.SetEncounterStoragePending(true);
            var response = _engine.ExitTacticalBattle(request);
            if (!response.Accepted || response.Settlement is null)
            {
                if (campaignBattle) _engine.SetEncounterStoragePending(false);
                return response;
            }

            // Cache before the write: a failed or cancelled checkpoint must stay retryable and
            // must never reach the engine (and its loss/settlement) a second time.
            _settledExits[request.BattleId] = response;
            await WriteContinuationCheckpointAsync(request.BattleId, cancellationToken).ConfigureAwait(false);
            return response;
        }
        finally
        {
            _checkpointGate.Release();
        }
    }

    /// <summary>
    /// Write the continuation checkpoint for an already-settled owned exit. SaveAsync captures the
    /// post-commit world under <see cref="SimulationEngine.CaptureSaveState"/>, which the engine
    /// refuses while a battle is active, so this must run only after ExitTacticalBattle cleared
    /// the session. Legacy/test connections without a save directory have nothing to write and
    /// are marked checkpointed.
    /// </summary>
    private async ValueTask WriteContinuationCheckpointAsync(string battleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_saveDirectory))
        {
            _checkpointedBattles.Add(battleId);
            _pendingLootCheckpoint = null;
            _pendingEncounterCheckpoint = null;
            _engine.SetEncounterStoragePending(false);
            return;
        }

        try
        {
            await SaveAsync(SaveSlotNaming.ContinuationSlotId, cancellationToken).ConfigureAwait(false);
            _checkpointedBattles.Add(battleId);
            _pendingLootCheckpoint = null;
            _pendingEncounterCheckpoint = null;
            _engine.SetEncounterStoragePending(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or OperationCanceledException or InvalidOperationException)
        {
            // The world is already committed: storage is a recoverable post-commit phase. The
            // Client maps this IOException to an Unavailable receipt instead of an engine rejection.
            throw new IOException($"campaign checkpoint failed: {error.Message}", error);
        }
    }

    public ValueTask<CarrierCommandResponse> RearmDroneAsync(RearmDroneRequest request, CancellationToken cancellationToken = default) =>
        TacticalCall(() => _engine.RearmDrone(request), cancellationToken);

    public async ValueTask<DemoCampaignResponse> ExecuteDemoCampaignAsync(DemoCampaignCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (command is ILootCampaignCommand loot)
        {
            await _checkpointGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_pendingLootCheckpoint is { } pending) await WriteContinuationCheckpointAsync(pending, cancellationToken).ConfigureAwait(false);
                var response = _engine.ExecuteDemoCampaign(command, out bool newlyApplied);
                if (newlyApplied)
                {
                    _pendingLootCheckpoint = loot.BattleId;
                    _engine.SetEncounterStoragePending(true);
                    await WriteContinuationCheckpointAsync(loot.BattleId, cancellationToken).ConfigureAwait(false);
                }
                return response;
            }
            finally { _checkpointGate.Release(); }
        }
        if (command is BeginEncounterBattleCommand begin && !_engine.IsTacticalBattleActive)
        {
            await _checkpointGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                DemoCampaignResponse? response = null;
                await WriteSaveAsync(SaveSlotNaming.ContinuationSlotId, null, cancellationToken,
                    capture: () => _engine.CapturePendingEncounterCheckpoint(begin),
                    afterWrite: saved =>
                    {
                        _pendingEncounterCheckpoint = saved;
                        response = _engine.ExecuteDemoCampaign(begin);
                    }).ConfigureAwait(false);
                return response!;
            }
            finally { _checkpointGate.Release(); }
        }
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _engine.ExecuteDemoCampaign(command);
        }, cancellationToken).ConfigureAwait(false);
    }

    private ValueTask<T> TacticalCall<T>(Func<T> action, CancellationToken token)
    {
        // Engine methods serialize against the same lock as world ticks. The adapter owns no battle state.
        if (token.IsCancellationRequested) return ValueTask.FromCanceled<T>(token);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ValueTask.FromResult(action());
        }
        catch (Exception error) { return ValueTask.FromException<T>(error); }
    }

    /// <summary>
    /// Thin request-response adapter: returns the exact quote the engine issued. The engine guards its world
    /// state and quote cache itself, so there is no save gate, no local cache and no recomputation here.
    /// </summary>
    public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(
        TradeQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ValueTask.FromResult(_engine.GetTradeQuote(request));
    }

    public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _engine.ReceiveDialogueCommand(command);
        return ValueTask.CompletedTask;
    }

    public ValueTask<StationTravelResult> TravelStationAsync(StationTravelCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ValueTask.FromResult(_engine.TravelStation(command));
    }

    public ValueTask SetSimulationSpeedAsync(
        SimulationSpeed speed,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _engine.SetSpeed(speed);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetObjectInteractionStateAsync(
        string? activeObjectId,
        string? selectedObjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        _engine.SetObjectInteractionState(activeObjectId, selectedObjectId);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _snapshotReaderStarted.TrySetResult();
        await foreach (var snapshot in _snapshots.ReadAllAsync(cancellationToken))
        {
            yield return snapshot;
        }
    }

    /// <summary>
    /// Capture the current world state (thread-safe against the background engine loop —
    /// see SimulationEngine._worldStateLock) and write it atomically to the given slot:
    /// serialize to a unique temp file, then rename over the slot's target path
    /// (saveDirectory/&lt;sanitized-slot-id&gt;.json, see SaveSlotNaming). A crash mid-write
    /// never corrupts the previous save for that slot. The gate covers capture, serialization,
    /// writing and rename: a queued save captures the world only after its predecessor commits.
    /// Failed or cancelled writes remove their own temporary file.
    /// </summary>
    public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) =>
        WriteSaveAsync(slotId, null, cancellationToken);

    private ScenarioFile? _pendingEncounterCheckpoint;
    private async ValueTask WriteSaveAsync(string slotId, long? minimumSequence, CancellationToken cancellationToken,
        Func<ScenarioFile>? capture = null, Action<ScenarioFile>? afterWrite = null)
    {
        if (string.IsNullOrWhiteSpace(_saveDirectory))
            throw new InvalidOperationException(
                "This LocalGameSessionConnection has no save directory configured.");

        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? tempPath = null;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // Capture stays inside the gate: Engine rejects a terminal world before any
            // directory/temp-file write, including saves queued while the player was alive.
            // An immutable pre-death capture may still finish its atomic write afterwards.
            if (slotId == SaveSlotNaming.ContinuationSlotId)
            {
                var snapshot = _engine.CaptureSnapshot();
                if (snapshot.Objects.Any(o => o.ObjectId == snapshot.PlayerShipObjectId && o.IsDestroyed) &&
                    snapshot.DemoCampaign?.WorldEncounter?.Phase != WorldEncounterPhases.Finished) return;
            }
            SaveStageForTests?.Invoke("before_capture");
            var saveState = capture is not null ? capture() : _engine.CaptureSaveState();
            if (capture is null && saveState.GameState.DemoCampaign?.WorldEncounter is { Phase: WorldEncounterPhases.Captured } &&
                _pendingEncounterCheckpoint is { } pending) saveState = pending;
            if (minimumSequence is { } required && (saveState.GameState.DemoCampaign?.CommitSequence ?? 0) < required)
                throw new InvalidOperationException("Requested campaign commit has not been captured.");
            string json = ScenarioLoader.Serialize(saveState);
            Directory.CreateDirectory(_saveDirectory);
            string targetPath = Path.Combine(_saveDirectory, SaveSlotNaming.ToFileName(slotId));
            tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
            if (WriteSaveTempForTests is { } write)
                await write(tempPath, json, cancellationToken).ConfigureAwait(false);
            else
                await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            SaveStageForTests?.Invoke("before_replace");
            File.Move(tempPath, targetPath, overwrite: true);
            afterWrite?.Invoke(saveState);
        }
        finally
        {
            try { if (tempPath is not null && File.Exists(tempPath)) File.Delete(tempPath); }
            finally { _saveGate.Release(); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _checkpointGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            try { await _engineLoopTask.ConfigureAwait(false); }
            finally
            {
                await _saveGate.WaitAsync().ConfigureAwait(false);
                try { _engine.Dispose(); _cts.Dispose(); }
                finally { _saveGate.Release(); }
            }
        }
        finally { _checkpointGate.Release(); }
    }
}
