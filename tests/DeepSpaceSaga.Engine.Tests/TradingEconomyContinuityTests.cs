using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingEconomyContinuityTests
{
    private static string State(TradingVoyageFixture f) => JsonSerializer.Serialize(SimulationEngine.NormalizeTradingContinuationForTests(f.Save().GameState));
    private static void Same(TradingVoyageFixture a, TradingVoyageFixture b)
    {
        Assert.Equal(State(a), State(b));
        Assert.Equal(JsonSerializer.Serialize(a.Snapshot.DockedStationTrade), JsonSerializer.Serialize(b.Snapshot.DockedStationTrade));
        Assert.Equal(JsonSerializer.Serialize(a.Snapshot.TradingRoutes), JsonSerializer.Serialize(b.Snapshot.TradingRoutes));
        Assert.Equal(JsonSerializer.Serialize(a.Snapshot.StationMarketKnowledge), JsonSerializer.Serialize(b.Snapshot.StationMarketKnowledge));
    }
    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    public void Continuous_and_reloaded_market_sequences_are_equivalent(long offset)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        a.Advance(GameCalendar.HourMs + offset);
        using var b = a.Reload(); Same(a, b);
        foreach (long delta in new[] { 1L, GameCalendar.HourMs, GameCalendar.HourMs + 37 })
        {
            a.Trade(TradeCommandTypes.Buy, a.OutboundItem, 2); b.Trade(TradeCommandTypes.Buy, b.OutboundItem, 2);
            a.Advance(delta); b.Advance(delta); Same(a, b);
        }
    }
    [Theory]
    [InlineData("before")]
    [InlineData("after-buy")]
    [InlineData("after-partial")]
    public void Continuous_and_reloaded_trade_sequences_preserve_partial_fill_receipts(string checkpoint)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        if (checkpoint != "before")
        {
            var receipt = a.Trade(TradeCommandTypes.Buy, a.OutboundItem, checkpoint == "after-partial" ? 5 : 2);
            if (checkpoint == "after-partial")
            {
                var unit = a.Engine.GetTradeQuote(new("unit-budget", "SPC-0001", QuotedTradeExecutionTests.CargoModuleId, TradeCommandTypes.Sell, a.OutboundItem, 1));
                Assert.Null(unit.DisabledReason);
                var save = a.Save();
                a.Engine.LoadScenario(save with
                {
                    GameState = save.GameState with
                    {
                        SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == a.Origin ? o with
                        { Credits = unit.TotalCredits * 2, MarketBudgetCredits = unit.TotalCredits * 2 } : o).ToArray()
                    }
                }, true);
                a.Capture();
                receipt = a.Trade(TradeCommandTypes.Sell, a.OutboundItem, 5);
            }
            if (checkpoint == "after-partial") { Assert.True(receipt.ExecutedQuantity < receipt.RequestedQuantity); Assert.NotEmpty(receipt.LimitReasons); }
        }
        using var b = a.Reload(); Same(a, b);
        if (checkpoint is "before" or "after-partial") { a.Trade(TradeCommandTypes.Buy, a.OutboundItem, 2); b.Trade(TradeCommandTypes.Buy, b.OutboundItem, 2); }
        a.Trade(TradeCommandTypes.Sell, a.OutboundItem, 1); b.Trade(TradeCommandTypes.Sell, b.OutboundItem, 1); Same(a, b);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    public void Continuous_and_reloaded_event_sequences_are_equivalent(long offset)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        long expiry = 0;
        for (int i = 0; i < 48 && expiry == 0; i++)
        {
            a.Advance(GameCalendar.HourMs);
            var active = a.Save().GameState.SpaceObjects.SelectMany(o => o.Events ?? []).FirstOrDefault(e => e.DefinitionId is not null && e.DurationMs > 0);
            if (active is not null) expiry = active.StartedGameTimeMs + active.DurationMs!.Value;
        }
        Assert.True(expiry > a.MotionTime);
        a.Advance(expiry + offset - a.MotionTime);
        using var b = a.Reload(); Same(a, b);
        a.Advance(GameCalendar.HourMs + 1); b.Advance(GameCalendar.HourMs + 1); Same(a, b);
    }
    [Theory]
    [InlineData("reserved")]
    [InlineData("moving")]
    [InlineData("docking")]
    [InlineData("arrived")]
    [InlineData("partial-sale")]
    [InlineData("amount-postings")]
    public void Continuous_and_reloaded_voyage_finances_are_equivalent(string checkpoint)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        a.Trade(TradeCommandTypes.Buy, a.OutboundItem, 5);
        a.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: a.Destination);
        if (checkpoint == "moving") { a.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate); a.Advance(1000); }
        if (checkpoint == "docking")
        {
            a.FinishFlightTo(a.Destination, beforeDialogue: f =>
            {
                using var loaded = f.Reload(); Same(f, loaded);
                Assert.NotNull(loaded.Snapshot.ActiveDialogue);
            });
            return;
        }
        if (checkpoint is "arrived" or "partial-sale" or "amount-postings") a.FinishFlightTo(a.Destination);
        if (checkpoint == "partial-sale") a.Trade(TradeCommandTypes.Sell, a.OutboundItem, 2);
        if (checkpoint == "amount-postings")
        {
            a.Engine.RecordVoyageAmount("actual-event-cost", SimulationEngine.VoyageAmountKind.EventCost, 7);
            a.Engine.RecordVoyageAmount("actual-passenger-payout", SimulationEngine.VoyageAmountKind.PassengerPayout, 13);
            a.Engine.RecordVoyageAmount("actual-passenger-penalty", SimulationEngine.VoyageAmountKind.PassengerPenalty, 2);
        }
        using var b = a.Reload(); Same(a, b);
        if (checkpoint is "reserved" or "moving") { a.FinishFlightTo(a.Destination); b.FinishFlightTo(b.Destination); }
        a.Trade(TradeCommandTypes.Sell, a.OutboundItem, 1); b.Trade(TradeCommandTypes.Sell, b.OutboundItem, 1);
        Same(a, b);
        a.FlyTo(a.Origin); b.FlyTo(b.Origin); Same(a, b);
    }
    [Fact]
    public void Next_motion_cycle_identity_after_completed_voyage_survives_load()
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        a.FlyTo(a.Destination);
        using var b = a.Reload();
        a.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: a.Origin);
        b.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: b.Origin);
        a.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate);
        b.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate);
        Same(a, b);
    }

    [Fact]
    public void Immediate_loaded_state_has_no_price_stock_money_or_progress_jump()
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 300);
        a.Trade(TradeCommandTypes.Buy, a.OutboundItem, 3);
        a.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: a.Destination);
        a.Advance(17);
        using var b = a.Reload(); Same(a, b);
        Assert.Equal(a.Snapshot.GameTimeMs, b.Snapshot.GameTimeMs);
        Assert.Equal(a.Save().GameState.MotionTimeMs, b.Save().GameState.MotionTimeMs);
    }
    [Fact]
    public void Duplicate_command_and_terminal_effects_remain_exactly_once_after_load()
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        var (departure, original) = a.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: a.Destination);
        var active = a.Save().GameState.VoyageState!;
        a.FinishFlightTo(a.Destination);
        using var b = a.Reload(); string before = State(b);
        Assert.Equal(original, b.Replay(departure));
        Assert.Equal(b.Snapshot.LastVoyageFuelSettlement, b.Engine.ReplayVoyageTerminalForTests(active, true));
        Assert.Equal(before, State(b));
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("behind")]
    [InlineData("exhausted")]
    public void Invalid_identity_counter_does_not_reset_world_or_allocator(string defect)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        a.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: a.Destination);
        a.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate);
        var save = a.Save();
        var counters = save.GameState.EngineIdentityCounters!;
        var bad = save with
        {
            GameState = save.GameState with
            {
                EngineIdentityCounters = defect switch
                { "missing" => null, "behind" => counters with { EngineCycle = 0 }, "exhausted" => counters with { EngineCycle = ulong.MaxValue }, _ => throw new InvalidOperationException() }
            }
        };
        string before = State(a);
        Assert.Contains("Save was not modified", Assert.Throws<ScenarioException>(() => a.Engine.LoadScenario(bad, true)).Message);
        Assert.Equal(before, State(a));
    }

    [Theory]
    [InlineData("manifest")]
    [InlineData("catalog")]
    [InlineData("profile")]
    [InlineData("events")]
    [InlineData("missing-events")]
    [InlineData("map")]
    public void Incompatible_fingerprint_rejects_without_modifying_loaded_world(string subsystem)
    {
        using var a = TradingVoyageFixture.Create(calendarRatio: 1);
        var save = a.Save(); var s = save.GameState;
        var bad = subsystem switch
        {
            "manifest" => s with { TradingEconomyContinuation = s.TradingEconomyContinuation! with { ConfigurationFingerprint = new string('0', 64) } },
            "catalog" => s with { CatalogCompatibility = s.CatalogCompatibility! with { Fingerprint = new string('0', 64) } },
            "profile" => s with { SpaceObjects = s.SpaceObjects.Select(o => o.MarketProfileId is null ? o : o with { MarketProfileFingerprint = new string('0', 64) }).ToArray() },
            "events" => s with { MarketEventCatalogFingerprint = new string('0', 64) },
            "missing-events" => s with { MarketEventCatalogFingerprint = null },
            "map" => s with { TradingMap = s.TradingMap! with { Rules = s.TradingMap.Rules with { ClearanceKm = s.TradingMap.Rules.ClearanceKm + 0.001 } } },
            _ => throw new InvalidOperationException()
        };
        string before = State(a);
        var error = Assert.Throws<ScenarioException>(() => a.Engine.LoadScenario(save with { GameState = bad }, true));
        Assert.Contains("Save was not modified", error.Message); Assert.Contains("fingerprint", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, State(a));
    }
}
