using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatPresentationResumeTests
{
    [Fact]
    public async Task Loaded_active_torpedo_restores_remaining_prediction_and_busy_panel()
    {
        await using var fixture = await CombatSessionFixture.Create();
        await fixture.Launch("active");
        var before = await fixture.Advance(2000);
        var loaded = await fixture.SaveAndRestore();
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(loaded with { SnapshotSequence = 0, SimulationTimeMs = 0 });
        buffer.Update(loaded with { SnapshotSequence = 0, SimulationTimeMs = 1000 });
        buffer.Update(loaded);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        var flightObject = Assert.Single(loaded.Objects.Where(o => o.OwnProjectile is not null));
        for (int reopen = 0; reopen < 3; reopen++)
        {
            screen.OnActivated();
            Render(screen);
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
            if (!row.Opened) { screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY); Render(screen); }
            Assert.Contains("Reloading", screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText);
            Assert.True(screen.CommandsPanel.AllCommandButtons.Single(b => b.CommandTypeId == CombatCommandTypes.Launch).Enabled); // aim remains selectable; authoritative reload forbids submission
            Assert.Empty(screen.CombatEffects.PointEffects);
            var path = screen.CombatTrajectories[flightObject.ObjectId].Prediction;
            Assert.NotEmpty(path);
            Assert.Equal(before.Objects.Single(o => o.OwnProjectile is not null).OwnProjectile, flightObject.OwnProjectile);
            Assert.Equal(flightObject.X, path[0].X, 6);
            Assert.Equal(flightObject.Y, path[0].Y, 6);
            screen.OnDeactivated();
            now += Stopwatch.Frequency * 5;
        }
    }

    [Fact]
    public async Task Loaded_completed_impact_has_no_explosion_or_terminal_trail()
    {
        const int hits = 1;
        await using var fixture = await CombatSessionFixture.Create();
        AuthoritativeSnapshot before = fixture.Engine.CaptureSnapshot();
        for (int shot = 1; shot <= hits; shot++)
        {
            var launched = await fixture.Launch("hit-" + shot);
            var flight = Assert.Single(launched.Objects.Where(o => o.OwnProjectile is not null)).OwnProjectile!;
            before = await fixture.Advance((long)Math.Ceiling(flight.DetonationMotionTimeMs) - launched.MotionTimeMs + 1000);
        }
        Assert.Contains(before.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        var loaded = await fixture.SaveAndRestore();
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(loaded);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        for (int reopen = 0; reopen < 3; reopen++)
        {
            screen.OnActivated();
            Render(screen);
            Assert.Empty(screen.CombatEffects.PointEffects);
            Assert.Empty(screen.CombatTrajectories);
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
            if (!row.Opened) { screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY); Render(screen); }
            Assert.Contains("Reloading", screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText);
            screen.OnDeactivated();
        }
        Assert.All(loaded.Objects, o => Assert.Null(o.HullCombat));
        Assert.Contains(loaded.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        Assert.DoesNotContain(loaded.CombatJournal, e => e.Type == CombatEventType.TorpedoHitRoll || e.Roll is not null);

    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
    }
}
