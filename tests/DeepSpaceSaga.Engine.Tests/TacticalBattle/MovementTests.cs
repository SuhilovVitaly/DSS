using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class MovementTests
{
    [Fact]
    public void EveryPublishedMovementSliceMatchesAtRoundedInputPrecision()
    {
        var battle = Battle.Create(AssemblyTests.WithoutModules(), "initial", 1);
        using var trace = ReferenceFixtureTests.Open("trace-seed-1");
        int compared = 0;
        foreach (var turn in trace.RootElement.GetProperty("data").GetProperty("turns").EnumerateArray())
        {
            foreach (var order in turn.GetProperty("orders").EnumerateArray())
            {
                var drone = battle.Drones.First(d => d.Id == order.GetProperty("id").GetString());
                drone.Order = new(order.GetProperty("speed").GetDouble(), order.GetProperty("turn").GetDouble());
            }
            var paths = TacticalBattleMovement.Move(battle);
            Assert.Equal(turn.GetProperty("positions").GetArrayLength(), paths.Length);
            foreach (var expected in turn.GetProperty("positions").EnumerateArray())
            {
                var drone = battle.Drones.First(d => d.Id == expected.GetProperty("id").GetString());
                double x = expected.GetProperty("x").GetDouble(), y = expected.GetProperty("y").GetDouble(), course = expected.GetProperty("course").GetDouble();
                // Each slice starts from rounded prior positions/course and uses rounded
                // speed/turn. At <=120 units, their half-micro-unit errors propagate
                // to <3 micro-units in XY, <1.5 micro-degrees in final course.
                Assert.InRange(Math.Abs(drone.Pose.X - x), 0, 0.000003);
                Assert.InRange(Math.Abs(drone.Pose.Y - y), 0, 0.000003);
                Assert.InRange(Math.Abs(TacticalGeometry.Norm(drone.Pose.Course - course)), 0, 0.0000015);
                Assert.Equal(5, paths.First(p => p.DroneId == drone.Id).Path.Length);
                drone.Pose = new(x, y, course); // isolate next movement slice from accumulated source rounding
                var sourceOrder = turn.GetProperty("orders").EnumerateArray().First(o => o.GetProperty("id").GetString() == drone.Id);
                drone.Speed = sourceOrder.GetProperty("speed").GetDouble();
                compared++;
            }
            foreach (var state in turn.GetProperty("states").EnumerateArray())
            {
                var drone = battle.Drones.FirstOrDefault(d => d.Id == state.GetProperty("id").GetString());
                if (drone is null) continue;
                drone.Status = state.GetProperty("status").GetString() switch
                {
                    "air" => TacticalDroneStatus.Air,
                    "disabled" => TacticalDroneStatus.Disabled,
                    "destroyed" => TacticalDroneStatus.Destroyed,
                    _ => throw new InvalidOperationException()
                };
                var factors = state.GetProperty("engines");
                for (int i = 0; i < factors.GetArrayLength(); i++) drone.Engines[i].Factor = factors[i].GetDouble();
            }
        }
        Assert.True(compared > 100);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void InactiveUnitsAndCarriersStayStillAndMovementDoesNotUseRandom()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        battle.Drones[0].Status = TacticalDroneStatus.Disabled;
        battle.Drones[1].Status = TacticalDroneStatus.Destroyed;
        var positions = battle.Drones.Take(2).Select(d => d.Pose).ToArray();
        var carriers = battle.Carriers.Select(c => c.Pose).ToArray();
        foreach (var drone in battle.Drones) drone.Order = new(1000, 1000);
        var paths = TacticalBattleMovement.Move(battle);
        Assert.Equal(8, paths.Length);
        Assert.Equal(positions, battle.Drones.Take(2).Select(d => d.Pose));
        Assert.Equal(carriers, battle.Carriers.Select(c => c.Pose));
        foreach (var drone in battle.Drones.Skip(2))
        {
            Assert.InRange(drone.Speed, drone.Limits.MinSpeed, drone.Limits.MaxSpeed);
            Assert.Equal(drone.Limits.TurnLimit(drone.Speed, battle.Catalog.Rules.TurnAtMaxSpeedFraction), drone.PreviousTurn);
        }
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void InvalidLaterOrderRejectsMovementAtomically()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        var before = battle.Drones.Select(d => (d.Pose, d.Speed, d.PreviousTurn)).ToArray();
        battle.Drones[^1].Order = new(double.NaN, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalBattleMovement.Move(battle));
        Assert.Equal(before, battle.Drones.Select(d => (d.Pose, d.Speed, d.PreviousTurn)));
        Assert.Equal(42, battle.Random.Calls);
    }
}
