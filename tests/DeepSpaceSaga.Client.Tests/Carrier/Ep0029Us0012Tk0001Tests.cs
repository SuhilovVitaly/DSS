using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0012 TK-0001: exiting an owned sortie settles the battle once and writes the
/// campaign-continuation checkpoint (atomic temp+replace) from the post-commit world. A failed
/// write leaves the accepted settlement retryable by BattleId without re-settling, manual saves
/// are still refused while a battle is active, and a stale BattleId on a reloaded session is a
/// plain NoBattle rejection that writes nothing.
/// </summary>
public sealed class Ep0029Us0012Tk0001Tests : IDisposable
{
    private const string ContinuationFile = "campaign-continuation.json";
    private static readonly TimeSpan SnapshotTimeout = TimeSpan.FromSeconds(15);

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"dss-ep0029-us0012-tk0001-{Guid.NewGuid():N}");
    private readonly string _saveDirectory;

    public Ep0029Us0012Tk0001Tests()
    {
        _saveDirectory = Path.Combine(_directory, "Saves");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");

    private static string TutorialScenarioPath =>
        Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    private LocalGameSessionConnection CreateConnection() =>
        LocalGameSessionConnection.CreateFromScenarioFile(SettingsPath, TutorialScenarioPath, _saveDirectory);

    private static async Task<AuthoritativeSnapshot> NextSnapshotAsync(
        IAsyncEnumerator<AuthoritativeSnapshot> snapshots,
        Func<AuthoritativeSnapshot, bool>? predicate = null,
        string? context = null)
    {
        while (true)
        {
            bool hasNext = await snapshots.MoveNextAsync().AsTask().WaitAsync(SnapshotTimeout);
            Assert.True(hasNext, $"Snapshot stream ended while waiting for {context ?? "a snapshot"}.");
            if (predicate is null || predicate(snapshots.Current))
                return snapshots.Current;
        }
    }

    private static async Task<string> StartOwnedBattleAsync(
        LocalGameSessionConnection connection,
        IAsyncEnumerator<AuthoritativeSnapshot> snapshots,
        string startCommandId)
    {
        var initial = await NextSnapshotAsync(snapshots,
            snapshot => snapshot.PlayerShipObjectId == "SPC-0001" && snapshot.CarrierOperations is not null,
            "the initial carrier snapshot");
        Assert.Equal(0, initial.CarrierOperations!.Revision);

        var start = await connection.StartTacticalBattleAsync(
            new StartTacticalBattleRequest(startCommandId, 1)
            {
                SourceShipId = "SPC-0001",
                ExpectedCarrierRevision = 0,
            });

        Assert.True(start.Accepted, $"owned tutorial start rejected: {start.Code}");
        Assert.NotNull(start.State);
        Assert.Equal(startCommandId, start.State!.BattleId);
        return start.State.BattleId;
    }

    private static CarrierStateData ReadCarrier(string path) =>
        Assert.Single(
            ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true)
                .GameState.SpaceObjects,
            ship => ship.ObjectId == "SPC-0001").CarrierState!;

    [Fact]
    public async Task ExitWritesContinuationCheckpoint()
    {
        await using var connection = CreateConnection();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();

        string battleId = await StartOwnedBattleAsync(connection, snapshots, "start-exit-writes");

        var exit = await connection.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-writes", battleId));

        Assert.True(exit.Accepted);
        var settlement = Assert.IsType<SortieSettlementSnapshot>(exit.Settlement);
        Assert.Equal(battleId, settlement.BattleId);
        Assert.Equal("SPC-0001", settlement.ShipId);
        Assert.Equal(1, settlement.CarrierRevision);
        Assert.Equal(0, settlement.ConsumedTorpedoes);
        Assert.Equal(SortieExitKind.Aborted, settlement.ExitKind);

        string path = Path.Combine(_saveDirectory, ContinuationFile);
        Assert.True(File.Exists(path), $"expected the continuation checkpoint at {path}");

        var slots = SaveSlotRepository.ListSlots(_saveDirectory);
        var slot = Assert.Single(slots, candidate => candidate.SlotId == "campaign-continuation");
        Assert.Equal("Campaign Continuation", slot.DisplayName);

        var carrier = ReadCarrier(path);
        Assert.Equal(1, carrier.Revision);
        var drones = Assert.IsAssignableFrom<IReadOnlyList<OwnedDroneData>>(carrier.OwnedDrones);
        Assert.Equal(4, drones.Count);
        Assert.Equal(36, drones.Sum(drone => drone.AmmoCurrent));
        var lastSettlement = Assert.IsType<SortieSettlementData>(carrier.LastSettlement);
        Assert.Equal(battleId, lastSettlement.BattleId);
        Assert.Equal(1, lastSettlement.CarrierRevision);
        Assert.Equal(0, lastSettlement.ConsumedTorpedoes);
        Assert.Equal("Aborted", lastSettlement.ExitKind);
    }

    [Fact]
    public async Task CheckpointFailureKeepsRetryWithoutResettlement()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_saveDirectory, "blocking file");

        await using var connection = CreateConnection();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();

        string battleId = await StartOwnedBattleAsync(connection, snapshots, "start-failure");

        var error = await Assert.ThrowsAsync<IOException>(async () =>
            await connection.ExitTacticalBattleAsync(new ExitTacticalBattleRequest("exit-failure", battleId)));

        Assert.Contains("campaign checkpoint failed", error.Message, StringComparison.Ordinal);
        Assert.Null(await connection.GetTacticalBattleStateAsync());
        Assert.True(File.Exists(_saveDirectory));
        Assert.Equal("blocking file", await File.ReadAllTextAsync(_saveDirectory));

        // The engine committed before the storage phase failed: exactly one revision increment.
        var committed = await NextSnapshotAsync(snapshots,
            snapshot => snapshot.CarrierOperations is { Revision: 1 },
            "the committed carrier revision");
        Assert.Equal(1, committed.CarrierOperations!.Revision);

        File.Delete(_saveDirectory);
        var retry = await connection.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-failure-retry", battleId));

        Assert.True(retry.Accepted);
        Assert.Equal("exit-failure", retry.CommandId); // the cached receipt, not a fresh engine reply
        var settlement = Assert.IsType<SortieSettlementSnapshot>(retry.Settlement);
        Assert.Equal(battleId, settlement.BattleId);
        Assert.Equal(1, settlement.CarrierRevision);
        Assert.Equal(0, settlement.ConsumedTorpedoes);

        string path = Path.Combine(_saveDirectory, ContinuationFile);
        Assert.True(File.Exists(path), $"expected the retried checkpoint at {path}");
        var carrier = ReadCarrier(path);
        Assert.Equal(1, carrier.Revision);
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(1, carrier.LastSettlement.CarrierRevision);

        var afterRetry = await NextSnapshotAsync(snapshots,
            snapshot => snapshot.CarrierOperations is not null,
            "the carrier snapshot after the retried write");
        Assert.Equal(1, afterRetry.CarrierOperations!.Revision);
    }

    [Fact]
    public async Task RetryAfterSuccessReturnsCachedSettlement()
    {
        await using var connection = CreateConnection();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();

        string battleId = await StartOwnedBattleAsync(connection, snapshots, "start-retry-success");

        var first = await connection.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-first", battleId));
        Assert.True(first.Accepted);
        var settlement = Assert.IsType<SortieSettlementSnapshot>(first.Settlement);
        Assert.Equal(1, settlement.CarrierRevision);

        var second = await connection.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-second", battleId));

        Assert.Same(first, second);
        Assert.Equal("exit-first", second.CommandId);
        Assert.Same(first.Settlement, second.Settlement);
        Assert.Equal(0, second.Settlement!.ConsumedTorpedoes);

        var afterRetry = await NextSnapshotAsync(snapshots,
            snapshot => snapshot.CarrierOperations is not null,
            "the carrier snapshot after the cached retry");
        Assert.Equal(1, afterRetry.CarrierOperations!.Revision);

        var carrier = ReadCarrier(Path.Combine(_saveDirectory, ContinuationFile));
        Assert.Equal(1, carrier.Revision);
        Assert.Equal(battleId, carrier.LastSettlement!.BattleId);
        Assert.Equal(1, carrier.LastSettlement.CarrierRevision);
    }

    [Fact]
    public async Task ManualSaveDuringBattleRejected()
    {
        await using var connection = CreateConnection();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();

        string battleId = await StartOwnedBattleAsync(connection, snapshots, "start-manual");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await connection.SaveAsync("manual"));
        Assert.Contains("tactical_battle_active", error.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_saveDirectory));
        Assert.False(File.Exists(Path.Combine(_saveDirectory, "manual.json")));
        Assert.False(File.Exists(Path.Combine(_saveDirectory, ContinuationFile)));

        var exit = await connection.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-manual", battleId));

        Assert.True(exit.Accepted);
        Assert.True(File.Exists(Path.Combine(_saveDirectory, ContinuationFile)));
        Assert.False(File.Exists(Path.Combine(_saveDirectory, "manual.json")));
    }

    [Fact]
    public async Task ContinuationSlotIsListedAndLoadable()
    {
        await using (var connection = CreateConnection())
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            string battleId = await StartOwnedBattleAsync(connection, snapshots, "start-loadable");
            var exit = await connection.ExitTacticalBattleAsync(
                new ExitTacticalBattleRequest("exit-loadable", battleId));
            Assert.True(exit.Accepted);
        }

        string path = Path.Combine(_saveDirectory, ContinuationFile);
        Assert.True(File.Exists(path));

        var slots = SaveSlotRepository.ListSlots(_saveDirectory);
        var slot = Assert.Single(slots, candidate => candidate.SlotId == "campaign-continuation");
        Assert.Equal("Campaign Continuation", slot.DisplayName);
        Assert.Equal(ContinuationFile, SaveSlotNaming.ToFileName(slot.SlotId));

        var disk = ReadCarrier(path);
        Assert.Equal(1, disk.Revision);
        var diskAmmo = disk.OwnedDrones!.ToDictionary(drone => drone.DroneId, drone => drone.AmmoCurrent);
        Assert.Equal(4, diskAmmo.Count);
        Assert.Equal(36, diskAmmo.Values.Sum());

        await using var loaded = LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, path, _saveDirectory);
        using var loadedTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var loadedSnapshots = loaded.ReadSnapshotsAsync(loadedTimeout.Token).GetAsyncEnumerator();
        var snapshot = await NextSnapshotAsync(loadedSnapshots,
            candidate => candidate.CarrierOperations is not null,
            "the loaded carrier snapshot");

        var carrier = snapshot.CarrierOperations!;
        Assert.Equal("SPC-0001", carrier.ShipId);
        Assert.Equal(1, carrier.Revision);
        // The save carries its own expanded wing; loading never re-grants the manifest defaults.
        Assert.Equal(diskAmmo, carrier.OwnedDrones.ToDictionary(drone => drone.Id, drone => drone.AmmoCurrent));
        Assert.False(loaded.MasterSeedWasMissingOnLoad);
    }

    [Fact]
    public async Task DuplicateExitAfterReloadIsNoBattle()
    {
        string battleId;
        await using (var connection = CreateConnection())
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            battleId = await StartOwnedBattleAsync(connection, snapshots, "start-reload");
            var exit = await connection.ExitTacticalBattleAsync(
                new ExitTacticalBattleRequest("exit-reload", battleId));
            Assert.True(exit.Accepted);
        }

        string path = Path.Combine(_saveDirectory, ContinuationFile);
        Assert.True(File.Exists(path));
        byte[] before = await File.ReadAllBytesAsync(path);
        DateTime savedAt = File.GetLastWriteTimeUtc(path);

        await using var reloaded = LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, path, _saveDirectory);
        using var loadedTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var loadedSnapshots = reloaded.ReadSnapshotsAsync(loadedTimeout.Token).GetAsyncEnumerator();
        var snapshot = await NextSnapshotAsync(loadedSnapshots,
            candidate => candidate.CarrierOperations is not null,
            "the reloaded carrier snapshot");
        Assert.Equal(1, snapshot.CarrierOperations!.Revision);

        var response = await reloaded.ExitTacticalBattleAsync(
            new ExitTacticalBattleRequest("exit-old-battle", battleId));

        Assert.False(response.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, response.Code);
        Assert.Null(response.Settlement);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.Equal(savedAt, File.GetLastWriteTimeUtc(path));
    }
}
