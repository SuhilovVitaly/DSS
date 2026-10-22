using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Shared bounded steering for evasion and its return; contains no AI or hidden projectile data.</summary>
public static class EvasionMotionMath
{
    public static double CompletionTime(EvasionSnapshot schedule) => schedule.Steering?.ReturnsAtMotionTimeMs ?? schedule.EndsAtMotionTimeMs;

    public static EvasionSnapshot Plan(ObjectMotionSnapshot pose, ObjectMotionSnapshot basis, double start, double end,
        string threat, EvadeSide side, double direction, double maxSpeed, double turnRate, double acceleration)
    {
        if (!double.IsFinite(turnRate) || turnRate <= 0 || !double.IsFinite(acceleration) || acceleration <= 0 ||
            !double.IsFinite(maxSpeed) || maxSpeed <= 0 || !double.IsFinite(start) || !double.IsFinite(end) || end < start)
            throw new ArgumentException("Invalid evasion engine limits or schedule.");
        var outward = Steer(pose, direction, maxSpeed, turnRate, acceleration, end - start);
        double returning = Math.Max(Math.Abs(Angle(outward.Direction, basis.Direction)) / turnRate,
            Math.Abs(outward.SpeedKmS - basis.SpeedKmS) / acceleration) * 1000;
        return new(start, end, threat, side, basis, new(direction, maxSpeed, turnRate, acceleration, end + returning));
    }

    public static ObjectMotionSnapshot Sample(ObjectMotionSnapshot state, EvasionSnapshot schedule, double elapsed)
    {
        var control = schedule.Steering!;
        if (schedule.BaseMotion is null || schedule.BaseMotion.Evasion is not null || schedule.BaseMotion.ObjectId != state.ObjectId ||
            !double.IsFinite(control.ReturnsAtMotionTimeMs) || control.ReturnsAtMotionTimeMs < schedule.EndsAtMotionTimeMs)
            throw new ArgumentException("Invalid evasion continuation.");
        double time = schedule.SampleMotionTimeMs;
        double first = Math.Min(elapsed, Math.Max(0, schedule.EndsAtMotionTimeMs - time));
        var pose = Steer(state, control.Direction, control.MaxSpeedKmS, control.TurnRateDegPerSec, control.AccelerationKmS2, first);
        double second = Math.Min(elapsed - first, Math.Max(0, control.ReturnsAtMotionTimeMs - Math.Max(time, schedule.EndsAtMotionTimeMs)));
        pose = Steer(pose, schedule.BaseMotion.Direction, schedule.BaseMotion.SpeedKmS,
            control.TurnRateDegPerSec, control.AccelerationKmS2, second);
        double sampleTime = time + elapsed;
        if (sampleTime < Math.Ceiling(control.ReturnsAtMotionTimeMs))
        {
            double coast = elapsed - first - second;
            pose = Steer(pose, schedule.BaseMotion.Direction, schedule.BaseMotion.SpeedKmS,
                control.TurnRateDegPerSec, control.AccelerationKmS2, coast);
            return pose with { Evasion = schedule with { SampleMotionTimeMs = sampleTime } };
        }
        var basis = schedule.BaseMotion with { X = pose.X, Y = pose.Y };
        // Keep the integer-ms engine scheduling seam identical to the authoritative executor.
        double wait = Math.Max(0, Math.Ceiling(control.ReturnsAtMotionTimeMs) - Math.Max(time, control.ReturnsAtMotionTimeMs));
        basis = Steer(basis, basis.Direction, basis.SpeedKmS, control.TurnRateDegPerSec, control.AccelerationKmS2, wait);
        return CurrentManeuverForecast.Sample(basis, Math.Max(0, elapsed - first - second - wait));
    }

    public static ObjectMotionSnapshot Steer(ObjectMotionSnapshot pose, double heading, double speed, double turnRate, double acceleration, double elapsedMs)
    {
        if (!double.IsFinite(heading) || !double.IsFinite(speed) || speed < 0 || !double.IsFinite(turnRate) || turnRate <= 0 ||
            !double.IsFinite(acceleration) || acceleration <= 0 || !double.IsFinite(elapsedMs) || elapsedMs < 0)
            throw new ArgumentException("Invalid steering limits.");
        double left = elapsedMs;
        while (left > 1e-9)
        {
            double angle = Angle(pose.Direction, heading), delta = speed - pose.SpeedKmS;
            double turn = Math.Abs(angle) < 1e-10 ? 0 : Math.CopySign(turnRate, angle);
            double accel = Math.Abs(delta) < 1e-12 ? 0 : Math.CopySign(acceleration, delta);
            double step = Math.Min(left, Math.Min(turn == 0 ? double.PositiveInfinity : Math.Abs(angle / turn) * 1000,
                accel == 0 ? double.PositiveInfinity : Math.Abs(delta / accel) * 1000));
            var p = HostileOrbitMath.Sample(new(0, long.MaxValue, 0, pose.X, pose.Y, pose.SpeedKmS, pose.Direction, accel, turn), step);
            pose = pose with { X = p.X, Y = p.Y, SpeedKmS = p.SpeedKmS, Direction = p.Direction };
            if (turn != 0 && step >= Math.Abs(angle / turn) * 1000 - 1e-8) pose = pose with { Direction = heading };
            if (accel != 0 && step >= Math.Abs(delta / accel) * 1000 - 1e-8) pose = pose with { SpeedKmS = speed };
            left -= step;
        }
        return pose;
    }

    private static double Angle(double from, double to) => (to - from + 540) % 360 - 180;
}
