using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers <see cref="TradeQuoteCalculator"/> — the pure sequential price curve of EP-0001-US-0015-TK-0002
/// (epic baseline Board/EP-0001-trading-system/Documentation.md:96): per unit
/// <c>stockFactor = clamp(1 + 0.70 × (1 − stock / targetStock), 0.65, 1.70)</c> taken from the virtual stock
/// before the unit moves, <c>raw = Product(station/event factors) × stockFactor × spread</c> (buy 1.15,
/// sell 0.85), <c>final = clamp(raw, 0.50, 3.00)</c>, one AwayFromZero round of <c>base × final</c>, minimum 1.
/// </summary>
public class TradeQuoteCalculatorTests
{
    private static TradeQuotePriceInput Input(
        long basePrice,
        long stock,
        long? target,
        TradeQuoteDirection direction,
        long quantity,
        params int[] factors) =>
        new(basePrice, factors, stock, target, direction, quantity, Array.Empty<TradePriceReason>());

    // TradeQuoteDirection is internal, so theory rows carry its name.
    private static TradeQuoteDirection Dir(string name) => Enum.Parse<TradeQuoteDirection>(name);

    private static long FirstUnit(TradeQuotePriceInput input) =>
        TradeQuoteCalculator.Calculate(input with { Quantity = 1 }).Curve.Single().UnitPriceCredits;

    private static long[] ExpandCurve(ImmutableArray<TradePriceStep> curve) =>
        curve.SelectMany(step => Enumerable.Repeat(step.UnitPriceCredits, checked((int)step.Quantity))).ToArray();

    private static long Total(TradeQuotePriceInput input) => TradeQuoteCalculator.Calculate(input).TotalCredits;

    private static string[] Codes(TradeQuotePriceResult result) => result.PriceReasons.Select(r => r.Code).ToArray();

    [Theory]
    // Buy, neutral station factors, base 1000, target 100.
    [InlineData("Buy", 100L, 1150L, "stock_normal")]      // 1.00 × 1.15
    [InlineData("Buy", 50L, 1553L, "stock_shortage")]     // 1.35 × 1.15 = 1.5525 -> 1552.5 -> 1553
    [InlineData("Buy", 1L, 1947L, "stock_shortage")]      // 1.693 × 1.15 = 1.94695 -> 1946.95 -> 1947
    [InlineData("Buy", 150L, 748L, "stock_surplus")]      // 0.65 × 1.15 = 0.7475 -> 747.5 -> 748
    [InlineData("Buy", 400L, 748L, "stock_surplus")]      // stock factor clamped at 0.65
    [InlineData("Sell", 100L, 850L, "stock_normal")]      // 1.00 × 0.85
    [InlineData("Sell", 0L, 1445L, "stock_shortage")]     // 1.70 × 0.85
    [InlineData("Sell", 150L, 553L, "stock_surplus")]     // 0.65 × 0.85 = 0.5525 -> 552.5 -> 553
    [InlineData("Sell", 1000L, 553L, "stock_surplus")]    // stock factor clamped at 0.65
    public void Shortage_normal_and_surplus_follow_baseline_and_final_clamp(
        string directionName, long stock, long expectedUnitPrice, string expectedBand)
    {
        var result = TradeQuoteCalculator.Calculate(Input(1000, stock, 100, Dir(directionName), 1));

        Assert.Equal(expectedUnitPrice, result.Curve.Single().UnitPriceCredits);
        Assert.Equal(expectedUnitPrice, result.TotalCredits);
        Assert.Contains(expectedBand, Codes(result));
        Assert.DoesNotContain("price_floor", Codes(result));
        Assert.DoesNotContain("price_ceiling", Codes(result));
    }

