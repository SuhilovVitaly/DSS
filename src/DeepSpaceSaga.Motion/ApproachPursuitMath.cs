namespace DeepSpaceSaga.Motion;

/// <summary>
/// Result of one `navigation.approach` pursuit step: the freshly recomputed aim point
/// (trailing behind the target along its current heading), whether the ship has reached
/// it, the ship's new direction after this step's clamped turn, and the course lock (if
/// any) to pass back into the next call.
/// </summary>
/// <param name="AimPointX">Recomputed aim point X, world units.</param>
/// <param name="AimPointY">Recomputed aim point Y, world units.</param>
/// <param name="IsArrived">Ship reached (or swept through) the aim point this step.</param>
/// <param name="NewDirectionDegrees">Ship direction after this step's clamped turn.</param>
/// <param name="LockedCourseDegrees">
/// When non-null the ship has aligned onto a straight-line course toward the aim point
/// and this course should be held (not re-derived from possibly-noisy geometry) on the
/// next call — pass it back as <c>lockedCourseDegrees</c>. Unlike
/// <see cref="NavigationWaypointMath"/>'s Orbit lock, this is NOT permanent: the caller
/// re-passes the value each call, and <see cref="Step"/> itself drops (re-derives) the
/// lock as soon as the freshly recomputed bearing to the (possibly-moved) aim point
/// drifts meaningfully away from it — because for Approach the aim point can genuinely
/// keep moving as the target moves. Null once arrived or while not yet aligned.
/// </param>
public readonly record struct ApproachStepResult(
    double AimPointX,
    double AimPointY,
    bool IsArrived,
    double NewDirectionDegrees,
    double? LockedCourseDegrees = null);

