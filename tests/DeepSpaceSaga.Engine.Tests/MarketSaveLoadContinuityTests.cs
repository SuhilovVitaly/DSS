using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.QuotedTradeExecutionTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MarketSaveLoadContinuityTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static string State(SimulationEngine engine, long time)
    {
        var state = engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time).GameState;
        // Linear NPC motion rebases its binary64 origin on load. Ignore sub-millimetre last-bit
        // pose differences only; every economic amount, identity, cursor and captured term stays exact.
        return Json(state with
        {
            SpaceObjects = state.SpaceObjects.Select(o => o with
            { PositionX = Math.Round(o.PositionX, 6), PositionY = Math.Round(o.PositionY, 6) }).ToArray()
        });
    }
    private static SimulationEngine Restore(ScenarioFile save)
    {
        var engine = new SimulationEngine(RealRegistry());
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        return engine;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Save_load_at_market_boundary_does_not_apply_interval_twice(long offset)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        long time = GameCalendar.HourMs + offset;
        f.Advance(time);
        var save = f.Save();
        using var loaded = Restore(save);
        Assert.Equal(State(f.Engine, time), State(loaded, time));
        Assert.Equal(State(f.Engine, time), State(loaded, time));
        long next = 3 * GameCalendar.HourMs + 73;
        Assert.Equal(State(f.Engine, next), State(loaded, next));
        Assert.Equal(State(f.Engine, next), State(loaded, next));
    }

    [Fact]
    public void Save_load_preserves_map_fields_stock_budget_and_prices()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Advance(5 * GameCalendar.HourMs + 7);
        var save = f.Save();
        using var loaded = Restore(save);
        Assert.Equal(Json(save.GameState.TradingMap), Json(loaded.CaptureSaveStateForTests(f.MotionTime, SimulationSpeed.Speed0, f.MotionTime).GameState.TradingMap));
        Assert.Equal(Json(save.GameState.StationResourceFields), Json(loaded.CaptureSaveStateForTests(f.MotionTime, SimulationSpeed.Speed0, f.MotionTime).GameState.StationResourceFields));
        Assert.Equal(Json(f.Snapshot.DockedStationTrade), Json(loaded.CaptureSnapshotForTests(f.MotionTime, SimulationSpeed.Speed0, f.MotionTime).DockedStationTrade));
        Assert.Equal(Json(f.Snapshot.StationMarketKnowledge), Json(loaded.CaptureSnapshotForTests(f.MotionTime, SimulationSpeed.Speed0, f.MotionTime).StationMarketKnowledge));
        var old = f.Engine.GetTradeQuote(new("old-price", ShipId, CargoModuleId, TradeCommandTypes.Buy, f.OutboundItem, 3));
        var fresh = loaded.GetTradeQuote(new("new-price", ShipId, CargoModuleId, TradeCommandTypes.Buy, f.OutboundItem, 3));
        Assert.NotEqual(old.QuoteId, fresh.QuoteId);
        Assert.Equal(old.TotalCredits, fresh.TotalCredits);
        Assert.Equal(old.ExecutableQuantity, fresh.ExecutableQuantity);
        Assert.Equal(old.Curve.ToArray(), fresh.Curve.ToArray());
        Assert.Equal(old.MarketRevision, fresh.MarketRevision);
        Assert.Equal(State(f.Engine, f.MotionTime), State(loaded, f.MotionTime));
    }

    [Fact]
    public void Event_start_and_expiry_match_continuous_run_after_load()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        long end = 0;
        for (int hour = 0; hour < 48 && end == 0; hour++)
        {
            f.Advance(GameCalendar.HourMs);
            var active = f.Save().GameState.SpaceObjects.SelectMany(o => o.Events ?? [])
                .FirstOrDefault(e => e.DefinitionId is not null && e.StartedGameTimeMs <= f.MotionTime && e.DurationMs is > 0);
            if (active is not null) end = checked(active.StartedGameTimeMs + active.DurationMs!.Value);
        }
        Assert.True(end > f.MotionTime);
        using var loaded = Restore(f.Save());
        foreach (long time in new[] { f.MotionTime, end - 1, end, end + 1, end + 2 * GameCalendar.HourMs })
        {
            Assert.Equal(State(f.Engine, time), State(loaded, time));
            var a = f.Engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time);
            var b = loaded.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time);
            Assert.Equal(Json(a.DockedStationTrade), Json(b.DockedStationTrade));
            Assert.Equal(Json(a.TradingRoutes), Json(b.TradingRoutes));
        }
    }

    [Fact]
    public void Market_revision_and_allocators_continue_monotonically()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Advance(GameCalendar.HourMs);
        var before = f.Save();
        using var loaded = Restore(before);
        var later = loaded.CaptureSaveStateForTests(2 * GameCalendar.HourMs, SimulationSpeed.Speed0, 2 * GameCalendar.HourMs);
        Assert.True(later.GameState.TradingEconomyContinuation!.NextMarketRevision >= before.GameState.TradingEconomyContinuation!.NextMarketRevision);
        Assert.Equal(before.GameState.TradingEconomyContinuation.NextMarketEventSequence + 1, later.GameState.TradingEconomyContinuation.NextMarketEventSequence);
        foreach (var station in later.GameState.SpaceObjects.Where(o => o.MarketProfileId is not null))
            Assert.True(station.MarketRevision >= before.GameState.SpaceObjects.Single(o => o.ObjectId == station.ObjectId).MarketRevision);
    }

    [Fact]
    public void Issued_quote_is_invalidated_but_executed_receipt_replays_after_load()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var quote = f.Engine.GetTradeQuote(new("done", ShipId, CargoModuleId, TradeCommandTypes.Buy, f.OutboundItem, 3));
        var command = Bind("durable", quote);
        var result = Apply(f.Engine, command);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        var issued = f.Engine.GetTradeQuote(new("pending", ShipId, CargoModuleId, TradeCommandTypes.Buy, f.OutboundItem, 1));
        using var loaded = Restore(f.Save());
        var before = State(loaded, 0);
        Assert.Equal(Json(result), Json(Apply(loaded, command)));
        Assert.Equal(before, State(loaded, 0));
        Assert.False(loaded.IsQuoteIssuedForTests(issued.QuoteId));
        var rejected = Apply(loaded, Bind("old-issued", issued));
        Assert.Equal(CommandResultStatus.Rejected, rejected.Status);
        Assert.Equal(CommandReasonCodes.StaleQuote, rejected.ReasonCode);
        var after = loaded.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0).GameState;
        var expected = JsonSerializer.Deserialize<GameStateData>(before)!;
        Assert.Equal(Json(expected with { CommandReceipts = null }), Json(after with { CommandReceipts = null }));
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("revision")]
    [InlineData("event")]
    [InlineData("station")]
    [InlineData("stock")]
    [InlineData("budget")]
    [InlineData("saved-revision")]
    public void Invalid_cross_reference_is_rejected_before_market_commit(string defect)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Advance(GameCalendar.HourMs);
        var save = f.Save();
        var state = save.GameState;
        var manifest = state.TradingEconomyContinuation!;
        state = defect switch
        {
            "cursor" => state with { TradingEconomyContinuation = manifest with { LastProcessedMarketGameTimeMs = state.GameTimeMs - 1 } },
            "revision" => state with { TradingEconomyContinuation = manifest with { NextMarketRevision = manifest.NextMarketRevision + 1 } },
            "event" => state with { TradingEconomyContinuation = manifest with { NextMarketEventSequence = manifest.NextMarketEventSequence + 1 } },
            "saved-revision" => state with { SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId == f.Origin ? o with { MarketRevision = null } : o).ToArray() },
            "station" => state with { SpaceObjects = state.SpaceObjects.Where(o => o.ObjectId != f.Destination).ToArray() },
            "stock" => state with
            {
                SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId == f.Origin ? o with
                { Inventory = o.Inventory!.Select(i => i with { Quantity = long.MaxValue }).ToArray() } : o).ToArray()
            },
            _ => state with { SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId == f.Origin ? o with { MarketBudgetCredits = long.MaxValue } : o).ToArray() }
        };
        string before = State(f.Engine, f.MotionTime);
        var error = Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(save with { GameState = state }, true));
        if (defect is "cursor" or "revision" or "event" or "saved-revision") Assert.Contains("Market continuation", error.Message);
        Assert.Equal(before, State(f.Engine, f.MotionTime));
    }
}
