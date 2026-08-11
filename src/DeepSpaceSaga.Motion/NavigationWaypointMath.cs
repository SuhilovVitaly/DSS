namespace DeepSpaceSaga.Motion;

/// <summary>
/// Result of one navigation waypoint step: the signed turn delta to apply (degrees,
/// 0 = fly straight), whether the ship has arrived, and the locked-on course (if any).
/// </summary>
/// <param name="TurnDeltaDegrees">Turn to apply this step (0 = fly straight).</param>
/// <param name="IsArrived">Ship has reached or flown past the target.</param>
/// <param name="LockedCourseDegrees">
/// When non-null the ship has locked onto a straight-line course through the target
/// and must hold this course until arrival. Pass this value back as
/// <c>lockedCourseDegrees</c> on the next call to prevent re-computing the bearing.
/// </param>
public readonly record struct NavigationStepResult(
    double TurnDeltaDegrees,
    bool IsArrived,
    double? LockedCourseDegrees = null);

/// <summary>
/// Pure, deterministic step math for navigation to a world point (shared by Engine and
/// Client). No state, no Engine/Contracts references — only numbers.
///
/// Model: once the ship's angular error is within tolerance, the current bearing is
/// locked as the final straight-line course. The ship then flies straight until
/// segment-based arrival or dot-behind arrival. This prevents the pure-pursuit
/// circling that occurs when the bearing is recomputed every step.
///
/// Direction convention: degrees, 0° = up, 90° = right, clockwise.
/// </summary>
public static class NavigationWaypointMath
{
    /// <summary>Distance (world units) at or below which the ship has arrived.</summary>
    public const double ArrivalEpsilon = 1.0;

    /// <summary>
    /// Check whether a straight-line segment from (startX, startY) to (endX, endY)
    /// passes within <see cref="ArrivalEpsilon"/> of the target. Used by callers
    /// after each straight advance to detect pass-through arrival before the next
    /// <see cref="Step"/> call — prevents re-steering toward an already-passed target.
    /// </summary>
    /// <returns>Tuple: IsArrived, ClosestX, ClosestY (the closest point on the segment to the target).</returns>
    public static (bool IsArrived, double ClosestX, double ClosestY) CheckSegmentArrival(
        double startX, double startY,
        double endX, double endY,
        double targetX, double targetY)
    {
        double segDx = endX - startX;
        double segDy = endY - startY;
        double lenSq = segDx * segDx + segDy * segDy;

        if (lenSq <= 0)
        {
            double d = Math.Sqrt((targetX - startX) * (targetX - startX) + (targetY - startY) * (targetY - startY));
            return d <= ArrivalEpsilon ? (true, startX, startY) : (false, 0, 0);
        }

        double tDx = targetX - startX;
        double tDy = targetY - startY;
        double t = Math.Clamp((tDx * segDx + tDy * segDy) / lenSq, 0.0, 1.0);

        double closestX = startX + t * segDx;
        double closestY = startY + t * segDy;
        double dist = Math.Sqrt(
            (targetX - closestX) * (targetX - closestX) +
            (targetY - closestY) * (targetY - closestY));

        return dist <= ArrivalEpsilon ? (true, closestX, closestY) : (false, 0, 0);
    }

