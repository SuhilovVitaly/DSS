namespace DeepSpaceSaga.Contracts;

/// <summary>Stable command type IDs for station trading (Buy/Sell cargo goods, Refuel).</summary>
public static class TradeCommandTypes
{
    public const string Buy = "trade.buy";
    public const string Sell = "trade.sell";
    public const string Refuel = "trade.refuel";
}
