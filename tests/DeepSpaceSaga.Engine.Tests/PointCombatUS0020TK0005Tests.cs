using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0008TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public class PointCombatUS0020TK0005Tests
{
    private static IEnumerable<CombatJournalEntry> Reloads(AuthoritativeSnapshot scene) => scene.CombatJournal.Where(e => e.Type == CombatEventType.TorpedoReloadCompleted);

    [Fact]
    public void Reload_before_at_after_deadline()
    {
        using var engine = Create(scatter: 0);
        engine.ReceiveCommand(Launch()); At(engine);
        Assert.Empty(Reloads(At(engine, 9999)));
        var scene = At(engine, 10000);
        var fact = Assert.Single(Reloads(scene));
        Assert.Equal(Launcher, fact.LauncherModuleId); Assert.Equal(10000, fact.MotionTimeMs);
        Assert.Empty(fact.ProjectileObjectId);
        Assert.Single(Reloads(At(engine, 20000)));
    }

    [Fact]
    public void Reload_two_launchers_two_cycles()
    {
        using var engine = Create(scatter: 0);
        engine.ReceiveCommand(Launch()); At(engine); At(engine, 10000);
        engine.ReceiveCommand(Launch("second")); At(engine, 10000);
        var events = Reloads(At(engine, 20000)).ToArray();
        Assert.Equal(new double[] { 10000, 20000 }, events.Select(e => e.MotionTimeMs));
        Assert.Equal(2, events.Select(e => e.EventId).Distinct().Count());
    }

    [Fact]
    public void Reload_independent_launchers_complete_at_same_deadline()
    {
        const string second = "MOD-PLAYER-COUNTERMEASURE-01";
        using var engine = Create(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId != second ? m : m with
            {
                ModuleTypeId = o.Modules!.Single(x => x.ModuleId == Launcher).ModuleTypeId,
                PowerState = "On"
            }).ToArray()
        }, scatter: 0);
        engine.ReceiveCommand(Launch());
        engine.ReceiveCommand(Launch("other") with { ModuleId = second }); At(engine);
        var events = Reloads(At(engine, 10000)).ToArray();
        Assert.Equal(new[] { second, Launcher }.Order(StringComparer.Ordinal), events.Select(e => e.LauncherModuleId));
    }

    [Fact]
    public void Reload_pause_high_speed_and_disabled()
    {
        using var engine = Create(scatter: 0);
        engine.ReceiveCommand(Launch()); At(engine);
        Assert.Empty(Reloads(At(engine)));
        Assert.Equal(10000, Assert.Single(Reloads(At(engine, 30000))).MotionTimeMs);
    }

    [Fact]
    public void Reload_presented_scene_and_privacy()
    {
        using var engine = Battle(enemyLauncherOn: true);
        engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0);
        var scene = At(engine, 30000);
        Assert.Empty(Reloads(scene));
    }
}
