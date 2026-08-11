using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// DSS-correct linear motion prediction.
/// Speed: km/s. Direction: degrees, 0° = up, clockwise.
/// 1 km/s = 10 world units/s (since 1 unit = 100 m).
///
/// For active NavigateToPoint cycles the predictor delegates turn decisions to
/// <see cref="NavigationWaypointMath.Step"/> so that client-side live prediction
/// matches the authoritative engine behaviour — including course-locking after
/// the ship aligns to the target bearing. Without this, the generic "turn every
/// interval" logic would keep rotating the ship between snapshots, producing the
/// visible "deviate → snap back" jitter.
/// </summary>
public sealed class LinearMotionPredictor : IMotionPredictor
{
    private const double UnitsPerKmS = 10.0; // 1 km/s → 10 world units/s

    public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        // Navigation cycle with locked or lockable course — use shared step math.
        if (state.ActiveEngineCommandType == ShipEngineCommandTypes.NavigateToPoint &&
            state.NavigationTargetX is { } targetX &&
            state.NavigationTargetY is { } targetY &&
            state.NavigationAngularInertiaDegPerSec > 0)
        {
            return PredictNavigation(state, elapsedMs, targetX, targetY);
        }

        // Generic turn-step prediction (until-cancel turns, step turns).
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
            untilNextTurnMs -= segmentMs;

            if (untilNextTurnMs == 0)
            {
                direction = NormalizeDirection(direction + state.TurnStepDegrees);
                untilNextTurnMs = state.TurnStepIntervalMs;
            }
        }

        return state with { X = x, Y = y, Direction = direction, TurnStepRemainingMs = untilNextTurnMs };
    }

    private static ObjectMotionSnapshot PredictNavigation(
        ObjectMotionSnapshot state, long elapsedMs, double targetX, double targetY)
    {
        if (elapsedMs <= 0)
            return state;

        int turnStep = Math.Abs(state.TurnStepDegrees);
        if (turnStep == 0 || state.TurnStepIntervalMs <= 0)
            return PredictStraight(state, elapsedMs, state.Direction);

        long intervalMs = state.TurnStepIntervalMs;
        long remainingMs = elapsedMs;
        long untilNextTurnMs = state.TurnStepRemainingMs;
        double x = state.X;
        double y = state.Y;
        double direction = state.Direction;
        double? lockedCourse = state.NavigationLockedCourseDegrees;

        // Phase: fly straight until the first turn interval boundary.
        if (untilNextTurnMs > 0)
        {
            long segmentMs = Math.Min(remainingMs, untilNextTurnMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, segmentMs);
            remainingMs -= segmentMs;
            untilNextTurnMs -= segmentMs;
        }

        // Step loop: each full interval, decide turn via shared navigation math.
        while (remainingMs >= intervalMs)
        {
            var step = NavigationWaypointMath.Step(
                x, y, direction, state.SpeedKmS,
                targetX, targetY,
                turnStep,
                state.NavigationAngularInertiaDegPerSec,
                stepTimeMs: intervalMs,
                lockedCourseDegrees: lockedCourse);

            // Apply turn first (even if arrived — Engine does the same: P2 fix).
            direction = NormalizeDirection(direction + step.TurnDeltaDegrees);
            lockedCourse = step.LockedCourseDegrees;

            if (step.IsArrived)
            {
                // Engine will cancel the cycle next snapshot — fly remaining time straight.
                AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, remainingMs);
                remainingMs = 0;
                untilNextTurnMs = intervalMs;
                break;
            }

            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, intervalMs);
            remainingMs -= intervalMs;
            untilNextTurnMs = intervalMs;
        }

        // Partial remainder (< intervalMs): fly straight, phase carries forward.
        if (remainingMs > 0)
        {
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, remainingMs);
            untilNextTurnMs -= remainingMs;
            remainingMs = 0;
        }

        return state with
        {
            X = x,
            Y = y,
            Direction = direction,
            TurnStepRemainingMs = untilNextTurnMs, // real phase — P1 fix
            NavigationLockedCourseDegrees = lockedCourse,
        };
    }

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