    /// <summary>
    /// Evaluate whether a navigate-to-point target is safe (reachable without infinite
    /// circling). Shared by Engine (authoritative) and Client (precheck).
    /// </summary>
    /// <returns>
    /// True when the target is safe to navigate to; false when the target is too close
    /// and should be rejected with <c>navigation_target_too_close</c>.
    /// </returns>
    public static bool IsTargetSafe(
        double shipX, double shipY,
        double directionDegrees,
        double speedKmS,
        double targetX, double targetY,
        int angularInertiaDegPerSec)
    {
        double dx = targetX - shipX;
        double dy = targetY - shipY;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Already on top of the target — too close, would loop.
        if (distance <= ArrivalEpsilon)
            return false;

        // Stationary ship can rotate in place — only blocked if inside ArrivalEpsilon.
        if (speedKmS <= 0 || angularInertiaDegPerSec <= 0)
            return true;

        double turnRadiusUnits = speedKmS * 1000.0
            / (angularInertiaDegPerSec * Math.PI / 180.0) / 100.0;

        // Target is outside the turn radius — reachable with standard stepwise turning.
        if (distance >= turnRadiusUnits)
            return true;

        // Target is inside the turn radius. Only safe if it lies on the current
        // straight-line path (ahead + perpendicular miss distance ≤ ArrivalEpsilon).
        double bearingDegrees = BearingDegrees(dx, dy);
        double delta = ShortestSignedAngleDegrees(directionDegrees, bearingDegrees);
        double directionRad = directionDegrees * Math.PI / 180.0;

        // Target ahead (dot > 0)?
        bool ahead = dx * Math.Sin(directionRad) - dy * Math.Cos(directionRad) > 0;
        if (!ahead)
            return false;

        // Perpendicular distance from target to the ship's forward ray:
        // direction vector v = (sin θ, -cos θ), |v| = 1.
        // Perp distance = |dx × v| / |v| = |dx * v_y - dy * v_x| = |dx * (-cos θ) - dy * sin θ|.
        double perpDistance = Math.Abs(dx * Math.Cos(directionRad) + dy * Math.Sin(directionRad));

        return perpDistance <= ArrivalEpsilon;
    }

    /// <summary>
    /// Compute one navigation step from the ship's current state toward the target.
    /// </summary>
    /// <param name="lockedCourseDegrees">
    /// When non-null the ship already locked a straight-line course through the target
    /// — do not re-compute the bearing; just hold the lock and check arrival.
    /// The caller should pass the <see cref="NavigationStepResult.LockedCourseDegrees"/>
    /// from the previous step.
    /// </param>
    /// <param name="stepTimeMs">
    /// Step time in ms (e.g. 250). Used for segment-based arrival detection.
    /// </param>
    public static NavigationStepResult Step(
        double x,
        double y,
        double directionDegrees,
        double speedKmS,
        double targetX,
        double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs = 0,
        double? lockedCourseDegrees = null)
    {
        double dx = targetX - x;
        double dy = targetY - y;
        double r = Math.Sqrt(dx * dx + dy * dy);

        if (r <= ArrivalEpsilon)
            return new NavigationStepResult(0, IsArrived: true);

        // If we already locked a course, hold it and just check arrival.
        // (The locked-course path checks dot ≤ 0 internally — terminal pass-through.)
        if (lockedCourseDegrees is { } locked)
        {
            return HoldLockedCourse(
                x, y, directionDegrees, locked, speedKmS,
                targetX, targetY, turnStepDegrees, stepTimeMs);
        }

        // Target behind the ship while NOT locked onto a course: the ship has not yet
        // started navigating toward this target — don't mark arrival prematurely.
        // Fall through to bearing/delta computation and turn logic.

        double bearingDegrees = BearingDegrees(dx, dy);
        double delta = ShortestSignedAngleDegrees(directionDegrees, bearingDegrees);

        // On course (± half a turn step): lock this bearing as the final course.
        // The residual lock delta is returned as TurnDeltaDegrees so the Engine
        // actually applies the final small correction (P1 fix).
        if (Math.Abs(delta) <= turnStepDegrees / 2.0)
        {
            return HoldLockedCourse(
                x, y, directionDegrees, bearingDegrees, speedKmS,
                targetX, targetY, turnStepDegrees, stepTimeMs,
                isNewLock: true);
        }

        // Turn radius check.
        if (angularInertiaDegPerSec <= 0)
            return new NavigationStepResult(0, IsArrived: false);

        double turnRadiusUnits = speedKmS <= 0
            ? 0
            : speedKmS * 1000.0 / (angularInertiaDegPerSec * Math.PI / 180.0) / 100.0;

        if (r < turnRadiusUnits)
            return new NavigationStepResult(0, IsArrived: false);

        double turnDelta = Math.Sign(delta) * Math.Min(Math.Abs(delta), turnStepDegrees);

        // Segment crossing check with post-turn direction.
        if (stepTimeMs > 0)
        {
            double finalDirection = directionDegrees + turnDelta;
            if (SegmentCrossesTarget(
                    x, y, finalDirection, speedKmS, stepTimeMs, targetX, targetY))
                return new NavigationStepResult(turnDelta, IsArrived: true);
        }

        return new NavigationStepResult(turnDelta, IsArrived: false);
    }

