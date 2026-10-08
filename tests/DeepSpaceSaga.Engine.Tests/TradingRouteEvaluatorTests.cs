using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingRouteEvaluatorTests
{
    internal static TradingMapStateData Map() => new(1, "fixture", 0,
        new(1, "A", 100, 2 * GameCalendar.HourMs, 6 * GameCalendar.HourMs, 20 * GameCalendar.HourMs, 1, 1,
            new[] { "A", "B", "C", "D", "E" }.Select(id => new TradingMapStationData(id, "market.fixture", id, "Medium")).ToArray(), [],
            [new("risk.safe", 1000), new("risk.pirate", 1500)]),
        [Edge("A", "B"), Edge("B", "C", 3 * GameCalendar.HourMs, 1500, "risk.pirate", "Medium"),
         Edge("C", "D", 7 * GameCalendar.HourMs, distanceClass: "Long"), Edge("D", "E"), Edge("E", "A"), Edge("A", "C")],
        [new("A", "D", ["item.water"]), new("C", "B", ["item.steel"])], []);

    internal static TradingMapEdgeData Edge(string from, string to, long time = GameCalendar.HourMs,
        int fuel = 1000, string risk = "risk.safe", string distanceClass = "Short") => new(from, to, 100, time, distanceClass, fuel, risk);
    internal static TradingRouteModifier Modifier(string id = "event-1", string from = "A", string to = "B",
        TradingRouteAvailability availability = TradingRouteAvailability.Restricted, int time = 1500, int fuel = 1200,
        int priority = 10, long start = 0, string reason = "Quarantine") =>
        new(id, "event.quarantine", priority, start, from, to, availability, time, fuel, reason);
    private static EffectiveTradingRoute AB(IEnumerable<EffectiveTradingRoute> routes) =>
        Assert.Single(routes, r => r.BaseEdge.FromStationObjectId == "A" && r.BaseEdge.ToStationObjectId == "B");

    [Fact]
    public void Base_safe_and_risky_routes_have_distinct_terms_and_do_not_mutate_map()
    {
        var map = Map(); string before = JsonSerializer.Serialize(map);
        var result = TradingRouteEvaluator.Evaluate(map, []);
        var safe = AB(result);
        var risky = Assert.Single(result, r => r.BaseEdge.FromStationObjectId == "B" && r.BaseEdge.ToStationObjectId == "C");
        Assert.Equal((GameCalendar.HourMs, 1000, TradingRouteRisk.Safe, TradingRouteAvailability.Available),
            (safe.EffectiveTravelEstimateGameTimeMs, safe.EffectiveFuelMultiplierPermille, safe.Risk, safe.Availability));
        Assert.Equal((3 * GameCalendar.HourMs, 1500, TradingRouteRisk.Elevated),
            (risky.EffectiveTravelEstimateGameTimeMs, risky.EffectiveFuelMultiplierPermille, risky.Risk));
        Assert.Null(safe.ReasonText); Assert.Empty(safe.ActiveEventIds);
        Assert.Equal(before, JsonSerializer.Serialize(map));
    }

    [Fact]
    public void Multipliers_round_each_factor_away_from_zero_and_follow_priority_before_time()
    {
        var map = Map() with { Edges = Map().Edges.Select(e => e.FromStationObjectId == "A" && e.ToStationObjectId == "B" ? e with { TravelEstimateGameTimeMs = 5 } : e).ToArray() };
        var first = Modifier("later-start", time: 500, fuel: 1001, priority: 20, start: 100, reason: "First");
        var second = Modifier("earlier-start", time: 1500, fuel: 1500, priority: 10, start: 0, reason: "Second");
        var actual = AB(TradingRouteEvaluator.Evaluate(map, [second, first]));
        // Time: round(5*.5)=3, then round(3*1.5)=5. Reversed priority would yield 4.
        // Fuel: 1000*1.001=1001, then round(1001*1.5)=1502.
        Assert.Equal(5, actual.EffectiveTravelEstimateGameTimeMs);
        Assert.Equal(1502, actual.EffectiveFuelMultiplierPermille);
        Assert.Equal(new[] { "later-start", "earlier-start" }, actual.ActiveEventIds.ToArray());
        Assert.Equal("First; Second", actual.ReasonText);
        Assert.Equal(4, AB(TradingRouteEvaluator.Evaluate(map, [first with { Priority = 0 }, second])).EffectiveTravelEstimateGameTimeMs);
    }

    [Fact]
    public void Availability_is_strictest_and_tie_order_is_start_then_ordinal_id()
    {
        var modifiers = new[] { Modifier("z", availability: TradingRouteAvailability.Unavailable, start: 2, reason: "Z"),
            Modifier("b", start: 1, reason: "B"), Modifier("a", start: 1, reason: "A") };
        var actual = AB(TradingRouteEvaluator.Evaluate(Map(), modifiers));
        Assert.Equal(TradingRouteAvailability.Unavailable, actual.Availability);
        Assert.Equal(TradingRouteRisk.Elevated, actual.Risk);
        Assert.Equal(new[] { "a", "b", "z" }, actual.ActiveEventIds.ToArray());
        Assert.Equal("A; B; Z", actual.ReasonText);
    }

    [Fact]
    public void Expired_modifier_absence_restores_exact_base_and_identity_modifier_keeps_safe_risk()
    {
        var map = Map(); var baseline = AB(TradingRouteEvaluator.Evaluate(map, []));
        var modified = AB(TradingRouteEvaluator.Evaluate(map, [Modifier()]));
        Assert.NotEqual(baseline.EffectiveTravelEstimateGameTimeMs, modified.EffectiveTravelEstimateGameTimeMs);
        var restored = AB(TradingRouteEvaluator.Evaluate(map, []));
        Assert.Equal(baseline, restored with { ActiveEventIds = baseline.ActiveEventIds });
        Assert.Empty(restored.ActiveEventIds);
        Assert.Equal(TradingRouteRisk.Safe, AB(TradingRouteEvaluator.Evaluate(map,
            [Modifier(availability: TradingRouteAvailability.Available, time: 1000, fuel: 1000)])).Risk);
    }

    [Fact]
    public void Connectivity_accepts_quarantine_and_rejects_bridge_and_isolated_station()
    {
        var map = Map();
        Assert.True(TradingRouteEvaluator.CanApply(map, [], [Modifier()]));
        Assert.True(TradingRouteEvaluator.CanApply(map, [], [Modifier(availability: TradingRouteAvailability.Unavailable)]));
        var isolateB = new[] { Modifier("AB", availability: TradingRouteAvailability.Unavailable),
            Modifier("BC", "B", "C", TradingRouteAvailability.Unavailable) };
        Assert.False(TradingRouteEvaluator.CanApply(map, [], isolateB));
        var chain = map with { Edges = [Edge("A", "B"), Edge("B", "C"), Edge("C", "D"), Edge("D", "E")] };
        Assert.False(TradingRouteEvaluator.CanApply(chain, [], [Modifier("bridge", "B", "C", TradingRouteAvailability.Unavailable)]));
        Assert.Equal(TradingRouteAvailability.Unavailable, isolateB[0].Availability);
        Assert.Equal(6, map.Edges.Count);
    }

    [Fact]
    public void Every_cargo_flow_endpoint_stays_connected_and_invalid_endpoint_is_contextual()
    {
        var map = Map();
        Assert.True(TradingRouteEvaluator.CanApply(map, [Modifier("existing")], [Modifier("other", "C", "D")]));
        var noPath = map with { Edges = [Edge("A", "B"), Edge("B", "C"), Edge("C", "D"), Edge("D", "E")] };
        Assert.False(TradingRouteEvaluator.CanApply(noPath, [], [Modifier("disconnect-flow", "C", "D", TradingRouteAvailability.Unavailable)]));
        var invalid = map with { CargoFlows = [new("A", "Unknown", ["item.water"])] };
        Assert.Contains("Cargo flow", Assert.Throws<ScenarioException>(() => TradingRouteEvaluator.CanApply(invalid, [], [])).Message);
    }

    [Fact]
    public void Canonical_result_is_independent_of_edge_endpoint_station_flow_and_modifier_order()
    {
        var map = Map(); var modifiers = new[] { Modifier("X"), Modifier("Y", "B", "C") };
        var shuffled = map with
        {
            Edges = map.Edges.Reverse().Select(e => e with { FromStationObjectId = e.ToStationObjectId, ToStationObjectId = e.FromStationObjectId }).ToArray(),
            CargoFlows = map.CargoFlows.Reverse().ToArray(),
            Rules = map.Rules with { Stations = map.Rules.Stations.Reverse().ToArray(), RiskProfiles = map.Rules.RiskProfiles.Reverse().ToArray() }
        };
        var reversed = modifiers.AsEnumerable().Reverse().Select(m => m with { FromStationObjectId = m.ToStationObjectId, ToStationObjectId = m.FromStationObjectId }).ToArray();
        Assert.Equal(JsonSerializer.Serialize(TradingRouteEvaluator.Evaluate(map, modifiers)), JsonSerializer.Serialize(TradingRouteEvaluator.Evaluate(shuffled, reversed)));
        Assert.Equal(TradingRouteEvaluator.CanApply(map, [], modifiers), TradingRouteEvaluator.CanApply(shuffled, [], reversed));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("nonedge")]
    [InlineData("duplicate")]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("enum")]
    [InlineData("id")]
    [InlineData("reason")]
    [InlineData("time-overflow")]
    [InlineData("fuel-overflow")]
    [InlineData("round-to-zero")]
    public void Malformed_modifier_or_overflow_is_contextual_and_CanApply_does_not_hide_it(string defect)
    {
        var map = Map(); var modifier = Modifier("bad-event");
        modifier = defect switch
        {
            "unknown" => modifier with { ToStationObjectId = "Unknown" },
            "nonedge" => modifier with { ToStationObjectId = "D" },
            "zero" => modifier with { TravelTimeMultiplierPermille = 0 },
            "negative" => modifier with { FuelMultiplierPermille = -1 },
            "enum" => modifier with { Availability = (TradingRouteAvailability)123 },
            "id" => modifier with { EventTypeId = "" },
            "reason" => modifier with { ReasonText = " " },
            "time-overflow" => modifier with { TravelTimeMultiplierPermille = int.MaxValue },
            "fuel-overflow" => modifier with { FuelMultiplierPermille = int.MaxValue },
            "round-to-zero" => modifier with { TravelTimeMultiplierPermille = 1 },
            _ => modifier,
        };
        if (defect is "time-overflow" or "round-to-zero") map = map with
        {
            Edges = map.Edges.Select(e => e.FromStationObjectId == "A" && e.ToStationObjectId == "B"
            ? e with { TravelEstimateGameTimeMs = defect == "time-overflow" ? long.MaxValue : 1 } : e).ToArray()
        };
        if (defect == "fuel-overflow") modifier = modifier with { FuelMultiplierPermille = int.MaxValue };
        // Two large factors are required to overflow the int fuel result from a 1000 baseline.
        var values = defect == "duplicate" ? new[] { modifier, modifier } : defect == "fuel-overflow"
            ? new[] { modifier, modifier with { EventId = "other-event" } } : [modifier];
        string before = JsonSerializer.Serialize(map);
        var error = Assert.Throws<ScenarioException>(() => TradingRouteEvaluator.Evaluate(map, values));
        Assert.Contains("event", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("route", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<ScenarioException>(() => TradingRouteEvaluator.CanApply(map, [], values));
        Assert.Equal(before, JsonSerializer.Serialize(map));
    }

    [Theory]
    [InlineData("disconnected")]
    [InlineData("safe-missing")]
    [InlineData("safe-duplicate")]
    [InlineData("edge-duplicate")]
    [InlineData("station-duplicate")]
    [InlineData("unknown-risk")]
    public void Malformed_base_dependency_is_rejected(string defect)
    {
        var map = Map();
        map = defect switch
        {
            "disconnected" => map with { Edges = [Edge("A", "B"), Edge("B", "C"), Edge("D", "E")] },
            "safe-missing" => map with { Rules = map.Rules with { RiskProfiles = [new("risk.pirate", 1500)] } },
            "safe-duplicate" => map with { Rules = map.Rules with { RiskProfiles = [.. map.Rules.RiskProfiles, new("RISK.SAFE", 1000)] } },
            "edge-duplicate" => map with { Edges = [.. map.Edges, map.Edges[0] with { FromStationObjectId = "B", ToStationObjectId = "A" }] },
            "station-duplicate" => map with { Rules = map.Rules with { Stations = [.. map.Rules.Stations.Take(4), map.Rules.Stations[0]] } },
            "unknown-risk" => map with { Edges = [.. map.Edges.Skip(1), map.Edges[0] with { RiskProfileId = "unknown" }] },
            _ => map,
        };
        Assert.Throws<ScenarioException>(() => TradingRouteEvaluator.Evaluate(map, []));
    }
}
