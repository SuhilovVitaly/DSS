using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageProfitAccountingTests
{
    private static TradingVoyageFixture Create(long quantity = 25, long? basis = 0, long? budget = null, long credits = 1_000_000) =>
        TradingVoyageFixture.Create(calendarRatio: 1, initialCredits: credits, destinationBudget: budget, adjust: save =>
        {
            var ship = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
            string item = save.GameState.SpaceObjects.Single(o => o.ObjectId == ship.DockedStationObjectId)
                .Inventory!.Where(c => c.Quantity > 0 && c.ItemTypeId != "item.fuel").OrderBy(c => c.ItemTypeId, StringComparer.Ordinal).First().ItemTypeId;
            return save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != ship.ObjectId ? o : o with
                    {
                        Modules = o.Modules!.Select(m => m.ModuleId != QuotedTradeExecutionTests.CargoModuleId ? m : m with
                        {
                            Cargo = (m.Cargo ?? []).Where(c => c.ItemTypeId != item).Append(new CargoStackData(item, quantity, basis,
                                basis is null ? ["legacy-unknown"] : ["produced"])).ToArray()
                        }).ToArray()
                    }).ToArray()
                }
            };
        });
    private static VoyageFinanceSnapshot Latest(TradingVoyageFixture f) => f.Snapshot.VoyageFinances[^1];
    private static long Formula(VoyageFinanceSnapshot f) => checked(f.GrossSalesCredits - f.CostOfGoodsSoldCredits!.Value
        - f.RouteFuelCostCredits - f.PortFeesAssessedCredits - f.EventCostsCredits + f.PassengerPayoutCredits - f.PassengerPenaltyCredits);

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Known_sale_can_publish_exact_profit_loss_and_break_even(int sign)
    {
        using var probe = Create();
        probe.FlyTo(probe.Destination);
        var probeReceipt = probe.Trade(TradeCommandTypes.Sell, probe.OutboundItem, 25);
        long basis = probeReceipt.TotalCredits - Latest(probe).RouteFuelCostCredits - Latest(probe).PortFeesAssessedCredits - sign;
        Assert.True(basis >= 0);
        using var f = Create(basis: basis);
        f.FlyTo(f.Destination);
        var sale = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 25);
        var report = Latest(f);
        Assert.Equal(25, sale.ExecutedQuantity);
        Assert.Equal(sale.TotalCredits, report.GrossSalesCredits);
        Assert.Equal(basis, report.CostOfGoodsSoldCredits);
        Assert.Equal(sign, report.NetProfitCredits);
        Assert.Equal(Formula(report), report.NetProfitCredits);
        Assert.DoesNotContain(report.UnsoldCargo, c => c.ItemTypeId == f.OutboundItem);
        Assert.Equal(0, report.EventCostsCredits);
        Assert.Equal(0, report.PassengerPayoutCredits);
        Assert.Equal(0, report.PassengerPenaltyCredits);
    }

    [Fact]
    public void Partial_sell_uses_executed_quantity_and_leaves_unsold_remainder()
    {
        using var f = Create(quantity: 25, basis: 250, budget: 50);
        f.FlyTo(f.Destination);
        var sale = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 25);
        Assert.InRange(sale.ExecutedQuantity, 1, 24);
        var report = Latest(f);
        var cargo = report.UnsoldCargo.Single(c => c.ItemTypeId == f.OutboundItem);
        Assert.Equal(25 - sale.ExecutedQuantity, cargo.Quantity);
        Assert.Equal(250 - sale.RealizedCargoCostCredits, cargo.CostBasisCredits);
        Assert.Equal(sale.TotalCredits, report.GrossSalesCredits);
        Assert.Equal(sale.RealizedCargoCostCredits, report.CostOfGoodsSoldCredits);
        Assert.Equal(Formula(report), report.NetProfitCredits);
    }

    [Fact]
    public void Sale_above_carried_quantity_allocates_receipt_once_and_excludes_local_purchase()
    {
        using var f = Create(quantity: 1, basis: 1000);
        f.FlyTo(f.Destination);
        var before = Latest(f);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 10);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(Latest(f)));
        var sale = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 8);
        var report = Latest(f);
        Assert.Equal((long)decimal.Round((decimal)sale.TotalCredits / 8, 0, MidpointRounding.AwayFromZero), report.GrossSalesCredits);
        Assert.Equal((long)decimal.Round((decimal)sale.RealizedCargoCostCredits!.Value / 8, 0, MidpointRounding.AwayFromZero), report.CostOfGoodsSoldCredits);
        Assert.DoesNotContain(report.UnsoldCargo, c => c.ItemTypeId == f.OutboundItem);
        Assert.Equal(3, f.Cargo(f.OutboundItem));
        string finance = JsonSerializer.Serialize(report);
        f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 3);
        Assert.Equal(finance, JsonSerializer.Serialize(Latest(f)));
    }

    [Fact]
    public void Unknown_cogs_keeps_gross_sales_but_net_unavailable()
    {
        using var f = Create(basis: null);
        f.FlyTo(f.Destination);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 1);
        var first = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 3);
        var report = Latest(f);
        Assert.Equal(first.TotalCredits, report.GrossSalesCredits);
        Assert.True(report.HasUnknownCostOfGoodsSold);
        Assert.Null(report.CostOfGoodsSoldCredits);
        Assert.Null(report.NetProfitCredits);
        Assert.Null(report.UnsoldCargo.Single(c => c.ItemTypeId == f.OutboundItem).CostBasisCredits);
        var second = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 2);
        Assert.Equal(first.TotalCredits + second.TotalCredits, Latest(f).GrossSalesCredits);
        Assert.Null(Latest(f).NetProfitCredits);
    }

    [Fact]
    public void Local_purchase_can_make_captured_residual_basis_unprovable_without_inventing_negative_value()
    {
        using var f = Create(quantity: 10);
        f.FlyTo(f.Destination);
        f.Trade(TradeCommandTypes.Buy, f.OutboundItem, 10);
        var sale = f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 3);
        Assert.True(sale.RealizedCargoCostCredits > 0);
        Assert.Equal(sale.RealizedCargoCostCredits, Latest(f).CostOfGoodsSoldCredits);
        var remainder = Latest(f).UnsoldCargo.Single(c => c.ItemTypeId == f.OutboundItem);
        Assert.Equal(7, remainder.Quantity);
        Assert.Null(remainder.CostBasisCredits);
        Assert.Equal(Formula(Latest(f)), Latest(f).NetProfitCredits);
    }

    [Fact]
    public void Port_debt_reduces_net_by_assessed_fee_not_paid_fee()
    {
        using var f = Create(credits: 0);
        f.FlyTo(f.Destination);
        var report = Latest(f);
        Assert.Equal(100, report.PortFeesAssessedCredits);
        Assert.Equal(0, report.PortFeesPaidCredits);
        Assert.Equal(100, report.OutstandingPortFeeDebtCredits);
        Assert.Equal(-report.RouteFuelCostCredits - 100, report.NetProfitCredits);
        f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 3);
        Assert.Equal(Formula(Latest(f)), Latest(f).NetProfitCredits);
    }

    [Fact]
    public void Repeated_command_or_snapshot_does_not_duplicate_voyage_sale()
    {
        using var f = Create();
        f.FlyTo(f.Destination);
        var q = f.Engine.GetTradeQuote(new("dedupe-sale", f.Snapshot.PlayerShipObjectId!, QuotedTradeExecutionTests.CargoModuleId,
            TradeCommandTypes.Sell, f.OutboundItem, 3));
        var (command, result) = f.Send(QuotedTradeExecutionTests.CargoModuleId, TradeCommandTypes.Sell, item: f.OutboundItem, quantity: 3, quote: q);
        Assert.Equal(CommandResultStatus.Executed, result!.Status);
        string before = JsonSerializer.Serialize(f.Snapshot.VoyageFinances);
        f.Replay(command); f.Capture(); f.Capture();
        Assert.Equal(before, JsonSerializer.Serialize(f.Snapshot.VoyageFinances));
        var retained = f.Snapshot.VoyageFinances;
        f.Trade(TradeCommandTypes.Sell, f.OutboundItem, 1);
        Assert.Equal(before, JsonSerializer.Serialize(retained));
        Assert.NotEqual(before, JsonSerializer.Serialize(f.Snapshot.VoyageFinances));
    }

    [Fact]
    public void Overflow_rejects_sale_without_world_money_cargo_or_ledger_mutation()
    {
        using var f = Create();
        f.FlyTo(f.Destination);
        f.Engine.RecordVoyageAmount("overflow-boundary", SimulationEngine.VoyageAmountKind.PassengerPayout, long.MaxValue);
        f.Capture();
        string before = JsonSerializer.Serialize(f.Engine.RuntimeObjects);
        string finance = JsonSerializer.Serialize(f.Snapshot.VoyageFinances);
        long credits = f.Snapshot.PlayerCredits;
        var q = f.Engine.GetTradeQuote(new("overflow-sale", f.Snapshot.PlayerShipObjectId!, QuotedTradeExecutionTests.CargoModuleId,
            TradeCommandTypes.Sell, f.OutboundItem, 25));
        Assert.Null(q.DisabledReason);
        var (_, result) = f.Send(QuotedTradeExecutionTests.CargoModuleId, TradeCommandTypes.Sell, item: f.OutboundItem, quantity: 25, quote: q);
        Assert.Equal(CommandResultStatus.Rejected, result!.Status);
        Assert.Equal("value_overflow", result.ReasonCode);
        Assert.Equal(credits, f.Snapshot.PlayerCredits);
        Assert.Equal(before, JsonSerializer.Serialize(f.Engine.RuntimeObjects));
        Assert.Equal(finance, JsonSerializer.Serialize(f.Snapshot.VoyageFinances));
        Assert.Null(result.TradeReceipt!.GrossResultCredits);
    }
}
