using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public class TorpedoSelfDestructTests
{
    [Fact]
    public void Paused_self_destruct_is_immediate_and_harmless()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("fire"));
        At(engine, 0);
        var flight = Assert.Single(At(engine, 100).Objects, o => o.Torpedo is not null);
        engine.ReceiveCommand(new("cancel", 1, Player, Launcher, CombatCommandTypes.SelfDestruct, flight.ObjectId));
        var after = At(engine, 100);
        Assert.DoesNotContain(after.Objects, o => o.Torpedo is not null);
        var impact = Assert.Single(after.CombatImpacts);
        Assert.Equal(TorpedoTerminationKind.SelfDestruct, impact.TerminationKind);
        Assert.Equal(string.Empty, impact.HitObjectId);
        Assert.Equal(0, impact.DamageApplied);
        Assert.NotEmpty(impact.FinalTrail);
        Assert.All(after.Objects, o => Assert.Equal(450, o.HullCombat!.CurrentHp));
        Assert.Null(after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
    }
    [Fact]
    public void Duplicate_or_stale_command_does_not_destroy_next_launch()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("one"));
        string id = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null).ObjectId;
        var cancel = new PlayerCommand("cancel", 1, Player, Launcher, CombatCommandTypes.SelfDestruct, id);
        engine.ReceiveCommand(cancel); At(engine, 0);
        engine.ReceiveCommand(Fire("two")); At(engine, 0);
        engine.ReceiveCommand(cancel);
        engine.ReceiveCommand(cancel with { CommandId = "stale" });
        var snapshot = At(engine, 0);
        Assert.NotEqual(id, Assert.Single(snapshot.Objects, o => o.Torpedo is not null).ObjectId);
        Assert.Single(snapshot.CombatImpacts);
        Assert.Equal("stale_projectile", snapshot.CommandResults.Single(r => r.CommandId == "stale").ReasonCode);
    }
}
