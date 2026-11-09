using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AiCommanderTests
{
    [Fact]
    public void ThreatsUseVmaxInclusiveBoundariesAndStableDistanceOrder()
    {
        var battle = Arena();
        var enemy = battle.Drones[4];
        enemy.Pose = new(300, 0, 180);
        enemy.Speed = 1;
        Assert.Same(enemy, Assert.Single(TacticalCommander.Threats(battle, "A")));
        double reach = 5 * enemy.Limits.MaxSpeed;
        enemy.Pose = new(reach, 0, 180);
        Assert.Single(TacticalCommander.Threats(battle, "A"));
        enemy.Pose = new(reach + 0.000001, 0, 180);
        Assert.Empty(TacticalCommander.Threats(battle, "A"));
        enemy.Pose = new(250, 0, 0); // radius qualifies even when flying away
        Assert.Single(TacticalCommander.Threats(battle, "A"));
        enemy.Pose = new(250.000001, 0, 0);
        Assert.Empty(TacticalCommander.Threats(battle, "A"));
        enemy.Pose = new(100, 0, 0);
        var tied = battle.Drones[5];
        tied.Status = TacticalDroneStatus.Air;
        tied.Pose = new(-100, 0, 0);
        Assert.Equal(new[] { enemy.Id, tied.Id }, TacticalCommander.Threats(battle, "A").Select(d => d.Id));
        enemy.Status = TacticalDroneStatus.Disabled;
        Assert.Same(tied, Assert.Single(TacticalCommander.Threats(battle, "A")));
    }

    [Fact]
    public void EnemyNearCarrierTriggersPursuitOfFirstNearestToCarrier()
    {
        var battle = Arena();
        var control = Arena();
        foreach (var b in new[] { battle, control })
        {
            b.Drones[0].Pose = new(500, 0, 0);
            b.Drones[4].Pose = new(100, 0, 180);
            b.Drones[5].Pose = new(90, 0, 180);
            b.Drones[5].Status = TacticalDroneStatus.Air;
        }
        var expected = TacticalSteering.Pursue(control, control.Drones[0], control.Drones[5]);
        Assert.Equal(expected, TacticalCommander.Interceptor(battle, battle.Drones[0]));
        Assert.Equal(44, battle.Random.Calls);
    }

    [Fact]
    public void EngageHoldAndScreenBranchesUseSpecifiedTargetsSpeedsAndDraws()
    {
        var battle = Arena();
        var control = Arena();
        battle.Drones[0].Pose = control.Drones[0].Pose = new(200, 0, 0);
        Assert.Equal(TacticalSteering.Pursue(control, control.Drones[0], control.Drones[4]), TacticalCommander.Interceptor(battle, battle.Drones[0]));
        Assert.Equal(44, battle.Random.Calls);
        battle = Arena();
        var d = battle.Drones[0];
        d.Pose = new(90, 0, 0);
        Assert.Equal(TacticalSteering.Steer(battle, d, 300, 0, d.Limits.MinSpeed), TacticalCommander.Interceptor(battle, d));
        Assert.Equal(50, TacticalCommander.Interceptor(battle, d).Speed);
        d.Pose = new(0, 200, 0);
        Assert.Equal(TacticalSteering.Steer(battle, d, 90, 0, d.Limits.MaxSpeed), TacticalCommander.Interceptor(battle, d));
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void ScreenFlankSignFollowsIndexAmongActiveInterceptors()
    {
        var battle = Arena(flank: 50);
        var first = battle.Drones[0];
        var second = battle.Drones[1];
        first.Role = second.Role = "interceptor";
        first.Pose = second.Pose = new(90, 0, 0);
        Assert.True(TacticalCommander.Interceptor(battle, first).Turn > 0);
        Assert.True(TacticalCommander.Interceptor(battle, second).Turn < 0);
        first.Status = TacticalDroneStatus.Disabled;
        Assert.True(TacticalCommander.Interceptor(battle, second).Turn > 0);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void PatrolStepUsesRawPatrolSpeedBeforeSteerClampsIt()
    {
        var battle = Arena(patrolSpeed: 200);
        var d = battle.Drones[0];
        d.Pose = new(70, 0, 100);
        d.Speed = 50;
        battle.Drones[4].Pose = new(1e9, 0, 0);
        var tuning = battle.Catalog.Ai.Interceptor;
        double rawStep = Math.Min(tuning.PatrolOrbitStepDeg, tuning.OrbitTurnFraction * d.Limits.TurnLimit(200, 0.5));
        var point = TacticalGeometry.Vec(rawStep, 70);
        var expected = TacticalSteering.Steer(battle, d, point.X, point.Y, 200);
        var actual = TacticalCommander.Interceptor(battle, d);
        Assert.Equal(expected, actual);
        double wrongStep = Math.Min(tuning.PatrolOrbitStepDeg, tuning.OrbitTurnFraction * d.Limits.TurnLimit(d.Limits.ClampSpeed(200, 50), 0.5));
        var wrong = TacticalGeometry.Vec(wrongStep, 70);
        Assert.NotEqual(TacticalSteering.Steer(battle, d, wrong.X, wrong.Y, 200).Turn, actual.Turn);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void CommanderLocksRoleChangesUntilMinimumTurnsAndMayKeepDifferentActualCounts()
    {
        var battle = Arena();
        foreach (var d in battle.Drones.Where(d => d.Side == "A")) d.Role = "fighter";
        battle.Turn = 1;
        TacticalCommander.Assign(battle, "A");
        Assert.All(battle.ActiveDrones("A"), d => Assert.Equal("fighter", d.Role));
        Assert.Equal(0, battle.RoleChanges);
        battle.Turn = 3;
        TacticalCommander.Assign(battle, "A");
        Assert.Equal(2, battle.ActiveDrones("A").Count(d => d.Role == "interceptor"));
        Assert.Equal(2, battle.ActiveDrones("A").Count(d => d.Role == "striker"));
        Assert.Equal(4, battle.RoleChanges);
        Assert.All(battle.ActiveDrones("A"), d => Assert.Equal(3, d.RoleSince));
        var roles = battle.ActiveDrones("A").Select(d => d.Role).ToArray();
        battle.Drones[4].Pose = new(1e9, 0, 0);
        battle.Turn = 4;
        battle.AiEvents.Clear();
        TacticalCommander.Assign(battle, "A");
        Assert.Equal(roles, battle.ActiveDrones("A").Select(d => d.Role));
        Assert.Empty(battle.AiEvents);
        Assert.Equal(4, battle.RoleChanges);
    }

    [Fact]
    public void CommanderPrefersExistingRoleBeforeDistance()
    {
        var battle = Arena();
        battle.Drones[0].Role = battle.Drones[1].Role = "interceptor";
        battle.Drones[0].Pose = new(900, 0, 0);
        battle.Drones[1].Pose = new(800, 0, 0);
        battle.Drones[2].Pose = new(0, 0, 0);
        battle.Drones[3].Pose = new(10, 0, 0);
        battle.Turn = 3;
        TacticalCommander.Assign(battle, "A");
        Assert.Equal("interceptor", battle.Drones[0].Role);
        Assert.Equal("interceptor", battle.Drones[1].Role);
        Assert.Equal(0, battle.Drones[0].RoleSince);
        Assert.Equal(2, battle.RoleChanges);
    }

    [Fact]
    public void WeakCarrierThresholdIsStrictAndChoosesOneStrikerWhenOutnumbered()
    {
        var battle = Arena();
        foreach (var d in battle.ActiveDrones("A")) d.Role = "fighter";
        foreach (var d in battle.Drones.Where(d => d.Side == "B"))
        {
            d.Status = TacticalDroneStatus.Air;
            d.Pose = new(1e9, 0, 0);
        }
        battle.Drones[0].Pose = new(900, 0, 0);
        battle.Carrier("B").Hull = 200;
        battle.Turn = 3;
        TacticalCommander.Assign(battle, "A");
        Assert.DoesNotContain(battle.ActiveDrones("A"), d => d.Role == "striker");
        battle.Carrier("B").Hull = 199;
        TacticalCommander.Assign(battle, "A");
        Assert.Same(battle.Drones[0], Assert.Single(battle.ActiveDrones("A"), d => d.Role == "striker"));
        Assert.Equal(1, battle.RoleChanges);
    }

    [Fact]
    public void AllInterceptorsSwitchImmediatelyWithTypedReasonAndPreserveHunt()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Ai = c.Ai with { Commander = c.Ai.Commander with { MinRoleTurns = 100 } } };
        var battle = Battle.Create(c, "initial", 1);
        foreach (var d in battle.ActiveDrones("A")) { d.Role = "interceptor"; d.Hunt = "Таг-1"; }
        battle.Planning(TacticalBattleAi.Plan);
        Assert.All(battle.ActiveDrones("A"), d => { Assert.Equal("fighter", d.Role); Assert.Equal(1, d.RoleSince); Assert.Equal("Таг-1", d.Hunt); });
        Assert.Equal(4, battle.RoleChanges);
        var events = battle.AiEvents.OfType<TacticalRoleChanged>().ToArray();
        Assert.Equal(4, events.Length);
        Assert.All(events, e => Assert.True(e.NoAttackers));
    }

    [Fact]
    public void WholeFirstPlanningStageMatchesEveryIndependentOrderAndCallCount()
    {
        var battle = Battle.Create(AssemblyTests.WithoutModules(), "initial", 1);
        var positions = battle.Drones.Select(d => d.Pose).ToArray();
        battle.Planning(TacticalBattleAi.Plan);
        using var trace = ReferenceFixtureTests.Open("trace-seed-1");
        var turn = trace.RootElement.GetProperty("data").GetProperty("turns")[0];
        Assert.Equal(turn.GetProperty("planningCalls").GetInt32(), battle.Random.Calls);
        var orders = turn.GetProperty("orders");
        for (int i = 0; i < battle.Drones.Length; i++)
        {
            Assert.Equal(orders[i].GetProperty("id").GetString(), battle.Drones[i].Id);
            Assert.Equal(orders[i].GetProperty("role").GetString(), battle.Drones[i].Role);
            Assert.InRange(Math.Abs(orders[i].GetProperty("speed").GetDouble() - battle.Drones[i].Order.Speed), 0, 0.0000005);
            Assert.InRange(Math.Abs(orders[i].GetProperty("turn").GetDouble() - battle.Drones[i].Order.Turn), 0, 0.0000005);
        }
        Assert.Equal(positions, battle.Drones.Select(d => d.Pose));
        Assert.Empty(battle.AiEvents);
        Assert.Equal(TacticalTurnPhase.Movement, battle.Phase);
    }

    private static Battle Arena(double flank = 0, double patrolSpeed = 65)
    {
        var c = AssemblyTests.Catalog();
        c = c with
        {
            Ai = c.Ai with
            {
                Pursuit = c.Ai.Pursuit with { SlowForTurn = false },
                Leash = new(1e8),
                Interceptor = c.Ai.Interceptor with { FlankOffset = flank, PatrolSpeed = patrolSpeed }
            }
        };
        var battle = Battle.Create(c, "initial", 1);
        battle.Carrier("A").Pose = new(0, 0, 0);
        battle.Carrier("B").Pose = new(1000, 0, 180);
        for (int i = 0; i < 4; i++) battle.Drones[i].Pose = new(i * 10, 50, 0);
        battle.Drones[0].Role = "interceptor";
        battle.Drones[0].Pose = new(90, 0, 0);
        foreach (var d in battle.Drones.Where(d => d.Side == "B")) d.Status = TacticalDroneStatus.Disabled;
        battle.Drones[4].Status = TacticalDroneStatus.Air;
        battle.Drones[4].Pose = new(300, 0, 180);
        battle.Drones[4].Speed = 1;
        return battle;
    }
}
