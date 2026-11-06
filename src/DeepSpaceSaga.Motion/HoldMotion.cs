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
        var pose = SampleArc(state.X, state.Y, state.SpeedKmS, state.Direction, acceleration, rate, poweredMs);
        bool capped = elapsedMs >= capMs;
        if (capped)
        {
            double limit = acceleration > 0 ? state.MaxSpeedKmS!.Value : 0;
            pose = SampleArc(pose.X, pose.Y, limit, pose.Direction, 0, rate, elapsedMs - poweredMs);
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

    // Exact integral of constant thrust and turn rate. The series keeps the
    // straight-course limit well conditioned for very small steering angles.
    private static (double X, double Y, double SpeedKmS, double Direction) SampleArc(
        double x, double y, double speed, double direction, double acceleration, double rate, double elapsedMs)
    {
        double t = elapsedMs / 1000;
        double h = rate * Math.PI / 180 * t / 2;
        double sinc = Math.Abs(h) < 1e-4 ? 1 - h * h / 6 + Math.Pow(h, 4) / 120 : Math.Sin(h) / h;
        double slope = Math.Abs(h) < 1e-4 ? h / 3 - h * h * h / 30 : (Math.Sin(h) - h * Math.Cos(h)) / (h * h);
        double mid = direction * Math.PI / 180 + h;
        double forward = t * (speed + acceleration * t / 2) * sinc;
        double lateral = acceleration * t * t / 2 * slope;
        return (x + 10 * (forward * Math.Sin(mid) + lateral * Math.Cos(mid)),
            y + 10 * (-forward * Math.Cos(mid) + lateral * Math.Sin(mid)), speed + acceleration * t,
            ((direction + rate * t) % 360 + 360) % 360);
    }
}
