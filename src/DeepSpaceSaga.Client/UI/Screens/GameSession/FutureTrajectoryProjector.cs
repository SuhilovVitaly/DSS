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
    /// Always uses the discrete-step predictor to exactly match the authoritative
    /// simulation — the TurnStepRemainingMs fix ensures accuracy without jitter.
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState)
    {
        return ProjectDiscrete(predictedState);
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

    private List<FutureTrajectoryPoint> ProjectDiscrete(ObjectMotionSnapshot predictedState)
    {
        var points = new List<FutureTrajectoryPoint>(MaxSamplePoints);

        for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
        {
            var projected = _predictor.Predict(predictedState, t);
            points.Add(new FutureTrajectoryPoint(projected.X, projected.Y));
        }

        return points;
    }

}

/// <summary>
/// A single point in the future trajectory, in world coordinates.
/// </summary>
internal readonly record struct FutureTrajectoryPoint(double X, double Y);
