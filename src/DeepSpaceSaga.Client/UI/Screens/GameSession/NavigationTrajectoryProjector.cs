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

        // Phase until the first cycle step: straight flight with the CURRENT course.
        double phaseStartX = x, phaseStartY = y;
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, phaseMs);

        // Check segment arrival after phase flight.
        var phaseArrival = NavigationWaypointMath.CheckSegmentArrival(
            phaseStartX, phaseStartY, x, y, targetX, targetY);
        if (phaseArrival.IsArrived)
        {
            points.Add(new FutureTrajectoryPoint(phaseArrival.ClosestX, phaseArrival.ClosestY));
            return points;
        }

        points.Add(new FutureTrajectoryPoint(x, y));

        long elapsedMs = phaseMs;
        double? lockedCourse = predicted.NavigationLockedCourseDegrees;
        string? navigationPhase = predicted.NavigationPhase;
        double? escapeCourse = predicted.NavigationEscapeCourseDegrees;
        double? requiredDepartureDistance = predicted.NavigationRequiredDepartureDistance;
        while (elapsedMs < FutureTrajectoryHorizonMs)
        {
            double stepStartX = x, stepStartY = y;

            var step = NavigationWaypointMath.StagedStep(
                x,
                y,
                direction,
                speedKmS,
                targetX,
                targetY,
                turnStepDegrees,
                predicted.NavigationAngularInertiaDegPerSec,
                stepTimeMs: intervalMs,
                phase: navigationPhase,
                lockedCourseDegrees: lockedCourse,
                escapeCourseDegrees: escapeCourse,
                requiredDepartureDistance: requiredDepartureDistance);

            lockedCourse = step.LockedCourseDegrees ?? lockedCourse;
            navigationPhase = step.NextNavigationPhase ?? navigationPhase;
            escapeCourse = step.EscapeCourseDegrees ?? escapeCourse;
            requiredDepartureDistance = step.RequiredDepartureDistance ?? requiredDepartureDistance;
            if (step.NextNavigationPhase == "Approach")
            {
                lockedCourse = null;
            }

            direction = NormalizeDirection(direction + step.TurnDeltaDegrees);

            if (step.IsArrived)
            {
                // Snap final point at the closest approach to the target on this segment.
                (double cx, double cy) = ClosestApproach(x, y, direction, speedKmS, intervalMs, targetX, targetY);
                points.Add(new FutureTrajectoryPoint(cx, cy));
                break;
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));

            // Check segment arrival after this advance.
            var segArrival = NavigationWaypointMath.CheckSegmentArrival(
                stepStartX, stepStartY, x, y, targetX, targetY);
            if (segArrival.IsArrived)
            {
                points[^1] = new FutureTrajectoryPoint(segArrival.ClosestX, segArrival.ClosestY);
                break;
            }

            elapsedMs += intervalMs;
        }

        return points;
    }

    private static (double X, double Y) ClosestApproach(
        double x, double y, double directionDegrees, double speedKmS, long intervalMs,
        double targetX, double targetY)
    {
        double stepDist = speedKmS * (intervalMs / 1000.0) * 10.0;
        double angleRad = directionDegrees * Math.PI / 180.0;
        double segDx = stepDist * Math.Sin(angleRad);
        double segDy = -stepDist * Math.Cos(angleRad);
        double tDx = targetX - x;
        double tDy = targetY - y;
        double dot = tDx * segDx + tDy * segDy;
        double lenSq = segDx * segDx + segDy * segDy;
        double t = lenSq > 0 ? Math.Clamp(dot / lenSq, 0.0, 1.0) : 0.0;
        return (x + t * segDx, y + t * segDy);
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
