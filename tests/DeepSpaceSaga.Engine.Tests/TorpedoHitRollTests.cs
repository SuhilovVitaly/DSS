using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using System.Text.Json;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoHitRollTests
{
    private static ScenarioFile Duel(bool engineEnabled, double distance = 0) => TorpedoImpactTests.Scenario(obj =>
    {
        if (obj.ObjectId == Player)
            return obj with
            {
                Crew = obj.Crew!.Select(crew => crew.CrewId ==
                    obj.Modules!.Single(module => module.ModuleId == Launcher).OperatorCrewId
                    ? crew with { AccuracySkill = 20 } : crew).ToArray()
            };
        if (obj.ObjectId != Target) return obj;
        return obj with
        {
            PositionY = -distance,
            Crew = obj.Crew!.Append(new ShipCrewMemberData("CHR-PIRATE-PILOT", "Pilot",
                ManeuverabilitySkill: 20)).ToArray(),
            Modules = obj.Modules!.Select(module => module.ModuleId == "MOD-PIRATE-ENGINE-01"
                ? module with
                {
                    ModuleTypeId = "module.engine.strizh",
                    PowerState = engineEnabled ? "On" : "Off",
                    OperatorCrewId = "CHR-PIRATE-PILOT"
                }
                : module).ToArray()
        };
    });

    [Fact]
    public void Fifty_percent_example_and_miss_no_damage()
    {
        using var engine = Create(Duel(true), LegacyWeaponRegistry(40, 50));
        engine.ReceiveCommand(Fire("fifty"));
        var launched = At(engine, 0);
        var flight = Assert.Single(launched.Objects, obj => obj.Torpedo is not null).Torpedo!;
        Assert.Equal(60m, flight.CapturedAttack);
        Assert.Equal(500, flight.HitChanceTenths);
        var before = engine.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == Target)
            .Modules!.Where(module => module is not null)
            .ToDictionary(module => module.ModuleId, module => module.StructurePoints);

        var resolved = At(engine, 1);
        var roll = Assert.Single(resolved.CombatJournal, entry => entry.Type == CombatEventType.TorpedoHitRoll);
        Assert.Equal(60m, roll.Attack);
        Assert.Equal(60m, roll.Defense);
        Assert.Equal(500, roll.ChanceTenths);
        Assert.Equal(roll.Roll <= roll.ChanceTenths, roll.Result == "hit");
        Assert.Equal(1UL, engine.CaptureSaveState().GameState.CombatState!.TorpedoHitRngCounter);
        if (roll.Result == "miss")
        {
            Assert.Equal(TorpedoTerminationKind.Miss, Assert.Single(resolved.CombatImpacts).TerminationKind);
            Assert.DoesNotContain(resolved.CombatJournal, entry => entry.Type == CombatEventType.ModuleHit);
            Assert.All(engine.CaptureSaveState().GameState.SpaceObjects.Single(obj => obj.ObjectId == Target).Modules!,
                module => Assert.Equal(before[module.ModuleId], module.StructurePoints));
        }
    }

    [Fact]
    public void Disabled_engine_target_easier()
    {
        using var engine = Create(Duel(false), LegacyWeaponRegistry(40, 50));
        engine.ReceiveCommand(Fire("disabled"));
        var launched = At(engine, 0);
        Assert.Equal(900, Assert.Single(launched.Objects, obj => obj.Torpedo is not null).Torpedo!.HitChanceTenths);
        var resolved = At(engine, 1);
        var roll = Assert.Single(resolved.CombatJournal, entry => entry.Type == CombatEventType.TorpedoHitRoll);
        Assert.Equal(20m, roll.Defense);
        Assert.Equal(900, roll.ChanceTenths);
    }

    [Fact]
    public void Partition_invariance()
    {
        var registry = LegacyWeaponRegistry(40, 50);
        var scenario = Duel(false, 100);
        using var whole = Create(scenario, registry);
        using var split = Create(scenario, registry);
        whole.ReceiveCommand(Fire("whole"));
        split.ReceiveCommand(Fire("split"));
        At(whole, 0);
        At(split, 0);
        At(split, 1000);
        var a = At(whole, 4000);
        var b = At(split, 4000);
        Assert.Equal(JsonSerializer.Serialize(a.CombatJournal.Select(entry => entry with { EventId = 0 })),
            JsonSerializer.Serialize(b.CombatJournal.Select(entry => entry with { EventId = 0 })));
        Assert.Equal(whole.CaptureSaveState().GameState.CombatState!.TorpedoHitRngState,
            split.CaptureSaveState().GameState.CombatState!.TorpedoHitRngState);
    }

    [Fact]
    public void Hit_stream_continues_after_save_load()
    {
        var registry = LegacyWeaponRegistry(40, 50);
        using var continuous = Create(Duel(false), registry);
        continuous.ReceiveCommand(Fire("first"));
        At(continuous, 0);
        var first = At(continuous, 1);
        Assert.Equal("miss", Assert.Single(first.CombatJournal,
            entry => entry.Type == CombatEventType.TorpedoHitRoll).Result);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            continuous.CaptureSaveStateForTests(1, SimulationSpeed.Speed0, 1)), true);
        using var resumed = new DeepSpaceSaga.Engine.SimulationEngine(registry);
        resumed.LoadScenario(save, isSave: true);
        At(continuous, 15001);
        At(resumed, 15001);
        continuous.ReceiveCommand(Fire("second"));
        resumed.ReceiveCommand(Fire("second"));
        At(continuous, 15001);
        At(resumed, 15001);
        var expected = At(continuous, 16000);
        var actual = At(resumed, 16000);
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(continuous.CaptureSaveState().GameState.CombatState!.TorpedoHitRngState,
            resumed.CaptureSaveState().GameState.CombatState!.TorpedoHitRngState);
        Assert.Equal(2UL, resumed.CaptureSaveState().GameState.CombatState!.TorpedoHitRngCounter);
    }
}
