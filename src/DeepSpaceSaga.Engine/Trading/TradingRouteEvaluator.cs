using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Trading;

internal sealed record TradingRouteModifier(string EventId, string EventTypeId, int Priority, long StartedGameTimeMs,
    string FromStationObjectId, string ToStationObjectId, TradingRouteAvailability Availability,
    int TravelTimeMultiplierPermille, int FuelMultiplierPermille, string ReasonText);

internal sealed record EffectiveTradingRoute(TradingMapEdgeData BaseEdge, long EffectiveTravelEstimateGameTimeMs,
    int EffectiveFuelMultiplierPermille, TradingRouteRisk Risk, TradingRouteAvailability Availability,
    string? ReasonText, ImmutableArray<string> ActiveEventIds);

internal static class TradingRouteEvaluator
{
    internal static ImmutableArray<EffectiveTradingRoute> Evaluate(TradingMapStateData map, IReadOnlyList<TradingRouteModifier> activeModifiers, bool clusterGeography = false)
    {
        if (map?.Rules?.Stations is null || map.Rules.RiskProfiles is null || map.Edges is null || map.CargoFlows is null || activeModifiers is null)
            throw new ScenarioException("Trading route evaluation requires map rules, edges, cargo flows and modifiers.");
        if (map.SchemaVersion != 1 || map.Rules.SchemaVersion != 1 || (clusterGeography ? map.Rules.Stations.Count < 2 : map.Rules.Stations.Count != 5))
            throw new ScenarioException("Trading route map requires schemaVersion 1 and exactly five stations.");
        var stations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var station in map.Rules.Stations)
            if (station is null || string.IsNullOrWhiteSpace(station.ObjectId) || !stations.TryAdd(station.ObjectId, station.ObjectId))
                throw new ScenarioException("Trading route map has an invalid or duplicate station ID.");
        string Station(string id, string context) => !string.IsNullOrWhiteSpace(id) && stations.TryGetValue(id, out var canonical)
            ? canonical : throw new ScenarioException($"{context}: unknown station '{id}'.");
        var risks = new Dictionary<string, TradingMapRiskData>(StringComparer.OrdinalIgnoreCase);
        foreach (var risk in map.Rules.RiskProfiles)
            if (risk is null || string.IsNullOrWhiteSpace(risk.RiskProfileId) || risk.FuelMultiplierPermille <= 0 || !risks.TryAdd(risk.RiskProfileId, risk))
                throw new ScenarioException("Trading route map has an invalid or duplicate risk profile.");
        if (!risks.TryGetValue("risk.safe", out var safe) || safe.RiskProfileId != "risk.safe" || safe.FuelMultiplierPermille != 1000)
            throw new ScenarioException("Trading route map requires the unique risk.safe profile with fuel multiplier 1000.");
        var edges = new Dictionary<(string From, string To), TradingMapEdgeData>();
        foreach (var edge in map.Edges)
        {
            if (edge is null) throw new ScenarioException("Trading route map contains a null edge.");
            string context = $"Trading route '{edge.FromStationObjectId}'/'{edge.ToStationObjectId}'";
            var pair = Pair(Station(edge.FromStationObjectId, context), Station(edge.ToStationObjectId, context));
            if (pair.From == pair.To || !double.IsFinite(edge.DistanceKm) || edge.DistanceKm <= 0 || edge.TravelEstimateGameTimeMs <= 0 ||
                edge.FuelMultiplierPermille <= 0 || edge.DistanceClass is not ("Short" or "Medium" or "Long") ||
                string.IsNullOrWhiteSpace(edge.RiskProfileId) || !risks.TryGetValue(edge.RiskProfileId, out var risk) ||
                risk.FuelMultiplierPermille != edge.FuelMultiplierPermille)
                throw new ScenarioException($"{context}: invalid distance/time/fuel/risk.");
            if (!edges.TryAdd(pair, edge with { FromStationObjectId = pair.From, ToStationObjectId = pair.To, RiskProfileId = risk.RiskProfileId }))
                throw new ScenarioException($"{context}: duplicate unordered edge.");
        }
        var baseReachable = new HashSet<string>(StringComparer.Ordinal) { stations.Values.Order(StringComparer.Ordinal).First() };
        bool added;
        do
        {
            added = false;
            foreach (var pair in edges.Keys)
            {
                if (baseReachable.Contains(pair.From)) added |= baseReachable.Add(pair.To);
                if (baseReachable.Contains(pair.To)) added |= baseReachable.Add(pair.From);
            }
        } while (added);
        if (baseReachable.Count != stations.Count) throw new ScenarioException("Trading route base map is disconnected.");
        foreach (var flow in map.CargoFlows)
        {
            if (flow is null) throw new ScenarioException("Trading route map contains a null cargo flow.");
            string from = Station(flow.FromStationObjectId, "Cargo flow"), to = Station(flow.ToStationObjectId, "Cargo flow");
            if (from == to || flow.ItemTypeIds is not { Count: > 0 } || flow.ItemTypeIds.Any(string.IsNullOrWhiteSpace))
                throw new ScenarioException($"Cargo flow '{from}'/'{to}' is invalid.");
        }
        var modifiers = new Dictionary<(string From, string To), List<TradingRouteModifier>>();
        var identities = new HashSet<(string Event, string From, string To)>();
        foreach (var modifier in activeModifiers)
        {
            if (modifier is null) throw new ScenarioException("Trading route modifiers contain null.");
            string context = $"Event '{modifier.EventId}', route '{modifier.FromStationObjectId}'/'{modifier.ToStationObjectId}'";
            var pair = Pair(Station(modifier.FromStationObjectId, context), Station(modifier.ToStationObjectId, context));
            if (!edges.ContainsKey(pair) || string.IsNullOrWhiteSpace(modifier.EventId) || string.IsNullOrWhiteSpace(modifier.EventTypeId) ||
                string.IsNullOrWhiteSpace(modifier.ReasonText) || modifier.Priority < 0 || modifier.StartedGameTimeMs < 0 ||
                modifier.TravelTimeMultiplierPermille <= 0 || modifier.FuelMultiplierPermille <= 0 || !Enum.IsDefined(modifier.Availability) ||
                !identities.Add((modifier.EventId, pair.From, pair.To)))
                throw new ScenarioException($"{context}: invalid reference, multiplier, availability or duplicate event/edge.");
            if (!modifiers.TryGetValue(pair, out var list)) modifiers[pair] = list = [];
            list.Add(modifier with { FromStationObjectId = pair.From, ToStationObjectId = pair.To });
        }
        var result = ImmutableArray.CreateBuilder<EffectiveTradingRoute>(edges.Count);
        foreach (var (pair, edge) in edges.OrderBy(e => e.Key.From, StringComparer.Ordinal).ThenBy(e => e.Key.To, StringComparer.Ordinal))
        {
            var matching = modifiers.GetValueOrDefault(pair, []).OrderByDescending(m => m.Priority)
                .ThenBy(m => m.StartedGameTimeMs).ThenBy(m => m.EventId, StringComparer.Ordinal)
                .ThenBy(m => m.FromStationObjectId, StringComparer.Ordinal).ThenBy(m => m.ToStationObjectId, StringComparer.Ordinal).ToArray();
            long time = edge.TravelEstimateGameTimeMs;
            int fuel = edge.FuelMultiplierPermille;
            var availability = TradingRouteAvailability.Available;
            foreach (var modifier in matching)
            {
                try
                {
                    time = Scale(time, modifier.TravelTimeMultiplierPermille);
                    fuel = checked((int)Scale(fuel, modifier.FuelMultiplierPermille));
                }
                catch (OverflowException error)
                { throw new ScenarioException($"Event '{modifier.EventId}', route '{pair.From}'/'{pair.To}': effective time/fuel overflow.", error); }
                if (time <= 0 || fuel <= 0)
                    throw new ScenarioException($"Event '{modifier.EventId}', route '{pair.From}'/'{pair.To}': effective time/fuel is nonpositive.");
                availability = (TradingRouteAvailability)Math.Max((int)availability, (int)modifier.Availability);
            }
            bool elevated = edge.RiskProfileId != safe.RiskProfileId || matching.Any(m =>
                m.Availability != TradingRouteAvailability.Available || m.TravelTimeMultiplierPermille != 1000 || m.FuelMultiplierPermille != 1000);
            result.Add(new(edge, time, fuel, elevated ? TradingRouteRisk.Elevated : TradingRouteRisk.Safe, availability,
                matching.Length == 0 ? null : string.Join("; ", matching.Select(m => m.ReasonText)), matching.Select(m => m.EventId).ToImmutableArray()));
        }
        return result.MoveToImmutable();
    }

    internal static bool CanApply(TradingMapStateData map, IReadOnlyList<TradingRouteModifier> activeModifiers, IReadOnlyList<TradingRouteModifier> candidateModifiers, bool clusterGeography = false)
    {
        if (activeModifiers is null || candidateModifiers is null) throw new ScenarioException("Route modifiers must be declared.");
        var evaluated = Evaluate(map, activeModifiers.Concat(candidateModifiers).ToArray(), clusterGeography);
        var adjacency = map.Rules.Stations.ToDictionary(s => s.ObjectId, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.OrdinalIgnoreCase);
        foreach (var route in evaluated.Where(r => r.Availability != TradingRouteAvailability.Unavailable))
        {
            adjacency[route.BaseEdge.FromStationObjectId].Add(route.BaseEdge.ToStationObjectId);
            adjacency[route.BaseEdge.ToStationObjectId].Add(route.BaseEdge.FromStationObjectId);
        }
        HashSet<string> Reachable(string origin)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { origin };
            var queue = new Queue<string>(); queue.Enqueue(origin);
            while (queue.TryDequeue(out var current))
                foreach (string next in adjacency[current].Order(StringComparer.Ordinal))
                    if (seen.Add(next)) queue.Enqueue(next);
            return seen;
        }
        // Evaluate has already resolved every cargo endpoint. A connected undirected
        // network connects every one of those pairs; repeated BFS per cargo flow is redundant.
        return Reachable(adjacency.Keys.Order(StringComparer.Ordinal).First()).Count == adjacency.Count;
    }

    private static (string From, string To) Pair(string from, string to) =>
        StringComparer.Ordinal.Compare(from, to) <= 0 ? (from, to) : (to, from);
    private static long Scale(long value, int factor) => checked((long)decimal.Round(value * (decimal)factor / 1000m, 0, MidpointRounding.AwayFromZero));
}
