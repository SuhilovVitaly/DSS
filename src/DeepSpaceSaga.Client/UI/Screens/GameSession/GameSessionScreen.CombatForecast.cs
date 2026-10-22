using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatForecastCache _combatForecastCache = new();
    internal CombatForecastCache ForecastCache => _combatForecastCache;
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
            // Forecast starts at the same permitted pose as the displayed scene,
            // including a hostile segment clamped at its confirmed endpoint.
            snapshot = snapshot with
            {
                SimulationTimeMs = snapshot.MotionTimeMs + delta,
                Objects = snapshot.Objects.Where(o => o.OwnProjectile is not null || o.ObservedProjectile is not null)
                    .Select(o => _predictor.Predict(o, delta)).ToImmutableArray()
            };
        }
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
    }

    private CombatTrajectoryProjector.Geometry? PreparedFlight(string id, SnapshotPrediction? prediction) =>
        prediction is not null && _preparedForecastGeometry.TryGetValue(id, out var geometry) ? geometry : null;
}
