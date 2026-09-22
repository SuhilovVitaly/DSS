using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Which side of the market a quote prices. Buy and Refuel take units out of station stock and use the buy
/// spread; Sell puts units into station stock and uses the sell spread (EP-0001-US-0015-TK-0002).
/// </summary>
internal enum TradeQuoteDirection
{
    Buy,
    Sell,
    Refuel,
}

/// <summary>
/// Pure pricing input for one quote row. <see cref="StationAndEventFactorsPermille"/> are fixed-point
/// (1000 = 1.0); <see cref="TargetStock"/> is null for a row without a positive target (legacy market, Fuel
/// service), which keeps the stock factor neutral. <see cref="StaticReasons"/> (base/station/event) are copied
/// to the result unchanged and never affect arithmetic.
/// </summary>
internal sealed record TradeQuotePriceInput(
    long BasePriceCredits,
    IReadOnlyList<int> StationAndEventFactorsPermille,
    long CurrentStock,
    long? TargetStock,
    TradeQuoteDirection Direction,
    long Quantity,
    IReadOnlyList<TradePriceReason> StaticReasons);

/// <summary>
/// Run-length curve (adjacent equal unit prices merged), its checked total and the price reasons.
/// </summary>
internal sealed record TradeQuotePriceResult(
    ImmutableArray<TradePriceStep> Curve,
    long TotalCredits,
    ImmutableArray<TradePriceReason> PriceReasons);

/// <summary>
/// Sequential price curve of EP-0001-US-0015-TK-0002 (epic baseline Board/EP-0001-trading-system/
/// Documentation.md:96). Each unit is priced from the virtual stock it sees before it moves:
/// <list type="number">
/// <item><c>stockFactor = clamp(1 + 0.70 × (1 − stock / target), 0.65, 1.70)</c>, or 1 without a target;</item>
/// <item><c>raw = Product(station/event factors) × stockFactor × spread</c> (buy/refuel 1.15, sell 0.85);</item>
/// <item><c>final = clamp(raw, 0.50, 3.00)</c> on the exact ratio;</item>
/// <item><c>unitPrice = RoundAwayFromZero(base × final)</c> — one division and one rounding, checked
/// <see cref="long"/>, minimum 1;</item>
/// <item>then the virtual stock moves by one: −1 for Buy/Refuel, +1 for Sell.</item>
/// </list>
/// Money and factors are decimal/long only, never float/double. The calculator knows no budgets or
/// capacities: the issuer passes an already limited quantity (or asks <see cref="MaxAffordablePrefix"/>).
/// Invalid input throws <see cref="ArgumentException"/>-family exceptions and arithmetic overflow throws
/// <see cref="OverflowException"/>, both before any result exists.
/// </summary>
internal static class TradeQuoteCalculator
{
    internal const int StockSlopePermille = 700;
    internal const int MinStockFactorPermille = 650;
    internal const int MaxStockFactorPermille = 1700;
    internal const int BuySpreadPermille = 1150;
    internal const int SellSpreadPermille = 850;
    internal const int MinFinalMultiplierPermille = 500;
    internal const int MaxFinalMultiplierPermille = 3000;
    internal const int PermilleDenominator = 1000;

    internal const string BasePriceReason = "base_price";
    internal const string StationProfileReason = "station_profile";
    internal const string EventReason = "event";
    internal const string StockShortageReason = "stock_shortage";
    internal const string StockNormalReason = "stock_normal";
    internal const string StockSurplusReason = "stock_surplus";
    internal const string BuySpreadReason = "buy_spread";
    internal const string SellSpreadReason = "sell_spread";
    internal const string PriceFloorReason = "price_floor";
    internal const string PriceCeilingReason = "price_ceiling";

