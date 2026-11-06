using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0002Tests
{
    private static ObjectMotionSnapshot Own() => new("own", 0, -120, 6, 90, RenderObjectType: SpaceObjectType.Missile,
        OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 0, -120, 0, -120, 300, -120, 0, 5000, 6, "slow", 9, 200, 20));
    private static ObjectMotionSnapshot Foreign() => new("foreign", -200, -120, 6, 270, RenderObjectType: SpaceObjectType.Missile,
        ObservedProjectile: new(ProjectileKind.Torpedo, -200, -120, -1, 0, 6, "slow", 9));
    private static AuthoritativeSnapshot Scene(params ObjectMotionSnapshot[] flights) => new(1, 0, SimulationSpeed.Speed0,
        new[] { new ObjectMotionSnapshot("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip) }.Concat(flights).ToImmutableArray(), "player", CombatJournal: []);
    private static TacticalMapSettings Map(double ppu) => new()
    {
        MetersPerPixel = [100 / ppu, 1000 / ppu, 10000 / ppu, 100000 / ppu, 1000000 / ppu],
        LabelDetailPpu = ppu,
        CompactMarkerPpu = ppu,
        ClusterPpu = ppu,
        TrailDetailPpu = ppu
    };

    [Fact]
    public void Point_effects_use_real_durations_deduplicate_and_do_not_replay_load_baseline()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now) { StartOnPresentation = true };
        var blast = new CombatJournalEntry(1, 0, CombatEventType.TorpedoDetonated, "enemy", "", "flight", 300, 0,
            ProjectileKind: ProjectileKind.Torpedo, BlastRadiusKm: 9);
        var shot = blast with { EventId = 2, Type = CombatEventType.ShotDown };
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(2, store.PointEffects.Count);
        now = Stopwatch.Frequency * 300 / 1000;
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(2, store.PointEffects.Count);
        Assert.Equal(.5, store.PointProgress(blast, 0, now), 5);
        now = Stopwatch.Frequency * 400 / 1000;
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(1, Assert.Single(store.PointEffects).Entry.EventId);
        now = Stopwatch.Frequency;
        store.ReceiveJournal([blast, shot], 0); Assert.Empty(store.PointEffects);
        store.ResetJournal([blast, shot]); store.ReceiveJournal([blast, shot], 0); Assert.Empty(store.PointEffects);
    }

    [Fact]
    public void Transport_watchdog_requires_real_time_instead_of_accelerated_scene_time()
    {
        long ticks = 0;
        var actual = new SnapshotBuffer(() => ticks);
        var mismatched = new SnapshotBuffer(() => ticks * 20);
        var state = Scene() with { CurrentSpeed = SimulationSpeed.Speed1 };
        var packet = new CombatPresentationUpdate(Guid.NewGuid(), 1, 0, [], state, false, 0);
        actual.UpdatePresentation(packet); mismatched.UpdatePresentation(packet);
        ticks = Stopwatch.Frequency * 60 / 1000; // still before the next100ms transport publication
        Assert.False(actual.PresentationPauseRequested);
        Assert.True(mismatched.PresentationPauseRequested);
        ticks = Stopwatch.Frequency * 1001 / 1000;
        Assert.True(actual.PresentationPauseRequested); // the real safety threshold is retained
    }
}
