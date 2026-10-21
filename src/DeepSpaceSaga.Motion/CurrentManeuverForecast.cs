using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Conditional continuation only. Never advances the confirmed scene or makes AI decisions.</summary>
public static class CurrentManeuverForecast
{
    private static readonly LinearMotionPredictor Predictor = new();

    internal readonly record struct Pose(double X, double Y, double SpeedKmS, double Direction);

    internal static Func<double, Pose> CreateSampler(ObjectMotionSnapshot state)
    {
        if (state.TurnHoldRateDegPerSec is not null || state.Evasion is not null || state.OwnProjectile is not null || state.ObservedProjectile is not null)
            return ms => { var p = Sample(state, ms); return new(p.X, p.Y, p.SpeedKmS, p.Direction); };
        if (state.HostileCombat is { Segment: var controls })
        {
            double a = controls.AccelerationKmS2;
            double cap = a > 0 ? Math.Max(0, (state.MaxSpeedKmS!.Value - state.SpeedKmS) / a) * 1000 :
                a < 0 ? Math.Max(0, -state.SpeedKmS / a) * 1000 : double.PositiveInfinity;
            var segment = new HostileMotionSegment(0, long.MaxValue, 0, state.X, state.Y,
                state.SpeedKmS, state.Direction, a, controls.TurnRateDegPerSec);
            HostileMotionSegment? coast = null;
            if (double.IsFinite(cap))
            {
                var limit = HostileOrbitMath.Sample(segment, cap);
                coast = segment with
                {
                    StartX = limit.X,
                    StartY = limit.Y,
                    StartSpeedKmS = Math.Max(0, limit.SpeedKmS),
                    StartDirectionDegrees = limit.Direction,
                    AccelerationKmS2 = 0
                };
            }
            return ms =>
            {
                var p = ms <= cap ? HostileOrbitMath.Sample(segment, ms) : HostileOrbitMath.Sample(coast!, ms - cap);
                return new(p.X, p.Y, Math.Max(0, p.SpeedKmS), p.Direction);
            };
        }
        if (LinearMotionPredictor.IsLinear(state))
        {
            double radians = state.Direction * Math.PI / 180;
            double vx = state.SpeedKmS / 100 * Math.Sin(radians), vy = -state.SpeedKmS / 100 * Math.Cos(radians);
            return ms => new(state.X + vx * ms, state.Y + vy * ms, state.SpeedKmS, state.Direction);
        }
        return ms => { var p = Sample(state, ms); return new(p.X, p.Y, p.SpeedKmS, p.Direction); };
    }

    private static bool HasUnscheduledStep(ObjectMotionSnapshot state) =>
        state.EngineManeuver?.CompletesOnMotionUpdate == true || state.Evasion?.BaseMotion.EngineManeuver?.CompletesOnMotionUpdate == true;

    public static bool HasRequiredData(ObjectMotionSnapshot state) => !HasUnscheduledStep(state) && (
        state.TurnHoldRateDegPerSec is not null || state.Evasion is not null ||
        state.OwnProjectile is not null || state.ObservedProjectile is not null ||
        state.HostileCombat is { Segment.AccelerationKmS2: 0 } ||
        state.HostileCombat is not null && state.MaxSpeedKmS is > 0 ||
        state.HostileCombat is null && (ValidEngineManeuver(state) || state.ActiveEngineCommandType is not
            (ShipEngineCommandTypes.Accelerate or ShipEngineCommandTypes.Brake or
             ShipEngineCommandTypes.SpeedSynchronization or ShipEngineCommandTypes.DirectionSynchronization or
             ShipEngineCommandTypes.TurnLeftStep or ShipEngineCommandTypes.TurnRightStep)));

    internal static bool ValidEngineManeuver(ObjectMotionSnapshot state) => state.EngineManeuver is { } m &&
        m.CommandType == state.ActiveEngineCommandType && double.IsFinite(m.CycleRemainingMs) &&
        m.CycleRemainingMs >= 0 && (m.CompletesOnMotionUpdate
            ? m is { CycleIntervalMs: 0, CycleRemainingMs: 0, MotionSampleTimeMs: >= 0 } && double.IsFinite(m.MotionSampleTimeMs.Value)
            : m.CycleIntervalMs > 0) && state.MaxSpeedKmS is > 0 &&
        double.IsFinite(m.AccelerationKmS2) && double.IsFinite(m.FirstSpeedDeltaKmS);