    [Fact]
    public void Shortage_normal_and_surplus_follow_baseline_and_final_clamp_exact_bounds()
    {
        // Exactly 3.00: target 23, stock 13 -> stock factor 30000/23000; 2.0 × 30000/23000 × 1.15 = 3.0.
        var ceilingExact = TradeQuoteCalculator.Calculate(Input(1000, 13, 23, TradeQuoteDirection.Buy, 1, 2000));
        Assert.Equal(3000, ceilingExact.TotalCredits);
        Assert.DoesNotContain("price_ceiling", Codes(ceilingExact));

        // One unit less stock pushes the raw multiplier above 3.00: clamped, same price, reason reported.
        var ceilingClamped = TradeQuoteCalculator.Calculate(Input(1000, 12, 23, TradeQuoteDirection.Buy, 1, 2000));
        Assert.Equal(3000, ceilingClamped.TotalCredits);
        Assert.Contains(new TradePriceReason("price_ceiling", 3000), ceilingClamped.PriceReasons);

        // Exactly 0.50: target 17, stock 23 -> stock factor 12800/17000; 0.625 × 1.25 × 12800/17000 × 0.85 = 0.5.
        var floorExact = TradeQuoteCalculator.Calculate(Input(1000, 23, 17, TradeQuoteDirection.Sell, 1, 625, 1250));
        Assert.Equal(500, floorExact.TotalCredits);
        Assert.DoesNotContain("price_floor", Codes(floorExact));

        // One unit more stock pushes the raw multiplier below 0.50: clamped, same price, reason reported.
        var floorClamped = TradeQuoteCalculator.Calculate(Input(1000, 24, 17, TradeQuoteDirection.Sell, 1, 625, 1250));
        Assert.Equal(500, floorClamped.TotalCredits);
        Assert.Contains(new TradePriceReason("price_floor", 500), floorClamped.PriceReasons);

        // Far outside both bounds.
        Assert.Equal(3000, Total(Input(1000, 1, 100, TradeQuoteDirection.Buy, 1, 3000)));
        Assert.Equal(500, Total(Input(1000, 1000, 100, TradeQuoteDirection.Sell, 1, 300)));
    }

    [Fact]
    public void Buy_curve_raises_later_units_and_sell_curve_lowers_them()
    {
        // Target 10, stock 10: buy units see stock 10, 9, 8, 7, 6 -> factors 1.00, 1.07, 1.14, 1.21, 1.28.
        var buy = TradeQuoteCalculator.Calculate(Input(1000, 10, 10, TradeQuoteDirection.Buy, 5));
        Assert.Equal(new long[] { 1150, 1231, 1311, 1392, 1472 }, ExpandCurve(buy.Curve));
        Assert.Equal(1150 + 1231 + 1311 + 1392 + 1472, buy.TotalCredits);

        // Sell units see stock 10, 11, 12, 13, 14 -> factors 1.00, 0.93, 0.86, 0.79, 0.72.
        var sell = TradeQuoteCalculator.Calculate(Input(1000, 10, 10, TradeQuoteDirection.Sell, 5));
        Assert.Equal(new long[] { 850, 791, 731, 672, 612 }, ExpandCurve(sell.Curve));
        Assert.Equal(850 + 791 + 731 + 672 + 612, sell.TotalCredits);

        // Refuel moves stock like Buy.
        var refuel = TradeQuoteCalculator.Calculate(Input(1000, 10, 10, TradeQuoteDirection.Refuel, 5));
        Assert.Equal(ExpandCurve(buy.Curve), ExpandCurve(refuel.Curve));
    }

    [Theory]
    [InlineData(1L, 100L, 100L, "Buy", 100L)]
    [InlineData(10L, 108L, 108L, "Buy", 108L)]
    [InlineData(10L, 108L, 108L, "Sell", 400L)]
    [InlineData(7L, 3L, 32L, "Sell", 250L)]
    [InlineData(10L, 500L, null, "Buy", 500L)]
    public void Curve_segments_are_adjacent_run_lengths_and_sum_exactly_to_total(
        long basePrice, long stock, long? target, string directionName, long quantity)
    {
        var direction = Dir(directionName);
        var input = Input(basePrice, stock, target, direction, quantity, 1100);
        var result = TradeQuoteCalculator.Calculate(input);

        Assert.All(result.Curve, step => Assert.True(step.Quantity > 0 && step.UnitPriceCredits >= 1));
        for (int i = 1; i < result.Curve.Length; i++)
            Assert.NotEqual(result.Curve[i - 1].UnitPriceCredits, result.Curve[i].UnitPriceCredits);

        Assert.Equal(quantity, result.Curve.Sum(step => step.Quantity));
        Assert.Equal(result.TotalCredits, result.Curve.Sum(step => checked(step.Quantity * step.UnitPriceCredits)));

        // Each unit is independently recomputable from the virtual stock it saw.
        long[] units = ExpandCurve(result.Curve);
        long delta = direction == TradeQuoteDirection.Sell ? 1 : -1;
        for (int k = 0; k < units.Length; k++)
            Assert.Equal(FirstUnit(input with { CurrentStock = stock + (delta * k) }), units[k]);
    }

