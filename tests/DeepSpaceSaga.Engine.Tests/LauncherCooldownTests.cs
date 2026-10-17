using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class LauncherCooldownTests
{
    [Fact]
    public void Second_launch_after_15s_while_first_flying_and_command_id_is_idempotent()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { PositionY = -1000 } : o);
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("first"));
        var first = At(engine, 0);
        string firstId = Assert.Single(first.Objects, o => o.Torpedo is not null).ObjectId;
        Assert.Equal(15000, first.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);

        At(engine, 14999);
        engine.ReceiveCommand(Fire("early"));
        var early = At(engine, 14999);
        Assert.Equal("launcher_reloading", early.CommandResults.Single(r => r.CommandId == "early").ReasonCode);
        Assert.Equal(firstId, Assert.Single(early.Objects, o => o.Torpedo is not null).ObjectId);

        At(engine, 15000);
        engine.ReceiveCommand(Fire("second"));
        engine.ReceiveCommand(Fire("second"));
        var second = At(engine, 15000);
        var ids = second.Objects.Where(o => o.Torpedo is not null).Select(o => o.ObjectId).ToArray();
        Assert.Equal(2, ids.Length);
        Assert.Contains(firstId, ids);
        Assert.Equal(ids, second.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds.ToArray());
        Assert.Equal(30000, second.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);

        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(15000, SimulationSpeed.Speed0, 15000)), true);
        using var restored = new DeepSpaceSaga.Engine.SimulationEngine(Registry.Value);
        restored.LoadScenario(save, isSave: true);
        Assert.Equal(ids, At(restored, 15000).Objects.Where(o => o.Torpedo is not null).Select(o => o.ObjectId));
        Assert.Equal(ids, At(restored, 15000).InstalledModules.Single(m => m.ModuleId == Launcher)
            .LauncherCombat!.ActiveTorpedoIds.ToArray());
    }

    [Fact]
    public void Npc_fires_on_cooldown_while_first_flight_is_active()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        Assert.DoesNotContain(At(engine, 500).Objects, o => o.Torpedo?.OwnerObjectId == Target);
        var first = At(engine, 1251);
        var firstTorpedo = Assert.Single(first.Objects, o => o.Torpedo?.OwnerObjectId == Target);
        long firstLaunch = firstTorpedo.Torpedo!.LaunchMotionTimeMs;
        Assert.InRange(firstLaunch, 501, 1251);
        long readyAt = firstLaunch + 15000;
        Assert.Equal(firstTorpedo.ObjectId,
            Assert.Single(At(engine, readyAt).Objects, o => o.Torpedo?.OwnerObjectId == Target).ObjectId);
        Assert.Equal(firstTorpedo.ObjectId,
            Assert.Single(At(engine, readyAt + 499).Objects, o => o.Torpedo?.OwnerObjectId == Target).ObjectId);
        var second = At(engine, readyAt + 1250);
        var ids = second.Objects.Where(o => o.Torpedo?.OwnerObjectId == Target).Select(o => o.ObjectId).ToArray();
        Assert.Equal(2, ids.Length);
        Assert.Contains(firstTorpedo.ObjectId, ids);
        Assert.InRange(second.CombatJournal.Last(e => e.Type == CombatEventType.Launch).MotionTimeMs,
            readyAt + 500, readyAt + 1250);
    }
}
