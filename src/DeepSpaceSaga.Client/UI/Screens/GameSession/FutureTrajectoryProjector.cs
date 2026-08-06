using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Computes future trajectory points from the current predicted state
/// using deterministic motion math.
/// For continuous-turn commands (TurnLeftUntilCancel / TurnRightUntilCancel)
/// uses analytical circular motion to avoid jitter from shifting TurnStepRemainingMs.
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class FutureTrajectoryProjector
{
    public const int FutureTrajectoryHorizonMs = 200_000;
    public const int FutureTrajectorySampleIntervalMs = 250;

    /// <summary>Maximum number of sample points per object (inclusive of t=0).</summary>
    internal static readonly int MaxSamplePoints = FutureTrajectoryHorizonMs / FutureTrajectorySampleIntervalMs + 1;

    /// <summary>1 km/s = 10 world units/s (since 1 unit = 100 m).</summary>
    private const double UnitsPerKmS = 10.0;

    private readonly IMotionPredictor _predictor;

    public FutureTrajectoryProjector(IMotionPredictor predictor)
    {
        _predictor = predictor;
    }

    /// <summary>
    /// Compute future world-coordinate trajectory points from the predicted state.
    /// For continuous-turn commands uses analytical circular motion;
    /// otherwise delegates to the discrete-step predictor.
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predictedState)
    {
        if (IsContinuousTurnCommand(predictedState.ActiveEngineCommandType)
            && predictedState.TurnStepIntervalMs > 0
            && predictedState.SpeedKmS > 0)
        {
            return ProjectCircularMotion(predictedState);
        }

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

    // ── Discrete-step projection (non-continuous turn, or stationary) ──

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

    // ── Continuous circular motion ─────────────────────────────────

    /// <summary>
    /// Project trajectory using analytical circular motion.
    /// Constant speed v and constant angular velocity ω produce a circular arc.
    /// This avoids jitter because TurnStepRemainingMs phase shifts are ignored.
    ///
    /// Formulas (derived from dx/dt = v*sin(dir), dy/dt = -v*cos(dir), dir = dir0 + ω*t):
    ///   x(t) = x0 + (v/ω) * (cos(dir0) − cos(dir0 + ω*t))
    ///   y(t) = y0 + (v/ω) * (sin(dir0) − sin(dir0 + ω*t))
    ///
    /// When |ω| ≈ 0 (straight line), falls back to linear motion via small-angle limit.
    ///
    /// Horizon is extended so the trajectory always closes at least one full circle:
    ///   period = 360° / |TurnStepDegrees| * TurnStepIntervalMs
    ///   horizon = max(200 s, period)
    /// </summary>
    private static List<FutureTrajectoryPoint> ProjectCircularMotion(ObjectMotionSnapshot state)
    {
        double absTurnStepDegrees = Math.Abs((double)state.TurnStepDegrees);
        long circlePeriodMs = (long)(360.0 / absTurnStepDegrees * state.TurnStepIntervalMs);
        long horizonMs = Math.Max(FutureTrajectoryHorizonMs, circlePeriodMs);
        int sampleCount = (int)(horizonMs / FutureTrajectorySampleIntervalMs) + 1;

        var points = new List<FutureTrajectoryPoint>(sampleCount);

        double v = state.SpeedKmS * UnitsPerKmS;               // world units / s
        double turnRateRadPerS = (double)state.TurnStepDegrees / state.TurnStepIntervalMs * 1000.0 * Math.PI / 180.0; // rad / s
        double initDirRad = state.Direction * Math.PI / 180.0;  // initial direction in radians

        for (long tMs = 0; tMs <= horizonMs; tMs += FutureTrajectorySampleIntervalMs)
        {
            double t = tMs / 1000.0; // seconds

            double x, y;
            if (Math.Abs(turnRateRadPerS) < 1e-9)
            {
                // Straight-line limit: v/ω → v·t when ω → 0
                x = state.X + v * t * Math.Sin(initDirRad);
                y = state.Y - v * t * Math.Cos(initDirRad);
            }
            else
            {
                double dirRad = initDirRad + turnRateRadPerS * t;
                double r = v / turnRateRadPerS; // signed radius (negative ω → clockwise arc reversed)
                x = state.X + r * (Math.Cos(initDirRad) - Math.Cos(dirRad));
                y = state.Y + r * (Math.Sin(initDirRad) - Math.Sin(dirRad));
            }

            points.Add(new FutureTrajectoryPoint(x, y));
        }

        return points;
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static bool IsContinuousTurnCommand(string? activeCommandType)
    {
        return activeCommandType == ShipEngineCommandTypes.TurnLeftUntilCancel
            || activeCommandType == ShipEngineCommandTypes.TurnRightUntilCancel;
    }
}

/// <summary>
/// A single point in the future trajectory, in world coordinates.
/// </summary>
internal readonly record struct FutureTrajectoryPoint(double X, double Y);
