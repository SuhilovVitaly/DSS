using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Conditional continuation only. Never advances the confirmed scene or makes AI decisions.</summary>
public static class CurrentManeuverForecast
{
    private static readonly LinearMotionPredictor Predictor = new();

    public static bool HasRequiredData(ObjectMotionSnapshot state) => state.EngineManeuver?.CompletesOnMotionUpdate != true &&
        (state.SpeedHoldAccelerationKmS2 is not null || state.TurnHoldRateDegPerSec is not null ||
         ValidEngineManeuver(state) || state.ActiveEngineCommandType is not
            (ShipEngineCommandTypes.Accelerate or ShipEngineCommandTypes.Brake or
             ShipEngineCommandTypes.SpeedSynchronization or ShipEngineCommandTypes.DirectionSynchronization or
             ShipEngineCommandTypes.TurnLeftStep or ShipEngineCommandTypes.TurnRightStep));
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
        if (state.TurnHoldRateDegPerSec is not null || state.SpeedHoldAccelerationKmS2 is not null) return HoldMotion.Sample(state, elapsedMs);
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
