using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatForecastCache _combatForecastCache = new();
    internal CombatForecastCache ForecastCache => _combatForecastCache;
    // Previews change their launch state every frame. Bound their synchronous work
    // instead of publishing an async result for a different physical launch time.
    private const int PreviewForecastSteps = 256;
    private SnapshotPrediction? _preparedForecastPrediction;
    private IReadOnlyDictionary<string, CombatForecastResult>? _preparedForecastResults;
    private double _preparedForecastTime;
    private readonly Dictionary<string, CombatTrajectoryProjector.Geometry> _preparedForecastGeometry = new(StringComparer.Ordinal);
    private void PrepareCombatForecasts()
    {
        var prediction = _buffer.LatestPrediction;
        var snapshot = prediction?.BufferedSnapshot.Snapshot;
        if (snapshot is not null && prediction is not null && CombatPredictionDelta(prediction) is > 0 and var delta)
        {
            UpdateCombatPoseObjects(snapshot);
            // Forecast starts at the same permitted pose as the displayed scene,
            // including a hostile segment clamped at its confirmed endpoint.
            snapshot = snapshot with
            {
                SimulationTimeMs = snapshot.MotionTimeMs + delta,
                Objects = snapshot.Objects.Select(o => _combatPoseObjectIds.Contains(o.ObjectId)
                    ? _predictor.Predict(o, delta) : o).ToImmutableArray()
            };
        }
        if (snapshot is not null) snapshot = WithObservedPlayerTargets(snapshot);
        _combatForecastCache.Prepare(_buffer.PresentationSessionId, snapshot,
            new CombatForecastOptions(CombatSettings.ForecastHorizonMs));
        _preparedForecastPrediction = prediction;
        double now = prediction is null ? 0 : prediction.BufferedSnapshot.Snapshot.MotionTimeMs + CombatPredictionDelta(prediction);
        if (!ReferenceEquals(_preparedForecastResults, _combatForecastCache.Results) || now != _preparedForecastTime)
        {
            foreach (var id in _preparedForecastGeometry.Keys.Where(id => !_combatForecastCache.Results.ContainsKey(id)).ToArray())
                _preparedForecastGeometry.Remove(id);
            foreach (var (id, result) in _combatForecastCache.Results)
            {
                var current = now > result.EndMotionTimeMs && result.Status == CombatForecastStatus.Contact
                    ? result with { Status = CombatForecastStatus.Recalculating } : result;
                _preparedForecastGeometry.TryGetValue(id, out var previous);
                var projectile = snapshot?.Objects.FirstOrDefault(o => o.ObjectId == id);
                _preparedForecastGeometry[id] = projectile is not null && (projectile.OwnProjectile is not null || projectile.ObservedProjectile is not null)
                    ? CombatTrajectoryProjector.FromPointForecast(current, projectile, now, previous)
                    : CombatTrajectoryProjector.FromForecast(current, now, previous);
            }
            _preparedForecastResults = _combatForecastCache.Results;
            _preparedForecastTime = now;
        }
        PrepareLaunchForecast(prediction);
        PrepareManualDefenseForecast(prediction);
    }

    private AuthoritativeSnapshot WithObservedPlayerTargets(AuthoritativeSnapshot snapshot)
    {
        HashSet<string>? targets = null;
        foreach (var obj in snapshot.Objects)
            if (obj.Torpedo is { } flight && flight.OwnerObjectId == snapshot.PlayerShipObjectId)
                (targets ??= new(StringComparer.Ordinal)).Add(flight.TargetObjectId);
        // Hover previews have no active flight. Avoid a collection pipeline and
        // allocations on that frame-sensitive path.
        if (targets is null) return snapshot;
        return snapshot with
        {
            Objects = snapshot.Objects.Select(o => o.RenderObjectType == SpaceObjectType.NpcShip && targets.Contains(o.ObjectId)
                ? FutureTrajectoryProjector.ObservedNpcForecastState(o, _buffer.GetNpcObservedTurnRate(o.ObjectId)) : o).ToImmutableArray()
        };
    }

    private CombatTrajectoryProjector.Geometry? PreparedFlight(string id, SnapshotPrediction? prediction)
    {
        if (prediction is null || !_preparedForecastGeometry.TryGetValue(id, out var geometry)) return null;
        // A changing target invalidates contact confidence, not the available
        // conditional flight path. Keep the path visible while observations settle.
        if (prediction.BufferedSnapshot.Snapshot is { } snapshot &&
            snapshot.Objects.FirstOrDefault(o => o.ObjectId == id)?.Torpedo is { } flight &&
            flight.OwnerObjectId == snapshot.PlayerShipObjectId &&
            snapshot.Objects.Any(o => o.ObjectId == flight.TargetObjectId && o.RenderObjectType == SpaceObjectType.NpcShip) &&
            !_buffer.HasStableNpcMotion(flight.TargetObjectId))
            return geometry with { Intercept = null, EtaSeconds = null, Status = CombatForecastStatus.InsufficientData };
        return geometry;
    }

    private void PrepareLaunchForecast(SnapshotPrediction? prediction)
    {
        if (prediction is null || !IsLaunchPreviewRequested() || FindLauncher()?.LauncherCombat is not { } launcher)
        { ClearLaunchPreview(); return; }
        var snapshot = prediction.BufferedSnapshot.Snapshot;
        var ownerSource = snapshot.Objects.FirstOrDefault(o => o.ObjectId == snapshot.PlayerShipObjectId);
        var targetSource = snapshot.Objects.FirstOrDefault(o => o.ObjectId == _selectedObjectId);
        if (ownerSource is null || targetSource is null) { ClearLaunchPreview(); return; }
        long delta = CombatPredictionDelta(prediction);
        var owner = _predictor.Predict(ownerSource, delta);
        var target = _predictor.Predict(targetSource, delta);
        if (target.RenderObjectType == SpaceObjectType.NpcShip)
            target = FutureTrajectoryProjector.ObservedNpcForecastState(target, _buffer.GetNpcObservedTurnRate(target.ObjectId));
        long time = snapshot.MotionTimeMs + delta;
        long? lifetime = FindLauncher()?.TorpedoMaxFlightTimeMs;
        var key = (owner, target, launcher, time, lifetime);
        if (_launchPreviewKey == key) return;
        LaunchPreviewRoute = TorpedoGuidanceMath.Plan(owner, target, launcher.SpeedKmS, launcher.TurnRateDegPerSec, time);
        if (lifetime is not > 0 || time > long.MaxValue - lifetime.Value)
        {
            LaunchPreviewGeometry = new([new(owner.X, owner.Y)], [], null)
            { Status = CombatForecastStatus.InsufficientData, EndMotionTimeMs = time };
            _launchPreviewKey = key;
            return;
        }
        var proposed = new TorpedoSnapshot(owner.ObjectId, FindLauncher()!.ModuleId, target.ObjectId, time,
            launcher.SpeedKmS, launcher.TurnRateDegPerSec, launcher.Damage, 0, LaunchPreviewRoute, [],
            ExpiresAtMotionTimeMs: time + lifetime.Value);
        var projectile = owner with { ObjectId = "launch-preview", Torpedo = proposed };
        var options = new CombatForecastOptions(CombatSettings.ForecastHorizonMs, MaxSteps: PreviewForecastSteps);
        var result = CombatForecast.Torpedo(projectile, target, time, options);
        LaunchPreviewGeometry = CombatTrajectoryProjector.FromForecast(result, time);
        _launchPreviewKey = key;
    }
}