/// <summary>
/// Pure, deterministic trailing-pursuit steering math for `navigation.approach`
/// (shared by Engine and Client — no state, no Engine/Contracts references, only
/// numbers). Unlike <see cref="NavigationWaypointMath.StagedStep"/> (Orbit), this
/// never locks a PERMANENT course: the aim point itself moves as the target moves,
/// so the target's freshly-passed-in current position/direction is always re-read.
///
/// Model: aimPoint = targetPosition − trailDistanceWorldUnits × unitVector(targetDirection).
/// The ship steers toward the aim point using the same turn-clamp convention as
/// <see cref="NavigationWaypointMath"/> (shortest signed angle, clamped to
/// turnStepDegrees per step). Arrival uses a closest-point-on-the-travelled-segment
/// test (mirroring <see cref="NavigationWaypointMath.CheckSegmentArrival"/>) rather
/// than a single end-of-step point sample, so a fast ship cannot tunnel through a
/// small tolerance ring within one step.
///
/// Anti-circling stabilization (Post-implementation bug fix #2, story-20260827-083137.md):
/// once the ship's heading is within tolerance of the bearing to the aim point, <see
/// cref="Step"/> holds that heading (via the caller-threaded, cycle-scoped
/// <c>lockedCourseDegrees</c> parameter) instead of re-deriving a slightly different
/// bearing from tiny geometric noise every call — the same stabilization
/// <see cref="NavigationWaypointMath.HoldLockedCourse"/> already uses for Orbit,
/// including its dot-product "aim point fallen behind the ship" arrival safeguard.
/// The crucial difference from Orbit: this lock is NOT permanent — <see cref="Step"/>
/// itself drops and re-derives it as soon as the live aim point drifts meaningfully
/// away from the held course, since (unlike Orbit's fixed point) the aim point can
/// genuinely keep moving as the target moves.
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
    /// <param name="lockedCourseDegrees">
    /// The course locked on a previous call (see <see cref="ApproachStepResult.LockedCourseDegrees"/>),
    /// or null if not yet aligned/locked. Cycle-scoped, NOT permanent — pass back
    /// exactly what the previous call returned; this method itself decides whether to
    /// keep holding it, drop it (aim point moved meaningfully), or newly acquire it.
    /// </param>
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
        long stepTimeMs,
        double? lockedCourseDegrees = null)
    {
        var (aimX, aimY) = ComputeAimPoint(targetX, targetY, targetDirectionDegrees, trailDistanceWorldUnits);

        double dx = aimX - shipX;
        double dy = aimY - shipY;
        double distanceToAim = Math.Sqrt(dx * dx + dy * dy);

        double newDirection = shipDirectionDegrees;
        double? newLockedCourse = lockedCourseDegrees;
        bool arrivedBehindShip = false;

        if (distanceToAim > ArrivalToleranceUnits && angularInertiaDegPerSec > 0 && turnStepDegrees > 0)
        {
            if (newLockedCourse is { } lockedCourse)
            {
                // Holding an existing lock: steer toward the locked heading rather than
                // a freshly recomputed bearing — mirrors
                // NavigationWaypointMath.HoldLockedCourse and is what prevents the
                // pure-pursuit circling this fix addresses.
                double lockDelta = ShortestSignedAngleDegrees(shipDirectionDegrees, lockedCourse);
                double lockTurnDelta = Math.Abs(lockDelta) <= turnStepDegrees
                    ? lockDelta
                    : Math.Sign(lockDelta) * turnStepDegrees;
                newDirection = NormalizeDegrees(shipDirectionDegrees + lockTurnDelta);

                if (Math.Abs(lockDelta) <= turnStepDegrees / 2.0)
                {
                    // Behind-the-ship arrival safeguard FIRST (mirrors
                    // NavigationWaypointMath's dot ≤ 0 check): once aligned with the
                    // locked course, if the aim point has fallen behind the ship's new
                    // heading, treat this as arrived — otherwise the ship endlessly
                    // re-chases a point it has already flown past. This must be checked
                    // BEFORE any bearing-drift staleness comparison below, because flying
                    // past a point naturally swings the raw bearing to it by a huge
                    // amount (it is now behind, not just "moved slightly") — that swing
                    // must resolve as arrival, not as a false "target moved, drop lock".
                    double dirRad = newDirection * Math.PI / 180.0;
                    double dot = dx * Math.Sin(dirRad) - dy * Math.Cos(dirRad);
                    if (dot <= 0)
                    {
                        arrivedBehindShip = true;
                    }
                    else
                    {
                        // Still ahead: the lock is only kept while the freshly
                        // recomputed bearing is still close to it (within one turn
                        // step) — beyond that, the aim point has moved enough (target
                        // genuinely moving) that the lock is stale and must be dropped
                        // so the bearing is re-derived fresh, in this SAME call. This is
                        // what keeps the lock cycle-scoped rather than permanent.
                        double bearingNow = BearingDegrees(dx, dy);
                        if (Math.Abs(ShortestSignedAngleDegrees(lockedCourse, bearingNow)) > turnStepDegrees)
                        {
                            newLockedCourse = null;
                            double delta = ShortestSignedAngleDegrees(shipDirectionDegrees, bearingNow);
                            double turnDelta = Math.Abs(delta) <= turnStepDegrees
                                ? delta
                                : Math.Sign(delta) * turnStepDegrees;
                            newDirection = NormalizeDegrees(shipDirectionDegrees + turnDelta);
                            if (Math.Abs(delta) <= turnStepDegrees / 2.0)
                                newLockedCourse = bearingNow;
                        }
                    }
                }
            }
            else
            {
                double bearing = BearingDegrees(dx, dy);
                double delta = ShortestSignedAngleDegrees(shipDirectionDegrees, bearing);
                double turnDelta = Math.Abs(delta) <= turnStepDegrees
                    ? delta
                    : Math.Sign(delta) * turnStepDegrees;
                newDirection = NormalizeDegrees(shipDirectionDegrees + turnDelta);

                // Newly aligned this step — lock the bearing as the course to hold,
                // exactly the anti-circling stabilization NavigationWaypointMath
                // already uses for Orbit (see this class's doc-comment).
                if (Math.Abs(delta) <= turnStepDegrees / 2.0)
                    newLockedCourse = bearing;
            }
        }

        double stepDistance = shipSpeedKmS * (stepTimeMs / 1000.0) * UnitsPerKmS;
        double angleRad = newDirection * Math.PI / 180.0;
        double endX = shipX + stepDistance * Math.Sin(angleRad);
        double endY = shipY - stepDistance * Math.Cos(angleRad);

        bool arrived = arrivedBehindShip
            || distanceToAim <= ArrivalToleranceUnits
            || ClosestDistanceOnSegment(shipX, shipY, endX, endY, aimX, aimY) <= ArrivalToleranceUnits;

        return new ApproachStepResult(aimX, aimY, arrived, newDirection, arrived ? null : newLockedCourse);
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
