using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AiSteeringTests
{
    [Fact]
    public void LeashUsesCarrierMidpointAndKeepsPassedSpeed()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Ai = c.Ai with { Leash = new(10), Pursuit = c.Ai.Pursuit with { SlowForTurn = false } } };
        var battle = Battle.Create(c, "initial", 1);
        var drone = battle.Drones[0];
        drone.Pose = new(0, 0, 0);
        var order = TacticalSteering.Steer(battle, drone, 0, 1000, 70);
        Assert.Equal(70, order.Speed);
        Assert.Equal(TacticalGeometry.Norm(TacticalGeometry.Bearing(0, 0, 450, 200)), order.Turn);
        Assert.NotEqual(TacticalGeometry.Bearing(0, 0, 110, 250), order.Turn);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void SlowForTurnUsesRecursiveSpeedAndStopsAtFirstSufficientTurnLimit()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        var drone = battle.Drones[0];
        var midpoint = battle.Midpoint;
        drone.Pose = new(midpoint.X, midpoint.Y, 0);
        var limits = drone.Limits;
        drone.Speed = limits.MaxSpeed;
        var delta = TacticalGeometry.Vec(33, 100);
        var order = TacticalSteering.Steer(battle, drone, midpoint.X + delta.X, midpoint.Y + delta.Y, limits.MaxSpeed);
        double lower = limits.ClampSpeed(drone.Speed - limits.Change, drone.Speed);
        double first = drone.Speed + (lower - drone.Speed) * 1 / 4.0;
        double second = first + (lower - first) * 2 / 4.0;
        Assert.True(limits.TurnLimit(first, 0.5) < 33);
        Assert.True(limits.TurnLimit(second, 0.5) >= 33);
        Assert.Equal(second, order.Speed);
        Assert.NotEqual(drone.Speed + (lower - drone.Speed) * 2 / 4.0, order.Speed);
        Assert.InRange(order.Turn, 32.999999999999, 33.000000000001);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void EqualMaximumAndMinimumSpeedUsesFullTurnWithoutDivisionByZero()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Frames = new(c.Frames.Entries.Select(e => (e.Id, e.Id == "heavy" ? e.Value with { MaxSpeedCap = e.Value.MinSpeed } : e.Value))) };
        var battle = Battle.Create(c, "initial", 1);
        var drone = battle.Drones[0];
        drone.Pose = new(450, 200, 0);
        var order = TacticalSteering.Steer(battle, drone, 450, 300, 50);
        Assert.Equal(50, order.Speed);
        Assert.Equal(drone.Limits.Turn, order.Turn);
        Assert.True(double.IsFinite(order.Turn));
    }

    [Fact]
    public void PredictionUsesCurrentSpeedAndUnchangedCourseWithoutRng()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        var enemy = battle.Drones[4];
        enemy.Pose = new(10, 20, 90);
        enemy.Speed = 1000; // current speed can exceed new damaged-engine limits until next order
        var predicted = TacticalSteering.Predict(enemy);
        Assert.Equal(10 + Math.Cos(90 * 0.017453292519943295) * 1000, predicted.X);
        Assert.Equal(1020, predicted.Y);
        Assert.Equal(90, predicted.Course);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void FirstPursuitMatchesIndependentTraceOrderAndConsumesExactlyXThenYDraws()
    {
        var battle = Battle.Create(AssemblyTests.WithoutModules(), "initial", 1);
        var drone = battle.Drones[0];
        var enemy = PyMath.MinByFirst(battle.ActiveDrones("B"), e => TacticalGeometry.Dist(drone.Pose.X, drone.Pose.Y, e.Pose.X, e.Pose.Y));
        var order = TacticalSteering.Pursue(battle, drone, enemy);
        using var trace = ReferenceFixtureTests.Open("trace-seed-1");
        var expected = trace.RootElement.GetProperty("data").GetProperty("turns")[0].GetProperty("orders")[0];
        Assert.InRange(Math.Abs(expected.GetProperty("speed").GetDouble() - order.Speed), 0, 0.0000005);
        Assert.InRange(Math.Abs(expected.GetProperty("turn").GetDouble() - order.Turn), 0, 0.0000005);
        Assert.Equal(44, battle.Random.Calls);
    }

    [Theory]
    [InlineData("fighter", "striker")]
    [InlineData("interceptor", "striker")]
    [InlineData("striker", "striker")]
    public void EffectiveRoleIgnoresInactiveEnemiesWithoutChangingStoredRoleOrHunt(string role, string expected)
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        var drone = battle.Drones[0];
        drone.Role = role;
        drone.Hunt = "Таг-1";
        foreach (var enemy in battle.Drones.Where(d => d.Side == "B")) enemy.Status = TacticalDroneStatus.Disabled;
        Assert.Equal(expected, TacticalSteering.EffectiveRole(battle, drone));
        Assert.Equal(role, drone.Role);
        Assert.Equal("Таг-1", drone.Hunt);
        battle.Drones[4].Status = TacticalDroneStatus.Air;
        Assert.Equal(role, TacticalSteering.EffectiveRole(battle, drone));
        Assert.Equal(42, battle.Random.Calls);
    }
}
