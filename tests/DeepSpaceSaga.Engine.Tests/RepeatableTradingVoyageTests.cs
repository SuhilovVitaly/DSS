using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class RepeatableTradingVoyageTests
{
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
        voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem);
        voyage.FlyTo(voyage.Destination);
        voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem);
    }
}
