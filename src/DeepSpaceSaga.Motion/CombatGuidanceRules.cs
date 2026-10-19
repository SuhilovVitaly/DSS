using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Shared physical guidance grid and constant-velocity assumption.</summary>
public static class CombatGuidanceRules
{
    public const int IntervalMs = 100;

    public static long NextBoundary(long time) => time > long.MaxValue - IntervalMs
        ? long.MaxValue : time - time % IntervalMs + IntervalMs;

    public static bool TargetChanged(ObjectMotionSnapshot baseline, long elapsedMs, ObjectMotionSnapshot pose)
    {
        var expected = TorpedoGuidanceMath.PredictSegment(new(baseline.X, baseline.Y,
            baseline.Direction, baseline.SpeedKmS, 0, 0), elapsedMs);
        return Math.Abs(expected.X - pose.X) > 1e-6 || Math.Abs(expected.Y - pose.Y) > 1e-6 ||
            Math.Abs((expected.Direction - pose.Direction + 540) % 360 - 180) > 1e-8 || baseline.SpeedKmS != pose.SpeedKmS;
    }
}
