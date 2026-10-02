using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class TorpedoCollisionTests
{
    private static CollisionPath Line(double x, double y, double vx = 0, double vy = 0) =>
        new(t => (x + vx * t, y + vy * t), Math.Sqrt(vx * vx + vy * vy), 0);

    [Fact]
    public void Swept_contact_catches_crossing_between_tick_endpoints()
    {
        var hit = TorpedoCollisionMath.FirstContact(Line(-100, 0, 1), Line(0, 0), 0, 200)!.Value;
        Assert.Equal(95, hit.MotionTimeMs, 5);
        Assert.Equal(-5, hit.X, 5);
        Assert.Null(TorpedoCollisionMath.FirstContact(Line(-100, 6, 1), Line(0, 0), 0, 200));
    }

    [Fact]
    public void Contact_at_radius_boundary_is_inclusive()
    {
        Assert.Equal(0, TorpedoCollisionMath.FirstContact(Line(3, 4), Line(0, 0), 0, 100)!.Value.MotionTimeMs);
        Assert.Equal(0, TorpedoCollisionMath.FirstContact(Line(0, 0), Line(0, 0), 0, 0)!.Value.MotionTimeMs);
        var tangent = TorpedoCollisionMath.FirstContact(Line(-10, 5, 1), Line(0, 0), 0, 20)!.Value;
        Assert.InRange(Math.Abs(tangent.MotionTimeMs - 10), 0, .002);
        Assert.Null(TorpedoCollisionMath.FirstContact(Line(-10, 5.0001, 1), Line(0, 0), 0, 20));
    }

    [Fact]
    public void Moving_obstacle_and_arc_first_contact_are_detected()
    {
        Assert.Equal(47.5, TorpedoCollisionMath.FirstContact(Line(-100, 0, 1), Line(0, 0, -1), 0, 100)!.Value.MotionTimeMs, 5);
        var segment = new TorpedoRouteSegment(0, 0, 0, 3, 90, 4000);
        var arc = new CollisionPath(t =>
        {
            var p = TorpedoGuidanceMath.PredictSegment(segment, t);
            return (p.X, p.Y);
        }, .03, .03 * Math.PI / 2000);
        double r = 60 / Math.PI;
        var obstacle = Line(r, -r);
        var hit = TorpedoCollisionMath.FirstContact(arc, obstacle, 0, 4000)!.Value;
        // Independent circle geometry: distance = 2R sin(delta-angle / 2).
        double expected = (Math.PI / 2 - 2 * Math.Asin(5 / (2 * r))) * 2000 / Math.PI;
        Assert.InRange(Math.Abs(hit.MotionTimeMs - expected), 0, .001);
        // Dense independent trigonometric oracle brackets the first crossing.
        double oracle = 0;
        for (; oracle <= 4000; oracle += .1)
        {
            double angle = oracle * Math.PI / 2000;
            double x = r * (1 - Math.Cos(angle)), y = -r * Math.Sin(angle);
            if (Math.Sqrt((x - r) * (x - r) + (y + r) * (y + r)) <= 5) break;
        }
        Assert.InRange(hit.MotionTimeMs, oracle - .101, oracle);
        TorpedoContact? partitioned = null;
        for (double start = 0; start < 4000 && partitioned is null; start += 137)
            partitioned = TorpedoCollisionMath.FirstContact(arc, obstacle, start, Math.Min(4000, start + 137));
        Assert.InRange(Math.Abs(partitioned!.Value.MotionTimeMs - hit.MotionTimeMs), 0, .001);
        Assert.Null(TorpedoCollisionMath.FirstContact(arc, Line(100, 100), 0, 4000));
        var tangent = TorpedoCollisionMath.FirstContact(arc, Line(r, -r - 5), 0, 4000)!.Value;
        Assert.InRange(Math.Abs(tangent.MotionTimeMs - 1000), 0, .1);
    }

    [Fact]
    public void Contact_is_partition_and_zoom_independent()
    {
        var p = Line(1e9, -1e9, .03);
        var q = Line(1e9 + 30000, -1e9);
        var whole = TorpedoCollisionMath.FirstContact(p, q, 0, 2_000_000)!.Value;
        TorpedoContact? split = null;
        for (double t = 0; t < 2_000_000 && split is null; t += 1234)
            split = TorpedoCollisionMath.FirstContact(p, q, t, Math.Min(t + 1234, 2_000_000));
        Assert.InRange(Math.Abs(whole.MotionTimeMs - split!.Value.MotionTimeMs), 0, .0001);
        Assert.Equal((30000 - 5) / .03, whole.MotionTimeMs, 4);
        // No camera/zoom/calendar inputs exist in the physical API.
    }
}