    private static NavigationStepResult HoldLockedCourse(
        double x, double y,
        double directionDegrees,
        double lockedCourseDegrees,
        double speedKmS,
        double targetX,
        double targetY,
        int turnStepDegrees,
        long stepTimeMs,
        bool isNewLock = false)
    {
        // Turn toward the locked course if not already aligned.
        double lockDelta = ShortestSignedAngleDegrees(directionDegrees, lockedCourseDegrees);

        double turnDelta;
        if (Math.Abs(lockDelta) <= turnStepDegrees / 2.0)
        {
            // On (or nearly on) the locked course.
            // When first locking, return the residual delta so the Engine applies it
            // (otherwise a sub-turn-step error is permanently lost — P1 fix).
            turnDelta = isNewLock && Math.Abs(lockDelta) <= turnStepDegrees ? lockDelta : 0;
        }
        else
        {
            turnDelta = Math.Sign(lockDelta) * Math.Min(Math.Abs(lockDelta), turnStepDegrees);
        }

        // Arrival checks.
        double dx = targetX - x;
        double dy = targetY - y;
        double directionRad = (directionDegrees + turnDelta) * Math.PI / 180.0;

        // Target behind us (dot ≤ 0) while on or locking to course.
        if (Math.Abs(lockDelta) <= turnStepDegrees / 2.0)
        {
            double dot = dx * Math.Sin(directionRad) - dy * Math.Cos(directionRad);
            if (dot <= 0)
                return new NavigationStepResult(turnDelta, IsArrived: true);
        }

        // Segment crossing check with (post-turn) direction.
        if (stepTimeMs > 0)
        {
            double finalDir = directionDegrees + turnDelta;
            if (SegmentCrossesTarget(
                    x, y, finalDir, speedKmS, stepTimeMs, targetX, targetY))
                return new NavigationStepResult(turnDelta, IsArrived: true);
        }

        return new NavigationStepResult(turnDelta, IsArrived: false,
            LockedCourseDegrees: lockedCourseDegrees);
    }

    private static bool SegmentCrossesTarget(
        double x, double y,
        double directionDegrees,
        double speedKmS,
        long stepTimeMs,
        double targetX,
        double targetY)
    {
        double stepDistance = speedKmS * (stepTimeMs / 1000.0) * 10.0;
        if (stepDistance <= 0)
            return false;

        double angleRad = directionDegrees * Math.PI / 180.0;
        double segDx = stepDistance * Math.Sin(angleRad);
        double segDy = -stepDistance * Math.Cos(angleRad);

        double tDx = targetX - x;
        double tDy = targetY - y;

        double dot = tDx * segDx + tDy * segDy;
        double lenSq = segDx * segDx + segDy * segDy;
        double t = lenSq > 0 ? Math.Clamp(dot / lenSq, 0.0, 1.0) : 0.0;

        double closestX = x + t * segDx;
        double closestY = y + t * segDy;

        double dist = Math.Sqrt(
            (targetX - closestX) * (targetX - closestX) +
            (targetY - closestY) * (targetY - closestY));

        return dist <= ArrivalEpsilon;
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
}
