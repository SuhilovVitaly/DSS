using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0018TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>Retired mode bonuses/breakdown replaced by immutable per-launch point profiles.</summary>
public sealed class FireModeRuntimeTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    [InlineData(99UL)]
    public void Profiles_never_enable_breakdown_or_hit_rolls_and_capture_only_launch_scatter(ulong seed)
    {
        foreach (string profile in new[] { "slow", "medium", "fast" })
        {
            using var engine = Create(seed); var before = engine.CaptureLaunchScatterState();
            engine.ReceiveCommand(Launch(profile: profile)); var state = At(engine);
            var flight = Assert.Single(state.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
            Assert.Equal(profile, flight.ProfileId); Assert.Equal(before.Counter + 2, engine.CaptureLaunchScatterState().Counter);
            var launcher = state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
            Assert.Null(typeof(LauncherCombatSnapshot).GetProperty("ModeId")); Assert.Null(typeof(LauncherCombatSnapshot).GetProperty("IsBrokenUntilDock")); Assert.Equal(10000, launcher.ReadyAtMotionTimeMs);
            Assert.Equal(0UL, engine.CaptureSaveState().GameState.CombatState!.LauncherBreakdownRngCounter);
            Assert.DoesNotContain(state.CombatJournal, e => e.ChanceTenths is not null || e.Roll is not null);
        }
    }

    [Fact]
    public void Torpedo_and_counter_profiles_capture_independent_geometry_and_reload_without_intercept_ratings()
    {
        using var engine = Create(); engine.ReceiveCommand(Launch()); At(engine);
        engine.ReceiveCommand(new("counter", 2, Player, "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Launch,
            TargetWorldX: 200, TargetWorldY: 100, ProfileId: "wide"));
        var state = At(engine); Assert.All(state.CommandResults, r => Assert.Equal(CommandResultStatus.Executed, r.Status));
        var torpedo = Assert.Single(state.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo).OwnProjectile!;
        var counter = Assert.Single(state.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile).OwnProjectile!;
        Assert.Equal(("slow", 6d, 9d), (torpedo.ProfileId, torpedo.SpeedKmS, torpedo.BlastRadiusKm));
        Assert.Equal(("wide", 15d, 1d), (counter.ProfileId, counter.SpeedKmS, counter.BlastRadiusKm));
        Assert.Equal(10000, state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        Assert.Equal(12500, state.InstalledModules.Single(m => m.ModuleId == counter.LauncherModuleId).Defense!.ReadyAtMotionTimeMs);
        Assert.Equal(4UL, engine.CaptureLaunchScatterState().Counter);
        Assert.DoesNotContain(state.Objects, o => o.Torpedo is not null || o.Countermeasure is not null);
        Assert.DoesNotContain(state.CombatJournal, e => e.Roll is not null || e.ChanceTenths is not null);
    }
}
