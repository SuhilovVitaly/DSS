using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DisabledModuleTests
{
    [Fact]
    public void Disabled_launcher_rejects_fire_then_recovers_at_deadline()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Player ? obj with
        {
            Modules = obj.Modules!.Select(module => module.ModuleId == Launcher
                ? module with { DisabledUntilMotionTimeMs = 60_000 } : module).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("too-early"));
        var blocked = At(engine, 0);
        Assert.Equal(CommandReasonCodes.ModuleDisabled,
            Assert.Single(blocked.CommandResults, result => result.CommandId == "too-early").ReasonCode);
        Assert.DoesNotContain(blocked.Objects, obj => obj.Torpedo is not null);

        At(engine, 60_000);
        engine.ReceiveCommand(Fire("ready"));
        var launched = At(engine, 60_000);
        Assert.Equal(CommandResultStatus.Executed,
            Assert.Single(launched.CommandResults, result => result.CommandId == "ready").Status);
        Assert.Single(launched.Objects, obj => obj.Torpedo is not null);
    }

    [Fact]
    public void Destroyed_launcher_has_distinct_rejection_reason()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Player ? obj with
        {
            Modules = obj.Modules!.Select(module => module.ModuleId == Launcher
                ? module with { StructurePoints = 0, Destroyed = true } : module).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("destroyed"));
        Assert.Equal(CommandReasonCodes.ModuleDestroyed,
            Assert.Single(At(engine, 0).CommandResults, result => result.CommandId == "destroyed").ReasonCode);
    }

    [Fact]
    public void Disabled_pd_rejects_toggle()
    {
        const string defense = "MOD-PLAYER-COUNTERMEASURE-01";
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Player ? obj with
        {
            Modules = obj.Modules!.Select(module => module.ModuleId == defense
                ? module with { DisabledUntilMotionTimeMs = 60_000 } : module).ToArray()
        } : obj);
        using var engine = Create(scenario);
        engine.ReceiveCommand(new("disabled-pd", 1, Player, defense, DefenseCommandTypes.Enable));
        Assert.Equal(CommandReasonCodes.ModuleDisabled,
            Assert.Single(At(engine, 0).CommandResults, result => result.CommandId == "disabled-pd").ReasonCode);
    }

    [Fact]
    public void Disabled_engine_coasts_without_ai_maneuver()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Target ? obj with
        {
            PositionY = -1_000,
            SpeedMps = 1_000,
            MovementType = "Linear",
            Modules = obj.Modules!.Select(module => module.ModuleTypeId == "module.engine.basic"
                ? module with { PowerState = "On", DisabledUntilMotionTimeMs = 60_000 } : module).ToArray()
        } : obj);
        using var engine = Create(scenario);
        var before = At(engine, 0).Objects.Single(obj => obj.ObjectId == Target);
        var during = At(engine, 1_000).Objects.Single(obj => obj.ObjectId == Target);
        Assert.Null(during.HostileCombat);
        Assert.Equal(before.Direction, during.Direction);
        Assert.Equal(before.SpeedKmS, during.SpeedKmS);
        Assert.Equal(before.Y - 10, during.Y, 6);
        var recovered = At(engine, 61_000).Objects.Single(obj => obj.ObjectId == Target);
        Assert.NotNull(recovered.HostileCombat);
    }

    [Fact]
    public void Combat_hit_interrupts_active_engine_cycle_and_preserves_coasting()
    {
        const string engineModule = "MOD-PLAYER-ENGINE-01";
        var scenario = PlayerDefeatTests.LethalScenario(150);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(obj => obj.ObjectId == Player ? obj with
                {
                    SpeedMps = 1_000,
                    MovementType = "Linear",
                    Modules = obj.Modules!.Select(module => module with
                    { PowerState = "On", ArmorClass = "light" }).ToArray()
                } : obj).ToArray()
            }
        };
        using var engine = Create(scenario, LegacyWeaponRegistry(100, 100));
        engine.ReceiveCommand(new("turn-before-hit", 1, Player, engineModule,
            ShipEngineCommandTypes.TurnRightUntilCancel));
        var started = At(engine, 1);
        Assert.Equal(ShipEngineCommandTypes.TurnRightUntilCancel,
            started.InstalledModules.Single(module => module.ModuleId == engineModule).ActiveCommandType);
        var hit = At(engine, 6_000);
        Assert.Equal(SessionOutcomeState.Running, hit.EffectiveOutcome.State);
        Assert.Contains(hit.CombatJournal, entry => entry.Type == CombatEventType.ModuleHit && entry.TargetObjectId == Player);
        var saved = engine.CaptureSaveState();
        var player = saved.GameState.SpaceObjects.Single(obj => obj.ObjectId == Player);
        var module = Assert.Single(player.Modules!);
        Assert.Null(module.ActiveCycle);
        Assert.True(module.DisabledUntilMotionTimeMs > hit.MotionTimeMs);
        var pose = hit.Objects.Single(obj => obj.ObjectId == Player);
        Assert.Null(pose.ActiveEngineCommandType);
        Assert.Null(pose.HostileCombat);
        engine.ReceiveCommand(new("turn-disabled", 2, Player, engineModule,
            ShipEngineCommandTypes.TurnRightStep));
        Assert.Equal(CommandReasonCodes.ModuleDisabled,
            Assert.Single(At(engine, 6_000).CommandResults, result => result.CommandId == "turn-disabled").ReasonCode);
    }

    [Fact]
    public void Disabled_pd_cancels_pending_burst_shots()
    {
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Target ? obj with
        {
            Modules = obj.Modules!.Where(module => module.ModuleTypeId == "module.countermeasure.launcher.basic")
                .Select(module => module with { ArmorClass = "light", StructurePoints = 150 }).ToArray()
        } : obj);
        using var initial = Create(scenario);
        var saved = initial.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var defenses = saved.GameState.DefenseState!;
        var pending = new PendingBurstShot(10_000, "torpedo-1", 2);
        var prepared = saved with
        {
            GameState = saved.GameState with
            {
                DefenseState = defenses with
                {
                    Launchers = defenses.Launchers.Select(row => row.OwnerObjectId == Target
                        ? row with { State = row.State with { Barrels = 2, PendingBurstShots = [pending] } } : row).ToArray()
                }
            }
        };
        using var engine = Create(prepared);
        engine.ReceiveCommand(Fire("hit-defense"));
        At(engine, 0);
        var hit = At(engine, 5_000);
        Assert.Contains(hit.CombatJournal, entry => entry.Type == CombatEventType.ModuleHit && entry.TargetObjectId == Target);
        var after = engine.CaptureSaveStateForTests(5_000, SimulationSpeed.Speed0).GameState.DefenseState!;
        Assert.Empty(after.Launchers.Single(row => row.OwnerObjectId == Target).State.PendingBurstShots!.Value);
    }
}
