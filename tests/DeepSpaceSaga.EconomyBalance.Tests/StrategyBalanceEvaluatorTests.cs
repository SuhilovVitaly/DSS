using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class StrategyBalanceEvaluatorTests
{
    private static BalanceStrategyEvidence S(string id, string origin = "A", string destination = "B", string distance = "Short",
        long net = 100, long? cogs = 1000, long time = 0, bool evt = false, string config = "starter", long fuel = 0,
        long fees = 0, long eventCost = 0, long payout = 0, long penalty = 0)
    {
        long gross = checked((cogs ?? 1000) + net + fuel + fees + eventCost - payout + penalty);
        var ledger = new BalanceLedgerEvidence(id, origin + "/" + destination, "ore", gross, cogs, fuel, fees, eventCost, payout, penalty, cogs is null ? null : net);
        var buy = new TradeExecutionReceipt(origin, "ore", null, 1, 2, 1, 1, cogs ?? 1000, []);
        var sell = new TradeExecutionReceipt(destination, "ore", null, 1, 2, 1, 1, gross, [], cogs, cogs is null ? null : gross - cogs);
        return new(time, evt, origin, destination, distance, "risk.safe", "ore", config, 100, config == "starter" ? 100 : 200,
            1, 1, 1, time, time + 1, cogs is null ? "unknown" : "completed", null,
            new("buy-" + id, origin, "ore", TradeCommandTypes.Buy, 1, 1, 1, 100, buy.TotalCredits, null),
            new("sell-" + id, destination, "ore", TradeCommandTypes.Sell, 1, 1, 1, 100, gross, null), buy, sell, ledger,
            ["sale-" + id, "fuel-" + id], [new(id, id, 2, 2, 2, 2, net, net, CommandReasonCodes.StaleQuote)])
        { BatchCeilingQuantity = 100 };
    }
    private static BalanceCaseEvidence C(params BalanceStrategyEvidence[] strategies)
    {
        var samples = strategies.GroupBy(s => s.StateGameTimeMs).OrderBy(g => g.Key).Select(g => new BalanceHourlySample(g.Key, [],
            g.GroupBy(s => (s.Origin, s.Destination)).Select(group => group.First()).Select(s => new BalanceRoute(s.Origin, s.Destination,
                s.DistanceClass, s.RiskProfileId, TradingRouteRisk.Safe, TradingRouteAvailability.Available, 1000, 1000, [])).ToImmutableArray(), [],
            g.Any(s => s.EventState) ? [new("A", "evt", "event", g.Key, g.Key + 1000, ["ore"], true)] : [])).ToImmutableArray();
        return new(1, strategies.FirstOrDefault()?.ShipConfigurationId ?? "starter", samples, strategies.ToImmutableArray(), "hash", "hash");
    }
    private static BalanceCaseEvidence Healthy() => C(S("short"), S("medium", "C", "D", "Medium", 200), S("long", "D", "E", "Long", -10),
        S("short-event", net: 300, time: 3_600_000, evt: true), S("medium-event", "C", "D", "Medium", 200, time: 3_600_000, evt: true),
        S("long-event", "D", "E", "Long", -10, time: 3_600_000, evt: true));
    private static ImmutableArray<BalanceViolation> E(BalanceCaseEvidence c) => StrategyBalanceEvaluator.EvaluateCase(c);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Safe_short_margin_uses_actual_departure_risk_after_positioning(bool becameElevated)
    {
        var strategy = S("departure"); var c = C(strategy);
        var route = c.HourlySamples[0].Routes[0];
        c = c with
        {
            HourlySamples = c.HourlySamples.Select(h => h with { Routes = [route with { Risk = becameElevated ? TradingRouteRisk.Safe : TradingRouteRisk.Elevated }] }).ToImmutableArray(),
            Strategies = [strategy with { DepartureRoute = route with { Risk = becameElevated ? TradingRouteRisk.Elevated : TradingRouteRisk.Safe }, DepartureGameTimeMs = strategy.BuyGameTimeMs }]
        };
        Assert.Equal(becameElevated, E(c).Any(v => v.Code == "short_margin_band"));
    }

    [Fact]
    public void Published_ledger_identity_is_audited_without_using_player_credit_delta()
    {
        var valid = S("identity", net: 300, fuel: 20, fees: 30, eventCost: 40, payout: 50, penalty: 60);
        Assert.DoesNotContain(E(C(valid)), v => v.Code == "ledger_formula_mismatch");
        var wrong = valid with { Ledger = valid.Ledger! with { NetProfitCredits = 301 } };
        var violation = Assert.Single(E(C(wrong)), v => v.Code == "ledger_formula_mismatch");
        Assert.Equal("A/B", violation.RouteId); Assert.Contains("PassengerPenaltyCredits", violation.Observed);
        Assert.Contains(E(C(valid with { Ledger = valid.Ledger! with { RouteFuelCostCredits = -1 } })), v => v.Code == "ledger_formula_mismatch");
        Assert.Contains(E(C(valid with { SellQuote = valid.SellQuote! with { TotalCredits = valid.SellQuote.TotalCredits + 1 } })), v => v.Code == "ledger_formula_mismatch");
    }

    [Theory]
    [InlineData(50, 150)]
    [InlineData(150, 350)]
    public void Short_fifty_and_one_fifty_and_medium_one_fifty_and_three_fifty_are_inclusive(long shortNet, long mediumNet)
    {
        var c = C(S("short", net: shortNet), S("medium", "C", "D", "Medium", mediumNet));
        Assert.DoesNotContain(E(c), v => v.Code is "short_margin_band" or "medium_margin_band");
        Assert.Contains(E(C(S("below", net: 49))), v => v.Code == "short_margin_band");
        Assert.Contains(E(C(S("above", "C", "D", "Medium", 351))), v => v.Code == "medium_margin_band");
    }

    [Fact]
    public void Unknown_or_zero_cogs_is_reported_incomparable_without_division()
    {
        var unknown = S("unknown", cogs: null); var zero = S("zero", "C", "D", cogs: 0);
        Assert.Null(StrategyBalanceEvaluator.Margin(unknown.Ledger)); Assert.Null(StrategyBalanceEvaluator.Margin(zero.Ledger));
        var result = E(C(unknown, zero));
        Assert.DoesNotContain(result, v => v.Code == "ledger_formula_mismatch");
        Assert.Contains(result, v => v.Code == "missing_comparable_strategy");
        Assert.DoesNotContain(result, v => v.Code == "route_margin_dominance");
        var partial = S("partial");
        partial = partial with
        {
            Outcome = "partial",
            RequestedQuantity = 2,
            ExecutedBuyQuantity = 2,
            BuyQuote = partial.BuyQuote! with { RequestedQuantity = 2, ExecutableQuantity = 2 },
            BuyReceipt = partial.BuyReceipt! with { RequestedQuantity = 2, ExecutedQuantity = 2 },
            SellQuote = partial.SellQuote! with { RequestedQuantity = 2 },
            SellReceipt = partial.SellReceipt! with { RequestedQuantity = 2 }
        };
        Assert.Empty(StrategyBalanceEvaluator.Leaders(C(partial)));
        Assert.DoesNotContain(E(C(partial)), v => v.Code == "ledger_formula_mismatch");
    }

    [Fact]
    public void Same_long_route_crosses_from_nonpositive_starter_to_positive_cargo_upgrade()
    {
        var starter = C(S("long", "D", "E", "Long", -1));
        var upgrade = C(S("upgrade", "D", "E", "Long", 1, config: "cargo-upgrade"));
        Assert.DoesNotContain(StrategyBalanceEvaluator.EvaluateCorpus([starter, upgrade]), v => v.Code == "long_upgrade_crossover");
        Assert.Contains(StrategyBalanceEvaluator.EvaluateCorpus([starter, upgrade with { Seed = 2 }]), v => v.Code == "long_upgrade_crossover");
        Assert.Contains(StrategyBalanceEvaluator.EvaluateCorpus([starter, C(S("different", "C", "E", "Long", 1, config: "cargo-upgrade"))]), v => v.Code == "long_upgrade_crossover");
        var brokenCap = upgrade with { Strategies = upgrade.Strategies.Select(s => s with { BatchCeilingQuantity = 0 }).ToImmutableArray() };
        Assert.Contains(StrategyBalanceEvaluator.EvaluateCorpus([starter, brokenCap]), v => v.Code == "long_upgrade_crossover");
    }

    [Fact]
    public void Maximum_equal_to_twice_median_passes_and_one_permille_above_fails()
    {
        BalanceCaseEvidence Values(long top) => C(S("one", "A", "B", net: 100), S("two", "B", "C", net: 200), S("top", "C", "D", net: top));
        Assert.DoesNotContain(E(Values(400)), v => v.Code == "route_margin_dominance");
        var violation = Assert.Single(E(Values(401)), v => v.Code == "route_margin_dominance");
        Assert.Contains("max=401;median=200", violation.Observed); Assert.Equal(0, violation.GameTimeMs);
    }

    [Fact]
    public void Even_median_and_tie_winner_are_canonical_under_reordered_input()
    {
        var c = C(S("a", "B", "C", net: 100), S("b", "A", "B", net: 100), S("c", "C", "D", net: 101), S("d", "D", "E", net: 202));
        var violation = Assert.Single(E(c), v => v.Code == "route_margin_dominance"); Assert.Contains("median=100.5", violation.Observed);
        var tie = C(S("b", "B", "C", net: 100), S("a", "A", "B", net: 100));
        var leader = Assert.Single(StrategyBalanceEvaluator.Leaders(tie));
        Assert.Equal("A/B", leader.RouteId); Assert.Equal(new[] { "A/B/ore", "B/C/ore" }, leader.TiedRouteItems);
        var reversed = c with { Strategies = c.Strategies.Reverse().ToImmutableArray(), HourlySamples = c.HourlySamples.Reverse().Select(s => s with { Routes = s.Routes.Reverse().ToImmutableArray() }).ToImmutableArray() };
        Assert.Equal(E(c).ToArray(), E(reversed).ToArray());
        Assert.Equal(BalanceCanonical.Json(StrategyBalanceEvaluator.Leaders(c)), BalanceCanonical.Json(StrategyBalanceEvaluator.Leaders(reversed)));
    }

    [Fact]
    public void Event_state_changes_leader_and_unavailable_routes_do_not_compete()
    {
        var c = Healthy();
        Assert.DoesNotContain(E(c), v => v.Code is "route_always_best" or "event_did_not_change_leader");
        var closed = c with { HourlySamples = c.HourlySamples.Select(s => s with { Routes = s.Routes.Select(r => r.Origin == "A" ? r with { Availability = TradingRouteAvailability.Unavailable } : r).ToImmutableArray() }).ToImmutableArray() };
        Assert.All(StrategyBalanceEvaluator.Leaders(closed), l => Assert.NotEqual("A/B", l.RouteId));
        Assert.DoesNotContain(StrategyBalanceEvaluator.EvaluateCorpus([c]), v => v.Code == "event_did_not_change_leader");
    }

    [Fact]
    public void One_route_winning_every_normal_and_event_state_is_reported()
    {
        var c = C(S("normal", net: 100), S("event", net: 100, time: 3_600_000, evt: true));
        var result = E(c);
        Assert.Contains(result, v => v.Code == "route_always_best"); Assert.Contains(result, v => v.Code == "event_did_not_change_leader");
    }

    [Fact]
    public void Replayed_command_stale_quote_and_duplicate_posting_cannot_add_profit()
    {
        var s = S("replay");
        Assert.DoesNotContain(E(C(s)), v => v.Code is "duplicate_ledger_posting" or "stale_quote_or_command_reapplied");
        var duplicate = s with { PostingIds = ["same", "same"] };
        Assert.Contains(E(C(duplicate)), v => v.Code == "duplicate_ledger_posting");
        var changed = s with { Replays = [s.Replays[0] with { PostingsAfter = 3, StaleQuoteReason = null }] };
        Assert.Contains(E(C(changed)), v => v.Code == "stale_quote_or_command_reapplied");
    }

    [Fact]
    public void Unchanged_market_revision_requires_zero_new_realized_profit()
    {
        var s = S("replay"); s = s with { Replays = [s.Replays[0] with { NetAfter = 101 }] };
        var result = E(C(s));
        Assert.Contains(result, v => v.Code == "unchanged_market_created_profit"); Assert.Contains(result, v => v.Code == "stale_quote_or_command_reapplied");
    }

    [Fact]
    public void Six_real_bound_legs_are_required_for_three_round_trip_proof()
    {
        var c = Healthy(); var first = c.Strategies[0];
        var legs = Enumerable.Range(0, 6).Select(i =>
        {
            var leg = i == 0 ? first : S("proof-" + i, i % 2 == 0 ? "A" : "B", i % 2 == 0 ? "B" : "A");
            return leg with
            {
                BuyGameTimeMs = i * 1000,
                SellGameTimeMs = i * 1000 + 1,
                BuyReceipt = leg.BuyReceipt! with { QuotedMarketRevision = 1 + i * 2, ResultMarketRevision = 2 + i * 2 },
                SellReceipt = leg.SellReceipt! with { QuotedMarketRevision = 1 + i * 2, ResultMarketRevision = 2 + i * 2 },
                BuyQuote = leg.BuyQuote! with { Revision = 1 + i * 2 },
                SellQuote = leg.SellQuote! with { Revision = 1 + i * 2 },
                Replays = [new("buy-" + i, leg.Ledger!.VoyageId, 2, 2, 2, 2, 100, 100, CommandReasonCodes.StaleQuote), new("sell-" + i, leg.Ledger.VoyageId, 2, 2, 2, 2, 100, 100, CommandReasonCodes.StaleQuote)]
            };
        }).ToImmutableArray();
        first = legs[0];
        var proof = first with { CompletedRoundTrips = 3, RoundTripLegs = legs };
        c = c with { Strategies = c.Strategies.SetItem(0, proof) };
        Assert.DoesNotContain(E(c), v => v.Expected.StartsWith("three real", StringComparison.Ordinal));
        Assert.Contains(E(c with { Strategies = c.Strategies.SetItem(0, proof with { RoundTripLegs = legs.RemoveAt(5) }) }), v => v.Expected.StartsWith("three real", StringComparison.Ordinal));
        var duplicate = proof with { RoundTripLegs = legs.SetItem(1, legs[0]) };
        Assert.Contains(E(c with { Strategies = c.Strategies.SetItem(0, duplicate) }), v => v.Code == "duplicate_ledger_posting");
        var fakeForward = S("proof-1") with { BuyGameTimeMs = 1000, SellGameTimeMs = 1001, Replays = legs[1].Replays };
        var brokenReturn = legs.SetItem(1, fakeForward);
        Assert.Contains(E(c with { Strategies = c.Strategies.SetItem(0, proof with { RoundTripLegs = brokenReturn }) }), v => v.Expected.StartsWith("three real", StringComparison.Ordinal));
        var sameWorldCommand = legs.SetItem(1, legs[1] with { Replays = legs[0].Replays });
        Assert.Contains(E(c with { Strategies = c.Strategies.SetItem(0, proof with { RoundTripLegs = sameWorldCommand }) }), v => v.Expected.StartsWith("three real", StringComparison.Ordinal));
        var backwardsTime = legs.SetItem(1, legs[1] with { BuyGameTimeMs = -1 });
        Assert.Contains(E(c with { Strategies = c.Strategies.SetItem(0, proof with { RoundTripLegs = backwardsTime }) }), v => v.Expected.StartsWith("three real", StringComparison.Ordinal));

    }

    [Fact]
    public void Margin_rounding_and_extreme_components_do_not_overflow_evaluator()
    {
        Assert.Equal(1, StrategyBalanceEvaluator.Margin(S("half", net: 1, cogs: 2000).Ledger));
        Assert.Equal(-1, StrategyBalanceEvaluator.Margin(S("half-neg", net: -1, cogs: 2000).Ledger));
        var large = S("large") with { Ledger = S("large").Ledger! with { GrossSalesCredits = long.MaxValue, CostOfGoodsSoldCredits = 1, NetProfitCredits = long.MaxValue - 1 } };
        Assert.Null(StrategyBalanceEvaluator.Margin(large.Ledger));
        Assert.Contains(E(C(large)), v => v.Code == "ledger_formula_mismatch");
        Assert.Contains(E(null!), v => v.Code == "missing_comparable_strategy");
        Assert.Contains(StrategyBalanceEvaluator.EvaluateCorpus(default), v => v.Code == "long_upgrade_crossover");
    }
}
