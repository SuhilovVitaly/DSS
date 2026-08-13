using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public class LocalSessionIntegrationTests
{
    [Fact]
    public async Task Engine_publishes_snapshots_with_incrementing_sequence()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        await using var connection = new LocalGameSessionConnection(engine);

        var snapshots = new List<AuthoritativeSnapshot>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        await foreach (var snapshot in connection.ReadSnapshotsAsync(cts.Token))
        {
            snapshots.Add(snapshot);
            if (snapshots.Count >= 3)
                break;
        }

        Assert.True(snapshots.Count >= 2, "Should receive at least 2 snapshots");

        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.True(
                snapshots[i].SnapshotSequence > snapshots[i - 1].SnapshotSequence,
                $"Sequence should be increasing: {snapshots[i].SnapshotSequence} > {snapshots[i - 1].SnapshotSequence}");
        }
    }

    [Fact]
    public async Task SendCommand_delivers_to_engine()
    {
        var engine = new SimulationEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        Assert.Equal(0, engine.ReceivedCommandCount);

        var command = new PlayerCommand("cmd-1", 1, "ship-1", "nav", "move");
        await connection.SendCommandAsync(command);

        Assert.Equal(1, engine.ReceivedCommandCount);
    }

    [Fact]
    public async Task SetObjectInteractionStateAsync_delivers_to_engine()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        await using var connection = new LocalGameSessionConnection(engine);

        await connection.SetObjectInteractionStateAsync("obj-1", "obj-1");

        Assert.Equal("obj-1", engine.ActiveObjectId);
        Assert.Equal("obj-1", engine.SelectedObjectId);
    }

    [Fact]
    public async Task GameSessionHandle_UpdateObjectInteractionState_reaches_the_engine()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("obj-1", 0, 0, SpeedKmS: 0, Direction: 0));
        var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);

        handle.UpdateObjectInteractionState("obj-1", null);

        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < deadline && engine.ActiveObjectId is null)
            await Task.Delay(25);

        Assert.Equal("obj-1", engine.ActiveObjectId);
        Assert.Null(engine.SelectedObjectId);
    }

    [Fact]
    public async Task SaveAsync_writes_a_valid_parsable_save_file()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson("""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "test",
            "spaceObjects": [
              { "objectId": "test", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 100, "positionY": 200, "speedMps": 1000, "directionDegrees": 45,
                "movementType": "Linear" }
            ]
          }
        }
        """));

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-test-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "Saves", "quicksave.json");

        await using var connection = new LocalGameSessionConnection(engine, savePath);
        try
        {
            // Saves/ does not exist yet — SaveAsync must create it.
            await connection.SaveAsync();

            Assert.True(File.Exists(savePath));
            var loaded = ScenarioLoader.LoadFromFile(savePath, allowNonZeroGameTime: true);
            Assert.NotNull(loaded);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_does_not_race_with_the_background_engine_loop()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson("""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "test",
            "spaceObjects": [
              { "objectId": "test", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """));

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-race-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "Saves", "quicksave.json");

        await using var connection = new LocalGameSessionConnection(engine, savePath);
        try
        {
            // Several concurrent SaveAsync calls while the background 1 Hz engine loop is
            // ticking — every completed write is an atomic temp-file + rename, so the file
            // on disk must always be fully valid, never partially written or corrupted.
            var saveTasks = Enumerable.Range(0, 5).Select(_ => connection.SaveAsync().AsTask()).ToArray();
            await Task.WhenAll(saveTasks);

            var loaded = ScenarioLoader.LoadFromFile(savePath, allowNonZeroGameTime: true);
            Assert.NotNull(loaded);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_throws_when_no_save_path_is_configured()
    {
        var engine = new SimulationEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await connection.SaveAsync());
    }

    [Fact]
    public async Task MasterSeedWasMissingOnLoad_surfaces_through_the_connection_for_legacy_saves()
    {
        // Closes the plumbing ТЗ-02A relies on: Program.cs's LocalGameSessionFactory checks
        // this connection-level property (not the engine directly) to decide whether to
        // write an InterfaceLog warning after CreateFromSaveFile.
        const string legacySaveJson = """
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 1000, "currentSpeed": "Speed0",
            "playerShipObjectId": "test",
            "spaceObjects": [
              { "objectId": "test", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-legacy-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "quicksave.json");
        Directory.CreateDirectory(dir);
        File.WriteAllText(savePath, legacySaveJson);

        string settingsPath = ResolveRealSettingsPath();

        try
        {
            await using var connection = LocalGameSessionConnection.CreateFromSaveFile(settingsPath, savePath);

            Assert.True(connection.MasterSeedWasMissingOnLoad);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task MasterSeedWasMissingOnLoad_is_false_when_the_save_already_carries_one()
    {
        const string saveJson = """
        {
          "scenarioMetadata": { "scenarioId": "quicksave", "name": "Quicksave" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 1000, "currentSpeed": "Speed0",
            "playerShipObjectId": "test",
            "masterSeed": 42,
            "spaceObjects": [
              { "objectId": "test", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-withseed-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "quicksave.json");
        Directory.CreateDirectory(dir);
        File.WriteAllText(savePath, saveJson);

        string settingsPath = ResolveRealSettingsPath();

        try
        {
            await using var connection = LocalGameSessionConnection.CreateFromSaveFile(settingsPath, savePath);

            Assert.False(connection.MasterSeedWasMissingOnLoad);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Navigate_command_propagates_authoritative_target_into_snapshots_and_client_projection()
    {
        // ТЗ-08.7 (AC8/AC9/AC10): an engine.navigate-to-point command sent through
        // the real connection surfaces as an authoritative NavigationTarget* on the
        // player ship in a snapshot, and the client-side NavigationTrajectoryProjector
        // then builds a non-empty trajectory from that snapshot alone.
        string settingsPath = ResolveRealSettingsPath();

        await using var connection = LocalGameSessionConnection.CreateFromSettingsFile(settingsPath);
        await using var handle = new GameSessionHandle(connection);

        await handle.SendEngineCommandAsync(
            "SPC-0001",
            "MOD-PLAYER-ENGINE-01",
            ShipEngineCommandTypes.NavigateToPoint,
            10300,
            9800);

        ObjectMotionSnapshot? shipWithTarget = null;
        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < deadline)
        {
            var ship = handle.Buffer.Latest?.Snapshot.Objects
                .FirstOrDefault(o => o.ObjectId == "SPC-0001");
            if (ship?.NavigationTargetX is not null)
            {
                shipWithTarget = ship;
                break;
            }

            await Task.Delay(25);
        }

        Assert.NotNull(shipWithTarget);
        Assert.Equal(10300.0, shipWithTarget.NavigationTargetX!.Value, precision: 6);
        Assert.Equal(9800.0, shipWithTarget.NavigationTargetY!.Value, precision: 6);

        var points = new NavigationTrajectoryProjector().Project(shipWithTarget);
        Assert.NotEmpty(points);
    }

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        return settingsPath;
    }
}
