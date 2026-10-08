using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class MarketHealthEvaluatorTests
{
    internal const long Hour = 3_600_000;
    internal static BalanceCaseEvidence Healthy()
    {
        var stations = Enumerable.Range(0, 5).Select(i => new BalanceStation(((char)('A' + i)).ToString(), 100, 200, 1,
            [new("ore", 100, 100, 200, 100, 100, []), new("other", 10, 100, 200, 10, 100, [])])).ToImmutableArray();
        var routes = Enumerable.Range(0, 4).Select(i => new BalanceRoute(((char)('A' + i)).ToString(), ((char)('B' + i)).ToString(),
            "Short", "risk.safe", TradingRouteRisk.Safe, TradingRouteAvailability.Available, Hour, 1000, [])).ToImmutableArray();
        return new(1, "starter", Enumerable.Range(0, 241).Select(i => new BalanceHourlySample(i * Hour, stations, routes, [new("A", "B", "ore")], [])).ToImmutableArray(), [], "hash", "hash");
    }
    internal static BalanceCaseEvidence Map(BalanceCaseEvidence c, Func<BalanceHourlySample, BalanceHourlySample> change) =>
        c with { HourlySamples = c.HourlySamples.Select(change).ToImmutableArray() };
    private static BalanceHourlySample Station(BalanceHourlySample s, string id, Func<BalanceStation, BalanceStation> change) =>
        s with { Stations = s.Stations.Select(station => station.StationId == id ? change(station) : station).ToImmutableArray() };
    private static BalanceHourlySample Stock(BalanceHourlySample s, string id, string item, Func<BalanceStock, BalanceStock> change) =>
        Station(s, id, st => st with { Stocks = st.Stocks.Select(stock => stock.ItemTypeId == item ? change(stock) : stock).ToImmutableArray() });
    private static ImmutableArray<BalanceViolation> Evaluate(BalanceCaseEvidence c) => MarketHealthEvaluator.Evaluate(c);

    [Fact]
    public void Healthy_five_station_ten_day_case_has_no_violations() => Assert.Empty(Evaluate(Healthy()));

    [Fact]
    public void Unavailable_bridge_reports_sorted_unreachable_stations_but_restricted_route_connects()
    {
        var c = Map(Healthy(), s => s.GameTimeMs != Hour ? s : s with { Routes = s.Routes.Select(r => r.Origin == "B" ? r with { Availability = TradingRouteAvailability.Unavailable } : r).ToImmutableArray() });
        var disconnected = Assert.Single(Evaluate(c), v => v.Code == "route_disconnected");
        Assert.Equal("C,D,E", disconnected.Observed); Assert.Equal(Hour, disconnected.GameTimeMs);
        c = Map(c, s => s with { Routes = s.Routes.Select(r => r.Availability == TradingRouteAvailability.Unavailable ? r with { Availability = TradingRouteAvailability.Restricted } : r).ToImmutableArray() });
        Assert.Empty(Evaluate(c));
    }

    [Fact]
    public void Missing_buy_or_sell_reports_station_and_exact_hour()
    {
        var c = Map(Healthy(), s => s.GameTimeMs != 7 * Hour ? s : Station(s, "C", st => st with { Stocks = st.Stocks.Select(stock => stock with { BuyMaximum = 0, SellMaximum = 0 }).ToImmutableArray() }));
        var violations = Evaluate(c);
        Assert.Equal(2, violations.Length);
        Assert.All(violations, v => { Assert.Equal("C", v.StationObjectId); Assert.Equal(7 * Hour, v.GameTimeMs); });
        Assert.Contains(violations, v => v.Code == "station_cannot_buy"); Assert.Contains(violations, v => v.Code == "station_cannot_sell");
    }

    [Fact]
    public void Zero_stock_at_exactly_twenty_five_percent_passes_and_one_more_sample_fails()
    {
        BalanceCaseEvidence ZeroThrough(int count) => Map(Healthy(), s => s.GameTimeMs is > 0 && s.GameTimeMs <= count * Hour ? Stock(s, "A", "ore", stock => stock with { Stock = 0, BuyMaximum = 0 }) : s);
        Assert.DoesNotContain(Evaluate(ZeroThrough(60)), v => v.Code == "zero_stock_ratio");
        var violation = Assert.Single(Evaluate(ZeroThrough(61)), v => v.Code == "zero_stock_ratio");
        Assert.Equal("61/240", violation.Observed); Assert.Equal("A", violation.StationObjectId); Assert.Equal("ore", violation.ItemTypeId);
        // t=0 does not enter the denominator.
        Assert.DoesNotContain(Evaluate(Map(ZeroThrough(60), s => s.GameTimeMs == 0 ? Stock(s, "A", "ore", stock => stock with { Stock = 0 }) : s)), v => v.Code == "zero_stock_ratio");
    }

    [Fact]
    public void Only_item_scoped_active_event_samples_leave_zero_stock_denominator()
    {
        BalanceCaseEvidence Event(string influenced) => Map(Healthy(), s => s.GameTimeMs is > 0 && s.GameTimeMs <= 61 * Hour
            ? Stock(s, "A", "ore", stock => stock with { Stock = 0, BuyMaximum = 0 }) with
            { Events = [new("A", "evt", "event", Hour, 62 * Hour, [influenced], false)] } : s);
        Assert.Empty(Evaluate(Event("ore")));
        var violation = Assert.Single(Evaluate(Event("other")), v => v.Code == "zero_stock_ratio");
        Assert.Equal("61/240", violation.Observed);
        // A neutral route-only event never exempts ore stock.
        Assert.Contains(Evaluate(Map(Event("other"), s => s with { Events = s.Events.Select(e => e with { InfluencedItems = [], InfluencesRoute = true }).ToImmutableArray() })), v => v.Code == "zero_stock_ratio");
    }

    [Fact]
    public void Stock_and_budget_outside_authoritative_bounds_are_reported()
    {
        var c = Map(Healthy(), s => s.GameTimeMs != 9 * Hour ? s : Stock(Station(s, "A", st => st with { Budget = -1 }), "A", "ore", stock => stock with { Stock = -1 }));
        c = Map(c, s => s.GameTimeMs != 10 * Hour ? s : Stock(Station(s, "B", st => st with { Budget = 201 }), "B", "ore", stock => stock with { Stock = 201 }));
        var violations = Evaluate(c);
        Assert.Equal(new[] { "budget_above_max", "budget_below_zero", "stock_above_max", "stock_below_zero" }, violations.Select(v => v.Code));
        Assert.All(violations, v => Assert.NotNull(v.GameTimeMs));
    }

    [Fact]
    public void Twenty_four_hour_positive_stock_gap_reports_first_unrecovered_window()
    {
        var c = Map(Healthy(), s => s.GameTimeMs >= 11 * Hour && s.GameTimeMs <= 34 * Hour ? Stock(s, "A", "ore", stock => stock with { Stock = 0 }) : s);
        var violation = Assert.Single(Evaluate(c), v => v.Code == "supply_not_recovered");
        Assert.Equal(11 * Hour, violation.GameTimeMs); Assert.Equal($"{11 * Hour}..{34 * Hour}", violation.Observed);
        Assert.DoesNotContain(Evaluate(c), v => v.Code == "zero_stock_ratio");
    }

    [Fact]
    public void Save_load_hash_mismatch_does_not_hide_other_violations()
    {
        var c = Map(Healthy(), s => Stock(s, "A", "ore", stock => stock with { Stock = 0 })) with { SaveLoadStateHash = "different" };
        var violations = Evaluate(c);
        Assert.Contains(violations, v => v.Code == "state_hash_mismatch"); Assert.Contains(violations, v => v.Code == "zero_stock_ratio");
        Assert.Contains(violations, v => v.Code == "supply_not_recovered");
    }

    [Fact]
    public void Violation_order_and_text_are_input_order_independent()
    {
        var c = Map(Healthy(), s => Station(s, "B", st => st with { Budget = -1 })) with { SaveLoadStateHash = "bad" };
        var reordered = Map(c, s => s with { Stations = s.Stations.Reverse().Select(st => st with { Stocks = st.Stocks.Reverse().ToImmutableArray() }).ToImmutableArray(), Routes = s.Routes.Reverse().ToImmutableArray() })
            with
        { HourlySamples = c.HourlySamples.Reverse().Select(s => s with { Stations = s.Stations.Reverse().ToImmutableArray(), Routes = s.Routes.Reverse().ToImmutableArray() }).ToImmutableArray() };
        Assert.Equal(Evaluate(c).ToArray(), Evaluate(reordered).ToArray());
    }

    [Fact]
    public void Malformed_times_keys_targets_and_missing_rows_return_contextual_violations()
    {
        foreach (var c in new[] { Healthy() with { HourlySamples = [] }, Healthy() with { HourlySamples = Healthy().HourlySamples.RemoveAt(0) },
            Healthy() with { HourlySamples = Healthy().HourlySamples.Add(Healthy().HourlySamples[0]) },
            Map(Healthy(), s => s.GameTimeMs == Hour ? s with { GameTimeMs = Hour + 1 } : s),
            Map(Healthy(), s => s with { Stations = s.Stations.Add(s.Stations[0]) }),
            Map(Healthy(), s => Station(s, "A", st => st with { Stocks = st.Stocks.Add(st.Stocks[0]) })),
            Map(Healthy(), s => Stock(s, "A", "ore", stock => stock with { Target = null, Maximum = null })),
            Map(Healthy(), s => s with { Routes = s.Routes.Add(s.Routes[0]) }),
            Map(Healthy(), s => Station(s, "A", st => st with { Stocks = [] })) })
        {
            Assert.Contains(Evaluate(c), v => v.Code == "invalid_evidence");
            Assert.All(Evaluate(c), v => { Assert.Equal(1UL, v.Seed); Assert.Equal("starter", v.ShipConfigurationId); Assert.NotEmpty(v.Expected); Assert.NotEmpty(v.Observed); });
        }
    }

    [Fact]
    public void All_event_affected_supply_has_explicit_empty_denominator_violation()
    {
        var c = Map(Healthy(), s => s.GameTimeMs == 0 ? s : s with { Events = [new("A", "evt", "event", Hour, 241 * Hour, ["ore"], false)] });
        var violation = Assert.Single(Evaluate(c), v => v.Code == "zero_stock_ratio");
        Assert.Equal("no eligible samples", violation.Observed);
    }

    [Fact]
    public void Null_default_and_changed_flow_evidence_does_not_throw()
    {
        Assert.Contains(Evaluate(null!), v => v.Code == "invalid_evidence");
        foreach (var c in new[] { Healthy() with { HourlySamples = default },
            Map(Healthy(), s => s with { Stations = default, Routes = default, CargoFlows = default, Events = default }),
            Map(Healthy(), s => s with { Stations = s.Stations.Add(null!) }),
            Map(Healthy(), s => s with { Events = [null!] }),
            Map(Healthy(), s => s.GameTimeMs == Hour ? s with { CargoFlows = [] } : s),
            Map(Healthy(), s => s with { CargoFlows = s.CargoFlows.Add(s.CargoFlows[0]) }) })
            Assert.Contains(Evaluate(c), v => v.Code == "invalid_evidence");
    }
}
