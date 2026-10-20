namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0006TK0001Tests
{
    [Fact]
    public void Relative_motion_threat_has_entry_and_closest_approach()
    {
        // r=(10,2), v_rel=(-2,0), R+.5=2.5: half-chord=1.5, entry=4.25, exit=5.75.
        var result = BlastThreatMath.Evaluate(0, 0, 1, 0, 100, 20, -3, 0, 1, 2);
        Assert.True(result.IsThreat);
        Assert.Equal(4.25, result.EntryAfterSeconds);
        Assert.Equal(5.75, result.ExitAfterSeconds);
        Assert.Equal(5, result.ClosestAfterSeconds);
        Assert.Equal(0, result.ClosestOffsetXKm);
        Assert.Equal(2, result.ClosestOffsetYKm);
        Assert.Equal(57.5, result.DangerStartX);
        Assert.Equal(20, result.DangerStartY);
        Assert.Equal(42.5, result.DangerEndX);
        Assert.Equal(20, result.DangerEndY);
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(25, false)]
    [InlineData(30, false)]
    public void Parallel_stationary_and_diverging_motion_are_finite(double offsetWorld, bool inside)
    {
        var parallel = BlastThreatMath.Evaluate(0, 0, 1, 0, 0, offsetWorld, 1, 0, 1, 2);
        Assert.Equal(inside, parallel.IsThreat);
        Assert.Equal(0, parallel.ClosestAfterSeconds);
        Assert.Equal(offsetWorld / 10, parallel.ClosestOffsetYKm);
        Assert.Null(parallel.ExitAfterSeconds);
        Assert.Null(parallel.DangerEndX);
        Assert.Equal(inside ? 0d : (double?)null, parallel.EntryAfterSeconds);
        var stationary = BlastThreatMath.Evaluate(0, 0, 0, 0, 0, offsetWorld, 0, 0, 0, 2);
        Assert.Equal(parallel.IsThreat, stationary.IsThreat);
        var diverging = BlastThreatMath.Evaluate(0, 0, 0, 0, 100, offsetWorld, 1, 0, 1, 2);
        Assert.False(diverging.IsThreat);
        Assert.Equal(0, diverging.ClosestAfterSeconds);
        Assert.Null(diverging.EntryAfterSeconds);
        Assert.True(double.IsFinite(diverging.ClosestOffsetXKm));
        Assert.True(double.IsFinite(diverging.ClosestOffsetYKm));
    }

    [Fact]
    public void Tangent_is_not_a_penetration_and_inside_diverging_still_has_remaining_danger()
    {
        var tangent = BlastThreatMath.Evaluate(0, 0, 0, 0, 100, 25, -1, 0, 1, 2);
        Assert.False(tangent.IsThreat);
        Assert.Equal(10, tangent.ClosestAfterSeconds);
        Assert.Null(tangent.EntryAfterSeconds);
        var inside = BlastThreatMath.Evaluate(0, 0, 0, 0, 10, 0, 1, 0, 1, 2);
        Assert.True(inside.IsThreat);
        Assert.Equal(0, inside.EntryAfterSeconds);
        Assert.Equal(1.5, inside.ExitAfterSeconds);
        Assert.Equal(0, inside.ClosestAfterSeconds);
        Assert.Equal(25, inside.DangerEndX);
    }

    [Fact]
    public void World_translation_and_velocity_rotation_preserve_times()
    {
        var result = BlastThreatMath.Evaluate(200, -100, 0, 1, 180, 0, 0, -2, 1, 2);
        Assert.True(result.IsThreat);
        Assert.Equal(4.25, result.EntryAfterSeconds);
        Assert.Equal(5.75, result.ExitAfterSeconds);
        Assert.Equal(5, result.ClosestAfterSeconds);
        Assert.Equal(-2, result.ClosestOffsetXKm);
        Assert.Equal(0, result.ClosestOffsetYKm);
        Assert.Equal(180, result.DangerStartX);
        Assert.Equal(-42.5, result.DangerStartY);
    }

    [Fact]
    public void Threat_uses_observed_unbounded_trace_without_foreign_endpoint()
    {
        var far = BlastThreatMath.Evaluate(0, 0, 0, 0, 10000, 0, -1, 0, 1, 0.5);
        Assert.True(far.IsThreat);
        Assert.Equal(999, far.EntryAfterSeconds);
        Assert.Equal(1000, far.ClosestAfterSeconds);
        Assert.Equal(1001, far.ExitAfterSeconds);
        Assert.DoesNotContain(typeof(BlastThreatResult).GetProperties(), p =>
            p.Name.Contains("Detonation", StringComparison.Ordinal) || p.Name.Contains("Deadline", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_geometry_fails_without_nan_results()
    {
        Assert.Throws<ArgumentException>(() => BlastThreatMath.Evaluate(0, 0, 0, 0, 1, 0, 0, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastThreatMath.Evaluate(0, 0, 0, 0, 1, 0, 1, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastThreatMath.Evaluate(0, 0, 0, 0, 1, 0, 1, 0, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastThreatMath.Evaluate(double.NaN, 0, 0, 0, 1, 0, 1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BlastThreatMath.Evaluate(0, 0, 0, 0, 1, 0, double.PositiveInfinity, 0, 1, 1));
    }
}
