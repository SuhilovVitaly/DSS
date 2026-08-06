using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// DSS-correct motion prediction.
/// Speed: km/s. Direction: degrees, 0° = up, clockwise.
/// 1 km/s = 10 world units/s (since 1 unit = 100 m).
/// </summary>
public sealed class LinearMotionPredictor : IMotionPredictor
{
    private const double UnitsPerKmS = 10.0;

    /// <summary>
    /// Predict using the authoritative discrete turn-step model (matches Engine).
    /// </summary>
    public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        if (state.TurnStepDegrees == 0 || state.TurnStepIntervalMs <= 0)
            return PredictStraight(state, elapsedMs, state.Direction);
        if (elapsedMs < 0)
            return PredictBackwardTurnSteps(state, elapsedMs);
        if (elapsedMs == 0)
            return state;

        long remainingMs = elapsedMs;
        long untilNextTurnMs = state.TurnStepRemainingMs;
        double x = state.X;
        double y = state.Y;
        double direction = state.Direction;

        while (remainingMs > 0)
        {
            long segmentMs = Math.Min(remainingMs, untilNextTurnMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, segmentMs);
            remainingMs -= segmentMs;

            if (segmentMs == untilNextTurnMs)
            {
                direction = NormalizeDirection(direction + state.TurnStepDegrees);
                untilNextTurnMs = state.TurnStepIntervalMs;
            }
        }

        return state with { X = x, Y = y, Direction = direction };
    }

    /// <summary>
    /// Predict using analytical circular motion for continuous-turn commands.
    /// Ignores TurnStepRemainingMs — the trajectory is a perfect circle.
    /// Available for visual prediction; Engine authoritative simulation uses discrete steps.
    /// </summary>
    public ObjectMotionSnapshot PredictContinuousTurn(ObjectMotionSnapshot state, long elapsedMs)
    {
        if (elapsedMs == 0)
            return state;

        double v = state.SpeedKmS * UnitsPerKmS;
        double turnRateRadPerS = (double)state.TurnStepDegrees / state.TurnStepIntervalMs
            * 1000.0 * Math.PI / 180.0;
        double initDirRad = state.Direction * Math.PI / 180.0;
        double t = elapsedMs / 1000.0;

        double newDir = NormalizeDirection(
            state.Direction + state.TurnStepDegrees * (elapsedMs / (double)state.TurnStepIntervalMs));

        if (Math.Abs(turnRateRadPerS) < 1e-9)
        {
            double xs = state.X + v * t * Math.Sin(initDirRad);
            double ys = state.Y - v * t * Math.Cos(initDirRad);
            return state with { X = xs, Y = ys, Direction = newDir };
        }

        double dirRad = initDirRad + turnRateRadPerS * t;
        double r = v / turnRateRadPerS;
        double xc = state.X + r * (Math.Cos(initDirRad) - Math.Cos(dirRad));
        double yc = state.Y + r * (Math.Sin(initDirRad) - Math.Sin(dirRad));

        return state with { X = xc, Y = yc, Direction = newDir };
    }

    // ── Discrete turn-step helpers ───────────────────────────────

    private static ObjectMotionSnapshot PredictBackwardTurnSteps(ObjectMotionSnapshot state, long elapsedMs)
    {
        long remainingMs = -elapsedMs;
        long intervalMs = state.TurnStepIntervalMs;
        long currentCycleElapsedMs = intervalMs - Math.Clamp(state.TurnStepRemainingMs, 0, intervalMs);
        double x = state.X;
        double y = state.Y;
        double direction = state.Direction;

        if (currentCycleElapsedMs > 0)
        {
            long segmentMs = Math.Min(remainingMs, currentCycleElapsedMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, -segmentMs);
            remainingMs -= segmentMs;
        }

        while (remainingMs > 0)
        {
            direction = NormalizeDirection(direction - state.TurnStepDegrees);
            long segmentMs = Math.Min(remainingMs, intervalMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, -segmentMs);
            remainingMs -= segmentMs;
        }

        return state with { X = x, Y = y, Direction = direction };
    }

    private static ObjectMotionSnapshot PredictStraight(ObjectMotionSnapshot state, long elapsedMs, double direction)
    {
        double x = state.X;
        double y = state.Y;
        AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, elapsedMs);
        return state with { X = x, Y = y };
    }

    private static void AdvanceStraight(ref double x, ref double y, double speedKmS, double direction, long elapsedMs)
    {
        double distance = speedKmS * (elapsedMs / 1000.0) * UnitsPerKmS;
        double angleRad = direction * Math.PI / 180.0;
        x += distance * Math.Sin(angleRad);
        y -= distance * Math.Cos(angleRad);
    }

    private static double NormalizeDirection(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
