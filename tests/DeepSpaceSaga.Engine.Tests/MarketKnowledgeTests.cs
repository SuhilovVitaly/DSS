using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MarketKnowledgeTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static StationMarketKnowledgeSnapshot Observation(TradingVoyageFixture f, string id) =>
        f.Snapshot.StationMarketKnowledge.Single(o => o.StationObjectId == id);

    [Fact]
    public void Known_five_station_map_gets_deterministic_initial_observations()
    {
        using var a = TradingVoyageFixture.Create();
        using var b = TradingVoyageFixture.Create();
        Assert.Equal(5, a.Snapshot.StationMarketKnowledge.Length);
        Assert.Equal(Json(a.Snapshot.StationMarketKnowledge), Json(b.Snapshot.StationMarketKnowledge));
        Assert.Equal(a.Snapshot.StationMarketKnowledge.Select(o => o.StationObjectId).Order(StringComparer.Ordinal),
            a.Snapshot.StationMarketKnowledge.Select(o => o.StationObjectId));
        Assert.All(a.Snapshot.StationMarketKnowledge, o =>
        {
            Assert.False(o.IsStale);
            Assert.True(o.IsAvailable);
            Assert.NotEmpty(o.StationRole);
            Assert.Equal(0, o.ObservedAtGameTimeMs);
            Assert.True(o.ObservedMarketRevision > 0);
            Assert.NotEmpty(o.StockBands);
            Assert.DoesNotContain(o.StockBands, b => b.ItemTypeId == "item.fuel");
            Assert.Equal(o.StockBands.Select(b => b.ItemTypeId).Order(StringComparer.Ordinal), o.StockBands.Select(b => b.ItemTypeId));
        });
    }

    [Fact]
    public void Unknown_station_gets_no_market_observation()
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        string remote = f.Destination;
        var scenario = save with
        {
            SaveFormatVersion = 0,
            GameState = save.GameState with
            {
                MarketKnowledge = null,
                TradingMap = null,
                StationResourceFields = null,
                VoyageState = null,
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.Station
                    ? o with { IsKnown = o.ObjectId != remote, MarketBudgetCredits = null, MarketProfileFingerprint = null, MarketRevision = null } : o).ToArray()
            }
        };
        using var engine = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        engine.LoadScenario(scenario);
        Assert.Equal(4, engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0).StationMarketKnowledge.Length);
        Assert.DoesNotContain(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0).StationMarketKnowledge, o => o.StationObjectId == remote);
    }

    [Fact]
    public void Unchanged_remote_market_keeps_observed_time_and_revision()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 300);
        var old = Json(f.Snapshot.StationMarketKnowledge);
        f.Advance(1);
        Assert.Equal(300, f.Snapshot.GameTimeMs);
        Assert.Equal(1, f.Snapshot.MotionTimeMs);
        Assert.Equal(old, Json(f.Snapshot.StationMarketKnowledge));
        Assert.Equal(old, Json(f.Capture().StationMarketKnowledge));
    }

    [Fact]
    public void Production_event_and_trade_revision_changes_mark_remote_observation_stale()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem);
        Assert.Equal((ulong)f.Snapshot.DockedStationTrade!.MarketRevision!, Observation(f, f.Origin).ObservedMarketRevision);
        var before = f.Snapshot.StationMarketKnowledge;
        var (_, undock) = f.Send("MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, target: f.Destination);
        Assert.Equal(CommandResultStatus.Executed, undock?.Status);
        f.Advance(GameCalendar.DayMs);
        var save = f.Save();
        Assert.Contains(save.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station), o => o.Events?.Count > 0);
        Assert.Contains(f.Snapshot.StationMarketKnowledge, o => o.IsStale);
        foreach (var observed in f.Snapshot.StationMarketKnowledge)
        {
            var previous = before.Single(o => o.StationObjectId == observed.StationObjectId);
            Assert.Equal(Json(previous with { IsStale = observed.IsStale }), Json(observed));
            long revision = save.GameState.SpaceObjects.Single(o => o.ObjectId == observed.StationObjectId).MarketRevision!.Value;
            Assert.Equal((ulong)revision != observed.ObservedMarketRevision, observed.IsStale);
        }
        Assert.Equal(Json(f.Snapshot.StationMarketKnowledge), Json(f.Capture().StationMarketKnowledge));
    }

    [Fact]
    public void Docking_refreshes_only_local_observation_and_exposes_exact_trade_projection()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 300);
        var initial = f.Snapshot.StationMarketKnowledge;
        f.FlyTo(f.Destination);
        Assert.Equal(f.Destination, f.Snapshot.DockedStationTrade!.StationObjectId);
        var current = Observation(f, f.Destination);
        Assert.False(current.IsStale);
        Assert.Equal(f.Snapshot.GameTimeMs, current.ObservedAtGameTimeMs);
        Assert.Equal((ulong)f.Snapshot.DockedStationTrade.MarketRevision!, current.ObservedMarketRevision);
        foreach (var item in current.StockBands)
            Assert.Equal(f.Snapshot.DockedStationTrade.Items.Single(i => i.ItemTypeId == item.ItemTypeId).StockState, item.StockState);
        foreach (var remote in f.Snapshot.StationMarketKnowledge.Where(o => o.StationObjectId != f.Destination))
            Assert.Equal(Json(initial.Single(o => o.StationObjectId == remote.StationObjectId) with { IsStale = remote.IsStale }), Json(remote));
        var localTime = current.ObservedAtGameTimeMs;
        f.Advance(1);
        Assert.Equal(localTime, Observation(f, f.Destination).ObservedAtGameTimeMs);
        f.Trade(TradeCommandTypes.Buy, f.ReturnItem);
        Assert.Equal(f.Snapshot.GameTimeMs, Observation(f, f.Destination).ObservedAtGameTimeMs);
    }

    [Fact]
    public void Undock_hides_exact_trade_projection_but_keeps_coarse_observation()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var before = Json(f.Snapshot.StationMarketKnowledge);
        var (_, result) = f.Send("MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, target: f.Destination);
        Assert.Equal(CommandResultStatus.Executed, result?.Status);
        Assert.Null(f.Snapshot.DockedStationTrade);
        Assert.Equal(before, Json(f.Snapshot.StationMarketKnowledge));
    }

    [Fact]
    public void Save_load_preserves_observed_values_and_recomputes_stale()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Send("MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, target: f.Destination);
        f.Advance(GameCalendar.DayMs);
        var save = f.Save();
        var path = Path.Combine(Path.GetTempPath(), $"dss-market-knowledge-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, Json(save));
            using var loaded = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
            loaded.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            var actual = loaded.CaptureSnapshotForTests(save.GameState.GameTimeMs, SimulationSpeed.Speed0, save.GameState.MotionTimeMs);
            Assert.Equal(Json(f.Snapshot.StationMarketKnowledge), Json(actual.StationMarketKnowledge));
            Assert.Equal(Json(save.GameState.MarketKnowledge), Json(loaded.CaptureSaveStateForTests(save.GameState.GameTimeMs, SimulationSpeed.Speed0).GameState.MarketKnowledge));
            Assert.DoesNotContain("isStale", Json(save.GameState.MarketKnowledge), StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown-station")]
    [InlineData("unknown-item")]
    [InlineData("future-time")]
    [InlineData("negative-time")]
    [InlineData("future-revision")]
    [InlineData("role")]
    [InlineData("duplicate-item")]
    [InlineData("enum")]
    [InlineData("missing-band")]
    [InlineData("missing-knowledge")]
    [InlineData("unknown-flag")]
    [InlineData("null-band")]
    public void Invalid_duplicate_unknown_or_future_market_knowledge_rejects_load_atomically(string invalid)
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        var observations = save.GameState.MarketKnowledge!.ToArray();
        var row = observations[0];
        var bands = row.StockBands.ToArray();
        observations[0] = invalid switch
        {
            "unknown-station" => row with { StationObjectId = "missing" },
            "unknown-item" => row with { StockBands = [bands[0] with { ItemTypeId = "missing" }] },
            "future-time" => row with { ObservedAtGameTimeMs = save.GameState.GameTimeMs + 1 },
            "negative-time" => row with { ObservedAtGameTimeMs = -1 },
            "future-revision" => row with { ObservedMarketRevision = ulong.MaxValue },
            "role" => row with { StationRole = "changed" },
            "duplicate-item" => row with { StockBands = bands.Append(bands[0]).ToArray() },
            "enum" => row with { StockBands = [bands[0] with { StockState = (StationMarketStockState)99 }] },
            "missing-band" => row with { StockBands = [] },
            "null-band" => row with { StockBands = [null!] },
            _ => row
        };
        var state = save.GameState with { MarketKnowledge = invalid == "duplicate" ? observations.Append(row).ToArray() : observations };
        if (invalid == "missing-knowledge") state = state with { MarketKnowledge = null };
        if (invalid == "unknown-flag") state = state with { SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId == row.StationObjectId ? o with { IsKnown = false } : o).ToArray() };
        var before = Json(f.Save());
        Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(save with { GameState = state }, isSave: true));
        Assert.Equal(before, Json(f.Save()));
    }

    [Fact]
    public void Legacy_missing_observations_remain_unknown_until_actual_local_visit()
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        loaded.LoadScenario(save with { SaveFormatVersion = 13, GameState = save.GameState with { MarketKnowledge = null } }, isSave: true);
        var snapshot = loaded.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.Equal(f.Origin, Assert.Single(snapshot.StationMarketKnowledge).StationObjectId);
        Assert.NotNull(snapshot.DockedStationTrade);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Availability_change_marks_remote_stale_without_replacing_observation(bool destroyed)
    {
        using var f = TradingVoyageFixture.Create();
        var save = f.Save();
        var dialogue = save.GameState.DialogueState!;
        var state = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == f.Destination ? o with { IsDestroyed = destroyed } : o).ToArray(),
            DialogueState = dialogue with
            {
                Progress = dialogue.Progress with
                {
                    StationAccessStates = dialogue.Progress.StationAccessStates.SetItem(f.Destination, new(f.Destination, !destroyed))
                }
            }
        };
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        loaded.LoadScenario(save with { GameState = state }, isSave: true);
        var observed = loaded.CaptureSnapshotForTests(0, SimulationSpeed.Speed0).StationMarketKnowledge.Single(o => o.StationObjectId == f.Destination);
        Assert.True(observed.IsAvailable);
        Assert.True(observed.IsStale);
        Assert.Equal(Json(Observation(f, f.Destination) with { IsStale = true }), Json(observed));
    }

    [Fact]
    public void Loaded_observations_and_published_snapshots_are_immutable_copies()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var save = f.Save();
        var observations = save.GameState.MarketKnowledge!.ToArray();
        int remoteIndex = Array.FindIndex(observations, o => o.StationObjectId == f.Destination);
        var bands = observations[remoteIndex].StockBands.ToArray();
        observations[remoteIndex] = observations[remoteIndex] with { StockBands = bands };
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        loaded.LoadScenario(save with { GameState = save.GameState with { MarketKnowledge = observations } }, isSave: true);
        var old = loaded.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        var oldJson = Json(old.StationMarketKnowledge);
        bands[0] = bands[0] with { ItemTypeId = "tampered" };
        observations[remoteIndex] = observations[remoteIndex] with { StationRole = "tampered" };
        var later = loaded.CaptureSnapshotForTests(GameCalendar.DayMs, SimulationSpeed.Speed0);
        Assert.Equal(oldJson, Json(old.StationMarketKnowledge));
        Assert.DoesNotContain("tampered", Json(later.StationMarketKnowledge));
        Assert.True(later.StationMarketKnowledge.Single(o => o.StationObjectId == f.Destination).IsStale);
    }

}
