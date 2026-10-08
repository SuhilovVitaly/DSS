using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageLedgerLifecycleTests
{
    private static VoyageFinanceSnapshot Latest(TradingVoyageFixture f) => f.Engine.VoyageFinancesForTests[^1];
    private static object? Invoke(SimulationEngine e, string name, params object?[] args) =>
        typeof(SimulationEngine).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(e, args);

    [Fact]
    public void Accepted_undock_opens_one_ledger_with_stable_voyage_id_and_opening_cargo()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 3);
        var save = f.Save();
        var (_, result) = f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        Assert.Equal(CommandResultStatus.Executed, result!.Status);
        var report = Assert.Single(f.Engine.VoyageFinancesForTests);
        Assert.Equal(f.Snapshot.ActiveVoyage!.VoyageId, report.VoyageId);
        Assert.Equal(f.Origin, report.OriginStationObjectId);
        Assert.Equal(f.Destination, report.DestinationStationObjectId);
        Assert.Equal(VoyageFinanceStates.InTransit, report.State);
        Assert.Null(report.CompletedGameTimeMs);
        Assert.Equal(0, report.NetProfitCredits);
        var expected = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId)
            .Modules!.SelectMany(m => m.Cargo ?? []).Where(c => c.Quantity > 0).OrderBy(c => c.ItemTypeId, StringComparer.Ordinal);
        Assert.Equal(expected.Select(c => (c.ItemTypeId, c.Quantity, c.CostBasisCredits)),
            report.UnsoldCargo.Select(c => (c.ItemTypeId, c.Quantity, c.CostBasisCredits)));
    }

    [Fact]
    public void Rejected_or_replayed_undock_does_not_open_or_finalize_extra_ledger()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var (_, rejected) = f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock);
        Assert.Equal(CommandResultStatus.Rejected, rejected!.Status);
        Assert.Empty(f.Engine.VoyageFinancesForTests);
        var (accepted, result) = f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        string before = JsonSerializer.Serialize(f.Engine.VoyageFinancesForTests);
        Assert.Equal(result, f.Replay(accepted));
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        Assert.Equal(before, JsonSerializer.Serialize(f.Engine.VoyageFinancesForTests));
    }

    [Fact]
    public void Arrival_posts_route_fuel_once_and_refuel_purchase_is_not_a_second_expense()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination, splitSnapshots: true);
        var report = Latest(f);
        Assert.Equal(VoyageFinanceStates.AwaitingRealization, report.State);
        Assert.Equal(f.Snapshot.LastVoyageFuelSettlement!.VoyageId, report.VoyageId);
        Assert.Equal(f.Snapshot.LastVoyageFuelSettlement.RouteFuelCostCredits, report.RouteFuelCostCredits);
        Assert.Equal(100, report.PortFeesAssessedCredits);
        Assert.Equal(-report.RouteFuelCostCredits - 100, report.NetProfitCredits);
        string before = JsonSerializer.Serialize(report);
        f.Capture(); f.Replay(f.LastDockCommand!);
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
        var tank = f.Snapshot.InstalledModules.Single(m => m.CommandTypeIds.Contains(TradeCommandTypes.Refuel));
        var quote = f.Engine.GetTradeQuote(new("refuel-ledger", f.Snapshot.PlayerShipObjectId!, tank.ModuleId, TradeCommandTypes.Refuel, "item.fuel", 1));
        Assert.Null(quote.DisabledReason);
        f.Send(tank.ModuleId, TradeCommandTypes.Refuel, item: "item.fuel", quantity: 1, quote: quote);
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
    }

    [Theory]
    [InlineData(1000000L, 100L, 0L)]
    [InlineData(25L, 25L, 75L)]
    [InlineData(0L, 0L, 100L)]
    public void Docking_fee_records_assessed_paid_and_debt_on_arrived_voyage(long credits, long paid, long debt)
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1, initialCredits: credits);
        f.FlyTo(f.Destination);
        var report = Latest(f);
        Assert.Equal(100, report.PortFeesAssessedCredits);
        Assert.Equal(paid, report.PortFeesPaidCredits);
        Assert.Equal(debt, report.OutstandingPortFeeDebtCredits);
        Assert.Equal(debt, f.Snapshot.PortFees!.Debt);
        Assert.Equal(-report.RouteFuelCostCredits - 100, report.NetProfitCredits);
    }

    [Fact]
    public void Daily_port_fee_uses_due_boundary_and_duplicate_boundary_is_idempotent()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination);
        long due = f.Snapshot.PortFees!.NextPortFeeDueGameTimeMs;
        f.Advance(due - f.MotionTime - 1);
        Assert.Equal(100, Latest(f).PortFeesAssessedCredits);
        f.Advance(1);
        Assert.Equal(200, Latest(f).PortFeesAssessedCredits);
        Assert.Equal(200, Latest(f).PortFeesPaidCredits);
        string before = JsonSerializer.Serialize(Latest(f));
        f.Capture();
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
    }

    [Fact]
    public void Next_accepted_undock_finalizes_previous_awaiting_realization()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination);
        string id = Latest(f).VoyageId;
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock);
        Assert.Equal(VoyageFinanceStates.AwaitingRealization, Latest(f).State);
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Origin);
        Assert.Equal(2, f.Engine.VoyageFinancesForTests.Length);
        Assert.Equal(id, f.Engine.VoyageFinancesForTests[0].VoyageId);
        Assert.Equal(VoyageFinanceStates.Finalized, f.Engine.VoyageFinancesForTests[0].State);
        Assert.Equal(VoyageFinanceStates.InTransit, Latest(f).State);
    }

    [Fact]
    public void Interrupted_voyage_is_terminal_and_rejects_later_postings()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        var state = f.Save().GameState.VoyageState!;
        // Exercise the exact immutable terminal candidate used before fuel/world mutation.
        var entries = Invoke(f.Engine, "PrepareVoyageTerminal", state, false);
        typeof(SimulationEngine).GetField("_voyageLedgers", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(f.Engine, entries);
        Assert.Equal(VoyageFinanceStates.Interrupted, Latest(f).State);
        string before = JsonSerializer.Serialize(Latest(f));
        f.Engine.RecordVoyageAmount("late", SimulationEngine.VoyageAmountKind.PassengerPayout, 100);
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
    }

    [Fact]
    public void Actual_amount_posting_is_idempotent_and_overflow_leaves_the_ledger_unchanged()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        f.Engine.RecordVoyageAmount("actual-payout", SimulationEngine.VoyageAmountKind.PassengerPayout, long.MaxValue);
        string before = JsonSerializer.Serialize(Latest(f));
        f.Engine.RecordVoyageAmount("actual-payout", SimulationEngine.VoyageAmountKind.PassengerPayout, long.MaxValue);
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
        Assert.Throws<OverflowException>(() => f.Engine.RecordVoyageAmount("overflow", SimulationEngine.VoyageAmountKind.PassengerPayout, 1));
        Assert.Throws<ArgumentException>(() => f.Engine.RecordVoyageAmount("", SimulationEngine.VoyageAmountKind.EventCost, 1));
        Assert.Equal(before, JsonSerializer.Serialize(Latest(f)));
        f.Engine.RecordVoyageAmount("expense", SimulationEngine.VoyageAmountKind.EventCost, 2);
        Assert.Equal(long.MaxValue - 2, Latest(f).NetProfitCredits);
    }

    [Fact]
    public void Daily_fee_overflow_leaves_world_money_debt_and_finance_unchanged()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.FlyTo(f.Destination);
        long due = f.Snapshot.PortFees!.NextPortFeeDueGameTimeMs;
        f.Advance(due - f.MotionTime - 1);
        var report = Latest(f);
        f.Engine.RecordVoyageAmount("expense-bound", SimulationEngine.VoyageAmountKind.EventCost,
            long.MaxValue - report.RouteFuelCostCredits - report.PortFeesAssessedCredits);
        string objects = JsonSerializer.Serialize(f.Engine.RuntimeObjects);
        string finance = JsonSerializer.Serialize(f.Engine.VoyageFinancesForTests);
        long credits = f.Engine.PlayerCredits;
        Assert.Throws<OverflowException>(() => f.Engine.CaptureSnapshotForTests(due, SimulationSpeed.Speed0, due));
        Assert.Equal(objects, JsonSerializer.Serialize(f.Engine.RuntimeObjects));
        Assert.Equal(finance, JsonSerializer.Serialize(f.Engine.VoyageFinancesForTests));
        Assert.Equal(credits, f.Engine.PlayerCredits);
    }

    [Fact]
    public void First_dock_without_inbound_voyage_does_not_invent_finance()
    {
        using var e = PortFeeScheduleTests.DockAt(0);
        e.CaptureSnapshotForTests(GameCalendar.DayMs);
        Assert.Empty(e.VoyageFinancesForTests);
    }

    [Fact]
    public void Ledger_retention_keeps_latest_fifty_without_dropping_active_entry()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        var ids = new List<string>();
        for (int i = 0; i < 52; i++)
        {
            string id = "retention-" + i; ids.Add(id);
            var state = new VoyageStateData(VoyagePhases.InTransit, id, f.Origin, f.Destination);
            Invoke(f.Engine, "BeginVoyageLedger", state, (long)i);
            var entries = Invoke(f.Engine, "PrepareVoyageTransport", id, f.Destination, (long)i, false, null, null);
            typeof(SimulationEngine).GetField("_voyageLedgers", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(f.Engine, entries);
        }
        Assert.Equal(ids.Skip(2), f.Engine.VoyageFinancesForTests.Select(v => v.VoyageId));
        Assert.Equal(50, f.Engine.VoyageFinancesForTests.Length);
        Assert.Equal(VoyageFinanceStates.AwaitingRealization, Latest(f).State);
        Assert.All(f.Engine.VoyageFinancesForTests[..^1], v => Assert.Equal(VoyageFinanceStates.Finalized, v.State));
    }
}
