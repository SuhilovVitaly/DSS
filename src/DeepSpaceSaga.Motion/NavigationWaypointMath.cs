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

    /// <summary>Safety factor for the full-turn reserve distance (10% margin).</summary>
    public const double TurnReserveSafetyFactor = 1.10;

    /// <summary>
    /// Compute the required departure distance for a close-target staged maneuver.
    /// After reaching this distance from the target, the ship can safely turn around
    /// and approach on a straight-line course.
    /// </summary>
    /// <param name="speedKmS">Ship speed, km/s.</param>
    /// <param name="angularInertiaDegPerSec">Angular inertia, degrees per second.</param>
    /// <param name="turnStepIntervalMs">Turn step interval, milliseconds (e.g. 250).</param>
    /// <returns>Required distance in world units.</returns>
    public static double RequiredDepartureDistance(
        double speedKmS,
        int angularInertiaDegPerSec,
        long turnStepIntervalMs)
    {
        if (speedKmS <= 0 || angularInertiaDegPerSec <= 0)
            return 0;

        double speedUnitsPerSec = speedKmS * 10.0;
        double angularVelocityRadPerSec = angularInertiaDegPerSec * Math.PI / 180.0;
        double turnRadiusUnits = speedUnitsPerSec / angularVelocityRadPerSec;
        double distancePerTurnStep = speedUnitsPerSec * turnStepIntervalMs / 1000.0;

        return 2.0 * turnRadiusUnits * TurnReserveSafetyFactor
               + Math.Max(distancePerTurnStep, 5.0 * ArrivalEpsilon);
    }

    /// <summary>
    /// Result of a staged navigation step.
    /// </summary>
    /// <param name="TurnDeltaDegrees">Turn to apply (0 = fly straight).</param>
    /// <param name="IsArrived">Terminal arrival — ship passed through the target.</param>
    /// <param name="LockedCourseDegrees">Course lock for the next step.</param>
    /// <param name="NextNavigationPhase">Next phase (null = no change).</param>
    /// <param name="EscapeCourseDegrees">Escape course to use (set when entering EscapeTurn).</param>
    /// <param name="RequiredDepartureDistance">Required departure distance (set when entering EscapeDepart).</param>
    public readonly record struct StagedNavigationStepResult(
        double TurnDeltaDegrees,
        bool IsArrived,
        double? LockedCourseDegrees = null,
        string? NextNavigationPhase = null,
        double? EscapeCourseDegrees = null,
        double? RequiredDepartureDistance = null);

    /// <summary>
    /// Compute one step of a staged navigation maneuver.
    /// </summary>
    /// <param name="phase">Current phase: "Approach", "EscapeTurn", or "EscapeDepart". Null/empty/unknown defaults to Approach.</param>
    /// <param name="escapeCourseDegrees">Current escape course (EscapeTurn/EscapeDepart).</param>
    /// <param name="requiredDepartureDistance">Required departure distance (EscapeDepart).</param>
    public static StagedNavigationStepResult StagedStep(
        double x, double y,
        double directionDegrees,
        double speedKmS,
        double targetX, double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs,
        string? phase,
        double? lockedCourseDegrees = null,
        double? escapeCourseDegrees = null,
        double? requiredDepartureDistance = null)
    {
        if (phase == "EscapeTurn")
            return EscapeTurnStep(x, y, directionDegrees, speedKmS, targetX, targetY,
                turnStepDegrees, angularInertiaDegPerSec, stepTimeMs,
                escapeCourseDegrees);

        if (phase == "EscapeDepart")
            return EscapeDepartStep(x, y, directionDegrees, speedKmS, targetX, targetY,
                turnStepDegrees, angularInertiaDegPerSec, stepTimeMs,
                escapeCourseDegrees, requiredDepartureDistance);

        // Default: Approach. If this approach belongs to a staged close-target
        // maneuver, the ship has already spent time departing to create room; do
        // not fall back into the old "inside turn radius => fly straight" wait,
        // because that is exactly what creates the wide pursuit loops.
        bool stagedApproach = escapeCourseDegrees is not null || requiredDepartureDistance is not null;
        var step = StepCore(x, y, directionDegrees, speedKmS, targetX, targetY,
            turnStepDegrees, angularInertiaDegPerSec, stepTimeMs, lockedCourseDegrees,
            enforceTurnRadius: !stagedApproach);
        return new StagedNavigationStepResult(step.TurnDeltaDegrees, step.IsArrived,
            LockedCourseDegrees: step.LockedCourseDegrees);
    }

    private static StagedNavigationStepResult EscapeTurnStep(
        double x, double y,
        double directionDegrees,
        double speedKmS,
        double targetX, double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs,
        double? escapeCourseDegrees)
    {
        // Compute or use existing escape course: bearing from target to ship.
        double escCourse = escapeCourseDegrees
            ?? BearingDegrees(x - targetX, y - targetY);

        double delta = ShortestSignedAngleDegrees(directionDegrees, escCourse);

        // On escape course → transition to EscapeDepart.
        if (Math.Abs(delta) <= turnStepDegrees / 2.0)
        {
            double lockTurnDelta = Math.Abs(delta) <= turnStepDegrees ? delta : 0;
            double reqDist = RequiredDepartureDistance(speedKmS, angularInertiaDegPerSec, stepTimeMs);
            return new StagedNavigationStepResult(lockTurnDelta, IsArrived: false,
                LockedCourseDegrees: escCourse,
                NextNavigationPhase: "EscapeDepart",
                EscapeCourseDegrees: escCourse,
                RequiredDepartureDistance: reqDist);
        }

        // Turn toward escape course.
        double turnDelta = Math.Sign(delta) * Math.Min(Math.Abs(delta), turnStepDegrees);
        return new StagedNavigationStepResult(turnDelta, IsArrived: false,
            EscapeCourseDegrees: escCourse);
    }

    private static StagedNavigationStepResult EscapeDepartStep(
        double x, double y,
        double directionDegrees,
        double speedKmS,
        double targetX, double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs,
        double? escapeCourseDegrees,
        double? requiredDepartureDistance)
    {
        double escCourse = escapeCourseDegrees ?? 0;
        double reqDist = requiredDepartureDistance ?? 0;
        double r = Math.Sqrt((targetX - x) * (targetX - x) + (targetY - y) * (targetY - y));

        // Far enough → transition to Approach (no locked course yet — fresh start).
        if (r >= reqDist)
            return new StagedNavigationStepResult(0, IsArrived: false,
                NextNavigationPhase: "Approach");

        // Still departing: hold the escape course.
        double lockDelta = ShortestSignedAngleDegrees(directionDegrees, escCourse);
        double turnDelta = Math.Abs(lockDelta) <= turnStepDegrees / 2.0 ? 0
            : Math.Sign(lockDelta) * Math.Min(Math.Abs(lockDelta), turnStepDegrees);

        return new StagedNavigationStepResult(turnDelta, IsArrived: false,
            LockedCourseDegrees: escCourse,
            EscapeCourseDegrees: escCourse,
            RequiredDepartureDistance: reqDist);
    }

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
    /// Evaluate whether a navigate-to-point target is trivially unreachable.
    /// Only rejects targets within <see cref="ArrivalEpsilon"/> (already on the point).
    /// Close targets that were previously rejected now use a staged maneuver
    /// (EscapeTurn → EscapeDepart → Approach) handled by the Engine.
    /// Shared by Engine (authoritative) and Client (precheck).
    /// </summary>
    /// <returns>False only when the target is inside ArrivalEpsilon and should be rejected.</returns>
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

        // Already on top of the target — degenerate, would loop endlessly.
        return distance > ArrivalEpsilon;
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
        return StepCore(
            x, y, directionDegrees, speedKmS,
            targetX, targetY,
            turnStepDegrees, angularInertiaDegPerSec,
            stepTimeMs, lockedCourseDegrees,
            enforceTurnRadius: true);
    }

    private static NavigationStepResult StepCore(
        double x,
        double y,
        double directionDegrees,
        double speedKmS,
        double targetX,
        double targetY,
        int turnStepDegrees,
        int angularInertiaDegPerSec,
        long stepTimeMs,
        double? lockedCourseDegrees,
        bool enforceTurnRadius)
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

        // Turn radius check for normal approach. Staged approach explicitly bypasses
        // this after the escape/departure phases, otherwise it can re-enter the loop
        // this maneuver exists to avoid.
        if (enforceTurnRadius)
        {
            if (angularInertiaDegPerSec <= 0)
                return new NavigationStepResult(0, IsArrived: false);

            double turnRadiusUnits = speedKmS <= 0
                ? 0
                : speedKmS * 1000.0 / (angularInertiaDegPerSec * Math.PI / 180.0) / 100.0;

            if (r < turnRadiusUnits)
                return new NavigationStepResult(0, IsArrived: false);
        }

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
