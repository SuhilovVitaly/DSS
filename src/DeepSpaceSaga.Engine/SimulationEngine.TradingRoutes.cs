using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ImmutableArray<TradingRouteModifier> BuildActiveRouteModifiers(IEnumerable<SpaceObjectRuntime> objects, long time) =>
        objects.Where(o => o.ObjectType == SpaceObjectType.Station && !o.Events.IsDefaultOrEmpty)
            .SelectMany(o => o.Events.Where(e => MarketEventActive(e, time) && e.RouteEffect?.FromStationObjectId is not null)
                .Select(RouteModifier)).ToImmutableArray();

    private TradingRouteModifier RouteModifier(StationEventRuntime evt)
    {
        var r = evt.RouteEffect!;
        var definition = _registry.StationMarketEvents.GetDefinition(_registry.StationMarketEvents.GetIndex(evt.DefinitionId!));
        if (!Enum.TryParse<TradingRouteAvailability>(r.Availability, out var availability) || !Enum.IsDefined(availability))
            throw new ScenarioException($"Event '{evt.EventId}' has invalid route availability.");
        return new(evt.EventId, evt.DefinitionId!, definition.Priority, evt.StartedGameTimeMs,
            r.FromStationObjectId!, r.ToStationObjectId!, availability, r.TravelTimeMultiplierPermille,
            r.FuelMultiplierPermille, definition.EffectSummaryKey);
    }

    private StationEventRuntime? BindRouteCandidate(TradingMapStateData? map, IEnumerable<SpaceObjectRuntime> objects,
        SpaceObjectRuntime station, StationEventRuntime evt, long time, ulong seed, bool clusterGeography = false)
    {
        if (map is null || evt.RouteEffect is null) return evt;
        string stationId = station.InitialMotion.ObjectId;
        // Include earlier candidates from the same station before they are published to the world.
        var active = BuildActiveRouteModifiers(objects.Where(o => o.InitialMotion.ObjectId != stationId).Append(station), time);
        var edges = TradingRouteEvaluator.Evaluate(map, [], clusterGeography).Select(e => e.BaseEdge)
            .Where(e => e.FromStationObjectId == stationId || e.ToStationObjectId == stationId)
            .OrderBy(e => e.FromStationObjectId, StringComparer.Ordinal).ThenBy(e => e.ToStationObjectId, StringComparer.Ordinal).ToArray();
        int offset = edges.Length == 0 ? 0 : (int)(MarketEventRoll(seed, stationId, evt.DefinitionId!,
            evt.StartedGameTimeMs / GameCalendar.HourMs, "route") % (ulong)edges.Length);
        for (int i = 0; i < edges.Length; i++)
        {
            var edge = edges[(offset + i) % edges.Length];
            var bound = evt with
            {
                RouteEffect = evt.RouteEffect with
                { FromStationObjectId = edge.FromStationObjectId, ToStationObjectId = edge.ToStationObjectId }
            };
            if (TradingRouteEvaluator.CanApply(map, active, [RouteModifier(bound)], clusterGeography)) return bound;
        }
        return null;
    }

    private void RestoreTradingRouteBindings(TradingMapStateData? map, List<SpaceObjectRuntime> objects, long time, ulong seed, bool clusterGeography = false)
    {
        foreach (int index in Enumerable.Range(0, objects.Count).OrderBy(i => objects[i].InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var station = objects[index];
            if (station.Events.IsDefaultOrEmpty) continue;
            var events = station.Events.ToBuilder();
            foreach (int j in Enumerable.Range(0, events.Count)
                .OrderByDescending(i => events[i].DefinitionId is { } id ? _registry.StationMarketEvents.GetDefinition(
                    _registry.StationMarketEvents.GetIndex(id)).Priority : -1)
                .ThenBy(i => events[i].StartedGameTimeMs).ThenBy(i => events[i].EventId, StringComparer.Ordinal))
            {
                var evt = events[j];
                if (evt.RouteEffect is not { } route) continue;
                if ((route.FromStationObjectId is null) != (route.ToStationObjectId is null))
                    throw new ScenarioException($"Event '{evt.EventId}' has an incomplete route binding.");
                if (map is null)
                {
                    if (route.FromStationObjectId is not null) throw new ScenarioException($"Event '{evt.EventId}' binds a route without a map.");
                    continue;
                }
                if (route.FromStationObjectId is not null)
                {
                    var edge = map.Edges.FirstOrDefault(e => Connects(e, route.FromStationObjectId, route.ToStationObjectId!));
                    if (edge is null || route.FromStationObjectId != station.InitialMotion.ObjectId && route.ToStationObjectId != station.InitialMotion.ObjectId)
                        throw new ScenarioException($"Event '{evt.EventId}' binds an unknown or nonincident route.");
                }
                else
                {
                    // Additive compatibility for US-0007 saves predating captured endpoints.
                    // Resolve once in stable station/event order, then persist the chosen edge.
                    var bound = BindRouteCandidate(map, objects, station with { Events = events.ToImmutable() }, evt, time, seed, clusterGeography);
                    if (bound is null) throw new ScenarioException($"Legacy event '{evt.EventId}' cannot preserve route connectivity.");
                    events[j] = bound;
                }
            }
            objects[index] = station with { Events = events.ToImmutable() };
        }
        if (map is null) return;
        var modifiers = BuildActiveRouteModifiers(objects, time);
        _ = TradingRouteEvaluator.Evaluate(map, modifiers, clusterGeography);
        foreach (var modifier in modifiers)
            if (!TradingRouteEvaluator.CanApply(map, modifiers.Where(m => m.EventId != modifier.EventId).ToArray(), [modifier], clusterGeography))
                throw new ScenarioException("Saved route events disconnect the trading network. Running world was not replaced.");
    }

    private EffectiveTradingRoute? FindEffectiveDepartureRoute(string origin, string destination) => CurrentVoyageMap() is not { } map ? null :
        TradingRouteEvaluator.Evaluate(map, BuildActiveRouteModifiers(_objects, _processedWorldTimeMs), _clusterMap is not null)
            .FirstOrDefault(e => Connects(e.BaseEdge, origin, destination));

    private ImmutableArray<TradingRouteSnapshot> BuildTradingRouteProjection(long time)
    {
        if (_tradingMap is null || _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId) is not
            { IsDocked: true, DockedStationObjectId: { } origin }) return [];
        return TradingRouteEvaluator.Evaluate(CurrentVoyageMap()!, BuildActiveRouteModifiers(_objects, time), _clusterMap is not null)
            .Where(e => e.BaseEdge.FromStationObjectId == origin || e.BaseEdge.ToStationObjectId == origin).Select(e =>
            {
                var b = e.BaseEdge;
                string destination = b.FromStationObjectId == origin ? b.ToStationObjectId : b.FromStationObjectId;
                return new TradingRouteSnapshot(origin, destination, b.DistanceClass, b.TravelEstimateGameTimeMs,
                    e.EffectiveTravelEstimateGameTimeMs, b.FuelMultiplierPermille, e.EffectiveFuelMultiplierPermille,
                    b.RiskProfileId, e.Risk, e.Availability, e.ReasonText, e.ActiveEventIds);
            }).OrderBy(e => e.DestinationStationObjectId, StringComparer.Ordinal).ToImmutableArray();
    }
}
