using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatForecastStationaryGuidanceTests
{
    private const double Radians = Math.PI / 180;
    private const int PlannerVersion = 1;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CombatForecast_stationary_shortcut_preserves_original_guidance_vectors(bool moving)
    {
        var random = new Random(34567);
        for (int i = 0; i < 5000; i++)
        {
            double offset = i % 4 == 0 ? 1e12 : 0;
            var origin = new ObjectMotionSnapshot("p", offset + random.NextDouble() * 2000 - 1000,
                offset + random.NextDouble() * 2000 - 1000, 0, random.NextDouble() * 360);
            var target = new ObjectMotionSnapshot("t", offset + random.NextDouble() * 2000 - 1000,
                offset + random.NextDouble() * 2000 - 1000, moving ? random.NextDouble() * 10 : 0, random.NextDouble() * 360);
            double speed = .1 + random.NextDouble() * 20;
            double turn = 1 + random.NextDouble() * 180;
            var expected = ReferencePlan(origin, target, speed, turn, 37);
            var actual = TorpedoGuidanceMath.Plan(origin, target, speed, turn, 37);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(expected), System.Text.Json.JsonSerializer.Serialize(actual));
            Assert.Equal(expected.HasIntercept, actual.HasIntercept);
            Assert.Equal(expected.Segments.Length, actual.Segments.Length);
            for (int j = 0; j < actual.Segments.Length; j++)
            {
                Assert.Equal(expected.Segments[j].AngularVelocityDegPerSec, actual.Segments[j].AngularVelocityDegPerSec);
                Assert.InRange(Math.Abs(expected.Segments[j].DurationMs - actual.Segments[j].DurationMs), 0, 1e-5);
                Assert.InRange(Math.Abs(expected.Segments[j].X - actual.Segments[j].X), 0, 1e-6);
                Assert.InRange(Math.Abs(expected.Segments[j].Y - actual.Segments[j].Y), 0, 1e-6);
            }
        }
    }

    // Frozen pre-forecast planner. Regression oracle uses the original angular search,
    // independently of the analytic tangent construction in production.
    private static TorpedoRoute ReferencePlan(ObjectMotionSnapshot projectilePose, ObjectMotionSnapshot targetPose,
        double speedKmS, double turnRateDegPerSec, long startMotionTimeMs = 0)
    {
        if (!double.IsFinite(speedKmS) || speedKmS <= 0)
            throw new ArgumentOutOfRangeException(nameof(speedKmS));
        if (!double.IsFinite(turnRateDegPerSec) || turnRateDegPerSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnRateDegPerSec));

        double bestTime = double.PositiveInfinity;
        ImmutableArray<TorpedoRouteSegment> best = default;
        // Search the two single-turn families in canonical order. The residual is
        // collinearity of the relative position and relative velocity at turn exit.
        // Its domain is one revolution, not a flight-time/range limit.
        foreach (int sign in new[] { -1, 1 })
        {
            double previousAngle = 0;
            double previous = Evaluate(0, sign, false);
            Evaluate(0, sign, true);
            for (int i = 1; i <= 720; i++)
            {
                double angle = i * .5;
                double value = Evaluate(angle, sign, false);
                if (value == 0) Evaluate(angle, sign, true);
                if (Math.Sign(value) != Math.Sign(previous))
                {
                    double low = previousAngle, high = angle, lowValue = previous;
                    for (int iteration = 0; iteration < 60; iteration++)
                    {
                        double mid = (low + high) / 2;
                        double residual = Evaluate(mid, sign, false);
                        if (Math.Sign(residual) == Math.Sign(lowValue))
                        {
                            low = mid;
                            lowValue = residual;
                        }
                        else high = mid;
                    }
                    Evaluate((low + high) / 2, sign, true);
                }
                previousAngle = angle;
                previous = value;
            }
        }

        if (!best.IsDefault)
            return new(startMotionTimeMs, PlannerVersion,
                best[0].DurationMs > 0 && best[0].AngularVelocityDegPerSec != 0
                    ? TorpedoRoutePhase.Turning : TorpedoRoutePhase.Straight, true, best);

        // Receding-horizon pursuit. Engine replans at fixed physical boundaries;
        // extrapolation continues along the final tangent without stopping.
        var aim = ApproachPursuitMath.ExtrapolatePosition(targetPose.X, targetPose.Y,
            targetPose.Direction, targetPose.SpeedKmS, 1000);
        double bearing = Math.Atan2(aim.X - projectilePose.X, -(aim.Y - projectilePose.Y)) / Radians;
        double delta = (bearing - projectilePose.Direction + 540) % 360 - 180;
        double turn = Math.CopySign(turnRateDegPerSec, delta);
        var pursuit = new TorpedoRouteSegment(projectilePose.X, projectilePose.Y,
            projectilePose.Direction, speedKmS, turn, Math.Min(100, Math.Abs(delta) / turnRateDegPerSec * 1000));
        return new(startMotionTimeMs, PlannerVersion, TorpedoRoutePhase.Turning, false, [pursuit]);

        double Evaluate(double angle, int sign, bool accept)
        {
            double turnMs = angle / turnRateDegPerSec * 1000;
            var arc = new TorpedoRouteSegment(projectilePose.X, projectilePose.Y,
                projectilePose.Direction, speedKmS, sign * turnRateDegPerSec, turnMs);
            var exit = TorpedoGuidanceMath.PredictSegment(arc, turnMs);
            var target = TorpedoGuidanceMath.PredictSegment(new(targetPose.X, targetPose.Y,
                targetPose.Direction, targetPose.SpeedKmS, 0, turnMs), turnMs);
            double dx = target.X - exit.X, dy = target.Y - exit.Y;
            double wx = 10 * (targetPose.SpeedKmS * Math.Sin(targetPose.Direction * Radians) - speedKmS * Math.Sin(exit.Direction * Radians));
            double wy = 10 * (-targetPose.SpeedKmS * Math.Cos(targetPose.Direction * Radians) + speedKmS * Math.Cos(exit.Direction * Radians));
            double residual = dx * wy - dy * wx;
            if (!accept) return residual;
            double squared = wx * wx + wy * wy;
            double straightSeconds = squared == 0 ? 0 : -(dx * wx + dy * wy) / squared;
            double error = Math.Sqrt(Math.Pow(dx + wx * straightSeconds, 2) + Math.Pow(dy + wy * straightSeconds, 2));
            double total = turnMs + straightSeconds * 1000;
            if (straightSeconds >= 0 && double.IsFinite(total) && total < bestTime &&
                error <= 1e-6 + 1e-12 * Math.Max(Math.Abs(dx), Math.Abs(dy)))
            {
                bestTime = total;
                best = [arc, new(exit.X, exit.Y, exit.Direction, speedKmS, 0, straightSeconds * 1000)];
            }
            return residual;
        }
    }

}
