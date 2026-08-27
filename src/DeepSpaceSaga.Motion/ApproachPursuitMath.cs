namespace DeepSpaceSaga.Motion;

/// <summary>
/// Result of one `navigation.approach` pursuit step: the freshly recomputed aim point
/// (trailing behind the target along its current heading), whether the ship has reached
/// it, and the ship's new direction after this step's clamped turn.
/// </summary>
/// <param name="AimPointX">Recomputed aim point X, world units.</param>
/// <param name="AimPointY">Recomputed aim point Y, world units.</param>
/// <param name="IsArrived">Ship reached (or swept through) the aim point this step.</param>
/// <param name="NewDirectionDegrees">Ship direction after this step's clamped turn.</param>
public readonly record struct ApproachStepResult(
    double AimPointX,
    double AimPointY,
    bool IsArrived,
    double NewDirectionDegrees);

/// <summary>
/// Pure, deterministic trailing-pursuit steering math for `navigation.approach`
/// (shared by Engine and Client — no state, no Engine/Contracts references, only
/// numbers). Unlike <see cref="NavigationWaypointMath.StagedStep"/> (Orbit), this
/// never locks a permanent course: every call re-aims from scratch using the
/// target's freshly-passed-in current position/direction, because the aim point
/// itself moves as the target moves.
///
/// Model: aimPoint = targetPosition − trailDistanceWorldUnits × unitVector(targetDirection).
/// The ship steers toward the aim point using the same turn-clamp convention as
/// <see cref="NavigationWaypointMath"/> (shortest signed angle, clamped to
/// turnStepDegrees per step). Arrival uses a closest-point-on-the-travelled-segment
/// test (mirroring <see cref="NavigationWaypointMath.CheckSegmentArrival"/>) rather
/// than a single end-of-step point sample, so a fast ship cannot tunnel through a
/// small tolerance ring within one step.
///
/// Direction convention: degrees, 0° = up, 90° = right, clockwise.
/// Speed convention: km/s (1 km/s = 10 world units/s, since 1 world unit = 100 m).
/// </summary>
public static class ApproachPursuitMath
{
    /// <summary>
    /// Default distance (world units) at or below which the ship is considered to have
    /// arrived at the aim point. This is a tuning default (500 m), not a hard
    /// requirement — callers may pass a different value if content ever needs to.
    /// </summary>
    public const double ArrivalToleranceUnits = 5.0;

    private const double UnitsPerKmS = 10.0; // 1 km/s → 10 world units/s.

    /// <summary>
    /// Compute the point trailing behind a moving (or stationary) target along its
    /// current heading. Deliberately takes no speed parameter — the target's
    /// direction alone defines the trailing offset, so a stationary target
    /// (speed ≈ 0, e.g. a Station) still yields a well-defined aim point using its
    /// Direction field.
    /// </summary>
    /// <param name="targetX">Target current X, world units.</param>
    /// <param name="targetY">Target current Y, world units.</param>
    /// <param name="targetDirectionDegrees">Target current heading, degrees.</param>
    /// <param name="trailDistanceWorldUnits">Distance to trail behind the target, world units.</param>
    public static (double X, double Y) ComputeAimPoint(
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double trailDistanceWorldUnits)
    {
        double angleRad = targetDirectionDegrees * Math.PI / 180.0;
        double forwardX = Math.Sin(angleRad);
        double forwardY = -Math.Cos(angleRad);

        return (
            targetX - trailDistanceWorldUnits * forwardX,
            targetY - trailDistanceWorldUnits * forwardY);
    }

    /// <summary>
    /// Constant-velocity position advance. Pure function shared by server and client so
    /// both can extrapolate a target's position identically between live re-reads.
    /// </summary>
    /// <param name="x">Current X, world units.</param>
    /// <param name="y">Current Y, world units.</param>
    /// <param name="directionDegrees">Heading, degrees.</param>
    /// <param name="speedKmS">Speed, km/s.</param>
    /// <param name="elapsedMs">Elapsed time, milliseconds.</param>
    public static (double X, double Y) ExtrapolatePosition(
        double x,
        double y,
        double directionDegrees,
        double speedKmS,
        long elapsedMs)
    {
        double distance = speedKmS * (elapsedMs / 1000.0) * UnitsPerKmS;
        double angleRad = directionDegrees * Math.PI / 180.0;

        return (
            x + distance * Math.Sin(angleRad),
            y - distance * Math.Cos(angleRad));
    }

