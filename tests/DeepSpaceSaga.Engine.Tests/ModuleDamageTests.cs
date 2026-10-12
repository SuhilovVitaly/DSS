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
        Assert.Equal(20m, BasicCombatEndToEndTests.Flight(firstLaunch).Torpedo!.ArmorPiercingPercent);
        Assert.Equal(100m, BasicCombatEndToEndTests.Flight(secondLaunch).Torpedo!.ArmorDamagePercent);
        var firstHit = first.AdvanceTo(BasicCombatEndToEndTests.EndTime(firstLaunch));
        var secondHit = second.AdvanceTo(BasicCombatEndToEndTests.EndTime(secondLaunch));
        var eventA = Assert.Single(firstHit.CombatJournal, e => e.Type == CombatEventType.ModuleHit);
        var eventB = Assert.Single(secondHit.CombatJournal, e => e.Type == CombatEventType.ModuleHit);
        Assert.Equal(eventA.HitModuleId, eventB.HitModuleId);
        Assert.Equal(0, eventA.StructureAfter);
        Assert.Contains(firstHit.CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed && e.HitModuleId == eventA.HitModuleId);
        Assert.Equal(1UL, first.Engine.CaptureSaveState().GameState.CombatState!.HitLocationRngCounter);
        Assert.Equal(first.Engine.CaptureSaveState().GameState.CombatState!.HitLocationRngState,
            second.Engine.CaptureSaveState().GameState.CombatState!.HitLocationRngState);
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
            File.WriteAllText(path, ScenarioLoader.Serialize(source.Engine.CaptureSaveState()));
            using var resumed = new BasicCombatEndToEndTests.CombatRun(path);
            var nextContinuous = continuous.Launch(2);
            var nextResumed = resumed.Launch(2);
            var expected = continuous.AdvanceTo(BasicCombatEndToEndTests.EndTime(nextContinuous));
            var actual = resumed.AdvanceTo(BasicCombatEndToEndTests.EndTime(nextResumed));
            Assert.Equal(expected.CombatJournal.Last(e => e.Type == CombatEventType.ModuleHit).HitModuleId,
                actual.CombatJournal.Last(e => e.Type == CombatEventType.ModuleHit).HitModuleId);
            Assert.Equal(continuous.Engine.CaptureSaveState().GameState.CombatState!.HitLocationRngState,
                resumed.Engine.CaptureSaveState().GameState.CombatState!.HitLocationRngState);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Light_module_hit_disables_for_one_minute()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Target ? obj with
        {
            Modules = obj.Modules!.Where(module => module.ModuleTypeId == "module.engine.basic")
                .Select(module => module with { ArmorClass = "light", StructurePoints = 150 }).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("light-armor"));
        At(engine, 0);
        var hit = At(engine, 5000);
        var moduleHit = Assert.Single(hit.CombatJournal, e => e.Type == CombatEventType.ModuleHit);
        Assert.Equal(60, moduleHit.ArmorBefore);
        Assert.Equal(0, moduleHit.ArmorAfter);
        Assert.Equal(60, moduleHit.StructureAfter);
        var target = engine.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == Target);
        var module = Assert.Single(target.Modules!);
        Assert.Equal(0, module.Armor);
        Assert.Equal(60, module.StructurePoints);
        Assert.Equal(moduleHit.MotionTimeMs + 60000, module.DisabledUntilMotionTimeMs);
        Assert.False(module.Destroyed);
    }

}
