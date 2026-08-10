using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Projects the future trajectory of an active navigation cycle
/// (engine.navigate-to-point) using EXACTLY the same deterministic math as the
/// engine: <see cref="NavigationWaypointMath.Step"/> for the turn decision and the
/// same straight-line advance formula as <see cref="LinearMotionPredictor"/>.
/// The cycle phase comes from the authoritative snapshot (TurnStepRemainingMs,
/// TurnStepIntervalMs, NavigationAngularInertiaDegPerSec), so the client-side line
/// matches the authoritative discrete motion (AC10).
/// Pure client-side — never touches the Engine.
/// </summary>
internal sealed class NavigationTrajectoryProjector
{
    /// <summary>Same horizon as the future trajectory — never longer than the engine can fly.</summary>
    public const int FutureTrajectoryHorizonMs = FutureTrajectoryProjector.FutureTrajectoryHorizonMs;

    /// <summary>
    /// Compute future world-coordinate trajectory points for an active navigation
    /// cycle, starting from the current predicted state. Empty when the snapshot does
    /// not carry an authoritative navigation target (no active navigate cycle).
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predicted)
    {
        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);

        if (predicted.NavigationTargetX is not { } targetX ||
            predicted.NavigationTargetY is not { } targetY)
        {
            return points;
        }

        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;

        // Defensive clamps mirror the engine's invariants: TurnStepDegrees is |module|
        // for navigation cycles, the interval is MinTurnIntervalMs (> 0).
        int turnStepDegrees = Math.Max(1, predicted.TurnStepDegrees);
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long phaseMs = Math.Max(1, predicted.TurnStepRemainingMs);

        points.Add(new FutureTrajectoryPoint(x, y));

        // Phase until the first cycle step: straight flight with the CURRENT course
        // (the engine rolls the motion forward to the completion time before deciding
        // the turn).
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, phaseMs);
        points.Add(new FutureTrajectoryPoint(x, y));

        long elapsedMs = phaseMs;
        while (elapsedMs < FutureTrajectoryHorizonMs)
        {
            var step = NavigationWaypointMath.Step(
                x,
                y,
                direction,
                speedKmS,
                targetX,
                targetY,
                turnStepDegrees,
                predicted.NavigationAngularInertiaDegPerSec);

            if (step.IsArrived)
                break;

            direction = NormalizeDirection(direction + step.TurnDeltaDegrees);
            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));

            elapsedMs += intervalMs;
        }

        return points;
    }

    private static (double X, double Y) AdvanceStraight(
        double x, double y, double directionDegrees, double speedKmS, long elapsedMs)
    {
        // Mirror of LinearMotionPredictor.AdvanceStraight (1 km/s = 10 world units/s).
        double distance = speedKmS * (elapsedMs / 1000.0) * 10.0;
        double angleRad = directionDegrees * Math.PI / 180.0;
        return (x + distance * Math.Sin(angleRad), y - distance * Math.Cos(angleRad));
    }

    private static double NormalizeDirection(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
