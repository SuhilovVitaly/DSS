using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class RepeatableTradingVoyageTests
{
    private const string ShipId = "SPC-0001";
    private const string BridgeId = "MOD-PLAYER-BRIDGE-01";
    private const string CargoId = "MOD-PLAYER-CARGO-01";

    [Fact]
    public void Two_round_trips_complete_without_reloading_or_replacing_cargo()
    {
        using var voyage = TradingVoyageFixture.Create();
        var receipts = new List<TradeExecutionReceipt>();
        for (int round = 0; round < 2; round++)
        {
            receipts.Add(voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem));
            voyage.FlyTo(voyage.Destination);
            receipts.Add(voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem));
            receipts.Add(voyage.Trade(TradeCommandTypes.Buy, voyage.ReturnItem));
            voyage.FlyTo(voyage.Origin);
            receipts.Add(voyage.Trade(TradeCommandTypes.Sell, voyage.ReturnItem));
        }
        Assert.Equal(8, receipts.Count);
        Assert.All(receipts, receipt => Assert.Equal(1, receipt.ExecutedQuantity));
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(17UL)]
    [InlineData(42UL)]
    public void Real_mvp_content_completes_return_load_for_fixed_seed_corpus(ulong seed)
    {
        using var voyage = TradingVoyageFixture.Create(seed, controlled: false);
        long outboundCargo = voyage.Cargo(voyage.OutboundItem);
        long returnCargo = voyage.Cargo(voyage.ReturnItem);
        voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem);
        voyage.FlyTo(voyage.Destination);
        voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem);
        voyage.Trade(TradeCommandTypes.Buy, voyage.ReturnItem);
        voyage.FlyTo(voyage.Origin);
        voyage.Trade(TradeCommandTypes.Sell, voyage.ReturnItem);
        Assert.Equal(outboundCargo, voyage.Cargo(voyage.OutboundItem));
        Assert.Equal(returnCargo, voyage.Cargo(voyage.ReturnItem));
    }

    [Fact]
    public void Flight_and_destination_reject_origin_market_quotes()
    {
        using var voyage = TradingVoyageFixture.Create();
        voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem);
        var oldQuote = voyage.Engine.GetTradeQuote(new TradeQuoteRequest("origin-quote", ShipId,
            CargoId, TradeCommandTypes.Buy, voyage.OutboundItem, 1));
        Assert.Null(oldQuote.DisabledReason);
        var (_, departed) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock,
            target: voyage.Destination);
        Assert.Equal(CommandResultStatus.Executed, departed?.Status);
        Assert.Null(voyage.Snapshot.DockedStationTrade);
        long credits = voyage.Snapshot.PlayerCredits;
        long cargo = voyage.Cargo(voyage.OutboundItem);
        long stock = voyage.Stock(voyage.Origin, voyage.OutboundItem);

        var (_, staleInFlight) = voyage.Send(CargoId, TradeCommandTypes.Buy,
            item: voyage.OutboundItem, quantity: 1, quote: oldQuote);
        var (_, unquotedInFlight) = voyage.Send(CargoId, TradeCommandTypes.Buy,
            item: voyage.OutboundItem, quantity: 1);
        Assert.Equal(CommandResultStatus.Rejected, staleInFlight?.Status);
        Assert.Equal(CommandResultStatus.Rejected, unquotedInFlight?.Status);
        Assert.Equal(credits, voyage.Snapshot.PlayerCredits);
        Assert.Equal(cargo, voyage.Cargo(voyage.OutboundItem));
        Assert.Equal(stock, voyage.Stock(voyage.Origin, voyage.OutboundItem));

        voyage.FinishFlightTo(voyage.Destination);
        Assert.Equal(voyage.Destination, voyage.Snapshot.DockedStationTrade?.StationObjectId);
        long destinationCredits = voyage.Snapshot.PlayerCredits;
        long destinationCargo = voyage.Cargo(voyage.OutboundItem);
        long destinationStock = voyage.Stock(voyage.Destination, voyage.OutboundItem);
        var (_, staleAtDestination) = voyage.Send(CargoId, TradeCommandTypes.Buy,
            item: voyage.OutboundItem, quantity: 1, quote: oldQuote);
        Assert.Equal(CommandResultStatus.Rejected, staleAtDestination?.Status);
        Assert.Equal(destinationCredits, voyage.Snapshot.PlayerCredits);
        Assert.Equal(destinationCargo, voyage.Cargo(voyage.OutboundItem));
        Assert.Equal(destinationStock, voyage.Stock(voyage.Destination, voyage.OutboundItem));
        voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem);
        voyage.Trade(TradeCommandTypes.Buy, voyage.ReturnItem);
        voyage.FlyTo(voyage.Origin);
        long returnCredits = voyage.Snapshot.PlayerCredits;
        long returnCargo = voyage.Cargo(voyage.ReturnItem);
        var (_, staleOnReturn) = voyage.Send(CargoId, TradeCommandTypes.Buy,
            item: voyage.OutboundItem, quantity: 1, quote: oldQuote);
        Assert.Equal(CommandResultStatus.Rejected, staleOnReturn?.Status);
        Assert.Equal(returnCredits, voyage.Snapshot.PlayerCredits);
        Assert.Equal(returnCargo, voyage.Cargo(voyage.ReturnItem));
        voyage.Trade(TradeCommandTypes.Sell, voyage.ReturnItem);
    }

    [Fact]
    public void Partial_sale_and_duplicate_command_preserve_exact_remaining_cargo()
    {
        long budget;
        using (var probe = TradingVoyageFixture.Create(calendarRatio: 1))
        {
            Assert.Equal(3, probe.Trade(TradeCommandTypes.Buy, probe.OutboundItem, 3).ExecutedQuantity);
            probe.FlyTo(probe.Destination);
            var sell = probe.Engine.GetTradeQuote(new TradeQuoteRequest("probe-sell", ShipId,
                CargoId, TradeCommandTypes.Sell, probe.OutboundItem, 3));
            Assert.Equal(3, sell.ExecutableQuantity);
            budget = QuotedTradeExecutionTests.PrefixTotal(sell, 2);
        }

        using var voyage = TradingVoyageFixture.Create(destinationBudget: budget, calendarRatio: 1);
        Assert.Equal(3, voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem, 3).ExecutedQuantity);
        voyage.FlyTo(voyage.Destination);
        var quote = voyage.Engine.GetTradeQuote(new TradeQuoteRequest("partial-sell", ShipId,
            CargoId, TradeCommandTypes.Sell, voyage.OutboundItem, 3));
        Assert.True(quote.ExecutableQuantity == 2,
            $"expected partial quote, got {quote.ExecutableQuantity}; configured budget={budget}, " +
            $"runtime budget={voyage.Save().GameState.SpaceObjects.Single(o => o.ObjectId == voyage.Destination).MarketBudgetCredits}, " +
            $"total={quote.TotalCredits}");
        long stockBefore = voyage.Stock(voyage.Destination, voyage.OutboundItem);
        long creditsBefore = voyage.Snapshot.PlayerCredits;
        var (command, result) = voyage.Send(CargoId, TradeCommandTypes.Sell,
            item: voyage.OutboundItem, quantity: 3, quote: quote);
        var receipt = Assert.IsType<TradeExecutionReceipt>(result?.TradeReceipt);
        Assert.Equal(2, receipt.ExecutedQuantity);
        Assert.Equal(1, voyage.Cargo(voyage.OutboundItem));
        Assert.Equal(stockBefore + 2, voyage.Stock(voyage.Destination, voyage.OutboundItem));
        Assert.Equal(creditsBefore + receipt.TotalCredits, voyage.Snapshot.PlayerCredits);

        voyage.Replay(command);
        Assert.Equal(1, voyage.Cargo(voyage.OutboundItem));
        Assert.Equal(stockBefore + 2, voyage.Stock(voyage.Destination, voyage.OutboundItem));
        Assert.Equal(creditsBefore + receipt.TotalCredits, voyage.Snapshot.PlayerCredits);
    }

    [Fact]
    public void Port_debt_rejects_departure_without_creating_voyage()
    {
        using var voyage = TradingVoyageFixture.Create(initialDebt: 7);
        var before = voyage.Snapshot;
        var (_, rejected) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock,
            target: voyage.Destination);
        Assert.Equal(CommandResultStatus.Rejected, rejected?.Status);
        Assert.Equal(CommandReasonCodes.VoyageOutstandingDebt, rejected?.ReasonCode);
        Assert.Null(voyage.Snapshot.ActiveVoyage);
        Assert.Equal(voyage.Origin, voyage.CurrentStationId);
        Assert.Equal(before.PortFees, voyage.Snapshot.PortFees);
        Assert.Equal(before.PlayerCredits, voyage.Snapshot.PlayerCredits);
    }

    [Fact]
    public void Duplicate_lifecycle_command_preserves_voyage_and_optional_fuel_reservation()
    {
        using var voyage = TradingVoyageFixture.Create();
        var (command, accepted) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock,
            target: voyage.Destination);
        Assert.Equal(CommandResultStatus.Executed, accepted?.Status);
        var active = Assert.IsType<VoyageSnapshot>(voyage.Snapshot.ActiveVoyage);
        long? fuel = voyage.Save().GameState.SpaceObjects.Single(o => o.ObjectId == ShipId)
            .Modules!.Single(module => module.ModuleId == "MOD-PLAYER-ENGINE-01").FuelAmountKg;

        voyage.Replay(command);
        Assert.Equal(active.VoyageId, voyage.Snapshot.ActiveVoyage?.VoyageId);
        Assert.Equal(active.ReservedFuelKg, voyage.Snapshot.ActiveVoyage?.ReservedFuelKg);
        Assert.Equal(fuel, voyage.Save().GameState.SpaceObjects.Single(o => o.ObjectId == ShipId)
            .Modules!.Single(module => module.ModuleId == "MOD-PLAYER-ENGINE-01").FuelAmountKg);
    }

    [Fact]
    public void Old_port_stops_billing_and_destination_starts_one_new_stay()
    {
        using var voyage = TradingVoyageFixture.Create();
        long credits = voyage.Snapshot.PlayerCredits;
        var oldFees = Assert.IsType<PortFeeSnapshot>(voyage.Snapshot.PortFees);
        var (_, accepted) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock,
            target: voyage.Destination);
        Assert.Equal(CommandResultStatus.Executed, accepted?.Status);
        Assert.Null(voyage.Snapshot.PortFees);
        voyage.Advance((oldFees.NextPortFeeDueGameTimeMs - voyage.Snapshot.GameTimeMs) / 300 + 1);
        Assert.Equal(credits, voyage.Snapshot.PlayerCredits);
        Assert.Null(voyage.Snapshot.DockedStationTrade);
        Assert.DoesNotContain(voyage.Snapshot.ShipEvents,
            shipEvent => shipEvent.EventType == "port_fee_renewed");
        voyage.FinishFlightTo(voyage.Destination);
        var newFees = Assert.IsType<PortFeeSnapshot>(voyage.Snapshot.PortFees);
        Assert.Equal(voyage.Snapshot.GameTimeMs, newFees.FirstPortFeeGameTimeMs);
        Assert.Equal(newFees.FirstPortFeeGameTimeMs + GameCalendar.DayMs,
            newFees.NextPortFeeDueGameTimeMs);
        Assert.Equal(credits - 100, voyage.Snapshot.PlayerCredits);
        voyage.Replay(Assert.IsType<PlayerCommand>(voyage.LastDockCommand));
        Assert.Equal(newFees, voyage.Snapshot.PortFees);
        Assert.Equal(credits - 100, voyage.Snapshot.PlayerCredits);
        voyage.Advance(GameCalendar.DayMs / 300);
        Assert.Equal(credits - 200, voyage.Snapshot.PlayerCredits);
        Assert.Single(voyage.Snapshot.ShipEvents,
            shipEvent => shipEvent.EventType == "port_fee_renewed");
    }

    [Fact]
    public void Snapshot_cadence_does_not_change_round_trip_receipts_or_approach_speed()
    {
        static (TradeExecutionReceipt[] Receipts, string[] VoyageIds, double[] Speeds,
            long Credits, long OutboundCargo, long ReturnCargo) Run(bool split)
        {
            using var voyage = TradingVoyageFixture.Create();
            var receipts = new List<TradeExecutionReceipt>();
            for (int round = 0; round < 2; round++)
            {
                receipts.Add(voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem));
                voyage.FlyTo(voyage.Destination, split);
                receipts.Add(voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem));
                receipts.Add(voyage.Trade(TradeCommandTypes.Buy, voyage.ReturnItem));
                voyage.FlyTo(voyage.Origin, split);
                receipts.Add(voyage.Trade(TradeCommandTypes.Sell, voyage.ReturnItem));
            }
            return (receipts.ToArray(), voyage.VoyageIds.ToArray(), voyage.ApproachSpeeds.ToArray(),
                voyage.Snapshot.PlayerCredits, voyage.Cargo(voyage.OutboundItem),
                voyage.Cargo(voyage.ReturnItem));
        }

        var whole = Run(false);
        var split = Run(true);
        Assert.Equal(whole.Receipts.Select(r => (r.StationObjectId, r.ItemTypeId,
            r.QuotedMarketRevision, r.ResultMarketRevision, r.RequestedQuantity,
            r.ExecutedQuantity, r.TotalCredits)),
            split.Receipts.Select(r => (r.StationObjectId, r.ItemTypeId,
                r.QuotedMarketRevision, r.ResultMarketRevision, r.RequestedQuantity,
                r.ExecutedQuantity, r.TotalCredits)));
        Assert.Equal(whole.VoyageIds, split.VoyageIds);
        Assert.Equal(whole.Speeds, split.Speeds);
        Assert.Equal(whole.Credits, split.Credits);
        Assert.Equal((whole.OutboundCargo, whole.ReturnCargo),
            (split.OutboundCargo, split.ReturnCargo));
    }
}
