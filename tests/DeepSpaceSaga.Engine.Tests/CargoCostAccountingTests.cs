using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CargoCostAccountingTests
{
    private const string Ice = "item.ice";
    private static ScenarioFile Save(SimulationEngine e) => e.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
    private static CargoStackData? Stack(SimulationEngine e, string item = Ice) => Save(e).GameState.SpaceObjects
        .Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId).Modules!.Single(m => m.ModuleId == QuotedTradeExecutionTests.CargoModuleId)
        .Cargo!.SingleOrDefault(c => c.ItemTypeId == item);
    private static SimulationEngine Create(long quantity = 0, long basis = 0) => QuotedTradeExecutionTests.CreateMarketEngine(adjust: save =>
        QuotedTradeExecutionTests.WithShipModules(save, m => m.ModuleId == QuotedTradeExecutionTests.CargoModuleId ? m with
        { Cargo = quantity == 0 ? [] : [new(Ice, quantity, basis, ["produced"])] } : m));
    private static (PlayerCommand Command, CommandResult Result) Trade(SimulationEngine e, string id, string type, long quantity)
    {
        var quote = QuotedTradeExecutionTests.Quote(e, type, Ice, quantity);
        Assert.Null(quote.DisabledReason);
        var command = QuotedTradeExecutionTests.Bind(id, quote);
        var result = QuotedTradeExecutionTests.Apply(e, command);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(quote.TotalCredits, result.TradeReceipt!.TotalCredits);
        return (command, result);
    }

    [Theory]
    [InlineData(2L, 1L, 1L, 1L)]
    [InlineData(3L, 10L, 1L, 3L)]
    [InlineData(3L, 10L, 2L, 7L)]
    [InlineData(3L, 10L, 3L, 10L)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue / 2, long.MaxValue / 2)]
    public void Weighted_removal_conserves_basis_with_midpoint_away_and_exact_full_sale(long quantity, long basis, long removed, long expected)
    {
        var source = new CargoStackRuntime(1, quantity, basis, ["mined"]);
        var result = SimulationEngine.RemoveCargoCost(source, removed);
        Assert.Equal(expected, result.RealizedCostCredits);
        Assert.Equal(basis, (result.Remaining?.CostBasisCredits ?? 0) + result.RealizedCostCredits);
        Assert.Equal(quantity - removed, result.Remaining?.Quantity ?? 0);
        if (removed == quantity) Assert.Null(result.Remaining);
    }

    [Fact]
    public void Source_union_is_sorted_and_unknown_mixing_remains_unknown_until_exhausted()
    {
        var source = new CargoStackRuntime(1, 3, 9, ["produced"]);
        var added = SimulationEngine.AddCargoCost(source, 1, 2, 7, "purchased");
        added = SimulationEngine.AddCargoCost(added, 1, 1, 0, "mined");
        Assert.Equal(6, added.Quantity);
        Assert.Equal(16, added.CostBasisCredits);
        Assert.Equal(new[] { "mined", "produced", "purchased" }, added.AcquisitionSources);
        var unknown = SimulationEngine.AddCargoCost(new(1, 3, null, ["legacy-unknown"]), 1, 2, 7, "purchased");
        Assert.Null(unknown.CostBasisCredits);
        Assert.Equal(new[] { "legacy-unknown" }, unknown.AcquisitionSources);
        var removed = SimulationEngine.RemoveCargoCost(unknown, 5);
        Assert.Null(removed.RealizedCostCredits);
        Assert.Null(removed.Remaining);
        var fresh = SimulationEngine.AddCargoCost(removed.Remaining, 1, 2, 7, "purchased");
        Assert.Equal(7, fresh.CostBasisCredits);
    }

    [Fact]
    public void Two_real_purchases_and_partial_sale_save_reload_replay_match_exact_basis_and_receipt()
    {
        using var engine = Create();
        var first = Trade(engine, "cost-buy-1", TradeCommandTypes.Buy, 60).Result.TradeReceipt!;
        var second = Trade(engine, "cost-buy-2", TradeCommandTypes.Buy, 10).Result.TradeReceipt!;
        Assert.NotEqual(first.TotalCredits * second.ExecutedQuantity, second.TotalCredits * first.ExecutedQuantity);
        long pooled = first.TotalCredits + second.TotalCredits;
        Assert.Equal(70, Stack(engine)!.Quantity);
        Assert.Equal(pooled, Stack(engine)!.CostBasisCredits);
        Assert.Equal(new[] { "purchased" }, Stack(engine)!.AcquisitionSources);
        Assert.Null(first.RealizedCargoCostCredits);
        var bounded = Save(engine);
        bounded = bounded with
        {
            GameState = bounded.GameState with
            {
                SpaceObjects = bounded.GameState.SpaceObjects.Select(o =>
            o.ObjectId == QuotedTradeExecutionTests.StationId ? o with { Credits = 50, MarketBudgetCredits = 50 } : o).ToArray()
            }
        };
        engine.LoadScenario(bounded, true);
        var (command, result) = Trade(engine, "partial-cost", TradeCommandTypes.Sell, 70);
        var receipt = result.TradeReceipt!;
        Assert.InRange(receipt.ExecutedQuantity, 1, 69);
        long realized = (long)decimal.Round((decimal)pooled * receipt.ExecutedQuantity / 70, 0, MidpointRounding.AwayFromZero);
        Assert.Equal(realized, receipt.RealizedCargoCostCredits);
        Assert.Equal(receipt.TotalCredits - realized, receipt.GrossResultCredits);
        Assert.Equal(70 - receipt.ExecutedQuantity, Stack(engine)!.Quantity);
        Assert.Equal(pooled - realized, Stack(engine)!.CostBasisCredits);
        string before = QuotedTradeExecutionTests.WorldProjection(engine);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(QuotedTradeExecutionTests.Apply(engine, command)));
        Assert.Equal(before, QuotedTradeExecutionTests.WorldProjection(engine));
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Save(engine)), true), true);
        string loadedBefore = QuotedTradeExecutionTests.WorldProjection(loaded);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(QuotedTradeExecutionTests.Apply(loaded, command)));
        Assert.Equal(loadedBefore, QuotedTradeExecutionTests.WorldProjection(loaded));
        Assert.Equal(Stack(engine)!.CostBasisCredits, Stack(loaded)!.CostBasisCredits);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Full_sale_reports_known_loss_break_even_or_profit_and_removes_all_basis(int resultSign)
    {
        using var engine = Create(3);
        var quote = QuotedTradeExecutionTests.Quote(engine, TradeCommandTypes.Sell, Ice, 3);
        long basis = quote.TotalCredits - resultSign;
        var save = QuotedTradeExecutionTests.WithShipModules(Save(engine), m => m with
        { Cargo = m.Cargo?.Select(c => c.ItemTypeId == Ice ? c with { CostBasisCredits = basis } : c).ToArray() });
        engine.LoadScenario(save, true);
        var receipt = Trade(engine, "full-cost", TradeCommandTypes.Sell, 3).Result.TradeReceipt!;
        Assert.Equal(basis, receipt.RealizedCargoCostCredits);
        Assert.Equal(resultSign, receipt.GrossResultCredits);
        Assert.Null(Stack(engine));
    }

    [Fact]
    public void Legacy_unknown_sale_has_null_cost_result_and_following_acquisition_is_known()
    {
        using var engine = Create(3);
        var old = QuotedTradeExecutionTests.WithShipModules(Save(engine), m => m with
        { Cargo = m.Cargo?.Select(c => c with { CostBasisCredits = null, AcquisitionSources = null }).ToArray() }) with
        { SaveFormatVersion = 12 };
        engine.LoadScenario(old, true);
        Trade(engine, "mixed-purchase", TradeCommandTypes.Buy, 1);
        Assert.Null(Stack(engine)!.CostBasisCredits);
        Assert.Equal(new[] { "legacy-unknown" }, Stack(engine)!.AcquisitionSources);
        var receipt = Trade(engine, "unknown-sale", TradeCommandTypes.Sell, 4).Result.TradeReceipt!;
        Assert.Null(receipt.RealizedCargoCostCredits);
        Assert.Null(receipt.GrossResultCredits);
        Assert.Null(Stack(engine));
        var purchase = Trade(engine, "fresh-purchase", TradeCommandTypes.Buy, 1).Result.TradeReceipt!;
        Assert.Equal(purchase.TotalCredits, Stack(engine)!.CostBasisCredits);
    }

    [Fact]
    public void Overflow_stale_and_replayed_commands_do_not_mutate_basis()
    {
        using var overflow = Create(3, long.MaxValue);
        var quote = QuotedTradeExecutionTests.Quote(overflow, TradeCommandTypes.Buy, Ice, 1);
        string before = QuotedTradeExecutionTests.WorldProjection(overflow);
        var rejected = QuotedTradeExecutionTests.Apply(overflow, QuotedTradeExecutionTests.Bind("basis-overflow", quote));
        Assert.Equal("value_overflow", rejected.ReasonCode);
        Assert.Null(rejected.TradeReceipt!.RealizedCargoCostCredits);
        Assert.Null(rejected.TradeReceipt.GrossResultCredits);
        Assert.Equal(before, QuotedTradeExecutionTests.WorldProjection(overflow));
        using var engine = Create(3, 17);
        var stale = QuotedTradeExecutionTests.Quote(engine, TradeCommandTypes.Sell, Ice, 1);
        Trade(engine, "outdate", TradeCommandTypes.Buy, 1);
        before = QuotedTradeExecutionTests.WorldProjection(engine);
        var result = QuotedTradeExecutionTests.Apply(engine, QuotedTradeExecutionTests.Bind("stale-cost", stale));
        Assert.Equal(CommandReasonCodes.StaleQuote, result.ReasonCode);
        Assert.Equal(before, QuotedTradeExecutionTests.WorldProjection(engine));
        Assert.Null(result.TradeReceipt!.RealizedCargoCostCredits);
    }

    [Fact]
    public void Ration_consumption_reduces_known_basis_with_quantity()
    {
        using var engine = RationScheduleTests.CreateEngine(rations: 8);
        var before = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var snapshot = engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs);
        var after = engine.CaptureSaveStateForTests(snapshot.GameTimeMs, SimulationSpeed.Speed0, snapshot.SimulationTimeMs);
        var old = before.GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId).Modules!.SelectMany(m => m.Cargo ?? []).Single(c => c.ItemTypeId == "item.food-rations");
        var remaining = after.GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId).Modules!.SelectMany(m => m.Cargo ?? []).Single(c => c.ItemTypeId == "item.food-rations");
        Assert.True(remaining.Quantity < old.Quantity);
        Assert.Equal(remaining.Quantity * 20, remaining.CostBasisCredits);
        Assert.Equal(new[] { "bootstrap" }, remaining.AcquisitionSources);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dialogue_grant_and_removal_preserve_basis_or_roll_back_all_candidate_changes(bool fail)
    {
        var (engine, _) = DialogueEffectTests.Create([
            new("AddCargoItem", ItemTypeId: "item.test", Quantity: 2),
            new("RemoveCargoItem", ItemTypeId: "item.test", Quantity: fail ? 99 : 1),
            new("EndDialogue")], initialCargo: [new("item.test", 3, 9, ["produced"])]);
        using (engine)
        {
            var before = engine.CaptureSaveState();
            DialogueTests.Choose(engine, "choose");
            var after = engine.CaptureSaveState();
            var stack = after.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules!.Single().Cargo!.Single();
            Assert.Equal(fail ? 3 : 4, stack.Quantity);
            Assert.Equal(fail ? 9 : 7, stack.CostBasisCredits);
            Assert.Equal(fail ? new[] { "produced" } : ["dialogue-grant", "produced"], stack.AcquisitionSources);
            if (fail) Assert.Equal(JsonSerializer.Serialize(before.GameState.SpaceObjects), JsonSerializer.Serialize(after.GameState.SpaceObjects));
        }
    }

    [Theory]
    [InlineData("negative")]
    [InlineData("one-null")]
    [InlineData("equation")]
    [InlineData("buy")]
    [InlineData("rejected")]
    public void Invalid_saved_cost_receipt_is_rejected_atomically(string corruption)
    {
        using var engine = Create(3, 17);
        var (command, result) = Trade(engine, "saved-cost", TradeCommandTypes.Sell, 1);
        var receipt = result.TradeReceipt!;
        var badResult = corruption switch
        {
            "negative" => result with { TradeReceipt = receipt with { RealizedCargoCostCredits = -1 } },
            "one-null" => result with { TradeReceipt = receipt with { GrossResultCredits = null } },
            "equation" => result with { TradeReceipt = receipt with { GrossResultCredits = receipt.GrossResultCredits + 1 } },
            "buy" => result with { CommandType = TradeCommandTypes.Buy },
            _ => result with { Status = CommandResultStatus.Rejected }
        };
        var save = Save(engine);
        var bad = save with { GameState = save.GameState with { CommandReceipts = save.GameState.CommandReceipts!.Select(r => r.CommandId == command.CommandId ? badResult : r).ToArray() } };
        string before = ScenarioLoader.Serialize(save);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
        Assert.Equal(before, ScenarioLoader.Serialize(Save(engine)));
    }
    [Fact]
    public void Quantity_and_basis_overflow_or_invalid_removal_leave_source_unchanged()
    {
        var source = new CargoStackRuntime(1, long.MaxValue, 17, ["purchased"]);
        Assert.Throws<OverflowException>(() => SimulationEngine.AddCargoCost(source, 1, 1, 0, "purchased"));
        Assert.Equal(long.MaxValue, source.Quantity);
        Assert.Equal(17, source.CostBasisCredits);
        var basisLimit = new CargoStackRuntime(1, 3, long.MaxValue, ["mined"]);
        Assert.Throws<OverflowException>(() => SimulationEngine.AddCargoCost(basisLimit, 1, 1, 1, "purchased"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEngine.RemoveCargoCost(basisLimit, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEngine.RemoveCargoCost(basisLimit, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEngine.AddCargoCost(null, 1, 1, 0, "legacy-unknown"));
        Assert.Equal(long.MaxValue, basisLimit.CostBasisCredits);
    }

}
