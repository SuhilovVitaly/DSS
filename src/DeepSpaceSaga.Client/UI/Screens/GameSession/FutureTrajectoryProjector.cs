using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Computes future trajectory points from the current predicted state
/// using deterministic motion math (IMotionPredictor).
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class FutureTrajectoryProjector
{
    public const int FutureTrajectoryHorizonMs = 20_000;
    public const int FutureTrajectorySampleIntervalMs = 250;

    internal static readonly int MaxSamplePoints = FutureTrajectoryHorizonMs / FutureTrajectorySampleIntervalMs + 1;

    private readonly IMotionPredictor _predictor;

    public FutureTrajectoryProjector(IMotionPredictor predictor)
    {
        _predictor = predictor;
    }

    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState)
    {
        var points = new List<FutureTrajectoryPoint>(MaxSamplePoints);

        for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
        {
            var projected = _predictor.Predict(predictedState, t);
            points.Add(new FutureTrajectoryPoint(projected.X, projected.Y));
        }

        return points;
    }

    public static bool ShouldDraw(ObjectMotionSnapshot state)
    {
        if (state.SpeedKmS > 0)
            return true;

        if (state.ActiveEngineCommandType is not null)
            return true;

        return false;
    }
}

internal readonly record struct FutureTrajectoryPoint(double X, double Y);
