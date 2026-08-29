using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// DSS-correct linear motion prediction.
/// Speed: km/s. Direction: degrees, 0° = up, clockwise.
/// 1 km/s = 10 world units/s (since 1 unit = 100 m).
///
/// For active Orbit cycles the predictor delegates turn decisions to
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
        // navigation.approach: trailing-pursuit cycle against a moving aim point —
        // never locks a course, re-aims every cycle. Checked before the Orbit branch
        // below since both populate NavigationTargetX/Y (different meaning — see the
        // doc-comment on ObjectMotionSnapshot.NavigationTargetX).
        if (state.ActiveEngineCommandType == NavigationComputerCommandTypes.Approach &&
            state.NavigationTargetX is { } bakedAimX &&
            state.NavigationTargetY is { } bakedAimY &&
            state.NavigationTargetSpeedKmS is { } targetSpeedKmS &&
            state.NavigationTargetDirectionDegrees is { } targetDirectionDegrees &&
            state.NavigationAngularInertiaDegPerSec > 0)
        {
            return PredictApproach(state, elapsedMs, bakedAimX, bakedAimY, targetDirectionDegrees, targetSpeedKmS);
        }

        // Navigation cycle with locked or lockable course — use shared step math.
        if (state.ActiveEngineCommandType == ShipEngineCommandTypes.Orbit &&
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
        string? navigationPhase = state.NavigationPhase;
        double? escapeCourse = state.NavigationEscapeCourseDegrees;
        double? requiredDepartureDistance = state.NavigationRequiredDepartureDistance;

        // Combined phase + step loop: fly straight segments, call Step at each
        // turn boundary (even if the remaining time is less than a full interval —
        // the Engine always runs the turn decision at the boundary).
        while (remainingMs > 0)
        {
            // At a turn boundary, decide the turn via shared navigation math.
            // Must happen BEFORE flying the segment, matching Engine order (P1 fix).
            if (untilNextTurnMs == 0)
            {
                var navStep = NavigationWaypointMath.StagedStep(
                    x, y, direction, state.SpeedKmS,
                    targetX, targetY,
                    turnStep,
                    state.NavigationAngularInertiaDegPerSec,
                    stepTimeMs: intervalMs,
                    phase: navigationPhase,
                    lockedCourseDegrees: lockedCourse,
                    escapeCourseDegrees: escapeCourse,
                    requiredDepartureDistance: requiredDepartureDistance);

                direction = NormalizeDirection(direction + navStep.TurnDeltaDegrees);
                lockedCourse = navStep.LockedCourseDegrees ?? lockedCourse;
                navigationPhase = navStep.NextNavigationPhase ?? navigationPhase;
                escapeCourse = navStep.EscapeCourseDegrees ?? escapeCourse;
                requiredDepartureDistance = navStep.RequiredDepartureDistance ?? requiredDepartureDistance;
                if (navStep.NextNavigationPhase == "Approach")
                {
                    lockedCourse = null;
                }
                untilNextTurnMs = intervalMs;

                if (navStep.IsArrived)
                {
                    AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, remainingMs);
                    return ClearNavigation(state, x, y, direction, untilNextTurnMs);
                }
            }

            long segmentMs = Math.Min(remainingMs, untilNextTurnMs);
            double segStartX = x, segStartY = y;
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, segmentMs);
            remainingMs -= segmentMs;
            untilNextTurnMs -= segmentMs;

            // Check segment arrival after this advance (catch pass-through).
            var segArrival = NavigationWaypointMath.CheckSegmentArrival(
                segStartX, segStartY, x, y, targetX, targetY);
            if (segArrival.IsArrived)
            {
                x = segArrival.ClosestX;
                y = segArrival.ClosestY;
                return ClearNavigation(state, x, y, direction, untilNextTurnMs);
            }
        }

        return state with
        {
            X = x,
            Y = y,
            Direction = direction,
            TurnStepRemainingMs = untilNextTurnMs, // real phase — P1 fix
            NavigationLockedCourseDegrees = lockedCourse,
            NavigationPhase = navigationPhase,
            NavigationEscapeCourseDegrees = escapeCourse,
            NavigationRequiredDepartureDistance = requiredDepartureDistance,
        };
    }

    /// <summary>
    /// Client-side prediction for an active <see cref="NavigationComputerCommandTypes.Approach"/>
    /// cycle. Unlike <see cref="PredictNavigation"/> (Orbit), the aim point itself is never
    /// permanently locked — the authoritative engine re-derives it from the target's live
    /// state every cycle. Between snapshots the client deliberately keeps the baked aim
    /// point fixed instead of extrapolating an assumed linear future for the target. The
    /// cycle-scoped course lock (<see cref="ApproachPursuitMath.Step"/>'s
    /// <c>lockedCourseDegrees</c>, story-20260827-083137.md Post-implementation bug fix #2)
    /// is threaded the same way <see cref="PredictNavigation"/> threads Orbit's, starting from
    /// the baked <see cref="ObjectMotionSnapshot.NavigationLockedCourseDegrees"/> and carried
    /// across turn-boundary Step calls within this same Predict call.
    /// </summary>
    private static ObjectMotionSnapshot PredictApproach(
        ObjectMotionSnapshot state,
        long elapsedMs,
        double bakedAimX,
        double bakedAimY,
        double targetDirectionDegrees,
        double targetSpeedKmS)
    {
        if (elapsedMs <= 0)
            return state;

        if (ApproachPursuitMath.IsFlyThroughPhase(state.NavigationPhase))
        {
            return PredictFlyThrough(
                state, elapsedMs, bakedAimX, bakedAimY, targetDirectionDegrees, targetSpeedKmS);
        }

        int turnStep = Math.Abs(state.TurnStepDegrees);
        if (turnStep == 0 || state.TurnStepIntervalMs <= 0)
            return PredictStraight(state, elapsedMs, state.Direction);

        long intervalMs = state.TurnStepIntervalMs;
        double trailDistance = Math.Max(0, state.NavigationApproachTrailDistanceWorldUnits ?? 0);
        string approachPhase = state.NavigationPhase == ApproachPursuitMath.TrailPhase && trailDistance > 0
            ? ApproachPursuitMath.TrailPhase
            : ApproachPursuitMath.FinalPhase;

        return RunApproachPursuit(
            state, elapsedMs, state.TurnStepRemainingMs, intervalMs, turnStep,
            state.X, state.Y, state.Direction, state.NavigationLockedCourseDegrees,
            trailDistance, approachPhase, bakedAimX, bakedAimY,
            targetDirectionDegrees, targetSpeedKmS);
    }

    /// <summary>
    /// Runs the per-turn trailing-pursuit loop (Trail then Final phase) shared by
    /// <see cref="PredictApproach"/>'s direct entry and <see cref="PredictFlyThrough"/>'s
    /// hand-off once its Dubins re-orientation curve completes.
    /// </summary>
    private static ObjectMotionSnapshot RunApproachPursuit(
        ObjectMotionSnapshot state,
        long remainingMs,
        long untilNextTurnMs,
        long intervalMs,
        int turnStep,
        double x,
        double y,
        double direction,
        double? lockedCourse,
        double trailDistance,
        string approachPhase,
        double aimBaseX,
        double aimBaseY,
        double targetDirectionDegrees,
        double targetSpeedKmS)
    {
        while (remainingMs > 0)
        {
            if (untilNextTurnMs == 0)
            {
                // Use only the current snapshot's target state. The server now re-bakes
                // this state from the live target every completed cycle (including mid
                // fly-through), so it is never more than one cycle stale — this does not
                // itself guess any further into the future than that.
                double aimX = aimBaseX;
                double aimY = aimBaseY;

                var step = ApproachPursuitMath.Step(
                    x, y, direction, state.SpeedKmS,
                    aimX, aimY, targetDirectionDegrees, targetSpeedKmS,
                    trailDistanceWorldUnits: 0,
                    turnStepDegrees: turnStep,
                    angularInertiaDegPerSec: state.NavigationAngularInertiaDegPerSec,
                    stepTimeMs: intervalMs,
                    lockedCourseDegrees: lockedCourse);

                direction = step.NewDirectionDegrees;
                lockedCourse = step.LockedCourseDegrees;
                untilNextTurnMs = intervalMs;

                if (step.IsArrived)
                {
                    if (approachPhase == ApproachPursuitMath.TrailPhase)
                    {
                        double angleRad = targetDirectionDegrees * Math.PI / 180.0;
                        aimBaseX = aimX + trailDistance * Math.Sin(angleRad);
                        aimBaseY = aimY - trailDistance * Math.Cos(angleRad);
                        approachPhase = ApproachPursuitMath.FinalPhase;
                        lockedCourse = null;
                        continue;
                    }

                    AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, remainingMs);
                    return ClearNavigation(state, x, y, direction, untilNextTurnMs);
                }
            }

            long segmentMs = Math.Min(remainingMs, untilNextTurnMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, segmentMs);
            remainingMs -= segmentMs;
            untilNextTurnMs -= segmentMs;
        }

        return state with
        {
            X = x,
            Y = y,
            Direction = direction,
            TurnStepRemainingMs = untilNextTurnMs,
            NavigationLockedCourseDegrees = lockedCourse,
            NavigationTargetX = aimBaseX,
            NavigationTargetY = aimBaseY,
            NavigationPhase = approachPhase,
        };
    }

    private static ObjectMotionSnapshot PredictFlyThrough(
        ObjectMotionSnapshot state,
        long elapsedMs,
        double targetX,
        double targetY,
        double targetDirectionDegrees,
        double targetSpeedKmS)
    {
        long intervalMs = Math.Max(1, state.TurnStepIntervalMs);
        long remainingMs = elapsedMs;
        long untilNextTurnMs = Math.Max(1, state.TurnStepRemainingMs);
        double x = state.X;
        double y = state.Y;
        double direction = state.Direction;
        string phase = state.NavigationPhase!;
        ApproachFlyThroughPlan? plan = null;

        if (phase.StartsWith(ApproachPursuitMath.FlyThroughPhasePrefix, StringComparison.Ordinal))
        {
            plan = new ApproachFlyThroughPlan(
                phase[ApproachPursuitMath.FlyThroughPhasePrefix.Length..],
                state.NavigationEscapeCourseDegrees ?? 0,
                state.NavigationRequiredDepartureDistance ?? 0,
                state.NavigationLockedCourseDegrees ?? 0);
        }

        while (remainingMs > 0)
        {
            long segmentMs = Math.Min(remainingMs, untilNextTurnMs);
            AdvanceStraight(ref x, ref y, state.SpeedKmS, direction, segmentMs);
            remainingMs -= segmentMs;
            untilNextTurnMs -= segmentMs;
            if (untilNextTurnMs > 0)
                continue;

            ApproachFlyThroughPlanStep step;
            if (plan is null)
            {
                plan = ApproachPursuitMath.CreateFlyThroughPlan(
                    x, y, direction, state.SpeedKmS,
                    targetX, targetY, targetDirectionDegrees,
                    state.NavigationAngularInertiaDegPerSec);
                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits: 0,
                    turnStepDegrees: Math.Abs(state.TurnStepDegrees));
            }
            else
            {
                double travelledUnits = state.SpeedKmS * (intervalMs / 1000.0) * 10.0;
                step = ApproachPursuitMath.AdvanceFlyThroughPlan(
                    plan.Value, direction, targetDirectionDegrees,
                    travelledUnits,
                    Math.Abs(state.TurnStepDegrees));
            }

            direction = step.NewDirectionDegrees;
            plan = step.RemainingPlan;
            untilNextTurnMs = intervalMs;

            if (step.IsArrived)
            {
                // The Dubins curve only ever aimed at the target's pose CAPTURED WHEN
                // THIS LEG WAS PLANNED, and its arrival is bookkeeping-based (cumulative
                // travelled distance vs. planned segment lengths) — the tracked (x, y)
                // rarely lands exactly on that pose (a real turn-quantization artifact,
                // same as the authoritative engine has at this point). Snapping straight
                // to (targetX, targetY) here (the old behavior) produced a visible
                // kink/near-miss, and for a genuinely moving target that pose is stale by
                // now besides. Hand off into the same live-tracking Final-phase pursuit
                // the engine uses (see SimulationEngine.ApplyApproachStep), re-baking the
                // aim point from the target's state extrapolated forward to THIS instant.
                long elapsedSoFarMs = elapsedMs - remainingMs;
                var (liveTargetX, liveTargetY) = ApproachPursuitMath.ExtrapolatePosition(
                    targetX, targetY, targetDirectionDegrees, targetSpeedKmS, elapsedSoFarMs);

                return RunApproachPursuit(
                    state, remainingMs, untilNextTurnMs, intervalMs,
                    Math.Max(1, Math.Abs(state.TurnStepDegrees)),
                    x, y, direction, lockedCourse: null, trailDistance: 0,
                    approachPhase: ApproachPursuitMath.FinalPhase,
                    aimBaseX: liveTargetX, aimBaseY: liveTargetY,
                    targetDirectionDegrees, targetSpeedKmS);
            }
        }

        var remainingPlan = plan;
        return state with
        {
            X = x,
            Y = y,
            Direction = direction,
            TurnStepRemainingMs = untilNextTurnMs,
            NavigationPhase = remainingPlan is null
                ? ApproachPursuitMath.FlyThroughPendingPhase
                : ApproachPursuitMath.FlyThroughPhasePrefix + remainingPlan.Value.Type,
            NavigationEscapeCourseDegrees = remainingPlan?.FirstRemainingUnits,
            NavigationRequiredDepartureDistance = remainingPlan?.SecondRemainingUnits,
            NavigationLockedCourseDegrees = remainingPlan?.ThirdRemainingUnits,
            NavigationTargetX = targetX,
            NavigationTargetY = targetY
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

    /// <summary>Return state with navigation metadata cleared after arrival.</summary>
    private static ObjectMotionSnapshot ClearNavigation(
        ObjectMotionSnapshot state, double x, double y, double direction, long turnStepRemainingMs)
    {
        return state with
        {
            X = x,
            Y = y,
            Direction = direction,
            ActiveEngineCommandType = null,
            TurnStepDegrees = 0,
            TurnStepRemainingMs = turnStepRemainingMs,
            TurnStepIntervalMs = 0,
            NavigationTargetX = null,
            NavigationTargetY = null,
            NavigationAngularInertiaDegPerSec = 0,
            NavigationLockedCourseDegrees = null,
            NavigationPhase = null,
            NavigationEscapeCourseDegrees = null,
            NavigationRequiredDepartureDistance = null,
            NavigationTargetSpeedKmS = null,
            NavigationTargetDirectionDegrees = null,
            NavigationApproachTrailDistanceWorldUnits = null,
        };
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