    [Theory]
    [InlineData("Buy")]
    [InlineData("Sell")]
    [InlineData("Refuel")]
    public void Quantity_one_matches_first_unit_of_larger_curve(string directionName)
    {
        var direction = Dir(directionName);
        var large = TradeQuoteCalculator.Calculate(Input(37, 60, 40, direction, 50, 1300));
        var one = TradeQuoteCalculator.Calculate(Input(37, 60, 40, direction, 1, 1300));

        Assert.Equal(large.Curve[0].UnitPriceCredits, one.Curve.Single().UnitPriceCredits);
        Assert.Equal(1, one.Curve.Single().Quantity);

        // Every shorter request is an exact prefix of the larger curve.
        long[] units = ExpandCurve(large.Curve);
        for (long q = 0; q <= 50; q++)
            Assert.Equal(units.Take((int)q).Sum(), Total(Input(37, 60, 40, direction, q, 1300)));
    }

    [Fact]
    public void Buy_1150_and_sell_850_prevent_immediate_roundtrip_gain()
    {
        long[] targets = { 4, 5, 7, 32, 108 };
        long[] bases = { 1, 7, 10, 1000 };
        int[][] factorSets = { Array.Empty<int>(), new[] { 500 }, new[] { 1500, 1300 }, new[] { 3000, 2000 } };

        foreach (long target in targets)
        {
            foreach (long basePrice in bases)
            {
                foreach (int[] factors in factorSets)
                {
                    // Stock sweeps shortage (below target), normal (at target) and surplus (above, into the
                    // saturated 0.65 tail) boundaries.
                    for (long stock = 0; stock <= target * 2; stock++)
                    {
                        // Same-stock unit: the buy price never falls below the sell price.
                        var buyOne = Input(basePrice, stock, target, TradeQuoteDirection.Buy, 1, factors);
                        var sellOne = Input(basePrice, stock, target, TradeQuoteDirection.Sell, 1, factors);
                        if (stock > 0)
                            Assert.True(FirstUnit(buyOne) >= FirstUnit(sellOne), $"same-stock T={target} S={stock} B={basePrice}");

                        foreach (long quantity in new[] { 1, stock / 2, stock, target })
                        {
                            if (quantity <= 0) continue;

                            // Buy q, then immediately sell the same q back.
                            if (quantity <= stock)
                            {
                                long paid = Total(Input(basePrice, stock, target, TradeQuoteDirection.Buy, quantity, factors));
                                long received = Total(Input(basePrice, stock - quantity, target, TradeQuoteDirection.Sell, quantity, factors));
                                Assert.True(received <= paid, $"buy->sell T={target} S={stock} q={quantity} B={basePrice} paid={paid} received={received}");
                            }

                            // Sell q, then immediately buy the same q back.
                            long got = Total(Input(basePrice, stock, target, TradeQuoteDirection.Sell, quantity, factors));
                            long cost = Total(Input(basePrice, stock + quantity, target, TradeQuoteDirection.Buy, quantity, factors));
                            Assert.True(got <= cost, $"sell->buy T={target} S={stock} q={quantity} B={basePrice} got={got} cost={cost}");
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void Buy_1150_and_sell_850_prevent_immediate_roundtrip_gain_at_ice_target()
    {
        // Real bounded-market Ice row: base 10 (items-resource.json), target 108 (QuotedTradeExecutionTests.IceTarget).
        const long IceTarget = 108;
        foreach (int[] factors in new[] { Array.Empty<int>(), new[] { 500 }, new[] { 1500 }, new[] { 2000, 1100 } })
        {
            for (long stock = 0; stock <= IceTarget * 3; stock++)
            {
                for (long quantity = 1; quantity <= stock; quantity = quantity < 8 ? quantity + 1 : quantity * 2)
                {
                    long paid = Total(Input(10, stock, IceTarget, TradeQuoteDirection.Buy, quantity, factors));
                    long received = Total(Input(10, stock - quantity, IceTarget, TradeQuoteDirection.Sell, quantity, factors));
                    Assert.True(received <= paid, $"buy->sell S={stock} q={quantity} paid={paid} received={received}");

                    long got = Total(Input(10, stock, IceTarget, TradeQuoteDirection.Sell, quantity, factors));
                    long cost = Total(Input(10, stock + quantity, IceTarget, TradeQuoteDirection.Buy, quantity, factors));
                    Assert.True(got <= cost, $"sell->buy S={stock} q={quantity} got={got} cost={cost}");
                }

                // Whole-stock prefix.
                if (stock > 0)
                {
                    long paidAll = Total(Input(10, stock, IceTarget, TradeQuoteDirection.Buy, stock, factors));
                    long receivedAll = Total(Input(10, 0, IceTarget, TradeQuoteDirection.Sell, stock, factors));
                    Assert.True(receivedAll <= paidAll, $"buy-all S={stock}");
                }
            }
        }
    }

    [Fact]
    public void Tiny_target_below_four_is_documented_edge()
    {
        // Risk R6 of story-20260922-165637: with targetStock <= 3 one unit moves the stock factor by >= 0.70 / 3,
        // which can outweigh the 1.15 / 0.85 spread. Pinned here so the edge stays visible, not silently "fixed".
        // Target 2, stock 3: buy at factor 0.65 (0.7475 -> 748), then sell back at stock 2 (factor 1.00 -> 850).
        long paid = Total(Input(1000, 3, 2, TradeQuoteDirection.Buy, 1));
        long received = Total(Input(1000, 2, 2, TradeQuoteDirection.Sell, 1));
        Assert.Equal(748, paid);
        Assert.Equal(850, received);
        Assert.True(received > paid);

        // Target 1, stock 2: same shape (0.65 buy, then 1.00 sell).
        Assert.True(Total(Input(1000, 1, 1, TradeQuoteDirection.Sell, 1)) > Total(Input(1000, 2, 1, TradeQuoteDirection.Buy, 1)));

        // Target 4 is the smallest target the anti-arbitrage guarantee covers: same probe, no gain.
        for (long stock = 1; stock <= 12; stock++)
            Assert.True(Total(Input(1000, stock - 1, 4, TradeQuoteDirection.Sell, 1)) <= Total(Input(1000, stock, 4, TradeQuoteDirection.Buy, 1)));
    }

    [Theory]
    [InlineData("Buy", "buy_spread", 1150, 1150L)]
    [InlineData("Refuel", "buy_spread", 1150, 1150L)]
    [InlineData("Sell", "sell_spread", 850, 850L)]
    public void Legacy_and_refuel_without_target_use_neutral_stock_factor(
        string directionName, string spreadCode, int spreadPermille, long expectedUnit)
    {
        var direction = Dir(directionName);
        foreach (long stock in new long[] { 5, 50, 100_000 })
        {
            var result = TradeQuoteCalculator.Calculate(Input(1000, stock, null, direction, 5));

            // Stock never moves the price without a target: one segment at the neutral-stock price.
            Assert.Equal(new[] { new TradePriceStep(5, expectedUnit) }, result.Curve.ToArray());
            Assert.Equal(5 * expectedUnit, result.TotalCredits);
            Assert.Contains(new TradePriceReason("stock_normal", 1000), result.PriceReasons);
            Assert.Contains(new TradePriceReason(spreadCode, spreadPermille), result.PriceReasons);
        }
    }

    [Fact]
    public void Legacy_and_refuel_without_target_use_neutral_stock_factor_and_keep_static_reasons_first()
    {
        var staticReasons = new[]
        {
            new TradePriceReason("base_price", 1000, "item.fuel"),
            new TradePriceReason("station_profile", 1100, "market.alpha"),
            new TradePriceReason("event", 1300, "event.blockade"),
        };
        var input = new TradeQuotePriceInput(10, new[] { 1100, 1300 }, 40, null, TradeQuoteDirection.Refuel, 3, staticReasons);

        var result = TradeQuoteCalculator.Calculate(input);

        // 10 × 1.1 × 1.3 × 1.15 = 16.445 -> 16.
        Assert.Equal(new[] { new TradePriceStep(3, 16) }, result.Curve.ToArray());
        Assert.Equal(
            new[]
            {
                staticReasons[0], staticReasons[1], staticReasons[2],
                new TradePriceReason("stock_normal", 1000),
                new TradePriceReason("buy_spread", 1150),
            },
            result.PriceReasons.ToArray());
    }

    [Fact]
    public void Midpoint_rounds_away_from_zero_once_after_all_factors()
    {
        // 10 × 0.85 = 8.5 -> 9 (AwayFromZero, not banker's 8).
        Assert.Equal(9, Total(Input(10, 0, null, TradeQuoteDirection.Sell, 1)));

        // 3 × 1.5 × 1.15 = 5.175 -> 5. Rounding after the station factor (4.5 -> 5, × 1.15 = 5.75 -> 6) would differ.
        Assert.Equal(5, Total(Input(3, 5, null, TradeQuoteDirection.Buy, 1, 1500)));

        // Stock factor in the chain: target 4, stock 2 -> 1.35; 2 × 1.35 × 1.15 = 3.105 -> 3.
        Assert.Equal(3, Total(Input(2, 2, 4, TradeQuoteDirection.Buy, 1)));

        // Non-terminating stock ratio: target 3, stock 1 -> factor 1.70 − 0.70/3; 3 × (4400/3000) × 0.85 = 3.74 -> 4.
        Assert.Equal(4, Total(Input(3, 1, 3, TradeQuoteDirection.Sell, 1)));

        // Exact midpoint through a non-terminating stock ratio: target 3, stock 2 -> factor 3700/3000 = 1.2333...
        // 300 × 3700/3000 × 1.15 = 425.5 -> 426 and 300 × 3700/3000 × 0.85 = 314.5 -> 315. Dividing the ratio
        // early (28-digit 1.2333...3) can land just below the midpoint and round down.
        Assert.Equal(426, Total(Input(300, 2, 3, TradeQuoteDirection.Buy, 1)));
        Assert.Equal(315, Total(Input(300, 2, 3, TradeQuoteDirection.Sell, 1)));

        // Base 1 at the 0.50 floor: 0.5 -> 1 (minimum one credit for a positive base).
        Assert.Equal(1, Total(Input(1, 1000, 10, TradeQuoteDirection.Sell, 1, 100)));
    }

    [Fact]
    public void Invalid_factor_stock_and_checked_overflow_fail_before_result()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(0, 10, 10, TradeQuoteDirection.Buy, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(-5, 10, 10, TradeQuoteDirection.Buy, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, 10, TradeQuoteDirection.Buy, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, -1, 10, TradeQuoteDirection.Sell, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, 0, TradeQuoteDirection.Buy, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, -3, TradeQuoteDirection.Buy, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, 10, TradeQuoteDirection.Buy, 1, 1000, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, 10, TradeQuoteDirection.Buy, 1, -1000)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 10, 10, (TradeQuoteDirection)42, 1)));

        // Buy/Refuel cannot move the virtual stock below zero.
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 3, 10, TradeQuoteDirection.Buy, 4)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.Calculate(Input(10, 3, null, TradeQuoteDirection.Refuel, 4)));

        // Null collections.
        Assert.Throws<ArgumentNullException>(() => TradeQuoteCalculator.Calculate(
            new TradeQuotePriceInput(10, null!, 10, 10, TradeQuoteDirection.Buy, 1, Array.Empty<TradePriceReason>())));
        Assert.Throws<ArgumentNullException>(() => TradeQuoteCalculator.Calculate(
            new TradeQuotePriceInput(10, Array.Empty<int>(), 10, 10, TradeQuoteDirection.Buy, 1, null!)));

        // Sell stock transfer overflows long.
        Assert.Throws<OverflowException>(() => TradeQuoteCalculator.Calculate(Input(10, long.MaxValue - 1, 10, TradeQuoteDirection.Sell, 2)));

        // Unit price overflows long.
        Assert.Throws<OverflowException>(() => TradeQuoteCalculator.Calculate(Input(long.MaxValue / 2, 10, 10, TradeQuoteDirection.Buy, 1, 3000)));

        // Total overflows long (constant no-target price × huge quantity).
        Assert.Throws<OverflowException>(() => TradeQuoteCalculator.Calculate(Input(long.MaxValue / 4, long.MaxValue, null, TradeQuoteDirection.Buy, 8)));

        // Zero quantity is a valid, empty curve; the issuer decides whether that is a disabled request.
        var empty = TradeQuoteCalculator.Calculate(Input(10, 10, 10, TradeQuoteDirection.Buy, 0));
        Assert.True(empty.Curve.IsEmpty);
        Assert.Equal(0, empty.TotalCredits);
    }

    [Fact]
    public void Max_affordable_prefix_matches_bruteforce_including_saturated_tail()
    {
        var cases = new[]
        {
            Input(10, 108, 108, TradeQuoteDirection.Buy, 0),            // walks up toward the 1.70 stock cap
            Input(10, 108, 108, TradeQuoteDirection.Sell, 0),           // reaches the 0.65 stock floor at 162
            Input(10, 400, 108, TradeQuoteDirection.Sell, 0),           // saturated from the first unit
            Input(1000, 60, 40, TradeQuoteDirection.Buy, 0, 2500),      // hits the 3.00 final ceiling early
            Input(1000, 20, 40, TradeQuoteDirection.Sell, 0, 500),      // hits the 0.50 final floor
            Input(7, 30, null, TradeQuoteDirection.Refuel, 0, 1100),    // no target: constant from the start
            Input(3, 5, 4, TradeQuoteDirection.Sell, 0),
        };

        foreach (var input in cases)
        {
            long cap = input.Direction == TradeQuoteDirection.Sell ? 300 : input.CurrentStock;
            long[] prefix = new long[cap + 1];
            for (long q = 1; q <= cap; q++)
                prefix[q] = Total(input with { Quantity = q });

            var limits = new SortedSet<long> { 0, 1, prefix[cap], prefix[cap] + 1, long.MaxValue };
            for (long q = 1; q <= cap; q++)
            {
                limits.Add(prefix[q]);
                limits.Add(prefix[q] - 1);
            }

            foreach (long physicalCap in new[] { 0, 1, cap / 3, cap })
            {
                foreach (long limit in limits)
                {
                    long expected = 0;
                    for (long q = 1; q <= physicalCap && prefix[q] <= limit; q++)
                        expected = q;

                    Assert.Equal(expected, TradeQuoteCalculator.MaxAffordablePrefix(input, physicalCap, limit));
                }
            }
        }

        // Saturated tail far beyond any per-unit walk: no target, constant price 9 (10 × 0.85 = 8.5 -> 9).
        var constant = Input(10, 0, null, TradeQuoteDirection.Sell, 0);
        Assert.Equal(long.MaxValue / 9, TradeQuoteCalculator.MaxAffordablePrefix(constant, long.MaxValue, long.MaxValue));
        Assert.Equal(1_000_000_000_000L, TradeQuoteCalculator.MaxAffordablePrefix(constant, 1_000_000_000_000L, long.MaxValue));
        Assert.Equal(1_000_000L / 9, TradeQuoteCalculator.MaxAffordablePrefix(constant, 1_000_000_000_000L, 1_000_000L));

        // Sell surplus tail: stock 1_000 over target 108 is saturated at 0.65; 10 × 0.65 × 0.85 = 5.525 -> 6.
        var surplus = Input(10, 1_000, 108, TradeQuoteDirection.Sell, 0);
        Assert.Equal(1_000_000L / 6, TradeQuoteCalculator.MaxAffordablePrefix(surplus, 10_000_000L, 1_000_000L));

        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.MaxAffordablePrefix(surplus, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.MaxAffordablePrefix(surplus, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeQuoteCalculator.MaxAffordablePrefix(Input(10, 3, 10, TradeQuoteDirection.Buy, 0), 4, 1000));
    }
}
