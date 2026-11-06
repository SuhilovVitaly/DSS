using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class SalvoTests
{
    [Fact]
    public void EveryIndependentTraceSalvoMatchesShotsStatesAndRngCalls()
    {
        using var trace = ReferenceFixtureTests.Open("trace-seed-1");
        var turns = trace.RootElement.GetProperty("data").GetProperty("turns");
        for (int turnIndex = 0; turnIndex < turns.GetArrayLength(); turnIndex++)
        {
            var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
            var turn = turns[turnIndex];
            if (turnIndex > 0)
                foreach (var state in turns[turnIndex - 1].GetProperty("states").EnumerateArray())
                {
                    var unit = battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers).First(u => u.Id == state.GetProperty("id").GetString());
                    unit.Hull = state.GetProperty("hull").GetInt32();
                    unit.Armor = state.GetProperty("armor").GetInt32();
                    if (unit is not TacticalDrone drone) continue;
                    drone.Status = state.GetProperty("status").GetString() switch { "air" => TacticalDroneStatus.Air, "disabled" => TacticalDroneStatus.Disabled, _ => TacticalDroneStatus.Destroyed };
                    drone.Jammed = state.GetProperty("jammed").GetInt32() != 0;
                    for (int i = 0; i < drone.Engines.Length; i++) drone.Engines[i].Factor = state.GetProperty("engines")[i].GetDouble();
                }
            foreach (var position in turn.GetProperty("positions").EnumerateArray())
                battle.Drones.First(d => d.Id == position.GetProperty("id").GetString()).Pose = new(
                    position.GetProperty("x").GetDouble(), position.GetProperty("y").GetDouble(), position.GetProperty("course").GetDouble());
            long before = turn.GetProperty("salvoCallsBefore").GetInt32();
            while (battle.Random.Calls < before) battle.Random.Random(); // published stage precondition; no simulated AI expectations
            var expectedShots = turn.GetProperty("shots").EnumerateArray().ToArray();
            var actual = TacticalBattleSalvo.Fire(battle, (d, _) =>
            {
                foreach (var shot in expectedShots)
                    if (shot.GetProperty("shooter").GetString() == d.Id) return shot.GetProperty("target").GetString();
                return null;
            });
            Assert.Equal(expectedShots.Length, actual.Shots.Length);
            for (int i = 0; i < expectedShots.Length; i++)
            {
                var expected = expectedShots[i];
                var shot = actual.Shots[i];
                Assert.Equal(expected.GetProperty("shooter").GetString(), shot.Shooter.Id);
                Assert.Equal(expected.GetProperty("target").GetString(), shot.Target.Id);
                Assert.Equal(expected.GetProperty("hit").GetInt32() != 0, shot.Hit);
                Assert.Equal(expected.GetProperty("damage").GetInt32(), shot.Damage);
                Assert.Equal(expected.GetProperty("rear").GetInt32() != 0, shot.Rear);
                Assert.Equal(expected.GetProperty("kill").GetInt32() != 0, shot.Kill);
                Assert.InRange(Math.Abs(expected.GetProperty("distance").GetDouble() - shot.Distance), 0, 0.000002);
                double angleBudget = 0.000001 + 0.000002 / shot.Distance * 57.3;
                Assert.InRange(Math.Abs(expected.GetProperty("angle").GetDouble() - shot.Angle), 0, angleBudget);
                Assert.InRange(Math.Abs(expected.GetProperty("chance").GetDouble() - shot.Chance), 0, 0.0000006);
            }
            Assert.Equal(turn.GetProperty("salvoCallsAfter").GetInt32(), battle.Random.Calls);
            foreach (var state in turn.GetProperty("states").EnumerateArray())
            {
                var unit = battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers).First(u => u.Id == state.GetProperty("id").GetString());
                Assert.Equal(state.GetProperty("hull").GetInt32(), unit.Hull);
                Assert.Equal(state.GetProperty("armor").GetInt32(), unit.Armor);
                if (unit is not TacticalDrone drone) continue;
                Assert.Equal(state.GetProperty("status").GetString(), drone.Status.ToString().ToLowerInvariant());
                Assert.Equal(state.GetProperty("jammed").GetInt32() != 0, drone.Jammed);
                Assert.Equal(state.GetProperty("engines").EnumerateArray().Select(e => e.GetDouble()), drone.Engines.Select(e => e.Factor));
            }
        }
    }

    [Theory]
    [InlineData(60, 10, 50, 10, 50)]
    [InlineData(60, 10, 20, 40, 20)]
    [InlineData(60, 100, 50, 60, 0)]
    [InlineData(40, 0, -5, 40, 0)]
    [InlineData(40, 0, 100, 0, 40)]
    public void ArmorPoolSplitUsesPiercingBeforeAbsorption(int damage, int piercing, int armor, int hull, int absorbed) => Assert.Equal((hull, absorbed), TacticalBattleSalvo.Split(damage, piercing, armor));

    [Fact]
    public void SimultaneousMutualDestructionStillFiresBothAndFirstLossUsesObjectOrder()
    {
        var (battle, a, b) = Arena(damage: 40, hull: 40);
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle);
        Assert.Equal(new[] { a.Id, b.Id }, salvo.Shots.Select(s => s.Shooter.Id));
        Assert.Equal(new[] { b.Id, a.Id }, salvo.Damage.Select(d => d.TargetId));
        Assert.Equal(TacticalDroneStatus.Destroyed, a.Status);
        Assert.Equal(TacticalDroneStatus.Destroyed, b.Status);
        Assert.Equal(0, a.Speed);
        Assert.Equal(0, b.Speed);
        Assert.Equal(4, battle.Random.Calls - calls);
        Assert.Equal("A", battle.Statistics.FirstLoss);
        Assert.Equal(80, battle.Statistics.Hull);
        Assert.Equal(2, battle.Statistics.Shots);
        Assert.Equal(2, battle.Statistics.Hits);
        Assert.Equal(80, Assert.Single(battle.Statistics.Weapons).Hull);
    }

    [Fact]
    public void MultipleHitsShareArmorButSoloLossUsesPreSalvoArmor()
    {
        var (battle, a, b) = Arena(damage: 40, hull: 100, systems: "weaponJammed");
        var second = ActivateSecondA(battle);
        b.Armor = 50;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d.Side == "A" ? b.Id : null);
        Assert.Equal(new[] { a.Id, second.Id }, salvo.Shots.Select(s => s.Shooter.Id));
        Assert.All(salvo.Shots, s => Assert.Equal(0, s.SoloHullLoss));
        Assert.Equal(70, b.Hull);
        Assert.Equal(0, b.Armor);
        Assert.Equal((30, 50), (salvo.Damage[0].Hull, salvo.Damage[0].Armor));
        Assert.Equal(0, battle.Statistics.Systems);
        Assert.Equal(0, Assert.Single(battle.Statistics.Weapons).Hull);
    }

    [Fact]
    public void MissConsumesOneDrawAndWithheldShotStillResetsDryWhenCandidatesExist()
    {
        var (battle, a, b) = Arena(chance: 0);
        a.Dry = 4;
        b.Dry = 5;
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? b.Id : null);
        var shot = Assert.Single(salvo.Shots);
        Assert.False(shot.Hit);
        Assert.Equal(0, shot.Damage);
        Assert.Equal(1, battle.Random.Calls - calls);
        Assert.Equal(0, a.Dry);
        Assert.Equal(0, b.Dry);
        Assert.Empty(salvo.Damage);
    }

    [Fact]
    public void RearHitAlwaysConsumesThirdDrawEvenWithZeroKillChance()
    {
        var (battle, a, b) = Arena();
        b.Pose = b.Pose with { Course = 0 };
        b.Armor = 80;
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? b.Id : null);
        var shot = Assert.Single(salvo.Shots);
        Assert.True(shot.Rear);
        Assert.False(shot.Kill);
        Assert.Equal(3, battle.Random.Calls - calls);
        Assert.Equal(60, b.Hull);
        Assert.Equal(80, b.Armor);
        Assert.Equal(1, battle.Statistics.RearHits);
        Assert.Equal(0, battle.Statistics.RearKills);
    }

    [Fact]
    public void FirstImpactThatExhaustsHullGetsTheLethalMarker()
    {
        var (battle, a, b) = Arena(hull: 60);
        var second = ActivateSecondA(battle);
        b.Armor = 0;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d.Side == "A" ? b.Id : null);
        Assert.Equal(new[] { a.Id, second.Id }, salvo.Shots.Select(s => s.Shooter.Id));
        Assert.Equal(new[] { false, true }, salvo.Shots.Select(s => s.LethalImpact));
        Assert.Equal(TacticalDroneStatus.Destroyed, b.Status);
    }

    [Fact]
    public void RearKillClipsToCurrentHullAndCreditsWholeLossToWeapon()
    {
        var (battle, a, b) = Arena(hull: 1000, killChance: 1);
        b.Pose = b.Pose with { Course = 0 };
        b.Armor = 80;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? b.Id : null);
        var shot = Assert.Single(salvo.Shots);
        Assert.True(shot.Kill);
        Assert.True(shot.LethalImpact);
        Assert.Equal(1000, Assert.Single(salvo.Damage).Hull);
        Assert.Equal(0, b.Hull);
        Assert.Equal(80, b.Armor);
        Assert.Equal(1000, Assert.Single(battle.Statistics.Weapons).Hull);
        Assert.Equal(1, battle.Statistics.RearKills);
    }

    [Fact]
    public void DisabledThresholdAppliesBeforeSystemsAndStopsUnit()
    {
        var (battle, a, b) = Arena(damage: 80, systems: "weaponJammed");
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? b.Id : null);
        Assert.Equal(20, b.Hull);
        Assert.Equal(TacticalDroneStatus.Disabled, b.Status);
        Assert.Equal(0, b.Speed);
        Assert.Equal(1, battle.Statistics.Disabled);
        Assert.Equal(0, battle.Statistics.Systems);
        Assert.Empty(salvo.Systems);
        Assert.DoesNotContain(TacticalBattleSalvo.Candidates(battle, a), c => c.Target == b);
        TacticalBattleSalvo.Fire(battle);
        Assert.Equal(1, battle.Statistics.Disabled);
    }

    [Theory]
    [InlineData("weaponJammed", 6)]
    [InlineData("engineDamaged", 7)]
    public void EachQualifyingHitRollsSystemsIncludingNoChange(string effect, long draws)
    {
        var (battle, _, b) = Arena(hull: 500, systems: effect);
        ActivateSecondA(battle);
        double speed = b.Speed;
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d.Side == "A" ? b.Id : null);
        Assert.Equal(2, battle.Statistics.Systems);
        Assert.Single(salvo.Systems);
        Assert.Equal(draws, battle.Random.Calls - calls);
        Assert.Equal(speed, b.Speed);
        if (effect == "weaponJammed") Assert.True(b.Jammed);
        else Assert.Equal(0.5, b.Engines[0].Factor);
    }

    [Fact]
    public void SeparateEngineHitsChooseOnlyStillIntactEnginesInListOrder()
    {
        var (battle, a, b) = Arena(hull: 500, systems: "other-effect-also-damages-engine");
        var other = battle.Drones[5];
        other.Status = TacticalDroneStatus.Air;
        other.Pose = new(10, 1, 180);
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d.Side == "B" ? a.Id : null);
        Assert.Equal(2, salvo.Systems.Length);
        Assert.Equal(new[] { 0, 1 }, salvo.Systems.Select(s => s.EngineIndex!.Value).Order());
        Assert.All(a.Engines, e => Assert.Equal(0.5, e.Factor));
        Assert.Equal(8, battle.Random.Calls - calls);
        Assert.Equal(2, battle.Statistics.Systems);
    }

    [Fact]
    public void CandidateOrderAndRolePriorityPreserveStableTiesAndCarrierRules()
    {
        var (battle, a, b) = Arena();
        b.Pose = new(20, 0, 180);
        var tied = battle.Drones[5];
        tied.Status = TacticalDroneStatus.Air;
        tied.Pose = b.Pose;
        var carrier = battle.Carrier("B");
        carrier.Pose = new(10, 0, 180);
        var candidates = TacticalBattleSalvo.Candidates(battle, a);
        Assert.Equal(new[] { b.Id, tied.Id, carrier.Id }, candidates.Select(c => c.Target.Id));
        Assert.Same(b, TacticalBattleSalvo.DefaultTarget(a, candidates)!.Target);
        a.Role = "striker";
        Assert.Same(carrier, TacticalBattleSalvo.DefaultTarget(a, candidates)!.Target);
        Assert.False(candidates[2].Terms.IsRear);
        a.Jammed = true;
        Assert.Empty(TacticalBattleSalvo.Candidates(battle, a));
        TacticalBattleSalvo.Fire(battle);
        Assert.Equal(1, a.Dry);
    }

    [Fact]
    public void CarrierDamageUsesArmorPoolAndSeparateHullCounterWithoutDisabledState()
    {
        var (battle, a, b) = Arena(damage: 100);
        b.Status = TacticalDroneStatus.Disabled;
        var carrier = battle.Carrier("B");
        carrier.Pose = new(10, 0, 0);
        carrier.Armor = 80;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? carrier.Id : null);
        var shot = Assert.Single(salvo.Shots);
        Assert.False(shot.Rear);
        Assert.Equal((20, 80), (salvo.Damage[0].Hull, salvo.Damage[0].Armor));
        Assert.Equal(380, carrier.Hull);
        Assert.True(carrier.IsActive);
        Assert.Equal(20, battle.Statistics.CarrierHull);
        Assert.Equal(20, battle.Statistics.Hull);
        Assert.Equal(80, battle.Statistics.Armor);
        Assert.Equal(0, battle.Statistics.Disabled);
    }

    [Fact]
    public void InvalidSelectionRejectsBeforeRandomStatisticsAndDryMutation()
    {
        var (battle, a, b) = Arena();
        a.Dry = 3;
        long calls = battle.Random.Calls;
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle, (d, _) => d == a ? b.Id : "missing"));
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Equal(0, battle.Statistics.Shots);
        Assert.Equal(3, a.Dry);
        Assert.Equal(100, a.Hull);
        Assert.Equal(100, b.Hull);
    }

    private static TacticalDrone ActivateSecondA(Battle battle)
    {
        var second = battle.Drones[1];
        second.Status = TacticalDroneStatus.Air;
        second.Pose = new(0, 1, 0);
        return second;
    }

    private static (Battle Battle, TacticalDrone A, TacticalDrone B) Arena(int damage = 40, int hull = 100, double chance = 100,
        double killChance = 0, string? systems = null)
    {
        var c = AssemblyTests.Catalog();
        c = c with
        {
            Rules = c.Rules with
            {
                Hit = c.Rules.Hit with { MinChance = chance, MaxChance = chance },
                Systems = c.Rules.Systems with
                {
                    MinHullLoss = systems is null ? 1000000 : 40,
                    Table = systems is null ? c.Rules.Systems.Table : [new(systems, 1)]
                }
            },
            Weapons = new(c.Weapons.Entries.Select(e => (e.Id, e.Value with { DamageMin = damage, DamageMax = damage, RearKillChance = killChance })))
        };
        var loadouts = c.Scenarios["initial"].Sides.SelectMany(s => s.Drones).ToDictionary(d => d.Id,
            _ => new TacticalLoadoutOverride(Weapon: "gun.sword.t1", Armor: "armor.none", Shield: "", Hull: hull), StringComparer.Ordinal);
        var battle = Battle.Create(c, "initial", 1, loadouts: loadouts);
        foreach (var drone in battle.Drones) drone.Status = TacticalDroneStatus.Disabled;
        var a = battle.Drones[0];
        var b = battle.Drones[4];
        a.Status = b.Status = TacticalDroneStatus.Air;
        a.Pose = new(0, 0, 0);
        b.Pose = new(10, 0, 180);
        battle.Carrier("A").Pose = new(-10000, 0, 0);
        battle.Carrier("B").Pose = new(10000, 0, 0);
        return (battle, a, b);
    }
}
