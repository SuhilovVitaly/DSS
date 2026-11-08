using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AnomalySalvoTests
{
    private static (Battle Battle, TacticalDrone A, TacticalDrone B) Arena(TacticalAnomalyKind kind, double sigma = 1e6, double chance = 100)
    {
        var (source, a, b) = SalvoTests.Arena(damage: 1, chance: chance);
        var catalog = source.Catalog with
        {
            Rules = source.Catalog.Rules with
            { Hit = source.Catalog.Rules.Hit with { MinChance = chance == 100 ? 100 : 5 } }
        };
        var battle = new Battle(catalog, source.ScenarioId, source.Seed, source.Drones, source.Carriers, source.Tactics, source.Random)
        { Anomalies = [new("zone", kind, [new(5, 0, 100, sigma, sigma, 0)])] };
        battle.InitializeFireOrder(); return (battle, a, b);
    }

    [Fact]
    public void Absorption_roll_precedes_hit_roll()
    {
        int absorbedCount = 0;
        for (int skip = 0; skip < 100; skip++)
        {
            var (battle, a, b) = Arena(TacticalAnomalyKind.Heat, 1);
            for (int i = 0; i < skip; i++) battle.Random.Random();
            var oracle = new Mulberry32(battle.Random.State);
            // Radius sqrt(-2 ln(.7))=.84460043; midpoint grid x=.025+.05k:34/200 inside.
            bool expectedAbsorbed = oracle.Random() < .17;
            if (!expectedAbsorbed) { oracle.Random(); oracle.RandInt(1, 1); }
            long calls = battle.Random.Calls;
            var shot = Assert.Single(TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false)).Shots);
            Assert.Equal(.17, shot.Absorption, 12); Assert.Equal(.83, shot.Chance, 12);
            Assert.Equal(expectedAbsorbed, shot.Absorbed); Assert.Equal(!expectedAbsorbed, shot.Hit);
            Assert.Equal(oracle.Calls, battle.Random.Calls - calls); Assert.Equal(oracle.State, battle.Random.State);
            if (expectedAbsorbed) absorbedCount++;
        }
        Assert.InRange(absorbedCount, 1, 99);
    }

    [Fact]
    public void No_extra_random_without_crossing()
    {
        var (battle, a, b) = Arena(TacticalAnomalyKind.Heat, .001);
        // Tiny zone falls between the midpoint samples, producing exactly zero absorption.
        long calls = battle.Random.Calls;
        var shot = Assert.Single(TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false)).Shots);
        Assert.Equal(0, shot.Absorption); Assert.False(shot.Absorbed); Assert.True(shot.Hit);
        Assert.Equal(2, battle.Random.Calls - calls);
    }

    [Fact]
    public void Absorbed_shot_has_no_damage_and_consumes_decoy()
    {
        var (battle, a, b) = Arena(TacticalAnomalyKind.Heat);
        b.Pose = b.Pose with { Course = 0 }; // rear shot must still have no rear-kill draw
        long calls = battle.Random.Calls; int hull = b.Hull, armor = b.Armor;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false, d == b));
        var shot = Assert.Single(salvo.Shots);
        Assert.True(shot.Absorbed); Assert.True(shot.Decoyed); Assert.True(shot.Rear);
        Assert.False(shot.Hit); Assert.False(shot.Kill); Assert.Equal(0, shot.Damage);
        Assert.Equal(0, shot.Chance); Assert.Equal(0, shot.ChanceWithoutDecoy);
        Assert.Equal((hull, armor), (b.Hull, b.Armor)); Assert.Empty(salvo.Damage); Assert.Empty(salvo.Systems);
        Assert.Equal(1, b.DecoyCharges); Assert.Equal(1, battle.Statistics.Absorbed);
        Assert.Equal(1, battle.Statistics.Shots); Assert.Equal(0, battle.Statistics.Hits);
        Assert.Equal(1, battle.Random.Calls - calls);
        var dto = TacticalBattleSnapshotBuilder.Shot(shot, 1);
        Assert.True(dto.Absorbed); Assert.Equal(1, dto.Absorption);
    }

    [Fact]
    public void Drone_in_dust_skips_without_random()
    {
        var (battle, a, _) = Arena(TacticalAnomalyKind.Dust);
        a.Jammed = true; long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (_, _) => new(null, false));
        Assert.Empty(salvo.Shots); Assert.All(salvo.Steps, s => Assert.Equal(TacticalFireSkipReason.InDust, s.SkipReason));
        Assert.Equal(2, battle.Statistics.DustSkips); Assert.Equal(calls, battle.Random.Calls);
        Assert.True(TacticalBattleSnapshotBuilder.Units(battle).Single(u => u.Id == a.Id).InDust);
        var evade = TacticalBattleSalvo.Fire(battle, (d, _) => new(null, d == a));
        Assert.Equal(TacticalFireSkipReason.Evading, evade.Steps.Single(s => s.ShooterId == a.Id).SkipReason);
    }

    [Fact]
    public void Drone_in_dust_is_still_targetable()
    {
        var (source, a, b) = SalvoTests.Arena(damage: 1);
        var battle = new Battle(source.Catalog, source.ScenarioId, source.Seed, source.Drones, source.Carriers, source.Tactics, source.Random)
        { Anomalies = [new("dust", TacticalAnomalyKind.Dust, [new(10, 0, 100, 1, 1, 0)])] };
        battle.InitializeFireOrder();
        Assert.Equal(b.Id, Assert.Single(TacticalBattleSalvo.Candidates(battle, a)).Target.Id);
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false));
        Assert.True(Assert.Single(salvo.Shots).Hit); Assert.Equal(2, battle.Random.Calls - calls);
        Assert.Equal(TacticalFireSkipReason.InDust, salvo.Steps.Single(s => s.ShooterId == b.Id).SkipReason);
    }

    [Fact]
    public void Default_target_uses_effective_chance()
    {
        var (battle, a, b) = Arena(TacticalAnomalyKind.Heat, 1);
        var carrier = battle.Carrier("B"); carrier.Pose = new(20, 0, 180);
        var candidates = TacticalBattleSalvo.Candidates(battle, a);
        Assert.Equal(.83, candidates.Single(c => c.Target == b).Chance, 12);
        Assert.True(candidates.Single(c => c.Target == carrier).Chance > .9);
        Assert.Same(carrier, TacticalBattleSalvo.DefaultTarget(a, candidates)!.Target);
        var targets = TacticalBattleSnapshotBuilder.Targets(battle, "A").Single(g => g.DroneId == a.Id);
        Assert.Equal(.17, targets.List.Single(c => c.TargetId == b.Id).Absorption, 12);
        Assert.Equal(carrier.Id, targets.DefaultTargetId);
    }

    [Fact]
    public void Shots_at_carriers_are_absorbed_too()
    {
        var (battle, a, _) = Arena(TacticalAnomalyKind.Heat);
        var carrier = battle.Carrier("B"); carrier.Pose = new(20, 0, 180);
        long calls = battle.Random.Calls; int hull = carrier.Hull;
        var shot = Assert.Single(TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? carrier.Id : null, false)).Shots);
        Assert.True(shot.Absorbed); Assert.Equal(hull, carrier.Hull); Assert.Equal(1, battle.Random.Calls - calls);
    }

    [Fact]
    public void Incoming_forecast_applies_decoy_before_absorption()
    {
        var (battle, a, b) = Arena(TacticalAnomalyKind.Heat, 1, 47);
        var incoming = Assert.Single(TacticalBattleSnapshotBuilder.Incoming(battle, "A"));
        Assert.Equal(a.Id, incoming.TargetId); Assert.Equal(.17, incoming.Absorption, 12);
        Assert.Equal(.47 * .83, incoming.Chance, 12);
        Assert.Equal(.205 * .83, incoming.ChanceIfDecoy!.Value, 12);
    }
}
