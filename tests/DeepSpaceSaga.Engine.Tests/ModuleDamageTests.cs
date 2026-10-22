using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleDamageTests
{
    [Fact]
    public void Damage_split_150_vs_light()
    {
        var first = ModuleDamageMath.Apply(150, 20, 100, 60, 150);
        Assert.Equal(0, first.ArmorAfter);
        Assert.Equal(60, first.StructureAfter);
        var second = ModuleDamageMath.Apply(150, 20, 100, first.ArmorAfter, first.StructureAfter);
        Assert.Equal(0, second.StructureAfter);
    }

    [Fact]
    public void Unarmored_dies_one_hit()
    {
        var result = ModuleDamageMath.Apply(150, 20, 100, 0, 150);
        Assert.Equal(0, result.ArmorAfter);
        Assert.Equal(0, result.StructureAfter);
        Assert.Equal(150, result.StructureLost);
    }

    [Fact]
    public void Hit_location_by_mass_deterministic()
    {
        using var first = new BasicCombatEndToEndTests.CombatRun();
        using var second = new BasicCombatEndToEndTests.CombatRun();
        var firstLaunch = first.Launch(1);
        var secondLaunch = second.Launch(1);
        Assert.Equal(20m, BasicCombatEndToEndTests.Flight(firstLaunch).OwnProjectile!.ArmorPiercingPercent);
        Assert.Equal(200, BasicCombatEndToEndTests.Flight(secondLaunch).OwnProjectile!.Damage);
        var firstHit = first.AdvanceTo(BasicCombatEndToEndTests.EndTime(firstLaunch));
        var secondHit = second.AdvanceTo(BasicCombatEndToEndTests.EndTime(secondLaunch));
        Assert.Single(firstHit.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        Assert.Equal(ScenarioLoader.Serialize(first.Save()), ScenarioLoader.Serialize(second.Save()));
        Assert.Equal(1UL, first.Save().GameState.CombatState!.HitLocationRngCounter);
        Assert.Equal(first.Save().GameState.CombatState!.HitLocationRngState,
            second.Save().GameState.CombatState!.HitLocationRngState);
    }

    [Fact]
    public void Hit_location_rng_continues_after_save_load()
    {
        using var continuous = new BasicCombatEndToEndTests.CombatRun();
        using var source = new BasicCombatEndToEndTests.CombatRun();
        var firstContinuous = continuous.Launch(1);
        var firstSource = source.Launch(1);
        continuous.AdvanceTo(BasicCombatEndToEndTests.EndTime(firstContinuous));
        source.AdvanceTo(BasicCombatEndToEndTests.EndTime(firstSource));
        string path = Path.Combine(Path.GetTempPath(), $"dss-module-hit-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(source.Save()));
            using var resumed = new BasicCombatEndToEndTests.CombatRun(path);
            var nextContinuous = continuous.Launch(2);
            var nextResumed = resumed.Launch(2);
            var expected = continuous.AdvanceTo(BasicCombatEndToEndTests.EndTime(nextContinuous));
            var actual = resumed.AdvanceTo(BasicCombatEndToEndTests.EndTime(nextResumed));
            Assert.Equal(ScenarioLoader.Serialize(continuous.Save()), ScenarioLoader.Serialize(resumed.Save()));
            Assert.Equal(continuous.Save().GameState.CombatState!.HitLocationRngState,
                resumed.Save().GameState.CombatState!.HitLocationRngState);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Light_module_positive_edge_hit_disables_for_eight_seconds()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Target ? obj with
        {
            Modules = obj.Modules!.Where(module => module.ModuleTypeId == "module.engine.basic")
                .Select(module => module with { ArmorClass = "light", StructurePoints = 150 }).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(LaunchAt("light-armor", 40, -100));
        At(engine, 0);
        var hit = At(engine, 5000);
        var fact = Assert.Single(hit.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        var target = engine.CaptureSaveStateForTests(5000, SimulationSpeed.Speed0, 5000).GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        var module = Assert.Single(target.Modules!);
        Assert.InRange(module.Armor!.Value, 1, 59); Assert.InRange(module.StructurePoints, 1, 149);
        Assert.Equal(fact.MotionTimeMs + 8000, module.DisabledUntilMotionTimeMs); Assert.False(module.Destroyed);
        Assert.Null(fact.HitModuleId); Assert.Null(fact.Damage); // public NPC damage stays masked

    }

}
