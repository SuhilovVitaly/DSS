using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AssemblyTests
{
    [Fact]
    public void SeededInitiativeIsStableIndependentAndDoesNotAdvanceCombatRng()
    {
        var catalog = Catalog();
        var one = Battle.Create(catalog, null, 1);
        var two = Battle.Create(catalog, null, 1);
        // Hand-derived Mulberry32 vector for uint(1) XOR 0x494E4954, six inclusive [40,55] draws.
        Assert.Equal(new[] { 55, 53, 59, 50, 45, 53, 43, 42, 44, 41 }, one.Drones.Select(d => d.Initiative));
        Assert.Equal(one.Drones.Select(d => d.Initiative), two.Drones.Select(d => d.Initiative));
        Assert.Equal(6, one.InitiativeRandom.Calls);
        Assert.Equal(42, one.Random.Calls);
        one.InitiativeRandom.Random();
        Assert.Equal(6, two.InitiativeRandom.Calls);
        Assert.Equal(42, one.Random.Calls);
    }

    [Fact]
    public void DegenerateInitiativeRangeConsumesOnlyInitiativeStream()
    {
        var catalog = Catalog();
        catalog = catalog with
        {
            Pilots = new(catalog.Pilots.Entries.Select(e =>
            (e.Id, e.Value with { Initiative = PilotSkill.Fixed(50) })))
        };
        var fixedBattle = Battle.Create(catalog, null, 1);
        Assert.Equal(0, fixedBattle.InitiativeRandom.Calls);
        catalog = catalog with
        {
            Pilots = new(catalog.Pilots.Entries.Select(e =>
            (e.Id, e.Id == "vera" ? e.Value with { Initiative = PilotSkill.Range(50, 50) } : e.Value)))
        };
        var rangeBattle = Battle.Create(catalog, null, 1);
        Assert.Equal(1, rangeBattle.InitiativeRandom.Calls);
        Assert.Equal(fixedBattle.Random.State, rangeBattle.Random.State);
        Assert.Equal(42, rangeBattle.Random.Calls);
        Assert.All(rangeBattle.Drones, d => Assert.Equal(50, d.Initiative));
    }

    [Fact]
    public void SnapshotAndOptionsExposeInitiativeWithoutReroll()
    {
        var catalog = Catalog();
        var battle = Battle.Create(catalog, null, 1);
        var expected = battle.Drones.Select(d => (int?)d.Initiative).ToArray();
        uint state = battle.InitiativeRandom.State;
        for (int turn = 0; turn < 3; turn++)
        {
            var units = TacticalBattleSnapshotBuilder.Units(battle);
            Assert.Equal(expected, units.Where(u => u.Kind == Contracts.TacticalUnitKind.Drone).Select(u => u.Initiative));
            Assert.All(units.Where(u => u.Kind == Contracts.TacticalUnitKind.Carrier), u => Assert.Null(u.Initiative));
            var options = TacticalBattleSnapshotBuilder.Options(catalog).Scenarios[0].Drones;
            Assert.Equal(new Contracts.TacticalSkillRange(40, 55, true), options[4].Initiative);
            Assert.Equal(new Contracts.TacticalSkillRange(55, 55, false), options[0].Initiative);
            Assert.Equal(state, battle.InitiativeRandom.State);
            if (turn < 2) battle.Step(_ => { });
        }
        Assert.Equal(state, battle.InitiativeRandom.State);
    }

    internal static TacticalBattleCatalog Catalog()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln")))
                return TacticalBattleContentLoader.Load(Path.Combine(dir.FullName, "src/DeepSpaceSaga.Client/Data/TacticalBattle"));
        throw new DirectoryNotFoundException("DSS repository not found.");
    }

    [Fact]
    public void InitialAssemblyMatchesEveryPublishedTraceFieldAndCalls()
    {
        var battle = Battle.Create(Catalog(), null, 1);
        using var trace = ReferenceFixtureTests.Open("trace-seed-1");
        var units = trace.RootElement.GetProperty("data").GetProperty("assembly").GetProperty("units");
        Assert.Equal(42, battle.Random.Calls);
        Assert.Equal(10, battle.Drones.Length);
        for (int i = 0; i < units.GetArrayLength(); i++)
        {
            var e = units[i];
            var d = battle.Drones[i];
            Assert.Equal(e.GetProperty("id").GetString(), d.Id);
            Assert.Equal(e.GetProperty("side").GetString(), d.Side);
            Assert.Equal(e.GetProperty("frame").GetString(), d.Frame.Id);
            Assert.Equal(e.GetProperty("accuracy").GetInt32(), d.Accuracy);
            Assert.Equal(e.GetProperty("maneuver").GetInt32(), d.Maneuver);
            Assert.InRange(Math.Abs(e.GetProperty("x").GetDouble() - d.Pose.X), 0, 0.0000005);
            Assert.InRange(Math.Abs(e.GetProperty("y").GetDouble() - d.Pose.Y), 0, 0.0000005);
            Assert.InRange(Math.Abs(e.GetProperty("course").GetDouble() - d.Pose.Course), 0, 0.0000005);
            Assert.Equal(e.GetProperty("speed").GetDouble(), d.Speed);
            Assert.Equal(e.GetProperty("hull").GetInt32(), d.Hull);
            Assert.Equal(e.GetProperty("armor").GetInt32(), d.Armor);
            Assert.Equal(e.GetProperty("role").GetString(), d.Role);
            Assert.True(d.IsActive);
            Assert.False(d.Jammed);
            Assert.Equal(0, d.RoleSince);
            Assert.Equal(0, d.Dry);
            Assert.Equal(0, d.Extend);
            Assert.Null(d.Hunt);
            Assert.Equal("in", d.StrikeMode);
            Assert.All(d.Engines, engine => Assert.Equal(1, engine.Factor));
        }
        Assert.Equal(new[] { "Скиталец", "Логово" }, battle.Carriers.Select(c => c.Id));
        Assert.Equal(110, battle.Carriers[0].Pose.X);
        Assert.Equal(790, battle.Carriers[1].Pose.X);
        Assert.Equal(-180, battle.Carriers[1].Pose.Course);
        Assert.Equal((450.0, 200.0), battle.Midpoint);
        Assert.Equal(0, battle.RoleChanges);
    }

    [Fact]
    public void OverridesAreShallowWithoutSlotRevalidationAndDoNotMutateCatalog()
    {
        var catalog = Catalog();
        var overrides = new Dictionary<string, TacticalLoadoutOverride>(StringComparer.Ordinal)
        {
            ["Вера"] = new(Weapon: "gun.auto", Armor: "plate.heavy", ShieldCount: 9, Hull: 123,
                Engines: ["drive.light", "drive.light", "drive.light"]),
            ["Юн"] = new(Shield: ""),
            ["Марта"] = new(Shield: "shield.t1"),
            ["Таг-2"] = new(Shield: "shield.t1")
        };
        var battle = Battle.Create(catalog, "initial", 1, loadouts: overrides);
        var vera = battle.Drones[0];
        Assert.Equal(123, vera.MaxHull);
        Assert.Equal("gun.auto", vera.Weapon.Id);
        Assert.Equal(130, vera.Armor);
        Assert.Equal(9, vera.ShieldCount);
        Assert.Equal(3, vera.Engines.Length);
        Assert.Null(battle.Drones[1].Shield);
        Assert.Equal(0, battle.Drones[1].ShieldCount);
        Assert.Equal(1, battle.Drones[2].ShieldCount);
        Assert.Equal(1, battle.Drones[5].ShieldCount);
        Assert.Equal(2, catalog.Layouts["heavy.sword.shield"].Engines.Length);
        Assert.Equal(2, catalog.Layouts["heavy.sword.shield"].ShieldCount);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void FixedTacticRolesAreStableByMaximumHullAndCommanderUsesStartTactic()
    {
        var battle = Battle.Create(Catalog(), "initial", 1, new Dictionary<string, string> { ["A"] = "raid" });
        Assert.Equal(new[] { "striker", "striker", "striker", "fighter" }, battle.Drones.Take(4).Select(d => d.Role));
        Assert.Equal(new[] { "striker", "interceptor", "fighter", "fighter", "fighter", "fighter" }, battle.Drones.Skip(4).Select(d => d.Role));
        Assert.All(battle.Drones, d => Assert.Equal(0, d.RoleSince));
        Assert.Equal(new[] { "raid", "commander" }, battle.Tactics);
        Assert.Equal(0, battle.RoleChanges);
    }

    [Fact]
    public void EqualSkillRangeConsumesDrawAndInitialSpeedUsesCurrentLimits()
    {
        var c = Catalog();
        c = c with { Pilots = new(c.Pilots.Entries.Select(e => (e.Id, e.Id == "vera" ? e.Value with { Accuracy = PilotSkill.Range(55, 55) } : e.Value))) };
        var scenario = c.Scenarios["initial"];
        var first = scenario.Sides[0];
        scenario = scenario with { Sides = scenario.Sides.SetItem(0, first with { Drones = first.Drones.SetItem(0, first.Drones[0] with { Speed = 10000 }) }) };
        c = c with { Scenarios = new([("initial", scenario)]) };
        var battle = Battle.Create(c, "initial", 1);
        Assert.Equal(43, battle.Random.Calls);
        Assert.Equal(55, battle.Drones[0].Accuracy);
        Assert.Equal(battle.Drones[0].Limits.MaxSpeed, battle.Drones[0].Speed);
    }

    [Fact]
    public void CarrierIgnoresLayoutHullAndBattlesOwnIndependentMutableState()
    {
        var c = Catalog();
        c = c with { Layouts = new(c.Layouts.Entries.Select(e => (e.Id, e.Id == "carrier.standard" ? e.Value with { Hull = 1, Armor = "plate.heavy" } : e.Value))) };
        var one = Battle.Create(c, "initial", 1);
        var two = Battle.Create(c, "initial", 1);
        Assert.Equal(400, one.Carriers[0].Hull);
        Assert.Equal(130, one.Carriers[0].Armor);
        Assert.Equal(35, one.Carriers[0].Maneuver);
        one.Drones[0].Engines[0].Factor = 0.5;
        one.Drones[0].Hull = 1;
        one.Random.Random();
        Assert.Equal(1, two.Drones[0].Engines[0].Factor);
        Assert.Equal(65, two.Drones[0].Hull);
        Assert.Equal(42, two.Random.Calls);
        Assert.Equal(43, one.Random.Calls);
    }
}
