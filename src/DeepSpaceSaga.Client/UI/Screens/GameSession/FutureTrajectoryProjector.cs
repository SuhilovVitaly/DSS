using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Computes future trajectory points from the current predicted state.
/// For continuous-turn commands uses PredictContinuousTurn (circular motion
/// from DeepSpaceSaga.Motion); otherwise uses the discrete-step Predict().
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class FutureTrajectoryProjector
{
    public const int FutureTrajectoryHorizonMs = 20_000;
    public const int FutureTrajectorySampleIntervalMs = 250;

    internal static readonly int MaxSamplePoints = FutureTrajectoryHorizonMs / FutureTrajectorySampleIntervalMs + 1;

    private readonly IMotionPredictor _predictor;
    private readonly LinearMotionPredictor _linearPredictor;

    public FutureTrajectoryProjector(IMotionPredictor predictor)
    {
        _predictor = predictor;
        _linearPredictor = (LinearMotionPredictor)predictor;
    }

    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState)
    {
        var points = new List<FutureTrajectoryPoint>(MaxSamplePoints);

        bool continuous = IsContinuousTurnCommand(predictedState.ActiveEngineCommandType)
            && predictedState.TurnStepIntervalMs > 0
            && predictedState.SpeedKmS > 0;

        for (long t = 0; t <= FutureTrajectoryHorizonMs; t += FutureTrajectorySampleIntervalMs)
        {
            var projected = continuous
                ? _linearPredictor.PredictContinuousTurn(predictedState, t)
                : _predictor.Predict(predictedState, t);
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

    private static bool IsContinuousTurnCommand(string? cmd)
        => cmd == ShipEngineCommandTypes.TurnLeftUntilCancel
        || cmd == ShipEngineCommandTypes.TurnRightUntilCancel;
}

internal readonly record struct FutureTrajectoryPoint(double X, double Y);