    internal static TradeQuotePriceResult Calculate(TradeQuotePriceInput input)
    {
        var pricing = Pricing.Create(input, input.Quantity);

        var curve = ImmutableArray.CreateBuilder<TradePriceStep>();
        long total = 0;
        bool floorHit = false;
        bool ceilingHit = false;
        long stock = input.CurrentStock;
        long remaining = input.Quantity;

        while (remaining > 0)
        {
            long price = pricing.UnitPrice(stock, out var clamp, out bool constantTail);
            floorHit |= clamp == PriceClampKind.Floor;
            ceilingHit |= clamp == PriceClampKind.Ceiling;

            // A saturated unit price holds for every later unit, so the rest of the request is one run.
            long run = constantTail ? remaining : 1;
            if (curve.Count > 0 && curve[^1].UnitPriceCredits == price)
                curve[^1] = new TradePriceStep(checked(curve[^1].Quantity + run), price);
            else
                curve.Add(new TradePriceStep(run, price));

            total = checked(total + checked(price * run));
            remaining -= run;
            if (remaining > 0)
                stock = pricing.MoveStock(stock);
        }

        var reasons = ImmutableArray.CreateBuilder<TradePriceReason>(input.StaticReasons.Count + 4);
        reasons.AddRange(input.StaticReasons);
        reasons.Add(pricing.StockBandReason(input.CurrentStock));
        reasons.Add(pricing.SpreadReason);
        if (floorHit)
            reasons.Add(new TradePriceReason(PriceFloorReason, MinFinalMultiplierPermille));
        if (ceilingHit)
            reasons.Add(new TradePriceReason(PriceCeilingReason, MaxFinalMultiplierPermille));

        return new TradeQuotePriceResult(curve.ToImmutable(), total, reasons.ToImmutable());
    }

    /// <summary>
    /// Largest prefix <c>q ≤ physicalCap</c> of the curve for <paramref name="input"/> whose checked total is
    /// <c>≤ creditLimit</c> (<see cref="TradeQuotePriceInput.Quantity"/> is ignored). Walks unit by unit only
    /// until the unit price saturates (no target, stock factor at its bound in the direction of travel, or final
    /// multiplier at its bound in that direction); the constant tail is then counted by division.
    /// </summary>
    internal static long MaxAffordablePrefix(TradeQuotePriceInput input, long physicalCap, long creditLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(physicalCap);
        ArgumentOutOfRangeException.ThrowIfNegative(creditLimit);
        var pricing = Pricing.Create(input, physicalCap);

        long quantity = 0;
        long total = 0;
        long stock = input.CurrentStock;
        while (quantity < physicalCap)
        {
            long price = pricing.UnitPrice(stock, out _, out bool constantTail);
            long budget = creditLimit - total;
            if (constantTail)
                return quantity + Math.Min(physicalCap - quantity, budget / price);

            if (price > budget)
                return quantity;

            total += price;
            quantity++;
            if (quantity < physicalCap)
                stock = pricing.MoveStock(stock);
        }

        return quantity;
    }

