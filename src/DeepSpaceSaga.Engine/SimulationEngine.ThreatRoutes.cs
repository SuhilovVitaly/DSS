using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ThreatRoutesSnapshot? BuildThreatRoutes()
    {
        if (_demoCampaign.PirateWorld is null || CurrentVoyageMap() is not { } map || _voyageState is null) return null;
        if (_voyageState.Phase != VoyagePhases.Docked)
            return new(_demoCampaign.Revision, [], _voyageState.ThreatRoute?.Option.RouteId);
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship.DockedStationObjectId is not { } origin) return new(_demoCampaign.Revision, []);
        var pose = PredictMotion(ship, Math.Max(0, _processedSimulationTimeMs - ship.StartGameTimeMs));
        var start = new ThreatRouteWaypoint(pose.X, pose.Y);
        var hazards = _demoCampaign.PirateWorld.Groups.Where(g => !g.Retired && g.PlayerObservation is not null)
            .Select(g => new ThreatRouteWaypoint(g.PlayerObservation!.X, g.PlayerObservation.Y)).ToArray();
        var options = ImmutableArray.CreateBuilder<ThreatRouteOption>();
        foreach (var route in TradingRouteEvaluator.Evaluate(map, BuildActiveRouteModifiers(_objects, _processedWorldTimeMs), _clusterMap is not null))
        {
            var edge = route.BaseEdge;
            if (edge.FromStationObjectId != origin && edge.ToStationObjectId != origin) continue;
            string destination = edge.FromStationObjectId == origin ? edge.ToStationObjectId : edge.FromStationObjectId;
            var target = _objects.Single(o => o.InitialMotion.ObjectId == destination);
            var targetPose = PredictMotion(target, Math.Max(0, _processedSimulationTimeMs - target.StartGameTimeMs));
            var end = new ThreatRouteWaypoint(targetPose.X, targetPose.Y);
            var direct = ImmutableArray.Create(end);
            var detour = ChooseThreatDetour(start, end, hazards);
            foreach (bool avoid in new[] { false, true })
            {
                var points = avoid ? detour : direct;
                double length = ThreatPathLength(start, points);
                var engineType = ship.Modules.Select(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex)).FirstOrDefault(IsEngineCategory);
                double cruise = GetMaxSpeedKmS(ship) ?? 0;
                double acceleration = (engineType?.LinearInertiaMps2 ?? 0) / 1000.0;
                long duration = ThreatTravelDuration(route, length / 10, ship, pose);
                long eta = checked(_processedWorldTimeMs + duration);
                string? blocked = route.Availability == TradingRouteAvailability.Unavailable ? CommandReasonCodes.RouteUnavailable :
                    ship.PortFeeDebt > 0 ? CommandReasonCodes.VoyageOutstandingDebt : null;
                var fuelReason = PrepareVoyageFuel(ship, length / 10, route.EffectiveFuelMultiplierPermille,
                    out _, out var parts, out _, out _);
                blocked ??= fuelReason;
                if (cruise <= 0 || acceleration <= 0) blocked ??= CommandReasonCodes.ModuleUnavailable;
                long? fuel = fuelReason is null ? parts.Sum(p => p.ReservedFuelKg) : null;
                var transport = BuildDemoTransportProjection();
                var deadlines = (transport?.PassengerContracts?.Active.Where(c => c.DestinationStationId == destination).Select(c => c.DeadlineGameTimeMs) ?? [])
                    .Concat(transport?.CargoContracts?.Contracts.Where(c => c.Status == StationJobStatuses.Active && c.DestinationStationId == destination)
                        .Select(c => c.DeadlineGameTimeMs) ?? []).ToArray();
                double exposure = ThreatExposure(start, points, hazards);
                options.Add(new((avoid ? "detour:" : "direct:") + destination, destination, avoid, points, eta, fuel,
                    hazards.Length == 0 ? "unknown" : exposure > 0 ? "exposed" : "lower", exposure,
                    deadlines.Length == 0 ? null : deadlines.Min() - eta, blocked));
            }
        }
        var published = options.ToImmutable();
        long revision = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(published))), 0) & long.MaxValue;
        return new(revision, published, _voyageState.ThreatRoute?.Option.RouteId);
    }

    internal static double ThreatPathLength(ThreatRouteWaypoint start, IEnumerable<ThreatRouteWaypoint> points)
    {
        double length = 0;
        foreach (var point in points) { length += Distance(start.X, start.Y, point.X, point.Y); start = point; }
        return length;
    }

    internal static double ThreatExposure(ThreatRouteWaypoint start, IEnumerable<ThreatRouteWaypoint> points,
        IReadOnlyList<ThreatRouteWaypoint> hazards)
    {
        double exposure = 0;
        foreach (var end in points)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y, a = dx * dx + dy * dy;
            if (a > 0) foreach (var hazard in hazards)
            {
                double x = start.X - hazard.X, y = start.Y - hazard.Y;
                double b = x * dx + y * dy, c = x * x + y * y - 2000 * 2000, d = b * b - a * c;
                if (d > 0) exposure += Math.Max(0, Math.Min(1, (-b + Math.Sqrt(d)) / a) -
                    Math.Max(0, (-b - Math.Sqrt(d)) / a)) * Math.Sqrt(a);
            }
            start = end;
        }
        return exposure;
    }

    internal static ImmutableArray<ThreatRouteWaypoint> ChooseThreatDetour(ThreatRouteWaypoint start,
        ThreatRouteWaypoint end, IReadOnlyList<ThreatRouteWaypoint> hazards)
    {
        double dx = end.X - start.X, dy = end.Y - start.Y, length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var choices = new List<ImmutableArray<ThreatRouteWaypoint>>();
        foreach (double offset in new[] { 4000.0, 8000, 16000, 32000, 64000 })
            foreach (int side in new[] { 1, -1 })
                choices.Add([new(start.X + dx * .25 - side * dy / length * offset, start.Y + dy * .25 + side * dx / length * offset),
                    new(start.X + dx * .75 - side * dy / length * offset, start.Y + dy * .75 + side * dx / length * offset), end]);
        return choices.OrderBy(p => ThreatExposure(start, p, hazards)).ThenBy(p => ThreatPathLength(start, p)).First();
    }

    private void PrepareThreatRoute(SelectDemoRouteCommand command, ref string? reason, ref Action? commit)
    {
        var routes = BuildThreatRoutes();
        if (command.RouteRevision != routes?.Revision) { reason = DemoCampaignReasons.StaleRevision; return; }
        var option = routes.Options.FirstOrDefault(o => o.RouteId == command.RouteId);
        if (option is null) { reason = CommandReasonCodes.RouteUnavailable; return; }
        if (option.UnavailableReason is { } unavailable) { reason = unavailable; return; }
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var pose = PredictMotion(ship, Math.Max(0, _processedSimulationTimeMs - ship.StartGameTimeMs));
        var selected = new ThreatRouteSelectionData(option, pose.X, pose.Y);
        commit = () => _voyageState = _voyageState! with { ThreatRoute = selected };
    }

    private EffectiveTradingRoute ApplySelectedThreatRoute(EffectiveTradingRoute route, string destination)
    {
        if (_voyageState?.ThreatRoute is not { } selected || selected.Option.DestinationStationId != destination) return route;
        double distance = ThreatPathLength(new(selected.OriginX, selected.OriginY), selected.Option.Waypoints) / 10;
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var pose = PredictMotion(ship, Math.Max(0, _processedSimulationTimeMs - ship.StartGameTimeMs));
        return route with
        {
            BaseEdge = route.BaseEdge with { DistanceKm = distance },
            EffectiveTravelEstimateGameTimeMs = ThreatTravelDuration(route, distance, ship, pose)
        };
    }

    private long ThreatTravelDuration(EffectiveTradingRoute route, double distanceKm, SpaceObjectRuntime ship, ObjectMotionSnapshot pose)
    {
        var type = ship.Modules.Select(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex)).FirstOrDefault(IsEngineCategory);
        double acceleration = (type?.LinearInertiaMps2 ?? 0) / 1000.0;
        double ramp = acceleration > 0 ? Math.Max(0, (GetMaxSpeedKmS(ship) ?? pose.SpeedKmS) - pose.SpeedKmS) / acceleration : 0;
        return checked(Math.Max(1, (long)Math.Ceiling(route.EffectiveTravelEstimateGameTimeMs * distanceKm / route.BaseEdge.DistanceKm + ramp * 300000)));
    }

    private void AdvanceThreatRoute(long time)
    {
        if (_voyageState is not { Phase: VoyagePhases.Undocking or VoyagePhases.InTransit, ThreatRoute: { } selected } state ||
            HasCapturedEncounter || _demoCampaign.PendingSensorReport is not null || selected.Suspended ||
            selected.NextWaypoint >= selected.Option.Waypoints.Length) return;
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship.IsDocked || ship.IsDestroyed) return;
        var pose = PredictMotion(ship, Math.Max(0, time - ship.StartGameTimeMs));
        var engine = ship.Modules.FirstOrDefault(m => IsEngineCategory(_registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex)));
        if (engine is null) return;
        if (engine.ActiveCycle is { } cycle &&
            (cycle.CommandType != ShipEngineCommandTypes.Accelerate || cycle.CommandId?.StartsWith("route:", StringComparison.Ordinal) != true ||
             pose.SpeedKmS + 1e-6 < (GetMaxSpeedKmS(ship) ?? pose.SpeedKmS))) return;
        if (selected.NavigationStarted)
        {
            var current = PredictMotion(ship, Math.Max(0, time - ship.StartGameTimeMs));
            var waypoint = selected.Option.Waypoints[selected.NextWaypoint];
            var type = _registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex);
            double tolerance = current.SpeedKmS * 10 * MinTurnIntervalMs(type.AngularInertiaDegPerSec ?? 1) / 1000.0 + 1;
            if (Distance(current.X, current.Y, waypoint.X, waypoint.Y) > tolerance)
            {
                _voyageState = state with { ThreatRoute = selected with { Suspended = true } };
                return;
            }
            selected = selected with { NextWaypoint = selected.NextWaypoint + 1, NavigationStarted = false };
            _voyageState = state with { ThreatRoute = selected };
            if (selected.NextWaypoint >= selected.Option.Waypoints.Length) return;
        }
        var target = selected.Option.Waypoints[selected.NextWaypoint];
        bool accelerate = pose.SpeedKmS + 1e-6 < (GetMaxSpeedKmS(ship) ?? pose.SpeedKmS);
        var command = new PlayerCommand($"route:{state.VoyageId}:{selected.NextWaypoint}:{time}", 0, ship.InitialMotion.ObjectId,
            engine.ModuleId, accelerate ? ShipEngineCommandTypes.Accelerate : ShipEngineCommandTypes.Orbit,
            TargetWorldX: target.X, TargetWorldY: target.Y);
        var result = TryStartEngineCommand(command, time);
        if (!accelerate && result.Disposition == CommandStartDisposition.Started)
            _voyageState = _voyageState! with { ThreatRoute = selected with { NavigationStarted = true } };
    }

    private CommandStartOutcome HandleThreatManualOverride(CommandStartOutcome outcome)
    {
        if (outcome.Disposition == CommandStartDisposition.Started &&
            _voyageState is { Phase: not VoyagePhases.Docked, ThreatRoute: { } route } voyage)
            _voyageState = voyage with { ThreatRoute = route with { Suspended = true } };
        return outcome;
    }
}
