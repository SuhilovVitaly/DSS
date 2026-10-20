using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0006TK0002Tests
{
    private static ObjectMotionSnapshot State(double end = 1000.5) => new("player", 0, 0, 6, 90,
        ActiveEngineCommandType: ManeuverCommandTypes.Evade,
        Evasion: new(0, end, "torpedo", EvadeSide.Left, new("player", 0, 0, 2, 0)));

    [Fact]
    public void Evasion_is_piecewise_and_fractional_return_does_not_extend_full_speed()
    {
        var state = State();
        var before = CurrentManeuverForecast.Sample(state, 500);
        Assert.Equal(30, before.X, 10); Assert.Equal(0, before.Y, 10);
        Assert.Equal(500, before.Evasion!.SampleMotionTimeMs);
        var after = CurrentManeuverForecast.Sample(state, 2000);
        Assert.Equal(60.03, after.X, 9); Assert.Equal(-19.99, after.Y, 9);
        Assert.Equal(2, after.SpeedKmS); Assert.Equal(0, after.Direction);
        Assert.Null(after.Evasion);
        var partitioned = CurrentManeuverForecast.Sample(CurrentManeuverForecast.Sample(state, 1000.75), 999.25);
        Assert.Equal(after.X, partitioned.X, 9); Assert.Equal(after.Y, partitioned.Y, 9);
        var predicted = new LinearMotionPredictor().Predict(state, 2000);
        Assert.Equal(after.X, predicted.X, 9); Assert.Equal(after.Y, predicted.Y, 9);
        Assert.False(LinearMotionPredictor.TryPredictLinearPosition(state, 2000, out _, out _));
    }

    [Fact]
    public void Evasion_rejects_recursive_or_wrong_actor_base()
    {
        var state = State();
        Assert.Throws<ArgumentException>(() => CurrentManeuverForecast.Sample(state with
        { Evasion = state.Evasion! with { BaseMotion = null! } }, 2000));
        Assert.Throws<ArgumentException>(() => CurrentManeuverForecast.Sample(state with
        { Evasion = state.Evasion! with { BaseMotion = state } }, 2000));
        Assert.Throws<ArgumentException>(() => CurrentManeuverForecast.Sample(state with
        { Evasion = state.Evasion! with { BaseMotion = new("other", 0, 0, 0, 0) } }, 2000));
    }

    [Fact]
    public void Resumed_orbit_applies_the_turn_at_the_exact_boundary()
    {
        var orbit = new ObjectMotionSnapshot("player", 0, 0, 2, 0, ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
            TurnStepDegrees: 1, TurnStepRemainingMs: 250, TurnStepIntervalMs: 250,
            NavigationTargetX: 1000, NavigationTargetY: 1000, NavigationAngularInertiaDegPerSec: 4);
        var predicted = new LinearMotionPredictor().Predict(orbit, 250);
        Assert.Equal(1, predicted.Direction);
        Assert.Equal(250, predicted.TurnStepRemainingMs);
        Assert.Equal(0, predicted.X);
        Assert.Equal(-5, predicted.Y);
    }
}