    /// <summary>
    /// Compute one `navigation.approach` pursuit step. The target's state must always be
    /// passed in fresh (never cached by the caller across calls) — this function holds no
    /// internal state and never locks a permanent course, since the aim point itself
    /// moves as the target moves.
    /// </summary>
    /// <param name="shipX">Ship current X, world units.</param>
    /// <param name="shipY">Ship current Y, world units.</param>
    /// <param name="shipDirectionDegrees">Ship current heading, degrees.</param>
    /// <param name="shipSpeedKmS">Ship current speed, km/s — used to project this step's travelled segment for arrival detection.</param>
    /// <param name="targetX">Target current X, world units (fresh, live read).</param>
    /// <param name="targetY">Target current Y, world units (fresh, live read).</param>
    /// <param name="targetDirectionDegrees">Target current heading, degrees (fresh, live read).</param>
    /// <param name="targetSpeedKmS">
    /// Target current speed, km/s (fresh, live read). Not used by the aim-point
    /// geometry itself (which is direction-only, by design — see
    /// <see cref="ComputeAimPoint"/>); kept in the signature for parity with the ship's
    /// kinematic state and for callers that also need it (e.g. baking/extrapolation).
    /// </param>
    /// <param name="trailDistanceWorldUnits">Distance to trail behind the target, world units.</param>
    /// <param name="turnStepDegrees">Maximum turn per step, degrees (module turn-step limit).</param>
    /// <param name="angularInertiaDegPerSec">Angular inertia, degrees per second (0 = cannot turn).</param>
    /// <param name="stepTimeMs">This step's elapsed time, milliseconds — used to project the travelled segment.</param>
    public static ApproachStepResult Step(
        double shipX,
        double shipY,
        double shipDirectionDegrees,
        double shipSpeedKmS,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS,
        double trailDistanceWorldUnits,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs)
    {
        var (aimX, aimY) = ComputeAimPoint(targetX, targetY, targetDirectionDegrees, trailDistanceWorldUnits);

        double dx = aimX - shipX;
        double dy = aimY - shipY;
        double distanceToAim = Math.Sqrt(dx * dx + dy * dy);

        double newDirection = shipDirectionDegrees;
        if (distanceToAim > ArrivalToleranceUnits && angularInertiaDegPerSec > 0 && turnStepDegrees > 0)
        {
            double bearing = BearingDegrees(dx, dy);
            double delta = ShortestSignedAngleDegrees(shipDirectionDegrees, bearing);
            double turnDelta = Math.Abs(delta) <= turnStepDegrees
                ? delta
                : Math.Sign(delta) * turnStepDegrees;
            newDirection = NormalizeDegrees(shipDirectionDegrees + turnDelta);
        }

        double stepDistance = shipSpeedKmS * (stepTimeMs / 1000.0) * UnitsPerKmS;
        double angleRad = newDirection * Math.PI / 180.0;
        double endX = shipX + stepDistance * Math.Sin(angleRad);
        double endY = shipY - stepDistance * Math.Cos(angleRad);

        bool arrived = distanceToAim <= ArrivalToleranceUnits
            || ClosestDistanceOnSegment(shipX, shipY, endX, endY, aimX, aimY) <= ArrivalToleranceUnits;

        return new ApproachStepResult(aimX, aimY, arrived, newDirection);
    }

    /// <summary>
    /// Closest distance from <paramref name="pointX"/>/<paramref name="pointY"/> to the
    /// line segment from (startX, startY) to (endX, endY). Same closest-point-on-segment
    /// technique as <see cref="NavigationWaypointMath.CheckSegmentArrival"/>, used here to
    /// detect a fast ship sweeping through the arrival zone mid-step rather than only
    /// sampling the step's end position.
    /// </summary>
    private static double ClosestDistanceOnSegment(
        double startX, double startY,
        double endX, double endY,
        double pointX, double pointY)
    {
        double segDx = endX - startX;
        double segDy = endY - startY;
        double lenSq = segDx * segDx + segDy * segDy;

        if (lenSq <= 0)
        {
            double dx0 = pointX - startX;
            double dy0 = pointY - startY;
            return Math.Sqrt(dx0 * dx0 + dy0 * dy0);
        }

        double tDx = pointX - startX;
        double tDy = pointY - startY;
        double t = Math.Clamp((tDx * segDx + tDy * segDy) / lenSq, 0.0, 1.0);

        double closestX = startX + t * segDx;
        double closestY = startY + t * segDy;

        double dx1 = pointX - closestX;
        double dy1 = pointY - closestY;
        return Math.Sqrt(dx1 * dx1 + dy1 * dy1);
    }

    private static double BearingDegrees(double dx, double dy)
    {
        double degrees = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static double ShortestSignedAngleDegrees(double fromDegrees, double toDegrees)
    {
        double raw = (toDegrees - fromDegrees) % 360;
        if (raw > 180)
            raw -= 360;
        else if (raw <= -180)
            raw += 360;
        return raw;
    }

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
