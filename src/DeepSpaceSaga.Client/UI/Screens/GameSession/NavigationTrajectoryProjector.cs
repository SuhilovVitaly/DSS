using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Projects the future trajectory of an active navigation cycle
/// (engine.orbit) using EXACTLY the same deterministic math as the
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
    /// Safety bound for <see cref="ProjectApproach"/>'s loop, which — unlike the generic
    /// Orbit-oriented branch above (still capped at <see cref="FutureTrajectoryHorizonMs"/>,
    /// 200s) — deliberately runs until actual arrival rather than truncating at a fixed
    /// horizon (story-20260827-083137.md, Post-implementation bug fix #2: the user
    /// explicitly asked for the preview not to be capped short of the target). With the
    /// companion convergence fixes (Approach's faster cadence + <see cref="ApproachPursuitMath.Step"/>'s
    /// course locking) the loop should always terminate via IsArrived long before this is
    /// ever reached — this is a generous order-of-magnitude-plus margin (10x
    /// FutureTrajectoryHorizonMs) purely as a backstop against a residual edge case, not a
    /// value expected to be hit in normal play.
    /// </summary>
    public const int ApproachTrajectoryMaxHorizonMs = 10 * FutureTrajectoryHorizonMs;

    /// <summary>
    /// Hard backstop on <see cref="ProjectApproach"/>'s loop iteration count, independent of
    /// <see cref="ApproachTrajectoryMaxHorizonMs"/> — protects against a pathologically small
    /// TurnStepIntervalMs (e.g. malformed/legacy snapshot data) driving an unbounded number
    /// of iterations within the time bound above.
    /// </summary>
    private const int ApproachTrajectoryMaxIterations = 20_000;

    /// <summary>
    /// If <see cref="ProjectApproach"/>'s distance to the (possibly moving) aim point hasn't
    /// improved on its best-so-far value in this many milliseconds of simulated flight, the
    /// chase is treated as stalled and the preview is truncated at the closest point actually
    /// reached — rather than continuing all the way to <see cref="ApproachTrajectoryMaxHorizonMs"/>.
    /// Without this, a target the ship can only almost (but never quite, within
    /// <see cref="ApproachPursuitMath.ArrivalToleranceUnits"/>) catch — e.g. marginally faster,
    /// or on a course the ship can only asymptotically approach — draws a preview line that
    /// passes right by the target early on and then keeps extending far past it in a long,
    /// visually wrong "escaping" straight line for the full 2000s backstop. Comfortably above
    /// the slowest legitimate warm-up (turning up to ~180° to face the target before any
    /// distance closes at all) for the smallest turn-rate content uses — a full 180° turn at
    /// module.engine.basic's 1°/250ms takes ~45s — while still cutting the line off within
    /// roughly a couple of minutes of simulated flight instead of the full 2000s cap.
    /// </summary>
    private const long ApproachStagnationTimeoutMs = 180_000;

    /// <summary>
    /// Compute future world-coordinate trajectory points for an active navigation
    /// cycle, starting from the current predicted state. Empty when the snapshot does
    /// not carry an authoritative navigation target (no active navigate cycle).
    /// </summary>
    public List<FutureTrajectoryPoint> Project(ObjectMotionSnapshot predicted)
    {
        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);

        // navigation.approach: trailing-pursuit preview against a moving aim point —
        // checked before the generic Orbit-oriented branch below since both populate
        // NavigationTargetX/Y (different meaning — see the doc-comment on
        // ObjectMotionSnapshot.NavigationTargetX).
        if (predicted.ActiveEngineCommandType == NavigationComputerCommandTypes.Approach &&
            predicted.NavigationTargetX is { } bakedAimX &&
            predicted.NavigationTargetY is { } bakedAimY &&
            predicted.NavigationTargetSpeedKmS is { } targetSpeedKmS &&
            predicted.NavigationTargetDirectionDegrees is { } targetDirectionDegrees)
        {
            return ProjectApproach(predicted, bakedAimX, bakedAimY, targetDirectionDegrees, targetSpeedKmS);
        }

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

    /// <summary>
    /// Preview trajectory for an active <see cref="NavigationComputerCommandTypes.Approach"/>
    /// cycle. Structurally mirrors the Orbit branch above (initial straight "wait" phase
    /// until the first cycle boundary, then one steering decision per interval), but unlike
    /// it (a) never locks a PERMANENT course — it re-steers toward the current snapshot's
    /// fixed aim point via <see cref="ApproachPursuitMath.Step"/> without assuming that the
    /// target will continue linearly, threading Step's cycle-scoped course lock the
    /// same way, and (b) runs until actual arrival rather than truncating at the fixed
    /// <see cref="FutureTrajectoryHorizonMs"/> (Post-implementation bug fix #2 — see
    /// <see cref="ApproachTrajectoryMaxHorizonMs"/>'s doc-comment for the safety-bound
    /// rationale).
    /// </summary>
    private List<FutureTrajectoryPoint> ProjectApproach(
        ObjectMotionSnapshot predicted,
        double bakedAimX,
        double bakedAimY,
        double targetDirectionDegrees,
        double targetSpeedKmS)
    {
        if (ApproachPursuitMath.IsFlyThroughPhase(predicted.NavigationPhase))
        {
            return ProjectFlyThrough(
                predicted, bakedAimX, bakedAimY, targetDirectionDegrees);
        }

        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);

        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;
        double? lockedCourse = predicted.NavigationLockedCourseDegrees;
        double trailDistance = Math.Max(0, predicted.NavigationApproachTrailDistanceWorldUnits ?? 0);
        string approachPhase = predicted.NavigationPhase == ApproachPursuitMath.TrailPhase && trailDistance > 0
            ? ApproachPursuitMath.TrailPhase
            : ApproachPursuitMath.FinalPhase;

        // The baked point is phase-relative: the trailing staging point during Trail,
        // and the target itself during Final. When Trail completes, this base is moved
        // forward by the configured offset so projection continues to the real target.
        double aimBaseX = bakedAimX;
        double aimBaseY = bakedAimY;

        int turnStepDegrees = Math.Max(1, Math.Abs(predicted.TurnStepDegrees));
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long phaseMs = Math.Max(1, predicted.TurnStepRemainingMs);

        // Preview deliberately treats the target state from the current snapshot as
        // fixed. The authoritative engine will re-read the real target next cycle;
        // guessing a long linear future here produced misleading off-screen routes.
        (double X, double Y) AimAt(long _) => (aimBaseX, aimBaseY);

        bool ContinueFromTrailToTarget(long elapsedMs, double trailAimX, double trailAimY)
        {
            if (approachPhase != ApproachPursuitMath.TrailPhase)
                return false;

            double angleRad = targetDirectionDegrees * Math.PI / 180.0;
            aimBaseX = trailAimX + trailDistance * Math.Sin(angleRad);
            aimBaseY = trailAimY - trailDistance * Math.Cos(angleRad);
            approachPhase = ApproachPursuitMath.FinalPhase;
            lockedCourse = null;
            return true;
        }

        points.Add(new FutureTrajectoryPoint(x, y));

        // Phase until the first cycle boundary: straight flight with the CURRENT course.
        // Unlike every subsequent interval (each covered by ApproachPursuitMath.Step's own
        // segment-sweep arrival check), this phase segment runs with no steering decision at
        // all, so it needs its own arrival check here — otherwise a ship already close to
        // (or aimed straight at) the target flies straight through it during this very first
        // segment and the preview keeps extending past the target instead of stopping there.
        double phaseStartX = x, phaseStartY = y;
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, phaseMs);

        var (phaseAimX, phaseAimY) = AimAt(phaseMs);
        var phaseArrival = ApproachPursuitMath.CheckSegmentArrival(
            phaseStartX, phaseStartY, x, y, phaseAimX, phaseAimY);
        if (phaseArrival.IsArrived)
        {
            // Arrival is tolerance-based, but the rendered route is a promise to the
            // player: it must visually terminate on the navigation aim point itself.
            // Keeping ClosestX/Y here leaves a visible gap at high zoom whenever the
            // straight phase passes near (rather than exactly through) the aim point.
            points.Add(new FutureTrajectoryPoint(phaseAimX, phaseAimY));
            if (!ContinueFromTrailToTarget(phaseMs, phaseAimX, phaseAimY))
                return points;

            x = phaseAimX;
            y = phaseAimY;
        }
        else
        {
            points.Add(new FutureTrajectoryPoint(x, y));
        }

        // Runs until actual arrival (IsArrived) or a stagnation timeout (see
        // ApproachStagnationTimeoutMs), bounded by ApproachTrajectoryMaxHorizonMs/
        // ApproachTrajectoryMaxIterations as an outer safety backstop only — see their
        // doc-comments. If the loop ever exhausts a bound WITHOUT arriving, that is not
        // automatically a bug: a stalled ship (SpeedKmS≈0) or one simply slower than a
        // fleeing target can never geometrically catch it, and legitimately exhausts the
        // bound every time. A hard assertion was considered here (the fix's design notes
        // suggested "surface it, don't silently swallow it") but rejected — a pure
        // client-side projector has no business running a reachability check to tell that
        // ordinary case apart from an actual residual convergence bug, so a hard assert
        // would false-fire on the ordinary case. In that situation the caller simply gets
        // the points computed up to the closest approach reached — an honest "this is as
        // close as it gets" preview, not a guaranteed-complete trajectory.
        long elapsedMs = phaseMs;
        int iterations = 0;
        double bestDistanceToAim = double.MaxValue;
        long bestDistanceElapsedMs = elapsedMs;
        int bestDistancePointIndex = points.Count - 1;
        while (elapsedMs < ApproachTrajectoryMaxHorizonMs && iterations < ApproachTrajectoryMaxIterations)
        {
            iterations++;

            var (aimX, aimY) = AimAt(elapsedMs);

            double distanceToAim = Math.Sqrt((aimX - x) * (aimX - x) + (aimY - y) * (aimY - y));
            if (distanceToAim < bestDistanceToAim)
            {
                bestDistanceToAim = distanceToAim;
                bestDistanceElapsedMs = elapsedMs;
                bestDistancePointIndex = points.Count - 1;
            }
            else if (elapsedMs - bestDistanceElapsedMs >= ApproachStagnationTimeoutMs)
            {
                points.RemoveRange(bestDistancePointIndex + 1, points.Count - bestDistancePointIndex - 1);
                var (fixedAimX, fixedAimY) = AimAt(bestDistanceElapsedMs);
                points.Add(new FutureTrajectoryPoint(fixedAimX, fixedAimY));
                return points;
            }

            var step = ApproachPursuitMath.Step(
                x, y, direction, speedKmS,
                aimX, aimY, targetDirectionDegrees, targetSpeedKmS,
                trailDistanceWorldUnits: 0,
                turnStepDegrees: turnStepDegrees,
                angularInertiaDegPerSec: predicted.NavigationAngularInertiaDegPerSec,
                stepTimeMs: intervalMs,
                lockedCourseDegrees: lockedCourse);

            direction = step.NewDirectionDegrees;
            lockedCourse = step.LockedCourseDegrees;

            if (step.IsArrived)
            {
                points.Add(new FutureTrajectoryPoint(aimX, aimY));
                if (!ContinueFromTrailToTarget(elapsedMs, aimX, aimY))
                    return points;

                x = aimX;
                y = aimY;
                bestDistanceToAim = double.MaxValue;
                bestDistanceElapsedMs = elapsedMs;
                bestDistancePointIndex = points.Count - 1;
                continue;
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));

            elapsedMs += intervalMs;
        }

        var (finalAimX, finalAimY) = AimAt(elapsedMs);
        points.Add(new FutureTrajectoryPoint(finalAimX, finalAimY));

        return points;
    }

    private static List<FutureTrajectoryPoint> ProjectFlyThrough(
        ObjectMotionSnapshot predicted,
        double targetX,
        double targetY,
        double targetDirectionDegrees)
    {
        var points = new List<FutureTrajectoryPoint>(FutureTrajectoryProjector.MaxSamplePoints);
        double x = predicted.X;
        double y = predicted.Y;
        double direction = predicted.Direction;
        double speedKmS = predicted.SpeedKmS;
        long intervalMs = Math.Max(1, predicted.TurnStepIntervalMs);
        long untilNextTurnMs = Math.Max(1, predicted.TurnStepRemainingMs);
        string phase = predicted.NavigationPhase!;
        ApproachFlyThroughPlan? plan = null;

        if (phase.StartsWith(ApproachPursuitMath.FlyThroughPhasePrefix, StringComparison.Ordinal))
        {
            plan = new ApproachFlyThroughPlan(
                phase[ApproachPursuitMath.FlyThroughPhasePrefix.Length..],
                predicted.NavigationEscapeCourseDegrees ?? 0,
                predicted.NavigationRequiredDepartureDistance ?? 0,
                predicted.NavigationLockedCourseDegrees ?? 0);
        }

        points.Add(new FutureTrajectoryPoint(x, y));
        (x, y) = AdvanceStraight(x, y, direction, speedKmS, untilNextTurnMs);
        points.Add(new FutureTrajectoryPoint(x, y));

        int iterations = 0;
        while (iterations++ < ApproachTrajectoryMaxIterations)
        {
            ApproachFlyThroughPlanStep step;
            if (plan is null)
            {
                plan = ApproachPursuitMath.CreateFlyThroughPlan(
                    x, y, direction, speedKmS,
                    targetX, targetY, targetDirectionDegrees,
                    predicted.NavigationAngularInertiaDegPerSec);
                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits: 0,
                    turnStepDegrees: Math.Max(1, Math.Abs(predicted.TurnStepDegrees)));
            }
            else
            {
                double travelledUnits = speedKmS * (intervalMs / 1000.0) * 10.0;
                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits,
                    Math.Max(1, Math.Abs(predicted.TurnStepDegrees)));
            }

            direction = step.NewDirectionDegrees;
            plan = step.RemainingPlan;
            if (step.IsArrived)
            {
                double terminalAngleRad = targetDirectionDegrees * Math.PI / 180.0;
                double terminalEntryDistance = Math.Max(
                    ApproachPursuitMath.ArrivalToleranceUnits * 2,
                    speedKmS * (intervalMs / 1000.0) * 20.0);
                points.Add(new FutureTrajectoryPoint(
                    targetX - terminalEntryDistance * Math.Sin(terminalAngleRad),
                    targetY + terminalEntryDistance * Math.Cos(terminalAngleRad)));
                points.Add(new FutureTrajectoryPoint(targetX, targetY));
                return points;
            }

            (x, y) = AdvanceStraight(x, y, direction, speedKmS, intervalMs);
            points.Add(new FutureTrajectoryPoint(x, y));
        }

        points.Add(new FutureTrajectoryPoint(targetX, targetY));
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
