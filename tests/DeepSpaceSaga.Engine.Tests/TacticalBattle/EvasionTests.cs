using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class EvasionTests
{
    [Fact]
    public void Evading_drone_does_not_fire_and_draws_no_random()
    {
        var (battle, a, b) = SalvoTests.Arena();
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => new(null, d == a));
        Assert.Empty(salvo.Shots);
        Assert.Equal(TacticalFireSkipReason.Evading, salvo.Steps.Single(s => s.ShooterId == a.Id).SkipReason);
        Assert.Equal(90, a.Consciousness);
        Assert.Equal(100, b.Consciousness);
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Equal(1, battle.Statistics.Evasions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(180)]
    public void Every_shot_at_evading_drone_gets_penalty_including_rear_and_carrier(int course)
    {
        var (original, a, b) = SalvoTests.Arena(damage: 1);
        var catalog = original.Catalog with { Rules = original.Catalog.Rules with
        { Hit = original.Catalog.Rules.Hit with { MinChance = 5, MaxChance = 95 } } };
        var battle = new Battle(catalog, "initial", 1, original.Drones, original.Carriers, original.Tactics, original.Random);
        battle.InitializeFireOrder();
        b.Pose = b.Pose with { Course = course };
        double raw = TacticalBattleSalvo.Candidates(battle, a).Single(c => c.Target.Id == b.Id).Terms.Raw;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == b ? new(null, true) : new(b.Id, false));
        var shot = Assert.Single(salvo.Shots);
        Assert.True(shot.TargetEvading);
        Assert.Equal(Math.Clamp(raw - 20, 5, 95) / 100, shot.Chance);
        Assert.Equal(1, battle.Statistics.ShotsAtEvading);
        Assert.True(TacticalBattleSnapshotBuilder.Shot(shot, 1).TargetEvading);
        // Carriers cannot evade: their candidate terms never carry the drone-only penalty.
        battle.Carrier("B").Pose = b.Pose;
        var carrier = TacticalBattleSalvo.Candidates(battle, a, new HashSet<string> { battle.Carrier("B").Id })
            .Single(c => c.Target == battle.Carrier("B"));
        Assert.Equal(0, carrier.Terms.Evasion);
    }

    [Fact]
    public void Cost_is_charged_at_plan_freeze_even_if_destroyed_before_step()
    {
        var (battle, a, b) = SalvoTests.Arena(damage: 100, hull: 100);
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == b ? new(null, true) : new(b.Id, false));
        Assert.Equal(90, b.Consciousness);
        Assert.Equal(TacticalDroneStatus.Destroyed, b.Status);
        Assert.Equal(TacticalFireSkipReason.BattleEnded, salvo.Steps.Single(s => s.ShooterId == b.Id).SkipReason);
    }

    [Theory]
    [InlineData(50, 100, false)]
    [InlineData(49, 10, true)]
    [InlineData(49, 9, false)]
    public void Ai_evades_strictly_below_half_hull_with_enough_consciousness(int hull, int mind, bool expected)
    {
        var (battle, a, _) = SalvoTests.Arena();
        a.Hull = hull;
        a.Consciousness = mind;
        Assert.Equal(expected, TacticalBattleSalvo.DefaultPlan(battle, a, TacticalBattleSalvo.Candidates(battle, a)).Evade);
    }

    [Fact]
    public void Ai_does_not_evade_below_min_consciousness()
    {
        var (battle, a, _) = SalvoTests.Arena();
        a.Hull = 40;
        a.Consciousness = 9;
        Assert.False(TacticalBattleSalvo.DefaultPlan(battle, a, []).Evade);
        long calls = battle.Random.Calls;
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle, (d, _) => new(null, true)));
        Assert.Equal(9, a.Consciousness);
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Equal(0, battle.Statistics.Evasions);
    }

    [Fact]
    public void Evading_skip_reason_priority()
    {
        var (battle, a, _) = SalvoTests.Arena();
        a.Jammed = true;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => new(null, d == a));
        Assert.Equal(TacticalFireSkipReason.Evading, salvo.Steps.Single(s => s.ShooterId == a.Id).SkipReason);
        Assert.Equal(90, a.Consciousness);
    }
}
