using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Computes future trajectory points from the current predicted state
/// using observed constant-speed trends for NPC ships and the shared predictor for other objects.
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class FutureTrajectoryProjector
{
    public const int FutureTrajectoryHorizonMs = 200_000;
    public const int FutureTrajectorySampleIntervalMs = 250;

    /// <summary>Maximum number of sample points per object (inclusive of t=0).</summary>
    internal static readonly int MaxSamplePoints = FutureTrajectoryHorizonMs / FutureTrajectorySampleIntervalMs + 1;

    private readonly IMotionPredictor _predictor;
    private readonly (double X, double Y)[] _turnPositions = new (double, double)[MaxSamplePoints];
    private ObjectMotionSnapshot? _cachedTurnState;
    private ObjectMotionSnapshot? _cachedTurnTerminal;

    public FutureTrajectoryProjector(IMotionPredictor predictor)
    {
        _predictor = predictor;
    }

    /// <summary>
    /// Compute future world-coordinate trajectory points from the predicted state.
    /// Uses the shared straight-line formula or discrete-step predictor as appropriate
    /// to match the authoritative simulation and preserve turn-cycle phase.
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState, double npcTurnRateDegPerSec = 0)
    {
        var points = new List<FutureTrajectoryPoint>(MaxSamplePoints);
        ProjectInto(predictedState, points, npcTurnRateDegPerSec);
        return points;
    }

    /// <summary>Fill caller-owned frame scratch space; immutable snapshots are never mutated.</summary>
    public void ProjectInto(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points, double npcTurnRateDegPerSec = 0)
    {
        points.Clear();
        if (state.RenderObjectType == SpaceObjectType.UnknownSpaceObject) return;
        if (state.RenderObjectType == SpaceObjectType.NpcShip)
        {
            ProjectObservedNpc(state, points, npcTurnRateDegPerSec);
            return;
        }
        if (state.HostileCombat is not null && CurrentManeuverForecast.HasRequiredData(state))
        {
            for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
            {
                var sample = CurrentManeuverForecast.Sample(state, t);
                points.Add(new(sample.X, sample.Y));
            }
            return;
        }
        if (_predictor is LinearMotionPredictor && LinearMotionPredictor.IsLinear(state))
        {
            double angle = state.Direction * Math.PI / 180;
            double sin = Math.Sin(angle), cos = Math.Cos(angle);
            for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
            {
                double distance = state.SpeedKmS * (t / 1000.0) * 10;
                points.Add(new(state.X + distance * sin, state.Y - distance * cos));
            }
            return;
        }
        if (TryProjectTurn(state, points)) return;
        ProjectDiscrete(state, points);
    }

    /// <summary>
    /// Player and target displays extend straight flight to the viewport edge instead of stopping
    /// after 200 seconds. Curved/closed motion retains the predictor's actual geometry.
    /// Time-based callers keep ProjectInto's bounded horizon.
    /// </summary>
    internal void ProjectViewportInto(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points,
        CameraState camera, int width, int height, double npcTurnRateDegPerSec = 0)
    {
        if (state.RenderObjectType == SpaceObjectType.UnknownSpaceObject) { points.Clear(); return; }
        if (state.RenderObjectType == SpaceObjectType.NpcShip)
        {
            points.Clear();
            if (npcTurnRateDegPerSec == 0)
            {
                points.Add(new(state.X, state.Y));
                if (state.SpeedKmS > 0) TrajectoryViewportGeometry.ExtendToEdge(points, state.Direction, camera, width, height);
            }
            else ProjectObservedNpc(state, points, npcTurnRateDegPerSec);
            return;
        }
        if (_predictor is LinearMotionPredictor && LinearMotionPredictor.IsLinear(state))
        {
            points.Clear();
            points.Add(new(state.X, state.Y));
            if (state.SpeedKmS > 0) TrajectoryViewportGeometry.ExtendToEdge(points, state.Direction, camera, width, height);
            return;
        }
        ProjectInto(state, points);
        var lastState = state == _cachedTurnState && _predictor is LinearMotionPredictor
            ? _cachedTurnTerminal! : _predictor.Predict(state, FutureTrajectoryHorizonMs);
        if (lastState.SpeedKmS > 0 && LinearMotionPredictor.IsLinear(lastState))
            TrajectoryViewportGeometry.ExtendToEdge(points, lastState.Direction, camera, width, height);
    }

    /// <summary>
    /// Determine whether a future trajectory should be drawn for the given object.
    /// </summary>
    public static bool ShouldDraw(ObjectMotionSnapshot state)
    {
        if (state.SpeedKmS > 0)
            return true;

        if (state.ActiveEngineCommandType is not null)
            return true;

        return false;
    }

    private static void ProjectObservedNpc(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points, double rate)
    {
        double h = state.Direction * Math.PI / 180;
        double w = rate * Math.PI / 180;
        double v = state.SpeedKmS * 10;
        for (int ms = 0; ms <= FutureTrajectoryHorizonMs; ms += FutureTrajectorySampleIntervalMs)
        {
            double t = ms / 1000.0;
            // Midpoint/sinc form of the circular integral stays stable as w approaches zero.
            double half = w * t / 2;
            double distance = v * t * (Math.Abs(half) < 1e-10 ? 1 : Math.Sin(half) / half);
            points.Add(new(state.X + distance * Math.Sin(h + half), state.Y - distance * Math.Cos(h + half)));
        }
    }

    private bool TryProjectTurn(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points)
    {
        if (_predictor is not LinearMotionPredictor) return false;
        if (state != _cachedTurnState)
        {
            if (!LinearMotionPredictor.TryPredictTurnPositions(state, FutureTrajectorySampleIntervalMs,
                    _turnPositions, out var terminal)) return false;
            _cachedTurnState = state;
            _cachedTurnTerminal = terminal;
        }
        foreach (var point in _turnPositions)
            points.Add(new(point.X, point.Y));
        return true;
    }

    private void ProjectDiscrete(ObjectMotionSnapshot predictedState, List<FutureTrajectoryPoint> points)
    {
        for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
        {
            var projected = _predictor.Predict(predictedState, t);
            points.Add(new FutureTrajectoryPoint(projected.X, projected.Y));
        }

    }

}

/// <summary>
/// A single point in the future trajectory, in world coordinates.
/// </summary>
internal readonly record struct FutureTrajectoryPoint(double X, double Y);
