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
        if (saved.Phase == VoyagePhases.Docking &&
            (state.DialogueState?.ActiveDialogue is not { } dialogue ||
             dialogue.DialogueDefinitionId != "dialogue.station-docking" ||
             dialogue.StationObjectId != saved.DestinationStationObjectId))
            throw new ScenarioException("Docking voyageState requires the destination docking dialogue.");
        return saved;
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
            ! _tradingMap.Edges.Any(e => Connects(e, ship.DockedStationObjectId, destination)) ||
            !_objects.Any(o => o.InitialMotion.ObjectId == destination && o.ObjectType == SpaceObjectType.Station && !o.IsDestroyed))
            return CommandReasonCodes.VoyageDestinationUnavailable;
        return null;
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
                        edge.TravelEstimateGameTimeMs, edge.DistanceClass, blocker is null, blocker));
                }
            }
            return new VoyageSnapshot(VoyagePhases.Docked, BlockReasonCode: state.BlockReasonCode,
                RouteOptions: options.OrderBy(o => o.DestinationStationObjectId, StringComparer.Ordinal).ToImmutableArray());
        }
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.DestinationStationObjectId);
        return new VoyageSnapshot(state.Phase, state.VoyageId, state.OriginStationObjectId,
            state.DestinationStationObjectId, target?.Name ?? state.DestinationStationObjectId,
            state.ProgressPermille, state.BlockReasonCode, ImmutableArray<VoyageRouteOptionSnapshot>.Empty);
    }

    private void UpdateVoyageForMotion(long motionTimeMs)
    {
        if (_voyageState is not { } state || state.Phase == VoyagePhases.Docked) return;
        if (state.Phase == VoyagePhases.Docking) return;
        if (motionTimeMs <= state.StartedMotionTimeMs) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.DestinationStationObjectId);
        if (ship is null || target is null || target.IsDestroyed)
        {
            _voyageState = state with { Phase = VoyagePhases.InTransit,
                BlockReasonCode = CommandReasonCodes.VoyageDestinationUnavailable };
            return;
        }
        var shipMotion = PredictMotion(ship, Math.Max(0, motionTimeMs - ship.StartGameTimeMs));
        var targetMotion = PredictMotion(target, Math.Max(0, motionTimeMs - target.StartGameTimeMs));
        double dx = targetMotion.X - shipMotion.X;
        double dy = targetMotion.Y - shipMotion.Y;
        double remaining = Math.Sqrt(dx * dx + dy * dy);
        int progress = (int)Math.Round(Math.Clamp(1 - remaining / state.InitialDistanceWorldUnits, 0, 1) * 1000,
            MidpointRounding.AwayFromZero);
        _voyageState = state with { Phase = VoyagePhases.InTransit,
            ProgressPermille = Math.Max(state.ProgressPermille, progress), BlockReasonCode = null };
    }

    private void ReconcileVoyageAfterDialogue()
    {
        if (_voyageState is not { Phase: VoyagePhases.Docking } state || _dialogue.Active is not null) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        _voyageState = ship is { IsDocked: true } && ship.DockedStationObjectId == state.DestinationStationObjectId
            ? new VoyageStateData(VoyagePhases.Docked)
            : state with { Phase = VoyagePhases.InTransit };
    }
}
