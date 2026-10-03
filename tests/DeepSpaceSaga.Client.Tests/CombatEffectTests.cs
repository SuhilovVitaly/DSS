using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatEffectTests
{
    private static CombatImpactSnapshot Impact(long id = 1) =>
        new(id, "torpedo", "player", "launcher", "target", "target", 500, 100, 100,
            [new(0, new(100, 115, 0, 3, 0, 500), 1)], 150);
    private static long Ticks(int ms) => Stopwatch.Frequency * ms / 1000;

    [Fact]
    public void Explosion_radius_and_alpha_progress_for_two_real_seconds()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now);
        store.Receive([Impact()]);
        var effect = Assert.Single(store.Active);
        Assert.Equal(0, effect.RadiusPx(now));
        Assert.Equal(200, effect.Alpha(now, 200));
        now = Ticks(1000);
        Assert.Equal(25, effect.RadiusPx(now));
        Assert.Equal(100, effect.Alpha(now, 200));
        Assert.Single(effect.Impact.FinalTrail);
        now = Ticks(2000);
        Assert.Equal(50, effect.RadiusPx(now));
        Assert.Equal(0, effect.Alpha(now, 200));
        store.Receive(default);
        Assert.Empty(store.Active);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Pause_and_acceleration_do_not_change_effect_lifetime(SimulationSpeed speed)
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var baseline = new AuthoritativeSnapshot(1, 0, speed, [new("player", 0, 0, 0, 0)], "player");
        buffer.Update(baseline);
        var palette = CombatVisualSettings.Default with { Explosion = new SKColor(255, 0, 255, 200) };
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now, combatSettings: palette);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        buffer.Update(baseline with { SnapshotSequence = 2, CombatImpacts = [Impact()] });
        screen.Render(canvas, 1280, 720);
        now = Ticks(1000);
        // The map is still rendered beneath modal overlays. Deactivation must not reset deadlines.
        screen.OnDeactivated();
        screen.OnActivated();
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "label_plaques") canvas.Clear(SKColors.Transparent);
            if (stage == "combat_effects")
            {
                var pixel = bitmap.GetPixel(765, 460);
                Assert.InRange(pixel.Alpha, (byte)50, (byte)100);
                Assert.True(pixel.Red > 240 && pixel.Blue > 240 && pixel.Green == 0);
            }
        };
        screen.Render(canvas, 1280, 720);
        Assert.Equal(25, Assert.Single(screen.CombatEffects.Active).RadiusPx(now));
        screen.RenderStageCompleted = null;
        now = Ticks(2000);
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.CombatEffects.Active);
        screen.OnDeactivated();
    }

    [Fact]
    public void Repeated_impact_snapshot_does_not_restart_explosion()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now);
        store.Receive([Impact()]);
        now = Ticks(1500);
        store.Receive([Impact(), Impact(2)]);
        Assert.Equal(2, store.Active.Count);
        Assert.Equal(37.5f, store.Active[0].RadiusPx(now));
        now = Ticks(2000);
        store.Receive([Impact(), Impact(2)]);
        Assert.Equal(2, Assert.Single(store.Active).Impact.EventId);
        now = Ticks(3500);
        store.Receive([Impact(), Impact(2)]);
        Assert.Empty(store.Active);
    }

    [Fact]
    public void Session_reset_does_not_replay_old_effects()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now);
        store.Receive([Impact()]);
        store.Reset([Impact(), Impact(2)]);
        store.Receive([Impact(), Impact(2)]);
        Assert.Empty(store.Active);
        store.Receive([Impact(3)]);
        Assert.Equal(3, Assert.Single(store.Active).Impact.EventId);
        store.Reset();
        store.Receive([Impact()]); // session-local IDs can start again
        Assert.Equal(1, Assert.Single(store.Active).Impact.EventId);

        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [], CombatImpacts: [Impact()]));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.CombatEffects.Active);
    }

    [Fact]
    public void Coalesced_snapshots_preserve_first_receipt_and_expire_while_map_is_hidden()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var baseline = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], "player");
        buffer.Update(baseline);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        buffer.Update(baseline with { SnapshotSequence = 2, CombatImpacts = [Impact()] });
        now = Ticks(1000);
        buffer.Update(baseline with { SnapshotSequence = 3, CombatImpacts = [Impact()] });
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Equal(25, Assert.Single(screen.CombatEffects.Active).RadiusPx(now));
        screen.OnDeactivated();
        now = Ticks(1500);
        buffer.Update(baseline with { SnapshotSequence = 4, CombatImpacts = [Impact(), Impact(2)] });
        now = Ticks(4000);
        buffer.Update(baseline with { SnapshotSequence = 5, CombatImpacts = [Impact(), Impact(2)] });
        // Older transport delivery must not overwrite either first-receipt timestamp.
        buffer.Update(baseline with { SnapshotSequence = 3, CombatImpacts = [Impact(), Impact(2)] });
        screen.OnActivated();
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.CombatEffects.Active);
        Assert.Equal(0, buffer.FindCombatImpactReceivedAtTimestamp(1));
        Assert.Equal(Ticks(1500), buffer.FindCombatImpactReceivedAtTimestamp(2));
        Assert.Null(new SnapshotBuffer(() => now).FindCombatImpactReceivedAtTimestamp(1));
        screen.OnDeactivated();
    }
    [Fact]
    public void Delayed_render_uses_receipt_time_and_cannot_revive_expired_fact()
    {
        long now = Ticks(3000);
        var store = new CombatEffectStore(() => now);
        store.Receive([Impact()], receivedAtTimestamp: 0);
        Assert.Empty(store.Active);
        store.Receive([Impact()]);
        Assert.Empty(store.Active);
    }
}