    public static ObjectMotionSnapshot Sample(ObjectMotionSnapshot state, double elapsedMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMs);
        if (!double.IsFinite(elapsedMs)) throw new ArgumentOutOfRangeException(nameof(elapsedMs));
        if (state.Evasion is { } evasion) return SampleEvasion(state, evasion, elapsedMs);
        if (state.TurnHoldRateDegPerSec is { } rate)
        {
            if (!double.IsFinite(rate)) throw new ArgumentException("Invalid held turn rate.", nameof(state));
            var p = HostileOrbitMath.Sample(new HostileMotionSegment(0, long.MaxValue, 0,
                state.X, state.Y, state.SpeedKmS, state.Direction, 0, rate), elapsedMs);
            return state with { X = p.X, Y = p.Y, Direction = p.Direction };
        }
        if (state.OwnProjectile is { } own)
        {
            if (!double.IsFinite(own.SpeedKmS) || own.SpeedKmS <= 0) throw new ArgumentException("Invalid own projectile speed.", nameof(state));
            double dx = own.DetonationX - own.X, dy = own.DetonationY - own.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double progress = distance == 0 ? 1 : elapsedMs * own.SpeedKmS / 100 / distance;
            var p = BlastMath.PositionAt(own.X, own.Y, own.DetonationX, own.DetonationY, progress);
            return state with { X = p.X, Y = p.Y, OwnProjectile = own with { X = p.X, Y = p.Y } };
        }
        if (state.ObservedProjectile is { } observed)
        {
            if (!double.IsFinite(observed.SpeedKmS) || observed.SpeedKmS <= 0) throw new ArgumentException("Invalid observed projectile speed.", nameof(state));
            double length = Math.Sqrt(observed.DirectionX * observed.DirectionX + observed.DirectionY * observed.DirectionY);
            if (length == 0 || !double.IsFinite(length)) throw new ArgumentException("Invalid observed projectile direction.", nameof(state));
            double distance = observed.SpeedKmS * elapsedMs / 100;
            double x = observed.X + observed.DirectionX / length * distance;
            double y = observed.Y + observed.DirectionY / length * distance;
            if (!double.IsFinite(x) || !double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(elapsedMs));
            return state with { X = x, Y = y, ObservedProjectile = observed with { X = x, Y = y } };
        }
        if (state.EngineManeuver is { CompletesOnMotionUpdate: true } pending && ValidEngineManeuver(state))
        {
            // A forecast sample is not an authoritative motion update. Continue
            // the confirmed course without inventing the pending step's deadline.
            var pose = Advance(state with { EngineManeuver = null }, elapsedMs);
            return pose with { EngineManeuver = pending with { MotionSampleTimeMs = pending.MotionSampleTimeMs + elapsedMs } };
        }
        if (!HasRequiredData(state)) return state;
        if (state.EngineManeuver is { } command && ValidEngineManeuver(state))
            return SampleEngineCommand(state, command, elapsedMs);
        if (state.HostileCombat is { Segment: var original })
        {
            // Integrate from the displayed pose, preserving the current control law.
            double seconds = elapsedMs / 1000;
            double acceleration = original.AccelerationKmS2;
            double capAt = acceleration > 0 ? Math.Max(0, (state.MaxSpeedKmS!.Value - state.SpeedKmS) / acceleration)
                : acceleration < 0 ? Math.Max(0, -state.SpeedKmS / acceleration) : double.PositiveInfinity;
            double powered = Math.Min(seconds, capAt);
            var segment = new HostileMotionSegment(0, long.MaxValue, 0, state.X, state.Y,
                state.SpeedKmS, state.Direction, acceleration, original.TurnRateDegPerSec);
            var sample = HostileOrbitMath.Sample(segment, powered * 1000);
            if (seconds > powered)
                sample = HostileOrbitMath.Sample(segment with
                {
                    StartX = sample.X,
                    StartY = sample.Y,
                    StartSpeedKmS = Math.Max(0, sample.SpeedKmS),
                    StartDirectionDegrees = sample.Direction,
                    AccelerationKmS2 = 0
                }, (seconds - powered) * 1000);
            return state with { X = sample.X, Y = sample.Y, Direction = sample.Direction, SpeedKmS = Math.Max(0, sample.SpeedKmS) };
        }
        long whole = (long)Math.Floor(elapsedMs);
        var a = Predictor.Predict(state, whole);
        if (whole == elapsedMs) return a;
        var b = Predictor.Predict(state, whole + 1);
        double f = elapsedMs - whole;
        return a with { X = a.X + (b.X - a.X) * f, Y = a.Y + (b.Y - a.Y) * f };
    }

    private static ObjectMotionSnapshot SampleEngineCommand(ObjectMotionSnapshot state, EngineManeuverSnapshot m, double elapsed)
    {
        double until = m.CycleRemainingMs;
        if (elapsed < until) return Advance(state, elapsed);
        var pose = Advance(state, until);
        double speed = pose.SpeedKmS, direction = pose.Direction;
        var orbitTarget = m.TargetOrbit is { } orbit && m.TargetOrbitSampleTimeMs is { } epoch
            ? OrbitalMotionMath.At(new("synchronization-target", 0, 0, 0, 0,
                WorldOffsetX: m.TargetWorldOffsetX, WorldOffsetY: m.TargetWorldOffsetY), orbit,
                checked(epoch + (long)Math.Round(until))) : null;
        if (m.CommandType is ShipEngineCommandTypes.Accelerate or ShipEngineCommandTypes.Brake)
            speed = Math.Clamp(speed + m.FirstSpeedDeltaKmS, 0, state.MaxSpeedKmS!.Value);
        else if (m.CommandType == ShipEngineCommandTypes.SpeedSynchronization && (orbitTarget?.SpeedKmS ?? m.CapturedSpeedKmS) is { } capturedSpeed)
            speed = Math.Min(state.MaxSpeedKmS!.Value, capturedSpeed);
        else if (m.CommandType == ShipEngineCommandTypes.DirectionSynchronization && (orbitTarget?.Direction ?? m.CapturedDirectionDegrees) is { } capturedDirection)
            direction = (capturedDirection % 360 + 360) % 360;
        else direction = ((direction + m.TurnStepDegrees) % 360 + 360) % 360;
        pose = pose with { SpeedKmS = speed, Direction = direction };
        double remaining = elapsed - until;
        if (!m.Repeats)
            return Advance(pose, remaining) with { ActiveEngineCommandType = null, EngineManeuver = null };

        // Engine acceleration is a speed change at a cycle boundary. Integrate
        // the clamped arithmetic series of constant-speed legs, not a ramp.
        long cycles = (long)Math.Floor(remaining / m.CycleIntervalMs);
        double deltaSpeed = m.AccelerationKmS2 * m.CycleIntervalMs / 1000;
        double active = deltaSpeed == 0 ? cycles : Math.Min(cycles, Math.Max(0,
            Math.Ceiling((deltaSpeed > 0 ? state.MaxSpeedKmS!.Value - speed : -speed) / deltaSpeed)));
        double sum = active * speed + deltaSpeed * active * (active - 1) / 2 +
            (cycles - active) * (deltaSpeed > 0 ? state.MaxSpeedKmS!.Value : deltaSpeed < 0 ? 0 : speed);
        double tailSpeed = Math.Clamp(speed + cycles * deltaSpeed, 0, state.MaxSpeedKmS!.Value);
        double tail = remaining - cycles * m.CycleIntervalMs;
        double distance = (sum * m.CycleIntervalMs + tailSpeed * tail) / 100;
        double radians = direction * Math.PI / 180;
        return pose with
        {
            X = pose.X + distance * Math.Sin(radians),
            Y = pose.Y - distance * Math.Cos(radians),
            SpeedKmS = tailSpeed,
            EngineManeuver = m with
            {
                CycleRemainingMs = m.CycleIntervalMs - tail,
                FirstSpeedDeltaKmS = deltaSpeed,
                TargetOrbitSampleTimeMs = m.TargetOrbitSampleTimeMs is { } time ? time + (long)elapsed : null
            }
        };
    }

    private static ObjectMotionSnapshot SampleEvasion(ObjectMotionSnapshot state, EvasionSnapshot evasion, double elapsed)
    {
        if (evasion.Steering is not null) return EvasionMotionMath.Sample(state, evasion, elapsed);
        if (!double.IsFinite(evasion.SampleMotionTimeMs) || evasion.SampleMotionTimeMs < 0 ||
            !double.IsFinite(evasion.EndsAtMotionTimeMs) || evasion.EndsAtMotionTimeMs < 0 ||
            evasion.BaseMotion is null || evasion.BaseMotion.Evasion is not null || evasion.BaseMotion.ObjectId != state.ObjectId)
            throw new ArgumentException("Invalid evasion continuation.", nameof(state));
        double first = Math.Min(elapsed, Math.Max(0, evasion.EndsAtMotionTimeMs - evasion.SampleMotionTimeMs));
        var pose = Advance(state with { Evasion = null, EngineManeuver = null }, first);
        double sampleTime = evasion.SampleMotionTimeMs + elapsed;
        if (sampleTime < evasion.EndsAtMotionTimeMs)
            return pose with { Evasion = evasion with { SampleMotionTimeMs = sampleTime } };
        var basis = evasion.BaseMotion with { X = pose.X, Y = pose.Y };
        double wait = Math.Max(0, Math.Ceiling(evasion.EndsAtMotionTimeMs) - Math.Max(evasion.SampleMotionTimeMs, evasion.EndsAtMotionTimeMs));
        double coast = Math.Min(elapsed - first, wait);
        var coastPose = Advance(basis with { EngineManeuver = null }, coast);
        basis = basis with { X = coastPose.X, Y = coastPose.Y };
        if (elapsed - first < wait)
            return basis with { Evasion = evasion with { SampleMotionTimeMs = sampleTime } };
        if (basis.EngineManeuver is { } command)
            basis = basis with { EngineManeuver = command with { MotionSampleTimeMs = Math.Ceiling(evasion.EndsAtMotionTimeMs) } };
        return Sample(basis, elapsed - first - coast);
    }

    private static ObjectMotionSnapshot Advance(ObjectMotionSnapshot state, double ms)
    {
        double radians = state.Direction * Math.PI / 180;
        return state with
        {
            X = state.X + state.SpeedKmS * ms / 100 * Math.Sin(radians),
            Y = state.Y - state.SpeedKmS * ms / 100 * Math.Cos(radians),
            EngineManeuver = state.EngineManeuver is { } m ? m with
            {
                CycleRemainingMs = m.CycleRemainingMs - ms,
                TargetOrbitSampleTimeMs = m.TargetOrbitSampleTimeMs is { } time ? time + (long)ms : null
            } : null
        };
    }
}
