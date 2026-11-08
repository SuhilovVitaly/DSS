using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class DecoyTests
{
    private static (Battle Battle, TacticalDrone A, TacticalDrone B) Arena(int damage = 1, double chance = 47)
    {
        var (source, a, b) = SalvoTests.Arena(damage: damage, chance: chance);
        var catalog = source.Catalog with
        {
            Rules = source.Catalog.Rules with
            { Hit = source.Catalog.Rules.Hit with { MinChance = 5 } }
        };
        var battle = new Battle(catalog, "initial", 1, source.Drones, source.Carriers, source.Tactics, source.Random);
        battle.InitializeFireOrder();
        return (battle, a, b);
    }

    [Fact]
    public void Decoy_lowers_only_first_shot_in_fire_order()
    {
        var (battle, a, b) = Arena();
        var second = SalvoTests.ActivateSecondA(battle);
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d.Side == "A" ? new(b.Id, false) : new(null, false, true));
        Assert.Equal(new[] { a.Id, second.Id }, salvo.Shots.Select(s => s.Shooter.Id));
        Assert.True(salvo.Shots[0].Decoyed);
        Assert.Equal(.47, salvo.Shots[0].ChanceWithoutDecoy);
        Assert.Equal(.205, salvo.Shots[0].Chance, 12);
        Assert.False(salvo.Shots[1].Decoyed);
        Assert.Null(salvo.Shots[1].ChanceWithoutDecoy);
        Assert.Equal(.47, salvo.Shots[1].Chance);
        Assert.Equal(1, b.DecoyCharges);
        Assert.Equal(new[] { b.Id }, salvo.DecoysLaunched);
        Assert.Equal(1, battle.Statistics.DecoysLaunched);
        Assert.Equal(1, battle.Statistics.ShotsDecoyed);
        Assert.Equal(.47, battle.Statistics.DecoyChanceBefore);
        Assert.Equal(.205, battle.Statistics.DecoyChanceAfter, 12);
        Assert.Equal(2 + salvo.Shots.Count(s => s.Hit), battle.Random.Calls - calls);
        var dto = TacticalBattleSnapshotBuilder.Shot(salvo.Shots[0], 1);
        Assert.True(dto.Decoyed);
        Assert.Equal(.47, dto.ChanceWithoutDecoy);
    }

    [Fact]
    public void Skipped_step_does_not_consume_decoy()
    {
        var (original, a, b) = Arena(damage: 100, chance: 95);
        var second = SalvoTests.ActivateSecondA(original);
        a.Initiative = 70; second.Initiative = 60; b.Initiative = 80;
        var catalog = original.Catalog with
        {
            Rules = original.Catalog.Rules with
            { Hit = original.Catalog.Rules.Hit with { MinChance = 100, MaxChance = 100 } }
        };
        var battle = new Battle(catalog, "initial", 1, original.Drones, original.Carriers, original.Tactics, original.Random);
        battle.InitializeFireOrder();
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == b ? new(a.Id, false, true) : new(b.Id, false));
        Assert.Equal(TacticalFireSkipReason.ShooterDestroyed, salvo.Steps.Single(s => s.ShooterId == a.Id).SkipReason);
        Assert.True(salvo.Shots.Single(s => s.Shooter == second).Decoyed);
        Assert.Equal(1, battle.Statistics.ShotsDecoyed);
    }

    [Fact]
    public void Unused_decoy_expires_after_salvo_and_charge_is_spent()
    {
        var (battle, a, b) = Arena();
        var first = TacticalBattleSalvo.Fire(battle, (d, _) => new(null, false, d == b));
        Assert.Empty(first.Shots);
        Assert.Equal(1, b.DecoyCharges);
        Assert.Equal(1, battle.Statistics.DecoysLaunched);
        var next = TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false));
        Assert.False(Assert.Single(next.Shots).Decoyed);
        Assert.Empty(next.DecoysLaunched);
    }

    [Fact]
    public void Decoy_launch_draws_no_random()
    {
        var (battle, a, _) = Arena();
        a.Jammed = true;
        long calls = battle.Random.Calls;
        TacticalBattleSalvo.Fire(battle, (d, _) => new(null, false, d == a));
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Equal(1, a.DecoyCharges);
        Assert.Equal(0, battle.Statistics.ShotsDecoyed);
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(41, true)]
    public void Ai_launches_decoy_at_threat_threshold(int chance, bool expected)
    {
        var (battle, a, b) = Arena(chance: chance);
        var plans = new Dictionary<string, TacticalFirePlan> { [a.Id] = new(b.Id, false), [b.Id] = new(null, false) };
        long calls = battle.Random.Calls;
        var selected = TacticalBattleSalvo.AiDecoys(battle, plans, _ => true);
        Assert.Equal(expected, selected[b.Id].Decoy);
        Assert.False(selected[a.Id].Decoy);
        Assert.False(plans[b.Id].Decoy);
        Assert.Equal(2, b.DecoyCharges);
        Assert.Equal(calls, battle.Random.Calls);
    }

    [Fact]
    public void Ai_does_not_launch_when_evading_or_empty()
    {
        foreach (bool evade in new[] { false, true })
        {
            var (battle, a, b) = Arena();
            b.DecoyCharges = evade ? 2 : 0;
            var plans = new Dictionary<string, TacticalFirePlan> { [a.Id] = new(b.Id, false), [b.Id] = new(null, evade) };
            Assert.False(TacticalBattleSalvo.AiDecoys(battle, plans, _ => true)[b.Id].Decoy);
        }
    }

    [Fact]
    public void Incoming_forecast_marks_only_first_shot()
    {
        var (battle, a, b) = Arena();
        var second = battle.Drones[5];
        second.Status = TacticalDroneStatus.Air;
        second.Pose = b.Pose with { Y = 1 };
        second.Initiative = 99; // order was frozen at assembly, so B remains first
        long calls = battle.Random.Calls;
        var rows = TacticalBattleSnapshotBuilder.Incoming(battle, "A").Where(r => r.TargetId == a.Id).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(b.Id, rows[0].ShooterId);
        Assert.NotNull(rows[0].ChanceIfDecoy);
        Assert.Null(rows[1].ChanceIfDecoy);
        Assert.Equal(TacticalHitChance.WithDecoy(rows[0].Chance, 1.5, .05), rows[0].ChanceIfDecoy);
        Assert.Equal(calls, battle.Random.Calls);
    }

    [Fact]
    public void Invalid_late_decoy_plan_is_atomic()
    {
        var (battle, a, b) = Arena();
        b.DecoyCharges = 0;
        long calls = battle.Random.Calls;
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle, (_, _) => new(null, false, true)));
        Assert.Equal(2, a.DecoyCharges);
        Assert.Equal(0, b.DecoyCharges);
        Assert.Equal(0, battle.Statistics.DecoysLaunched);
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle, (_, _) => new(null, true, true)));
        Assert.Equal(100, a.Consciousness);
    }

    [Fact]
    public void Charge_is_spent_even_if_destroyed_before_own_step()
    {
        var (original, a, b) = Arena(damage: 100);
        var catalog = original.Catalog with
        {
            Rules = original.Catalog.Rules with
            { Hit = original.Catalog.Rules.Hit with { MinChance = 100, MaxChance = 100 } }
        };
        var battle = new Battle(catalog, "initial", 1, original.Drones, original.Carriers, original.Tactics, original.Random);
        battle.InitializeFireOrder();
        TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? new(b.Id, false) : new(null, false, true));
        Assert.Equal(TacticalDroneStatus.Destroyed, b.Status);
        Assert.Equal(1, b.DecoyCharges);
    }
}
