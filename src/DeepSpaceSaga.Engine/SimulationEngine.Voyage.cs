using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private VoyageStateData? _voyageState;

    private static VoyageStateData? ValidateVoyageState(GameStateData state, IReadOnlyList<SpaceObjectRuntime> objects)
    {
        var ship = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId);
        var saved = state.VoyageState;
        if (saved is null)
            return state.TradingMap is not null && ship is { IsDocked: true }
                ? new VoyageStateData(VoyagePhases.Docked) : null;
        if (state.TradingMap is null || ship is null)
            throw new ScenarioException("voyageState requires a materialized map and player ship.");
        if (saved.BlockReasonCode is not null && string.IsNullOrWhiteSpace(saved.BlockReasonCode))
            throw new ScenarioException("voyageState.blockReasonCode is invalid.");
        bool hasTerms = saved.TravelEstimateGameTimeMs is not null || saved.FuelMultiplierPermille is not null ||
            saved.RiskProfileId is not null || saved.ActiveEventIds is not null || saved.StartedGameTimeMs is not null || saved.ArrivalGameTimeMs is not null;
        if (hasTerms && (saved.Phase == VoyagePhases.Docked || saved.TravelEstimateGameTimeMs is not > 0 ||
            saved.FuelMultiplierPermille is not > 0 || string.IsNullOrWhiteSpace(saved.RiskProfileId) ||
            saved.ActiveEventIds is null || saved.ActiveEventIds.Any(string.IsNullOrWhiteSpace) ||
            saved.ActiveEventIds.Distinct(StringComparer.Ordinal).Count() != saved.ActiveEventIds.Count ||
            saved.StartedGameTimeMs is not >= 0 || saved.StartedGameTimeMs > state.GameTimeMs ||
            saved.ArrivalGameTimeMs is null || saved.StartedGameTimeMs > long.MaxValue - saved.TravelEstimateGameTimeMs ||
            saved.ArrivalGameTimeMs != saved.StartedGameTimeMs + saved.TravelEstimateGameTimeMs))
            throw new ScenarioException("voyageState captured route terms are invalid.");
        if (saved.Phase == VoyagePhases.Docked)
        {
            if (!ship.IsDocked || saved.VoyageId is not null || saved.OriginStationObjectId is not null ||
                saved.DestinationStationObjectId is not null || saved.ProgressPermille != 0 ||
                saved.InitialDistanceWorldUnits != 0 || saved.StartedMotionTimeMs != 0)
                throw new ScenarioException("Docked voyageState conflicts with the player ship.");
            return saved;
        }
        if (saved.Phase is not (VoyagePhases.Undocking or VoyagePhases.InTransit or VoyagePhases.Docking) ||
            (ship.IsDocked && (saved.Phase != VoyagePhases.Docking ||
                ship.DockedStationObjectId != saved.DestinationStationObjectId)) ||
            string.IsNullOrWhiteSpace(saved.VoyageId) ||
            string.IsNullOrWhiteSpace(saved.OriginStationObjectId) ||
            string.IsNullOrWhiteSpace(saved.DestinationStationObjectId) ||
            saved.OriginStationObjectId == saved.DestinationStationObjectId ||
            saved.StartedMotionTimeMs < 0 || saved.StartedMotionTimeMs > state.MotionTimeMs ||
            !double.IsFinite(saved.InitialDistanceWorldUnits) || saved.InitialDistanceWorldUnits <= 0 ||
            saved.ProgressPermille is < 0 or > 1000)
            throw new ScenarioException("Active voyageState is invalid.");
        bool HasStation(string id) => objects.Any(o => o.InitialMotion.ObjectId == id && o.ObjectType == SpaceObjectType.Station);
        if (!HasStation(saved.OriginStationObjectId) || !HasStation(saved.DestinationStationObjectId) ||
            !state.TradingMap.Edges.Any(e => Connects(e, saved.OriginStationObjectId, saved.DestinationStationObjectId)))
            throw new ScenarioException("Active voyageState does not follow a materialized edge.");
        if (hasTerms && !state.TradingMap.Rules.RiskProfiles.Any(r => r.RiskProfileId == saved.RiskProfileId))
            throw new ScenarioException("Active voyageState captured risk profile is unknown.");
        if (saved.Phase == VoyagePhases.Docking &&
            (state.DialogueState?.ActiveDialogue is not { } dialogue ||
             dialogue.DialogueDefinitionId != "dialogue.station-docking" ||
             dialogue.StationObjectId != saved.DestinationStationObjectId))
            throw new ScenarioException("Docking voyageState requires the destination docking dialogue.");
        return saved with { ActiveEventIds = saved.ActiveEventIds?.ToImmutableArray(), FuelReservationParts = saved.FuelReservationParts?.ToImmutableArray() };
    }

    private static bool Connects(TradingMapEdgeData edge, string a, string b) =>
        edge.FromStationObjectId == a && edge.ToStationObjectId == b ||
        edge.FromStationObjectId == b && edge.ToStationObjectId == a;

    private string? ResolveVoyageDepartureBlock(SpaceObjectRuntime ship, string? destination)
    {
        if (_voyageState is { Phase: not VoyagePhases.Docked }) return CommandReasonCodes.VoyageAlreadyActive;
        if (ship.PortFeeDebt > 0) return CommandReasonCodes.VoyageOutstandingDebt;
        if (string.IsNullOrWhiteSpace(destination)) return CommandReasonCodes.VoyageDestinationRequired;
        if (_tradingMap is null || ship.DockedStationObjectId is null ||
            !_tradingMap.Edges.Any(e => Connects(e, ship.DockedStationObjectId, destination)) ||
            !_objects.Any(o => o.InitialMotion.ObjectId == destination && o.ObjectType == SpaceObjectType.Station && !o.IsDestroyed))
            return CommandReasonCodes.RouteUnavailable;
        var route = FindEffectiveDepartureRoute(ship.DockedStationObjectId, destination);
        if (route is null || route.Availability == TradingRouteAvailability.Unavailable ||
            _processedWorldTimeMs > long.MaxValue - route.EffectiveTravelEstimateGameTimeMs)
            return CommandReasonCodes.RouteUnavailable;
        return PrepareVoyageFuel(ship, route.BaseEdge.DistanceKm, route.EffectiveFuelMultiplierPermille,
            out _, out _, out _, out _);
    }

    private VoyageSnapshot? BuildVoyageSnapshot()
    {
        if (_tradingMap is null || _voyageState is not { } state) return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (state.Phase == VoyagePhases.Docked)
        {
            var options = ImmutableArray.CreateBuilder<VoyageRouteOptionSnapshot>();
            if (ship?.DockedStationObjectId is { } origin)
            {
                foreach (var edge in _tradingMap.Edges)
                {
                    if (edge.FromStationObjectId != origin && edge.ToStationObjectId != origin) continue;
                    string destination = edge.FromStationObjectId == origin ? edge.ToStationObjectId : edge.FromStationObjectId;
                    var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == destination && o.ObjectType == SpaceObjectType.Station);
                    string? blocker = ResolveVoyageDepartureBlock(ship, destination);
                    options.Add(new VoyageRouteOptionSnapshot(destination, station?.Name ?? destination,
                        FindEffectiveDepartureRoute(origin, destination)!.EffectiveTravelEstimateGameTimeMs, edge.DistanceClass, blocker is null, blocker));
                }
            }
            return new VoyageSnapshot(VoyagePhases.Docked, BlockReasonCode: state.BlockReasonCode,
                RouteOptions: options.OrderBy(o => o.DestinationStationObjectId, StringComparer.Ordinal).ToImmutableArray());
        }
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.DestinationStationObjectId);
        var fuel = ProjectVoyageFuel(state);
        return new VoyageSnapshot(state.Phase, state.VoyageId, state.OriginStationObjectId,
            state.DestinationStationObjectId, target?.Name ?? state.DestinationStationObjectId,
            state.ProgressPermille, state.BlockReasonCode, ImmutableArray<VoyageRouteOptionSnapshot>.Empty,
            state.FuelReservationParts is null ? null : fuel.Reserved,
            state.FuelReservationParts is null ? null : fuel.Consumed,
            state.FuelReservationParts is null ? null : fuel.Cost);
    }

    private void UpdateVoyageForMotion(long motionTimeMs)
    {
        if (_voyageState is not { } state || state.Phase == VoyagePhases.Docked) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.DestinationStationObjectId);
        if (ship is null || ship.IsDestroyed || target is null || target.IsDestroyed)
        {
            SettleVoyageFuel(state, arrived: false);
            _voyageState = null;
            int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
            if (shipIndex >= 0 && _objects[shipIndex].IsDocked)
                _objects[shipIndex] = _objects[shipIndex] with
                {
                    IsDocked = false,
                    DockedStationObjectId = null,
                    FirstPortFeeGameTimeMs = null,
                    NextPortFeeDueGameTimeMs = null
                };
            if (_dialogue.Active?.StationObjectId == state.DestinationStationObjectId)
                EndDialogue(motionTimeMs, null, "dialogue_aborted");
            return;
        }
        if (state.Phase == VoyagePhases.Docking || motionTimeMs <= state.StartedMotionTimeMs) return;
        var shipMotion = PredictMotion(ship, Math.Max(0, motionTimeMs - ship.StartGameTimeMs));
        var targetMotion = PredictMotion(target, Math.Max(0, motionTimeMs - target.StartGameTimeMs));
        double dx = targetMotion.X - shipMotion.X;
        double dy = targetMotion.Y - shipMotion.Y;
        double remaining = Math.Sqrt(dx * dx + dy * dy);
        int progress = (int)Math.Round(Math.Clamp(1 - remaining / state.InitialDistanceWorldUnits, 0, 1) * 1000,
            MidpointRounding.AwayFromZero);
        _voyageState = state with
        {
            Phase = VoyagePhases.InTransit,
            ProgressPermille = Math.Max(state.ProgressPermille, progress),
            BlockReasonCode = null
        };
    }

    private void ReconcileVoyageAfterDialogue()
    {
        if (_dialogue.Active is not null) return;
        // A free-flight start can dock without an existing voyage. Publish departure
        // options for that first visit as soon as its dialogue has finished.
        if (_tradingMap is not null && _voyageState is null &&
            _objects.Any(o => o.InitialMotion.ObjectId == PlayerShipObjectId && o.IsDocked))
            _voyageState = new VoyageStateData(VoyagePhases.Docked);
        if (_voyageState is not { Phase: VoyagePhases.Docking } state) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is { IsDocked: true } && ship.DockedStationObjectId == state.DestinationStationObjectId)
        {
            SettleVoyageFuel(state, arrived: true);
            _voyageState = new VoyageStateData(VoyagePhases.Docked);
        }
        else _voyageState = state with { Phase = VoyagePhases.InTransit };
    }
}
