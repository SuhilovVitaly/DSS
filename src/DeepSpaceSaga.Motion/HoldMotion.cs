using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Shared analytical continuation of independent turn and speed controls.</summary>
public static class HoldMotion
{
    public static ObjectMotionSnapshot Sample(ObjectMotionSnapshot state, double elapsedMs)
    {
        if (!double.IsFinite(elapsedMs) || elapsedMs < 0) throw new ArgumentOutOfRangeException(nameof(elapsedMs));
        double acceleration = state.SpeedHoldAccelerationKmS2 ?? 0;
        double rate = state.TurnHoldRateDegPerSec ?? 0;
        if (!double.IsFinite(acceleration) || !double.IsFinite(rate) || !double.IsFinite(state.SpeedKmS) || state.SpeedKmS < 0 ||
            acceleration > 0 && (state.MaxSpeedKmS is not > 0 || !double.IsFinite(state.MaxSpeedKmS.Value)))
            throw new ArgumentException("Invalid held motion controls or speed limit.", nameof(state));
        double capMs = acceleration > 0 ? Math.Max(0, (state.MaxSpeedKmS!.Value - state.SpeedKmS) / acceleration) * 1000 :
            acceleration < 0 ? -state.SpeedKmS / acceleration * 1000 : double.PositiveInfinity;
        double poweredMs = Math.Min(elapsedMs, capMs);
        var segment = new HostileMotionSegment(0, long.MaxValue, 0, state.X, state.Y, state.SpeedKmS, state.Direction, acceleration, rate);
        var pose = HostileOrbitMath.Sample(segment, poweredMs);
        bool capped = elapsedMs >= capMs;
        if (capped)
        {
            double limit = acceleration > 0 ? state.MaxSpeedKmS!.Value : 0;
            pose = HostileOrbitMath.Sample(segment with
            {
                StartX = pose.X,
                StartY = pose.Y,
                StartSpeedKmS = limit,
                StartDirectionDegrees = pose.Direction,
                AccelerationKmS2 = 0
            }, elapsedMs - poweredMs);
        }
        bool releaseLatch = state.SpeedHoldLatched && acceleration > 0 && capped;
        return state with
        {
            X = pose.X,
            Y = pose.Y,
            Direction = pose.Direction,
            SpeedKmS = Math.Max(0, pose.SpeedKmS),
            SpeedHoldAccelerationKmS2 = releaseLatch ? null : state.SpeedHoldAccelerationKmS2,
            SpeedHoldLatched = !releaseLatch && state.SpeedHoldLatched
        };
    }
}
