using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void History_is_complete_and_independent_of_snapshot_frequency(bool unreachable)
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 1000,
            PositionY = -1000,
            SpeedMps = unreachable ? 4000 : 500,
            DirectionDegrees = 90,
            MovementType = "Linear"
        });
        using var whole = Create(scenario);
        using var split = Create(scenario);
        const long launch = 1234, end = 11234;
        foreach (var engine in new[] { whole, split })
        {
            At(engine, launch);
            engine.ReceiveCommand(Fire("history"));
            At(engine, launch);
        }
        var expected = Flight(At(whole, end));
        foreach (long time in new long[] { 1241, 1301, 2233, 4567, 8901, end }) At(split, time);
        var actual = Flight(At(split, end));
        AssertHistory(actual.Torpedo!.Trail, launch, end, actual.X, actual.Y);
        Assert.Equal(Player, actual.Torpedo.OwnerObjectId);
        Assert.Equal(Target, actual.Torpedo.TargetObjectId);
        Assert.Equal(300, actual.Torpedo.DistanceTravelledWorldUnits, 8);
        Assert.Equal(expected.Torpedo!.Trail.Length, actual.Torpedo.Trail.Length);
        for (double time = launch; time <= end; time += 37)
        {
            var a = Position(expected.Torpedo.Trail, time);
            var b = Position(actual.Torpedo.Trail, time);
            Assert.Equal(a.X, b.X, 7);
            Assert.Equal(a.Y, b.Y, 7);
        }
        Assert.Equal(JsonSerializer.Serialize(actual.Torpedo), JsonSerializer.Serialize(Flight(At(split, end)).Torpedo));
    }

    [Fact]
    public void Replan_preserves_executed_path_without_jump()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 1000,
            PositionY = -1000,
            SpeedMps = 4000,
            DirectionDegrees = 90,
            MovementType = "Linear"
        }));
        engine.ReceiveCommand(Fire("replan"));
        At(engine, 0);
        var first = Flight(At(engine, 350));
        var history = first.Torpedo!.Trail;
        string frozen = JsonSerializer.Serialize(history);
        var later = Flight(At(engine, 1500));
        Assert.True(later.Torpedo!.Route.StartMotionTimeMs > first.Torpedo.Route.StartMotionTimeMs);
        Assert.Equal(frozen, JsonSerializer.Serialize(history));
        for (double time = 0; time <= 350; time += 7)
        {
            var before = Position(history, time);
            var after = Position(later.Torpedo.Trail, time);
            Assert.Equal(before.X, after.X, 8);
            Assert.Equal(before.Y, after.Y, 8);
        }
        AssertHistory(later.Torpedo.Trail, 0, 1500, later.X, later.Y);
    }

    [Fact]
    public void Final_history_stops_at_first_contact()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(null, Obstacle("rock", 0, -50)));
        engine.ReceiveCommand(Fire("contact"));
        At(engine, 0);
        var impact = Assert.Single(At(engine, 10000).CombatImpacts);
        Assert.Equal("rock", impact.HitObjectId);
        Assert.InRange(Math.Abs(impact.MotionTimeMs - 1500), 0, 1e-5);
        Assert.Equal(-45, impact.Y, 6);
        AssertHistory(impact.FinalTrail, 0, impact.MotionTimeMs, impact.X, impact.Y);
        Assert.Equal(45, impact.FinalTrail.Sum(s => s.Segment.DurationMs * s.Segment.SpeedKmS / 100), 6);
    }

    [Fact]
    public void Long_straight_flight_does_not_add_per_tick_samples()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { PositionY = -100000 } : o));
        engine.ReceiveCommand(Fire("long"));
        At(engine, 0);
        for (long time = 37; time < 60000; time += 137) At(engine, time);
        var projectile = Flight(At(engine, 60000));
        var segment = Assert.Single(projectile.Torpedo!.Trail);
        Assert.Equal(60000, segment.Segment.DurationMs);
        Assert.Equal(0, segment.Segment.AngularVelocityDegPerSec);
        Assert.Equal(1800, projectile.Torpedo.DistanceTravelledWorldUnits);
        AssertHistory(projectile.Torpedo.Trail, 0, 60000, 0, -1800);
    }

    private static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) =>
        Assert.Single(snapshot.Objects.Where(o => o.Torpedo is not null));

    private static (double X, double Y, double Direction) Position(ImmutableArray<TrailSegment> history, double time)
    {
        var segment = history.First(s => time <= s.StartMotionTimeMs + s.Segment.DurationMs + 1e-8);
        return TorpedoGuidanceMath.PredictSegment(segment.Segment, time - segment.StartMotionTimeMs);
    }

    private static void AssertHistory(ImmutableArray<TrailSegment> history, double start, double end, double x, double y)
    {
        Assert.NotEmpty(history);
        double cursor = start;
        (double X, double Y, double Direction)? previous = null;
        foreach (var entry in history)
        {
            Assert.Equal(cursor, entry.StartMotionTimeMs, 7);
            Assert.True(entry.Segment.DurationMs > 0);
            Assert.True(entry.Segment.SpeedKmS > 0);
            if (previous is { } p)
            {
                Assert.Equal(p.X, entry.Segment.X, 7);
                Assert.Equal(p.Y, entry.Segment.Y, 7);
                Assert.InRange(Math.Abs((p.Direction - entry.Segment.Direction + 540) % 360 - 180), 0, 1e-7);
            }
            cursor += entry.Segment.DurationMs;
            previous = TorpedoGuidanceMath.PredictSegment(entry.Segment, entry.Segment.DurationMs);
        }
        Assert.Equal(end, cursor, 7);
        Assert.Equal(x, previous!.Value.X, 6);
        Assert.Equal(y, previous.Value.Y, 6);
    }
}
