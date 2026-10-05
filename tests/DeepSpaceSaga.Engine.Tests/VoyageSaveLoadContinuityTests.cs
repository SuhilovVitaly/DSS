using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageSaveLoadContinuityTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static void SameFinance(ScenarioFile a, ScenarioFile b)
    {
        Assert.Equal(Json(a.GameState.VoyageLedgers), Json(b.GameState.VoyageLedgers));
        Assert.Equal(Json(a.GameState.VoyageFuelSettlements), Json(b.GameState.VoyageFuelSettlements));
        Assert.Equal(Json(a.GameState.VoyageState), Json(b.GameState.VoyageState));
        Assert.Equal(a.GameState.PlayerTokens, b.GameState.PlayerTokens);
        Assert.Equal(Json(a.GameState.SpaceObjects.Single(o => o.ObjectId == a.GameState.PlayerShipObjectId).Modules),
            Json(b.GameState.SpaceObjects.Single(o => o.ObjectId == b.GameState.PlayerShipObjectId).Modules));
        Assert.Equal(Json(a.GameState.TradingEconomyContinuation), Json(b.GameState.TradingEconomyContinuation));
    }
    private static VoyageStateData Depart(TradingVoyageFixture f)
    {
        var (_, result) = f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        Assert.Equal(CommandResultStatus.Executed, result!.Status);
        return f.Save().GameState.VoyageState!;
    }

    [Fact]
    public void Save_load_after_reservation_preserves_tank_and_escrow_conservation()
    {
        using var f = TradingVoyageFixture.Create();
        var tank = f.Save().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules!.Single(m => m.FuelAmountKg is not null);
        var voyage = Depart(f);
        using var loaded = f.Reload();
        SameFinance(f.Save(), loaded.Save());
        var remaining = loaded.Save().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules!.Single(m => m.ModuleId == tank.ModuleId);
        Assert.Equal(tank.FuelAmountKg, remaining.FuelAmountKg + voyage.FuelReservationParts!.Sum(p => p.ReservedFuelKg));
        Assert.Equal(tank.FuelCostBasisCredits, remaining.FuelCostBasisCredits + voyage.FuelReservationParts!.Sum(p => p.ReservedFuelCostBasisCredits));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(300L)]
    public void Mid_voyage_save_load_preserves_progress_route_terms_and_active_ledger(long ratio)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: ratio);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 3);
        Depart(f);
        f.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate);
        f.Advance(10000);
        using var loaded = f.Reload();
        SameFinance(f.Save(), loaded.Save());
        f.Advance(1000); loaded.Advance(1000);
        SameFinance(f.Save(), loaded.Save());
        Assert.Equal(VoyageFinanceStates.InTransit, Assert.Single(loaded.Engine.VoyageFinancesForTests).State);
    }

    [Fact]
    public void Arrival_after_load_settles_fuel_and_ledger_exactly_once()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 3);
        var voyage = Depart(f);
        using var loaded = f.Reload();
        f.FinishFlightTo(f.Destination, splitSnapshots: true);
        loaded.FinishFlightTo(loaded.Destination, splitSnapshots: true);
        SameFinance(f.Save(), loaded.Save());
        using var arrived = loaded.Reload();
        SameFinance(loaded.Save(), arrived.Save());
        string before = Json(arrived.Save().GameState);
        Assert.Equal(arrived.Snapshot.LastVoyageFuelSettlement, arrived.Engine.ReplayVoyageTerminalForTests(voyage, true));
        Assert.Equal(before, Json(arrived.Save().GameState));
        Assert.Single(arrived.Save().GameState.VoyageFuelSettlements!);
    }

    [Fact]
    public void Interrupted_voyage_after_load_refunds_unused_fuel_once()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var voyage = Depart(f);
        var state = f.Save();
        var damaged = state with
        {
            GameState = state.GameState with
            {
                VoyageState = voyage with { ProgressPermille = 500 },
                SpaceObjects = state.GameState.SpaceObjects.Select(o => o.ObjectId == f.Destination ? o with { IsDestroyed = true } : o).ToArray()
            }
        };
        f.Engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(damaged), true), true);
        f.Capture();
        using var loaded = f.Reload();
        SameFinance(f.Save(), loaded.Save());
        var receipt = loaded.Snapshot.LastVoyageFuelSettlement!;
        Assert.True(receipt.ReturnedFuelKg > 0);
        Assert.Equal(VoyageFinanceStates.Interrupted, Assert.Single(loaded.Engine.VoyageFinancesForTests).State);
        string before = Json(loaded.Save().GameState);
        Assert.Equal(receipt, loaded.Engine.ReplayVoyageTerminalForTests(voyage, false));
        Assert.Equal(before, Json(loaded.Save().GameState));
    }

    [Fact]
    public void Duplicate_terminal_callback_replays_settlement_after_command_receipt_eviction()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var old = Depart(f);
        f.FinishFlightTo(f.Destination);
        var receipt = f.Snapshot.LastVoyageFuelSettlement!;
        f.FlyTo(f.Origin);
        Assert.NotEqual(old.VoyageId, f.Snapshot.LastVoyageFuelSettlement!.VoyageId);
        for (int i = 0; i <= SimulationEngine.CommandReceiptLimit; i++)
            f.Send(QuotedTradeExecutionTests.BridgeModuleId, "unknown-command");
        Assert.DoesNotContain(f.Save().GameState.CommandReceipts!, r => r.CommandId == old.VoyageId);
        using var loaded = f.Reload();
        string before = Json(loaded.Save().GameState);
        Assert.Equal(receipt, loaded.Engine.ReplayVoyageTerminalForTests(old, true));
        // A departed identity cannot be used to reserve fuel again after journal eviction.
        loaded.Engine.ReceiveCommand(VoyageLifecycleTests.Undock(old.VoyageId!, loaded.Destination));
        loaded.Capture();
        Assert.Equal(before, Json(loaded.Save().GameState));
        Assert.Equal(SimulationEngine.CommandReceiptLimit, loaded.Save().GameState.CommandReceipts!.Count);
    }

    [Fact]
    public void Partial_sale_and_next_port_fee_after_load_continue_existing_postings()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 5);
        f.FlyTo(f.Destination);
        f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 2);
        using var loaded = f.Reload();
        SameFinance(f.Save(), loaded.Save());
        long due = f.Snapshot.PortFees!.NextPortFeeDueGameTimeMs;
        f.Advance(due - f.MotionTime); loaded.Advance(due - loaded.MotionTime);
        f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 1); loaded.Trade(TradeCommandTypes.Sell, loaded.OutboundItem, 1);
        SameFinance(f.Save(), loaded.Save());
        using var second = loaded.Reload();
        SameFinance(loaded.Save(), second.Save());
        Assert.Equal(200, second.Engine.VoyageFinancesForTests[^1].PortFeesAssessedCredits);
    }

    [Theory]
    [InlineData("missing-history")]
    [InlineData("missing-settlements")]
    [InlineData("missing-active")]
    [InlineData("duplicate-ledger")]
    [InlineData("wrong-binding")]
    [InlineData("invented-total")]
    [InlineData("invalid-net")]
    [InlineData("missing-postings")]
    [InlineData("terminal-active")]
    public void Invalid_voyage_reservation_ledger_combination_is_rejected_before_commit(string defect)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        Depart(f);
        var save = f.Save(); var state = save.GameState; var ledger = Assert.Single(state.VoyageLedgers!);
        var bad = defect switch
        {
            "missing-history" => state with { VoyageLedgers = null },
            "missing-settlements" => state with { VoyageFuelSettlements = null },
            "missing-active" => state with { VoyageLedgers = [] },
            "duplicate-ledger" => state with { VoyageLedgers = [ledger, ledger] },
            "wrong-binding" => state with { VoyageLedgers = [ledger with { Finance = ledger.Finance with { VoyageId = "different" } }] },
            "invented-total" => state with { VoyageLedgers = [ledger with { Finance = ledger.Finance with { GrossSalesCredits = 1 } }] },
            "invalid-net" => state with { VoyageLedgers = [ledger with { Finance = ledger.Finance with { NetProfitCredits = 1 } }] },
            "missing-postings" => state with { VoyageLedgers = [ledger with { Postings = null! }] },
            "terminal-active" => state with { TradingEconomyContinuation = state.TradingEconomyContinuation! with { DurableTerminalReceiptIds = [ledger.Finance.VoyageId] } },
            _ => throw new InvalidOperationException()
        };
        string before = Json(state);
        Assert.Contains("Save was not modified", Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(save with { GameState = bad }, true)).Message);
        Assert.Equal(before, Json(f.Save().GameState));
    }

    [Theory]
    [InlineData("posting-duplicate")]
    [InlineData("posting-negative")]
    [InlineData("fee-conservation")]
    [InlineData("missing-terminal")]
    [InlineData("settlement-cost")]
    public void Invalid_terminal_postings_are_rejected_without_repeating_effects(string defect)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination);
        var save = f.Save(); var state = save.GameState; var ledger = Assert.Single(state.VoyageLedgers!);
        var posting = ledger.Postings[0];
        var bad = defect switch
        {
            "posting-duplicate" => state with { VoyageLedgers = [ledger with { Postings = [posting, posting] }] },
            "posting-negative" => state with { VoyageLedgers = [ledger with { Postings = [posting with { GrossSalesCredits = -1 }] }] },
            "fee-conservation" => state with { VoyageLedgers = [ledger with { Postings = ledger.Postings.Select(p => p with { PortFeesAssessedCredits = p.PortFeesAssessedCredits + 1 }).ToArray() }] },
            "missing-terminal" => state with { TradingEconomyContinuation = state.TradingEconomyContinuation! with { DurableTerminalReceiptIds = [] } },
            "settlement-cost" => state with { VoyageFuelSettlements = [state.LastVoyageFuelSettlement! with { RouteFuelCostCredits = state.LastVoyageFuelSettlement!.RouteFuelCostCredits + 1 }] },
            _ => throw new InvalidOperationException()
        };
        string before = Json(state);
        Assert.Throws<ScenarioException>(() => f.Engine.LoadScenario(save with { GameState = bad }, true));
        Assert.Equal(before, Json(f.Save().GameState));
    }

    [Fact]
    public void Legacy_active_history_is_unknown_and_next_current_save_remains_loadable()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        Depart(f);
        var legacy = TradingEconomySaveSchemaTests.WithoutNewContinuation(f.Save(), 14);
        f.Engine.LoadScenario(legacy, true); f.Capture();
        Assert.True(Assert.Single(f.Engine.VoyageFinancesForTests).HasUnknownCostOfGoodsSold);
        Assert.Null(f.Engine.VoyageFinancesForTests[0].NetProfitCredits);
        using var loaded = f.Reload();
        SameFinance(f.Save(), loaded.Save());
    }
}