    private readonly struct Pricing
    {
        private readonly long _basePrice;
        private readonly decimal _factorProduct;
        private readonly long? _target;
        private readonly bool _sell;
        private readonly int _spread;

        private Pricing(long basePrice, decimal factorProduct, long? target, bool sell)
        {
            _basePrice = basePrice;
            _factorProduct = factorProduct;
            _target = target;
            _sell = sell;
            _spread = sell ? SellSpreadPermille : BuySpreadPermille;
        }

        internal TradePriceReason SpreadReason =>
            new(_sell ? SellSpreadReason : BuySpreadReason, _spread);

        internal static Pricing Create(TradeQuotePriceInput input, long quantity)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(input.StationAndEventFactorsPermille, nameof(input.StationAndEventFactorsPermille));
            ArgumentNullException.ThrowIfNull(input.StaticReasons, nameof(input.StaticReasons));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.BasePriceCredits, nameof(input.BasePriceCredits));
            ArgumentOutOfRangeException.ThrowIfNegative(quantity, nameof(input.Quantity));
            ArgumentOutOfRangeException.ThrowIfNegative(input.CurrentStock, nameof(input.CurrentStock));
            if (input.TargetStock is { } target)
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(target, nameof(input.TargetStock));
            for (int i = 0; i < input.StationAndEventFactorsPermille.Count; i++)
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                    input.StationAndEventFactorsPermille[i], nameof(input.StationAndEventFactorsPermille));

            bool sell;
            switch (input.Direction)
            {
                case TradeQuoteDirection.Buy:
                case TradeQuoteDirection.Refuel:
                    sell = false;
                    // Units leave the stock one by one; the virtual stock never goes below zero.
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, input.CurrentStock, nameof(input.Quantity));
                    break;
                case TradeQuoteDirection.Sell:
                    sell = true;
                    _ = checked(input.CurrentStock + quantity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input), input.Direction, "Unknown trade quote direction.");
            }

            return new Pricing(
                input.BasePriceCredits,
                StationPricing.FactorProduct(input.StationAndEventFactorsPermille),
                input.TargetStock,
                sell);
        }

        internal long MoveStock(long stock) => _sell ? checked(stock + 1) : checked(stock - 1);

        /// <summary>
        /// Unit price at <paramref name="stock"/>. <paramref name="constantTail"/> is true when every later unit
        /// in the direction of travel has exactly this price.
        /// </summary>
        internal long UnitPrice(long stock, out PriceClampKind clamp, out bool constantTail)
        {
            // Stock factor as a ratio over (1000 × target): 1700·T − 700·S, clamped to [650·T, 1700·T]. Kept
            // undivided so the only division is the final one inside ComputeClampedUnitPriceCredits.
            decimal stockNumerator;
            decimal stockDenominator;
            bool stockSaturated;
            if (_target is { } target)
            {
                decimal raw = RawStockNumerator(target, stock);
                decimal low = (decimal)MinStockFactorPermille * target;
                decimal high = (decimal)MaxStockFactorPermille * target;
                stockNumerator = Math.Clamp(raw, low, high);
                stockDenominator = (decimal)PermilleDenominator * target;
                stockSaturated = _sell ? raw <= low : raw >= high;
            }
            else
            {
                stockNumerator = PermilleDenominator;
                stockDenominator = PermilleDenominator;
                stockSaturated = true;
            }

            decimal numerator = _factorProduct * stockNumerator * _spread;
            decimal denominator = stockDenominator * PermilleDenominator;
            long price = StationPricing.ComputeClampedUnitPriceCredits(
                _basePrice, numerator, denominator, MinFinalMultiplierPermille, MaxFinalMultiplierPermille, out clamp);

            // Buy/Refuel raise the multiplier unit by unit, Sell lowers it: saturation holds only at the bound
            // the curve is moving toward.
            constantTail = stockSaturated || clamp == (_sell ? PriceClampKind.Floor : PriceClampKind.Ceiling);
            return price;
        }

        /// <summary>
        /// Starting stock band: the stock factor of the first unit above, at or below 1.00. No target is neutral.
        /// </summary>
        internal TradePriceReason StockBandReason(long stock)
        {
            if (_target is not { } target)
                return new TradePriceReason(StockNormalReason, PermilleDenominator);

            decimal clamped = Math.Clamp(
                RawStockNumerator(target, stock),
                (decimal)MinStockFactorPermille * target,
                (decimal)MaxStockFactorPermille * target);
            decimal neutral = (decimal)PermilleDenominator * target;
            string code = clamped > neutral ? StockShortageReason
                : clamped < neutral ? StockSurplusReason
                : StockNormalReason;
            int permille = (int)Math.Round(clamped / target, MidpointRounding.AwayFromZero);
            return new TradePriceReason(code, permille);
        }

        // 1000 × target × (1 + 0.70 × (1 − stock / target)) = 1700·target − 700·stock.
        private static decimal RawStockNumerator(long target, long stock) =>
            ((decimal)MaxStockFactorPermille * target) - ((decimal)StockSlopePermille * stock);
    }
}
