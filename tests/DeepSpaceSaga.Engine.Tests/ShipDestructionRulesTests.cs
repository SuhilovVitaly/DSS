using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ShipDestructionRulesTests
{
    private static readonly string[] CriticalRoles = ["bridge", "engine"];

    [Fact]
    public void Two_of_nine_survives_three_kills()
    {
        var modules = Enumerable.Range(0, 9)
            .Select(i => new ShipModuleLife("module-" + i, null, i < 2)).ToArray();
        Assert.Null(ShipDestructionMath.Cause(modules, "module-1", CriticalRoles, 0.3333m));
        modules[2] = modules[2] with { Destroyed = true };
        Assert.Equal("threshold:3/9", ShipDestructionMath.Cause(modules, "module-2", CriticalRoles, 0.3333m));
    }

    [Theory]
    [InlineData("bridge")]
    [InlineData("engine")]
    public void Critical_module_kills(string role)
    {
        ShipModuleLife[] modules = [new("critical", role, true), new("other", null, false), new("third", null, false),
            new("fourth", null, false)];
        Assert.Equal("critical:critical", ShipDestructionMath.Cause(modules, "critical", CriticalRoles, 0.3333m));
    }

    [Fact]
    public void Mukha_one_module_kills()
    {
        ShipModuleLife[] modules = [new("launcher", null, true), new("engine", "engine", false)];
        Assert.Equal("threshold:1/2", ShipDestructionMath.Cause(modules, "launcher", CriticalRoles, 0.3333m));
    }

    [Fact]
    public void Npc_becomes_wreck()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Target ? obj with
        {
            Modules = obj.Modules!.Where(module => module.ModuleTypeId == "module.engine.basic")
                .Select(module => module with { StructurePoints = 150 }).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(LaunchAt("destroy-engine"));
        At(engine, 0);
        var snapshot = At(engine, 5000);
        var death = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Null(death.DestroyReason);
        Assert.DoesNotContain(snapshot.Objects, obj => obj.ObjectId == Target);
        Assert.Single(snapshot.Objects, obj => obj.ObjectId == death.Result && obj.ObjectType == SpaceObjectType.Wreck);
        Assert.Empty(snapshot.CombatImpacts);
    }

    [Fact]
    public void Player_module_death_triggers_ep8_defeat_once()
    {
        var source = PlayerDefeatTests.LethalScenario();
        var scenario = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(obj => obj.ObjectId == Player ? obj with
                {
                    Modules = obj.Modules!.Where(module => module.ModuleTypeId == "module.engine.basic")
                        .Select(module => module with { StructurePoints = 150 }).ToArray()
                } : obj).ToArray()
            }
        };
        using var engine = Create(scenario);
        var final = At(engine, 6000);
        var death = Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal("critical:MOD-PLAYER-ENGINE-01", death.DestroyReason);
        Assert.Equal(death.EventId, final.EffectiveOutcome.DeathEventId);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        Assert.Single(final.Objects, obj => obj.ObjectId == final.EffectiveOutcome.WreckObjectId);
        var later = At(engine, 20000);
        Assert.Equal(final.EffectiveOutcome, later.EffectiveOutcome);
        Assert.Equal(final.GameTimeMs, later.GameTimeMs);
        Assert.Equal(final.MotionTimeMs, later.MotionTimeMs);
    }
}
