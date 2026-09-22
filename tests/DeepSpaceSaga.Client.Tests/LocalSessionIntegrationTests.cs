using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using SkiaSharp;

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

    private static SimulationEngine CreateEngineWithTestScenario()
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
        return engine;
    }

    [Fact]
    public async Task SaveAsync_writes_a_valid_parsable_save_file()
    {
        var engine = CreateEngineWithTestScenario();

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-test-{Guid.NewGuid():N}");
        string saveDirectory = Path.Combine(dir, "Saves");

        await using var connection = new LocalGameSessionConnection(engine, saveDirectory);
        try
        {
            // Saves/ does not exist yet — SaveAsync must create it.
            await connection.SaveAsync("quicksave");

            string savePath = Path.Combine(saveDirectory, "quicksave.json");
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
    public async Task SaveAsync_writes_to_a_file_named_after_the_sanitized_slot_id()
    {
        var engine = CreateEngineWithTestScenario();

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-slotname-{Guid.NewGuid():N}");
        string saveDirectory = Path.Combine(dir, "Saves");

        await using var connection = new LocalGameSessionConnection(engine, saveDirectory);
        try
        {
            // Illegal characters (':', '#', '!') must be stripped by the shared
            // SaveSlotNaming helper, giving a predictable on-disk file name.
            await connection.SaveAsync("My Save #1!");

            string expectedPath = Path.Combine(saveDirectory, "My Save 1.json");
            Assert.True(File.Exists(expectedPath));

            var loaded = ScenarioLoader.LoadFromFile(expectedPath, allowNonZeroGameTime: true);
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
        string saveDirectory = Path.Combine(dir, "Saves");

        await using var connection = new LocalGameSessionConnection(engine, saveDirectory);
        try
        {
            // Several concurrent SaveAsync calls (same slot) while the background 1 Hz
            // engine loop is ticking — every completed write is an atomic temp-file +
            // rename, so the file on disk must always be fully valid, never partially
            // written or corrupted.
            var saveTasks = Enumerable.Range(0, 5).Select(_ => connection.SaveAsync("quicksave").AsTask()).ToArray();
            await Task.WhenAll(saveTasks);

            string savePath = Path.Combine(saveDirectory, "quicksave.json");
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
    public async Task Concurrent_saves_to_different_slots_do_not_collide()
    {
        var engine = CreateEngineWithTestScenario();

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-multislot-{Guid.NewGuid():N}");
        string saveDirectory = Path.Combine(dir, "Saves");

        await using var connection = new LocalGameSessionConnection(engine, saveDirectory);
        try
        {
            string[] slotIds = ["slot-a", "slot-b", "slot-c"];
            var saveTasks = slotIds.Select(slotId => connection.SaveAsync(slotId).AsTask()).ToArray();
            await Task.WhenAll(saveTasks);

            foreach (var slotId in slotIds)
            {
                string savePath = Path.Combine(saveDirectory, $"{slotId}.json");
                Assert.True(File.Exists(savePath), $"Expected {savePath} to exist");
                var loaded = ScenarioLoader.LoadFromFile(savePath, allowNonZeroGameTime: true);
                Assert.NotNull(loaded);
            }
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_throws_when_no_save_directory_is_configured()
    {
        var engine = new SimulationEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await connection.SaveAsync("quicksave"));
    }

    [Fact]
    public async Task MasterSeedWasMissingOnLoad_surfaces_through_the_connection_for_legacy_saves()
    {
        // Closes the plumbing ТЗ-02A relies on: Program.cs's LocalGameSessionFactory checks
        // this connection-level property (not the engine directly) to decide whether to
        // write an InterfaceLog warning after CreateFromSaveFile.
        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-legacy-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "quicksave.json");
        Directory.CreateDirectory(dir);

        string settingsPath = ResolveRealSettingsPath();
        File.WriteAllText(savePath, CreateCurrentCatalogSave(settingsPath, includeMasterSeed: false));

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
        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-withseed-{Guid.NewGuid():N}");
        string savePath = Path.Combine(dir, "quicksave.json");
        Directory.CreateDirectory(dir);

        string settingsPath = ResolveRealSettingsPath();
        File.WriteAllText(savePath, CreateCurrentCatalogSave(settingsPath, includeMasterSeed: true));

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
        // ТЗ-08.7 (AC8/AC9/AC10): an engine.orbit command sent through
        // the real connection surfaces as an authoritative NavigationTarget* on the
        // player ship in a snapshot, and the client-side NavigationTrajectoryProjector
        // then builds a non-empty trajectory from that snapshot alone.
        string settingsPath = ResolveRealSettingsPath();

        await using var connection = LocalGameSessionConnection.CreateFromSettingsFile(settingsPath);
        await using var handle = new GameSessionHandle(connection);

        await handle.SendEngineCommandAsync(
            "SPC-0001",
            "MOD-PLAYER-ENGINE-01",
            ShipEngineCommandTypes.Orbit,
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

    [Fact]
    public async Task SaveScreen_New_Save_action_produces_a_listable_slot_file_end_to_end()
    {
        // Wires SaveScreen exactly as SkiaWindow.OpenSaveWindowAsync does — bound to a
        // real GameSessionHandle/LocalGameSessionConnection and SaveSlotRepository, not
        // fakes — to prove the New-Save UI path actually reaches disk and becomes
        // listable, not just that individual pieces work in isolation.
        var engine = CreateEngineWithTestScenario();

        string dir = Path.Combine(Path.GetTempPath(), $"dss-save-uiflow-{Guid.NewGuid():N}");
        string saveDirectory = Path.Combine(dir, "Saves");

        var connection = new LocalGameSessionConnection(engine, saveDirectory);
        await using var handle = new GameSessionHandle(connection);
        try
        {
            var saveScreen = new SaveScreen(
                () => SaveSlotRepository.ListSlots(saveDirectory),
                slotId => handle.SaveAsync(slotId).AsTask().GetAwaiter().GetResult(),
                slotId => SaveSlotRepository.DeleteSlot(saveDirectory, slotId));

            // Render once so the screen captures screen width/height (mirrors real usage).
            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            saveScreen.Render(canvas, 1920, 1080);

            foreach (char c in "My Playthrough")
                saveScreen.OnTextInput(c);

            var (sx, sy) = Center(SaveLayout.SaveButtonRect());
            saveScreen.OnMouseDown(sx, sy); // NEW SAVE → SaveAsync → refresh list

            var slots = SaveSlotRepository.ListSlots(saveDirectory);
            var written = Assert.Single(slots, s => s.SlotId == "My Playthrough");
            Assert.True(File.Exists(Path.Combine(saveDirectory, "My Playthrough.json")));

            var loaded = ScenarioLoader.LoadFromFile(
                Path.Combine(saveDirectory, "My Playthrough.json"), allowNonZeroGameTime: true);
            Assert.NotNull(loaded);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFromScenarioFile_starts_a_session_from_an_explicitly_chosen_scenario()
    {
        // The New Game -> scenario picker path: LocalGameSessionConnection.CreateFromScenarioFile
        // must bootstrap from the given scenario file, not settings.json's defaultScenario.
        string settingsPath = ResolveRealSettingsPath();
        string dockedScenarioPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(settingsPath)!, "Scenarios", "Docked", "scenario.json"));

        await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(settingsPath, dockedScenarioPath);
        await using var handle = new GameSessionHandle(connection);

        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < deadline && handle.Buffer.Latest is null)
            await Task.Delay(25);

        var ship = handle.Buffer.Latest?.Snapshot.Objects.FirstOrDefault(o => o.ObjectId == "SPC-0001");
        Assert.NotNull(ship);
        Assert.Equal(0, ship!.SpeedKmS);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("docked")]
    public async Task Client_build_output_starts_scenario_and_publishes_first_snapshot(string scenarioId)
    {
        // Regression: Settings.json now points itemTypes at the split Data/Items directory.
        // Source-tree tests still pass when that directory is absent from Client/bin, while
        // the real New Game path fails before publishing a snapshot. Exercise the packaged
        // paths and the local connection together, with a hard timeout against a silent hang.
        string settingsPath = ResolveClientBuildOutputSettingsPath();
        string scenariosDirectory = Path.Combine(Path.GetDirectoryName(settingsPath)!, "Scenarios");
        var selectedScenario = Assert.Single(
            ScenarioRepository.ListScenarios(scenariosDirectory),
            scenario => scenario.ScenarioId == scenarioId);

        await using var connection = await Task.Run(() =>
                LocalGameSessionConnection.CreateFromScenarioFile(
                    settingsPath,
                    selectedScenario.ScenarioPath))
            .WaitAsync(TimeSpan.FromSeconds(5));
        await using var handle = new GameSessionHandle(connection);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && handle.Buffer.Latest is null)
            await Task.Delay(25);

        var snapshot = handle.Buffer.Latest;
        Assert.NotNull(snapshot);
        Assert.Equal("SPC-0001", snapshot!.Snapshot.PlayerShipObjectId);
        Assert.Contains(snapshot.Snapshot.Objects, obj => obj.ObjectId == "SPC-0002");

        if (scenarioId == "docked")
        {
            Assert.Equal(SimulationSpeed.Speed0, snapshot.Snapshot.CurrentSpeed);
            var playerShip = Assert.Single(snapshot.Snapshot.Objects,
                obj => obj.ObjectId == snapshot.Snapshot.PlayerShipObjectId);
            Assert.True(playerShip.IsDocked);
            Assert.Equal("SPC-0002", playerShip.DockedStationObjectId);
        }
    }

    [Fact]
    public void ScenarioRepository_lists_every_scenario_json_found_under_the_real_Scenarios_directory()
    {
        // Exercises the real on-disk Scenarios/ tree (Default, Default_500, Docked) the
        // same way SkiaWindow's ListScenarios() will at runtime — proving Name/Description
        // round-trip through ScenarioLoader rather than just testing an isolated temp dir.
        string settingsPath = ResolveRealSettingsPath();
        string scenariosDirectory = Path.Combine(Path.GetDirectoryName(settingsPath)!, "Scenarios");

        var scenarios = ScenarioRepository.ListScenarios(scenariosDirectory);

        Assert.Contains(scenarios, s => s.ScenarioId == "default" && !string.IsNullOrWhiteSpace(s.Description));
        Assert.Contains(scenarios, s => s.ScenarioId == "docked" && !string.IsNullOrWhiteSpace(s.Description));
        Assert.All(scenarios, s => Assert.True(File.Exists(s.ScenarioPath)));
    }

    private static string ResolveClientBuildOutputSettingsPath()
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string clientBinRoot = Path.Combine(repositoryRoot, "src", "DeepSpaceSaga.Client", "bin");

        foreach (string configuration in new[] { "Debug", "Release" })
        {
            string candidate = Path.Combine(clientBinRoot, configuration, "net8.0", "Settings.json");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            $"DeepSpaceSaga.Client build output was not found under '{clientBinRoot}'.");
    }

    [Fact]
    public void ScenarioRepository_ListScenarios_returns_empty_for_a_missing_directory()
    {
        string missingDirectory = Path.Combine(Path.GetTempPath(), $"dss-no-scenarios-{Guid.NewGuid():N}");

        var scenarios = ScenarioRepository.ListScenarios(missingDirectory);

        Assert.Empty(scenarios);
    }

    [Fact]
    public void ScenarioRepository_ListScenarios_skips_an_invalid_scenario_file_without_throwing()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-scenarios-invalid-{Guid.NewGuid():N}");
        string validDir = Path.Combine(dir, "Good");
        string invalidDir = Path.Combine(dir, "Bad");
        Directory.CreateDirectory(validDir);
        Directory.CreateDirectory(invalidDir);

        File.WriteAllText(Path.Combine(validDir, "scenario.json"), """
        {
          "scenarioMetadata": { "scenarioId": "good", "name": "Good", "description": "Works fine." },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "SPC-0001",
            "spaceObjects": [
              { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
            ]
          }
        }
        """);
        File.WriteAllText(Path.Combine(invalidDir, "scenario.json"), "{ not valid json");

        try
        {
            var scenarios = ScenarioRepository.ListScenarios(dir);

            var good = Assert.Single(scenarios);
            Assert.Equal("good", good.ScenarioId);
            Assert.Equal("Works fine.", good.Description);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- EP-0001-US-0015-TK-0005: local quote transport ----------------------------------

    private const string QuoteShipId = "SPC-0001";
    private const string QuoteStationId = "SPC-0002";
    private const string QuoteCargoModuleId = "MOD-PLAYER-CARGO-01";
    private const string QuoteItemTypeId = "item.ice";
    private static readonly TimeSpan QuoteTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Local_connection_returns_exact_authoritative_quote_without_world_mutation()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-quote-exact-{Guid.NewGuid():N}");
        try
        {
            var engine = CreateProfileMarketEngine(dir);
            await using var connection = new LocalGameSessionConnection(engine);
            IGameSessionConnection session = connection;
            long publishedRevision = await ReadPublishedMarketRevisionAsync(session);
            string worldBefore = QuoteWorldState(engine);

            var request = IceBuy("req-exact", 5);
            var viaConnection = await session.GetTradeQuoteAsync(request).AsTask().WaitAsync(QuoteTimeout);

            Assert.Null(viaConnection.DisabledReason);
            Assert.StartsWith("QTE-", viaConnection.QuoteId);
            Assert.Equal("req-exact", viaConnection.RequestId);
            Assert.Equal(QuoteStationId, viaConnection.StationObjectId);
            Assert.Equal(QuoteShipId, viaConnection.ObjectId);
            Assert.Equal(QuoteCargoModuleId, viaConnection.ModuleId);
            Assert.Equal(TradeCommandTypes.Buy, viaConnection.CommandType);
            Assert.Equal(QuoteItemTypeId, viaConnection.ItemTypeId);
            Assert.Equal(5, viaConnection.RequestedQuantity);
            Assert.Equal(5, viaConnection.ExecutableQuantity);
            Assert.Equal(publishedRevision, viaConnection.MarketRevision);
            Assert.Equal(5, viaConnection.Curve.Sum(step => step.Quantity));
            Assert.Equal(viaConnection.Curve.Sum(step => step.Quantity * step.UnitPriceCredits), viaConnection.TotalCredits);
            Assert.False(viaConnection.PriceReasons.IsDefaultOrEmpty);

            // The adapter hands back the very object the engine issued and cached — no copy, no recomputation.
            Assert.Same(engine.GetTradeQuote(request), viaConnection);

            // A fresh direct issue for the same binding carries identical terms.
            var direct = engine.GetTradeQuote(request with { RequestId = "req-exact-direct" });
            Assert.NotEqual(direct.QuoteId, viaConnection.QuoteId);
            AssertSameQuoteTerms(direct, viaConnection);

            Assert.Equal(worldBefore, QuoteWorldState(engine));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Quote_request_honors_precancelled_token_and_disposed_connection()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-quote-cancel-{Guid.NewGuid():N}");
        try
        {
            var engine = CreateProfileMarketEngine(dir);
            var connection = new LocalGameSessionConnection(engine);
            IGameSessionConnection session = connection;
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await session.GetTradeQuoteAsync(IceBuy("req-cancel", 5), cancelled.Token));

            // The cancelled request never reached the engine: its RequestId is still free for another binding.
            var afterCancel = await session.GetTradeQuoteAsync(IceBuy("req-cancel", 6)).AsTask().WaitAsync(QuoteTimeout);
            Assert.Null(afterCancel.DisabledReason);
            Assert.Equal(6, afterCancel.ExecutableQuantity);

            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await session.GetTradeQuoteAsync(IceBuy("req-disposed", 5)));
            // Cancellation is checked before disposal, like every other session operation.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await session.GetTradeQuoteAsync(IceBuy("req-disposed", 5), cancelled.Token));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Quote_request_during_snapshot_loop_completes_without_deadlock()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-quote-loop-{Guid.NewGuid():N}");
        try
        {
            var engine = CreateProfileMarketEngine(dir);
            await using var connection = new LocalGameSessionConnection(engine);
            IGameSessionConnection session = connection;
            await session.SetSimulationSpeedAsync(SimulationSpeed.Speed1);

            using var readerStop = new CancellationTokenSource();
            var sequences = new System.Collections.Concurrent.ConcurrentQueue<ulong>();
            var reader = Task.Run(async () =>
            {
                try
                {
                    await foreach (var snapshot in session.ReadSnapshotsAsync(readerStop.Token))
                        sequences.Enqueue(snapshot.SnapshotSequence);
                }
                catch (OperationCanceledException) when (readerStop.IsCancellationRequested) { }
            });

            var quoteTasks = Enumerable.Range(0, 40)
                .Select(i => Task.Run(async () =>
                    await session.GetTradeQuoteAsync(IceBuy($"req-loop-{i}", 1 + i % 5))))
                .ToArray();
            var quotes = await Task.WhenAll(quoteTasks).WaitAsync(QuoteTimeout);

            Assert.All(quotes, quote =>
            {
                Assert.Null(quote.DisabledReason);
                Assert.Equal(quote.RequestedQuantity, quote.ExecutableQuantity);
            });
            Assert.Equal(quotes.Length, quotes.Select(quote => quote.QuoteId).Distinct(StringComparer.Ordinal).Count());

            // The snapshot loop keeps publishing while and after quotes are served.
            var deadline = DateTime.UtcNow + QuoteTimeout;
            while (DateTime.UtcNow < deadline && sequences.Count < 2)
                await Task.Delay(25);
            readerStop.Cancel();
            await reader.WaitAsync(QuoteTimeout);

            ulong[] published = sequences.ToArray();
            Assert.True(published.Length >= 2, "The engine loop should keep publishing snapshots.");
            for (int i = 1; i < published.Length; i++)
                Assert.True(published[i] > published[i - 1]);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Save_load_preserves_revision_but_invalidates_old_quote_token()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-quote-saveload-{Guid.NewGuid():N}");
        string saveDirectory = Path.Combine(dir, "Saves");
        try
        {
            TradeQuoteSnapshot staleQuote;
            long savedRevision;
            var engine = CreateProfileMarketEngine(dir);
            await using (var connection = new LocalGameSessionConnection(engine, saveDirectory))
            {
                IGameSessionConnection session = connection;

                // Execute one quote so the saved revision is above the initial one.
                var executed = await session.GetTradeQuoteAsync(IceBuy("req-before-save", 3)).AsTask().WaitAsync(QuoteTimeout);
                Assert.Null(executed.DisabledReason);
                await session.SendCommandAsync(BindQuote("cmd-before-save", executed));
                var executedResult = await WaitForCommandResultAsync(session, "cmd-before-save");
                Assert.Equal(CommandResultStatus.Executed, executedResult.Status);

                staleQuote = await session.GetTradeQuoteAsync(IceBuy("req-stale", 2)).AsTask().WaitAsync(QuoteTimeout);
                Assert.Null(staleQuote.DisabledReason);
                savedRevision = staleQuote.MarketRevision;
                Assert.Equal(executed.MarketRevision + 1, savedRevision);

                await connection.SaveAsync("quote-slot");
            }

            string savePath = Path.Combine(saveDirectory, "quote-slot.json");
            var loadedEngine = SimulationEngine.CreateFromSaveFile(ResolveRealSettingsPath(), savePath);
            await using var loadedConnection = new LocalGameSessionConnection(loadedEngine, saveDirectory);
            IGameSessionConnection loaded = loadedConnection;

            // The same RequestId and binding gets a brand-new quote on the saved revision.
            var reissued = await loaded.GetTradeQuoteAsync(IceBuy("req-stale", 2)).AsTask().WaitAsync(QuoteTimeout);
            Assert.Null(reissued.DisabledReason);
            Assert.StartsWith("QTE-", reissued.QuoteId);
            Assert.NotEqual(staleQuote.QuoteId, reissued.QuoteId);
            Assert.Equal(savedRevision, reissued.MarketRevision);
            AssertSameQuoteTerms(staleQuote, reissued);

            // The pre-load quote token is not accepted by the new session.
            await loaded.SendCommandAsync(BindQuote("cmd-stale", staleQuote));
            var staleResult = await WaitForCommandResultAsync(loaded, "cmd-stale");
            Assert.Equal(CommandResultStatus.Rejected, staleResult.Status);
            Assert.Equal(CommandReasonCodes.StaleQuote, staleResult.ReasonCode);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Repeated_request_id_roundtrips_same_quote_and_conflict_is_preserved()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-quote-repeat-{Guid.NewGuid():N}");
        try
        {
            var engine = CreateProfileMarketEngine(dir);
            await using var connection = new LocalGameSessionConnection(engine);
            IGameSessionConnection session = connection;

            var first = await session.GetTradeQuoteAsync(IceBuy("req-repeat", 4)).AsTask().WaitAsync(QuoteTimeout);
            var repeated = await session.GetTradeQuoteAsync(IceBuy("req-repeat", 4)).AsTask().WaitAsync(QuoteTimeout);
            Assert.Null(first.DisabledReason);
            Assert.Same(first, repeated);

            var conflict = await session.GetTradeQuoteAsync(IceBuy("req-repeat", 7)).AsTask().WaitAsync(QuoteTimeout);
            Assert.Equal("request_id_conflict", conflict.DisabledReason);
            Assert.Equal("", conflict.QuoteId);
            Assert.Equal(0, conflict.ExecutableQuantity);
            Assert.Equal(0, conflict.TotalCredits);
            Assert.Empty(conflict.Curve);
            Assert.Equal(7, conflict.RequestedQuantity);

            // The conflict neither replaced nor evicted the live quote.
            var afterConflict = await session.GetTradeQuoteAsync(IceBuy("req-repeat", 4)).AsTask().WaitAsync(QuoteTimeout);
            Assert.Same(first, afterConflict);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static TradeQuoteRequest IceBuy(string requestId, long quantity) =>
        new(requestId, QuoteShipId, QuoteCargoModuleId, TradeCommandTypes.Buy, QuoteItemTypeId, quantity);

    private static PlayerCommand BindQuote(string commandId, TradeQuoteSnapshot quote) =>
        new(commandId, 1, quote.ObjectId, quote.ModuleId, quote.CommandType,
            ItemTypeId: quote.ItemTypeId, Quantity: quote.RequestedQuantity,
            QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision);

    /// <summary>
    /// The shipped Docked scenario (player ship docked at SPC-0002), with SPC-0002 turned into a real
    /// profile market (market.mining from the real catalog) so quotes run along the sequential curve.
    /// </summary>
    private static SimulationEngine CreateProfileMarketEngine(string directory)
    {
        string settingsPath = ResolveRealSettingsPath();
        string dockedPath = Path.Combine(Path.GetDirectoryName(settingsPath)!, "Scenarios", "Docked", "scenario.json");
        var root = JsonNode.Parse(File.ReadAllText(dockedPath))!;
        var station = root["gameState"]!["spaceObjects"]!.AsArray()
            .Single(obj => (string?)obj!["objectId"] == QuoteStationId)!.AsObject();
        station["marketProfileId"] = "market.mining";
        station["stationSize"] = "Medium";
        station.Remove("inventory");

        Directory.CreateDirectory(directory);
        string scenarioPath = Path.Combine(directory, "scenario.json");
        File.WriteAllText(scenarioPath, root.ToJsonString());
        return SimulationEngine.CreateFromScenarioFile(settingsPath, scenarioPath);
    }

    /// <summary>Money, stock, cargo, budget and market revisions — everything a quote must never change.</summary>
    private static string QuoteWorldState(SimulationEngine engine)
    {
        var save = engine.CaptureSaveState();
        var state = save.GameState;
        var station = state.SpaceObjects.Single(obj => obj.ObjectId == QuoteStationId);
        var ship = state.SpaceObjects.Single(obj => obj.ObjectId == QuoteShipId);
        return ScenarioLoader.Serialize(save with
        {
            GameState = state with
            {
                CommandReceipts = null,
                SpaceObjects = [ship, station],
            },
        });
    }

    private static void AssertSameQuoteTerms(TradeQuoteSnapshot expected, TradeQuoteSnapshot actual)
    {
        Assert.Equal(expected.MarketRevision, actual.MarketRevision);
        Assert.Equal(expected.StationObjectId, actual.StationObjectId);
        Assert.Equal(expected.ObjectId, actual.ObjectId);
        Assert.Equal(expected.ModuleId, actual.ModuleId);
        Assert.Equal(expected.CommandType, actual.CommandType);
        Assert.Equal(expected.ItemTypeId, actual.ItemTypeId);
        Assert.Equal(expected.RequestedQuantity, actual.RequestedQuantity);
        Assert.Equal(expected.ExecutableQuantity, actual.ExecutableQuantity);
        Assert.Equal(expected.MaximumQuantity, actual.MaximumQuantity);
        Assert.Equal(expected.TotalCredits, actual.TotalCredits);
        Assert.Equal(expected.DisabledReason, actual.DisabledReason);
        Assert.True(expected.Curve.SequenceEqual(actual.Curve), "Quote curves differ.");
        Assert.True(expected.LimitReasons.SequenceEqual(actual.LimitReasons, StringComparer.Ordinal), "Limit reasons differ.");
        Assert.True(expected.PriceReasons.SequenceEqual(actual.PriceReasons), "Price reasons differ.");
    }

    private static async Task<long> ReadPublishedMarketRevisionAsync(IGameSessionConnection connection)
    {
        using var timeout = new CancellationTokenSource(QuoteTimeout);
        await foreach (var snapshot in connection.ReadSnapshotsAsync(timeout.Token))
        {
            if (snapshot.DockedStationTrade?.MarketRevision is { } revision)
                return revision;
        }

        throw new InvalidOperationException("The snapshot stream ended without a docked profile market.");
    }

    private static async Task<CommandResult> WaitForCommandResultAsync(IGameSessionConnection connection, string commandId)
    {
        using var timeout = new CancellationTokenSource(QuoteTimeout);
        await foreach (var snapshot in connection.ReadSnapshotsAsync(timeout.Token))
        {
            if (snapshot.CommandResults.IsDefault)
                continue;
            foreach (var result in snapshot.CommandResults)
            {
                if (result.CommandId == commandId)
                    return result;
            }
        }

        throw new InvalidOperationException($"The snapshot stream ended without a result for '{commandId}'.");
    }

    private static (float x, float y) Center((float X, float Y, float W, float H) local)
    {
        float panelLeft = SaveLayout.PanelLeft(1920);
        float panelTop = SaveLayout.PanelTop(1080);
        return (panelLeft + local.X + local.W / 2f, panelTop + local.Y + local.H / 2f);
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

    private static string CreateCurrentCatalogSave(string settingsPath, bool includeMasterSeed)
    {
        using var source = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);
        var save = source.CaptureSaveState();
        var state = save.GameState with
        {
            GameTimeMs = 1000,
            SimulationTimeMs = 1000,
            MasterSeed = includeMasterSeed ? 42UL : null
        };
        return ScenarioLoader.Serialize(save with { SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion, GameState = state });
    }
}
