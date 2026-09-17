using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Computes future trajectory points from the current predicted state
/// using the discrete-step predictor to exactly match the authoritative simulation.
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class FutureTrajectoryProjector
{
    public const int FutureTrajectoryHorizonMs = 200_000;
    public const int FutureTrajectorySampleIntervalMs = 250;

    /// <summary>Maximum number of sample points per object (inclusive of t=0).</summary>
    internal static readonly int MaxSamplePoints = FutureTrajectoryHorizonMs / FutureTrajectorySampleIntervalMs + 1;

    private readonly IMotionPredictor _predictor;

    public FutureTrajectoryProjector(IMotionPredictor predictor)
    {
        _predictor = predictor;
    }

    /// <summary>
    /// Compute future world-coordinate trajectory points from the predicted state.
    /// Uses the shared straight-line formula or discrete-step predictor as appropriate
    /// to match the authoritative simulation and preserve turn-cycle phase.
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState)
    {
        var points = new List<FutureTrajectoryPoint>(MaxSamplePoints);
        ProjectInto(predictedState, points);
        return points;
    }

    /// <summary>Fill caller-owned frame scratch space; immutable snapshots are never mutated.</summary>
    public void ProjectInto(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points)
    {
        points.Clear();
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
        ProjectDiscrete(state, points);
    }

    /// <summary>
    /// Player and target displays extend straight flight to the viewport edge instead of stopping
    /// after 200 seconds. Curved/closed motion retains the predictor's actual geometry.
    /// Time-based callers keep ProjectInto's bounded horizon.
    /// </summary>
    internal void ProjectViewportInto(ObjectMotionSnapshot state, List<FutureTrajectoryPoint> points,
        CameraState camera, int width, int height)
    {
        if (_predictor is LinearMotionPredictor && LinearMotionPredictor.IsLinear(state))
        {
            points.Clear();
            points.Add(new(state.X, state.Y));
            if (state.SpeedKmS > 0) TrajectoryViewportGeometry.ExtendToEdge(points, state.Direction, camera, width, height);
            return;
        }
        ProjectInto(state, points);
        var lastState = _predictor.Predict(state, FutureTrajectoryHorizonMs);
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
