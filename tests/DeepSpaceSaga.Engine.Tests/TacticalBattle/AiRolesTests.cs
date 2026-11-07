using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AiRolesTests
{
    [Fact]
    public void StrikerUsesStrictBreakInclusiveExtendAndRetainsHysteresis()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Ai = c.Ai with { Pursuit = c.Ai.Pursuit with { SlowForTurn = false } } };
        var battle = Battle.Create(c, "initial", 1);
        var d = battle.Drones[0];
        battle.Carrier("A").Pose = battle.Carrier("B").Pose = new(0, 0, 0);
        d.Pose = new(40, 0, 0);
        TacticalRoles.Striker(battle, d);
        Assert.Equal("in", d.StrikeMode);
        d.Pose = new(30, 0, 0);
        var retreat = TacticalRoles.Striker(battle, d);
        Assert.Equal("out", d.StrikeMode);
        Assert.Equal(d.Limits.ClampSpeed(d.Limits.MaxSpeed, d.Speed), retreat.Speed);
        Assert.Equal(0, retreat.Turn); // retreat point is (extend+break, 0), ahead of the drone
        d.Pose = new(100, 0, 0);
        TacticalRoles.Striker(battle, d);
        Assert.Equal("out", d.StrikeMode);
        d.Pose = new(140, 0, 0);
        var attack = TacticalRoles.Striker(battle, d);
        Assert.Equal("in", d.StrikeMode);
        Assert.Equal(50, attack.Speed);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void FighterBreakAndExtendAreStraightWithoutLeashAndEmitOnlyActualBreak()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Ai = c.Ai with { Fighter = c.Ai.Fighter with { ExtendTurns = 3 } } };
        var battle = Battle.Create(c, "initial", 1);
        var d = battle.Drones[0];
        d.Pose = new(1e6, 1e6, 90);
        d.Dry = 3;
        Assert.Equal(0, TacticalRoles.Fighter(battle, d).Turn);
        Assert.Equal(0, d.Dry);
        Assert.Equal(2, d.Extend);
        Assert.IsType<TacticalFighterBreak>(Assert.Single(battle.AiEvents));
        Assert.Equal(0, TacticalRoles.Fighter(battle, d).Turn);
        Assert.Equal(1, d.Extend);
        Assert.Equal(0, TacticalRoles.Fighter(battle, d).Turn);
        Assert.Equal(0, d.Extend);
        Assert.Single(battle.AiEvents);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void HuntSurvivesRoleChangesAndIsReplacedOnlyWhenTargetLeavesBattle()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        var d = battle.Drones[0];
        d.Pose = new(450, 200, 0);
        var kept = battle.Drones[4];
        var closer = battle.Drones[5];
        foreach (var e in battle.Drones.Where(e => e.Side == "B")) e.Status = TacticalDroneStatus.Disabled;
        kept.Status = closer.Status = TacticalDroneStatus.Air;
        kept.Pose = new(550, 200, 180);
        closer.Pose = new(500, 200, 180);
        d.Hunt = kept.Id;
        TacticalRoles.Fighter(battle, d);
        Assert.Equal(kept.Id, d.Hunt);
        d.Role = "striker";
        TacticalRoles.Striker(battle, d);
        d.Role = "fighter";
        TacticalRoles.Fighter(battle, d);
        Assert.Equal(kept.Id, d.Hunt);
        kept.Status = TacticalDroneStatus.Disabled;
        TacticalRoles.Fighter(battle, d);
        Assert.Equal(closer.Id, d.Hunt);
        Assert.Equal(48, battle.Random.Calls);
    }

    [Fact]
    public void TailPreservesFirstEqualScoreAcrossEnemyOrderAndDeduplicatesClampedSpeeds()
    {
        var (battle, drone, turn) = TailArena();
        long calls = battle.Random.Calls;
        var order = TacticalTail.Find(battle, drone)!.Value;
        Assert.Equal(50, order.Speed);
        Assert.Equal(-turn, order.Turn);
        Assert.Equal(calls + 1, battle.Random.Calls);
        var (reversed, other, otherTurn) = TailArena(reverse: true);
        var reversedOrder = TacticalTail.Find(reversed, other)!.Value;
        Assert.Equal(otherTurn, reversedOrder.Turn);
        Assert.Equal(50, reversedOrder.Speed);
    }

    [Fact]
    public void TailUsesOneDrawForFoundButRejectedCandidateAndNoneForJammedOrNoCandidate()
    {
        var (battle, drone, _) = TailArena(chance: 0);
        long calls = battle.Random.Calls;
        Assert.Null(TacticalTail.Find(battle, drone));
        Assert.Equal(calls + 1, battle.Random.Calls);
        drone.Jammed = true;
        Assert.Null(TacticalTail.Find(battle, drone));
        Assert.Equal(calls + 1, battle.Random.Calls);
        drone.Jammed = false;
        foreach (var e in battle.Drones.Where(e => e.Side == "B")) e.Pose = e.Pose with { X = 1e9 };
        Assert.Null(TacticalTail.Find(battle, drone));
        Assert.Equal(calls + 1, battle.Random.Calls);
    }

    [Fact]
    public void TakesTailFlagAppliesOnlyToEffectiveStrikerAndBaseManeuverRunsFirst()
    {
        var (battle, d, _) = TailArena(takesTail: false);
        var proposed = new TacticalOrder(50, 0);
        long calls = battle.Random.Calls;
        Assert.Equal(proposed, TacticalTail.Apply(battle, d, "striker", proposed));
        Assert.Equal(calls, battle.Random.Calls);
        Assert.NotEqual(proposed, TacticalTail.Apply(battle, d, "fighter", proposed));
        Assert.Equal(calls + 1, battle.Random.Calls);
        // The fighter still records hunt and consumes pursuit draws even when tail replaces its order.
        var baseOrder = TacticalRoles.Fighter(battle, d);
        Assert.NotNull(d.Hunt);
        Assert.Equal(calls + 3, battle.Random.Calls);
        TacticalTail.Apply(battle, d, "fighter", baseOrder);
        Assert.Equal(calls + 4, battle.Random.Calls);
    }

    private static (Battle Battle, TacticalDrone Drone, double Turn) TailArena(bool reverse = false, double chance = 1, bool takesTail = true)
    {
        var c = AssemblyTests.Catalog();
        c = c with
        {
            Ai = c.Ai with { Tail = new(chance, 1, 1, 1, 1), Striker = c.Ai.Striker with { TakesTail = takesTail } },
            Frames = new(c.Frames.Entries.Select(e => (e.Id, e.Id == "heavy" ? e.Value with { MinSpeed = 50, MaxSpeedCap = 50 } : e.Value))),
            Weapons = new(c.Weapons.Entries.Select(e => (e.Id, e.Id == "gun.sword.t1" ? e.Value with { Range = 100, ArcDeg = 180 } : e.Value)))
        };
        if (reverse)
        {
            var scenario = c.Scenarios["initial"];
            var side = scenario.Sides[1];
            scenario = scenario with { Sides = scenario.Sides.SetItem(1, side with { Drones = side.Drones.SetItem(0, side.Drones[1]).SetItem(1, side.Drones[0]) }) };
            c = c with { Scenarios = new([("initial", scenario)]) };
        }
        var battle = Battle.Create(c, "initial", 1);
        var d = battle.Drones[0];
        d.Pose = new(0, 0, 0);
        d.Speed = 50;
        double turn = d.Limits.TurnLimit(50, 0.5);
        var end = TacticalGeometry.Integrate(d.Pose, 50, -turn, 4);
        var offset = TacticalGeometry.Vec(end.Course, 10);
        foreach (var e in battle.Drones.Where(e => e.Side == "B")) e.Status = TacticalDroneStatus.Disabled;
        var negative = battle.Drones.First(e => e.Id == "Таг-1");
        var positive = battle.Drones.First(e => e.Id == "Таг-2");
        negative.Status = positive.Status = TacticalDroneStatus.Air;
        negative.Pose = new(end.X + offset.X, end.Y + offset.Y, end.Course);
        positive.Pose = new(negative.Pose.X, -negative.Pose.Y, -negative.Pose.Course);
        negative.Speed = positive.Speed = 0;
        return (battle, d, turn);
    }
}
